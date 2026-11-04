"""Edicao cirurgica de .save do Rocket League.

Nao reserializa o save. Troca so os bytes dos objetos de configuracao
(video/gameplay/camera) e mantem todos os outros objetos (garagem, presets,
tinta, inventario) byte a byte. Formato nativo do jogo:
  BoolProperty  -> Size=0, valor em 1 byte apos o tag
  ByteProperty  -> EnumName + Value, Size = len(Value) (sem o EnumName)
"""
from __future__ import annotations

import struct
from dataclasses import dataclass, field

from nixwrap.save_file._crypto import aes_decrypt, aes_encrypt, crc32

OBJHEADER = b"\xff\xff\xff\xff"


class SaveFormatError(RuntimeError):
    pass


def read_str(buf: bytes, off: int) -> tuple[str, int]:
    if off + 4 > len(buf):
        raise SaveFormatError("string fora do buffer")
    n = struct.unpack_from("<i", buf, off)[0]
    off += 4
    if n == 0:
        return "", off
    if n > 0:
        end = off + n
        if n > 1_000_000 or end > len(buf):
            raise SaveFormatError("string ascii invalida")
        return buf[off:end - 1].decode("latin-1"), end
    end = off + (-n) * 2
    if -n > 1_000_000 or end > len(buf):
        raise SaveFormatError("string utf16 invalida")
    return buf[off:end - 2].decode("utf-16-le"), end


def write_str(s: str) -> bytes:
    if s == "":
        return struct.pack("<i", 0)
    try:
        data = s.encode("ascii") + b"\x00"
        return struct.pack("<i", len(data)) + data
    except UnicodeEncodeError:
        data = s.encode("utf-16-le") + b"\x00\x00"
        return struct.pack("<i", -(len(data) // 2)) + data


# ---------------------------------------------------------------- properties

@dataclass
class Prop:
    name: str
    tag: str
    raw: bytes
    value: object = None
    enum: str | None = None
    struct_name: str | None = None
    struct_body: bytes | None = None
    legacy: bool = False


def parse_props(blob: bytes, legacy: bool = False) -> tuple[list[Prop], bytes]:
    """Retorna (propriedades, bytes apos o None final).

    legacy=True le o formato do codec antigo, que soma o nome da struct ao Size.
    """
    props: list[Prop] = []
    off = 0
    while True:
        start = off
        name, off = read_str(blob, off)
        if name == "None":
            return props, blob[off:]
        tag, off = read_str(blob, off)
        if off + 8 > len(blob):
            raise SaveFormatError(f"tag truncado em {name}")
        size, _idx = struct.unpack_from("<ii", blob, off)
        off += 8
        if size < 0 or size > len(blob):
            raise SaveFormatError(f"tamanho invalido em {name}")
        value: object = None
        enum: str | None = None
        sname: str | None = None
        sbody: bytes | None = None
        if tag == "BoolProperty":
            value = blob[off] != 0
            off += 1
        elif tag == "ByteProperty":
            enum, off = read_str(blob, off)
            if enum == "None":
                value = blob[off]
                off += 1
            else:
                value, off = read_str(blob, off)
        elif tag == "StructProperty":
            name_start = off
            sname, off = read_str(blob, off)
            body_len = size - (off - name_start) if legacy else size
            if body_len < 0:
                raise SaveFormatError(f"struct invalida em {name}")
            sbody = blob[off:off + body_len]
            off += body_len
        else:
            body = blob[off:off + size]
            if tag == "IntProperty" and size == 4:
                value = struct.unpack("<i", body)[0]
            elif tag == "FloatProperty" and size == 4:
                value = struct.unpack("<f", body)[0]
            elif tag in ("StrProperty", "NameProperty"):
                value, _ = read_str(body, 0)
            off += size
        if off > len(blob):
            raise SaveFormatError(f"propriedade {name} passa do objeto")
        props.append(Prop(name, tag, blob[start:off], value, enum, sname, sbody, legacy))


def parse_object(blob: bytes) -> tuple[list[Prop], bytes]:
    """Objeto nativo ou regravado pelo codec antigo — o que consumir o blob inteiro."""
    err: Exception | None = None
    for legacy in (False, True):
        try:
            props, tail = parse_props(blob, legacy)
        except (SaveFormatError, struct.error, IndexError) as ex:
            err = ex
            continue
        if tail == b"":
            return props, tail
        err = SaveFormatError("sobra de bytes apos None")
    raise SaveFormatError(f"objeto ilegivel: {err}")


def _canonical_stream(body: bytes) -> bytes:
    """Struct com property stream interno: normaliza; binaria (Vector etc.) fica como esta."""
    try:
        props, tail = parse_props(body, legacy=True)
    except (SaveFormatError, struct.error, IndexError):
        return body
    if tail != b"":
        return body
    return b"".join(_canonical(p) for p in props) + write_str("None")


def _tag(name: str, tag: str, size: int) -> bytes:
    return write_str(name) + write_str(tag) + struct.pack("<ii", size, 0)


def enc_bool(name: str, v: bool) -> bytes:
    return _tag(name, "BoolProperty", 0) + (b"\x01" if v else b"\x00")


def enc_int(name: str, v: int) -> bytes:
    return _tag(name, "IntProperty", 4) + struct.pack("<i", int(v))


def enc_byte_enum(name: str, enum: str, v: str) -> bytes:
    val = write_str(v)
    return write_str(name) + write_str("ByteProperty") + struct.pack("<ii", len(val), 0) + write_str(enum) + val


def _canonical(p: Prop) -> bytes:
    """Regrava Bool/Byte no formato nativo (conserta saves do codec antigo)."""
    if p.tag == "BoolProperty":
        return enc_bool(p.name, bool(p.value))
    if p.tag == "ByteProperty" and p.enum and p.enum != "None" and isinstance(p.value, str):
        return enc_byte_enum(p.name, p.enum, p.value)
    if p.tag == "StructProperty" and p.legacy and p.struct_name is not None and p.struct_body is not None:
        body = _canonical_stream(p.struct_body)
        return _tag(p.name, "StructProperty", len(body)) + write_str(p.struct_name) + body
    return p.raw


def decode_video_options(raw_prop: bytes) -> list[tuple[str, str]] | None:
    _name, off = read_str(raw_prop, 0)
    _tagn, off = read_str(raw_prop, off)
    off += 8
    count = struct.unpack_from("<i", raw_prop, off)[0]
    off += 4
    if count < 0 or count > 64:
        return None
    out: list[tuple[str, str]] = []
    for _ in range(count):
        rest = raw_prop[off:]
        try:
            elem, after = parse_props(rest)
        except SaveFormatError:
            return None
        off += len(rest) - len(after)
        d = {p.name: p.value for p in elem}
        if not isinstance(d.get("Id"), str) or not isinstance(d.get("Value"), str):
            return None
        out.append((d["Id"], d["Value"]))
    if off != len(raw_prop):
        return None
    return out


def enc_video_options(opts: list[tuple[str, str]]) -> bytes:
    payload = struct.pack("<i", len(opts))
    for oid, val in opts:
        idb = write_str(oid)
        vb = write_str(val)
        payload += _tag("Id", "NameProperty", len(idb)) + idb
        payload += _tag("Value", "StrProperty", len(vb)) + vb
        payload += write_str("None")
    return _tag("VideoOptions", "ArrayProperty", len(payload)) + payload


class PropEditor:
    def __init__(self, blob: bytes):
        self.props, self.tail = parse_object(blob)
        self.original = blob

    def get(self, name: str) -> Prop | None:
        for p in self.props:
            if p.name == name:
                return p
        return None

    def _put(self, name: str, raw: bytes, tag: str, value, enum=None) -> None:
        new = Prop(name, tag, raw, value, enum)
        for i, p in enumerate(self.props):
            if p.name == name:
                self.props[i] = new
                return
        self.props.append(new)

    def put(self, prop: Prop) -> None:
        self._put(prop.name, _canonical(prop), prop.tag, prop.value, prop.enum)

    def set_bool(self, name: str, v: bool) -> None:
        self._put(name, enc_bool(name, v), "BoolProperty", v)

    def set_int(self, name: str, v: int) -> None:
        self._put(name, enc_int(name, v), "IntProperty", int(v))

    def set_enum(self, name: str, enum_default: str, v: str) -> None:
        cur = self.get(name)
        enum = cur.enum if cur is not None and cur.enum and cur.enum != "None" else enum_default
        self._put(name, enc_byte_enum(name, enum, v), "ByteProperty", v, enum)

    def remove(self, name: str) -> None:
        self.props = [p for p in self.props if p.name != name]

    def video_options(self) -> list[tuple[str, str]] | None:
        p = self.get("VideoOptions")
        if p is None or p.tag != "ArrayProperty":
            return None
        return decode_video_options(p.raw)

    def set_video_options(self, opts: list[tuple[str, str]]) -> None:
        self._put("VideoOptions", enc_video_options(opts), "ArrayProperty", None)

    def build(self) -> bytes:
        return b"".join(_canonical(p) for p in self.props) + write_str("None") + self.tail


# --------------------------------------------------------------- container

@dataclass
class SaveFile:
    file_hdr_len: int
    head: bytes            # foosball..typever (Epic 20 bytes, Steam 8)
    root: bytes            # svdata[0:first_obj]  (OBJHEADER + root stream)
    objects: list[bytes]   # cada um SEM o OBJHEADER inicial
    types: list[tuple[str, int]] = field(default_factory=list)  # (tipo, object_index)
    plain_len: int = 0

    def index_of(self, type_name: str) -> list[int]:
        return [i for i, (t, _) in enumerate(self.types) if t == type_name]

    def type_names(self) -> set[str]:
        return {t for t, _ in self.types}


def parse_save(raw: bytes) -> SaveFile:
    if len(raw) < 8 + 16:
        raise SaveFormatError("arquivo curto")
    enc_len, crc = struct.unpack_from("<II", raw, 0)
    if enc_len % 16 or 8 + enc_len != len(raw):
        raise SaveFormatError("tamanho do bloco cifrado nao bate com o arquivo")
    enc = raw[8:]
    if crc32(enc) != crc:
        raise SaveFormatError("CRC invalido")
    return parse_plain(aes_decrypt(enc), enc_len)


def parse_plain(dec: bytes, enc_len: int | None = None) -> SaveFile:
    if enc_len is None:
        enc_len = len(dec)
    # Epic: foosball, 0x7FFFFFFF, engine, licensee, typever, svlen.
    # Steam (SaveData): foosball, engine, svlen.
    hlen = 20 if struct.unpack_from("<I", dec, 4)[0] == 0x7FFFFFFF else 8
    head = dec[:hlen]
    svlen = struct.unpack_from("<i", dec, hlen)[0]
    sv_start = hlen + 4
    sv_end = sv_start + svlen - 4
    if svlen < 8 or sv_end > len(dec):
        raise SaveFormatError("svlen invalido")
    sv = dec[sv_start:sv_end]
    off = sv_end
    ntypes = struct.unpack_from("<i", dec, off)[0]
    off += 4
    if ntypes < 0 or ntypes > 1_000_000:
        raise SaveFormatError("tabela de tipos invalida")
    entries: list[tuple[str, int, int]] = []
    for _ in range(ntypes):
        tn, off = read_str(dec, off)
        fp, oi = struct.unpack_from("<II", dec, off)
        off += 8
        entries.append((tn, fp, oi))
    if any(b != 0 for b in dec[off:]) or len(dec) - off >= 16:
        raise SaveFormatError("bytes inesperados apos a tabela de tipos")

    starts = [fp - 4 for _, fp, _ in entries]
    if starts:
        if starts != sorted(starts) or len(set(starts)) != len(starts) or starts[0] < 4:
            raise SaveFormatError("objetos fora de ordem")
        if starts[-1] >= len(sv):
            raise SaveFormatError("objeto fora do svdata")
    objects: list[bytes] = []
    for i, s in enumerate(starts):
        e = starts[i + 1] if i + 1 < len(starts) else len(sv)
        chunk = sv[s:e]
        if chunk[:4] != OBJHEADER:
            raise SaveFormatError("objeto sem OBJHEADER")
        objects.append(chunk[4:])
    root = sv[:starts[0]] if starts else sv
    return SaveFile(
        file_hdr_len=enc_len,
        head=head,
        root=root,
        objects=objects,
        types=[(t, oi) for t, _, oi in entries],
        plain_len=len(dec),
    )


def build_plain(sf: SaveFile) -> bytes:
    sv = bytearray(sf.root)
    table = bytearray()
    for (tn, oi), blob in zip(sf.types, sf.objects):
        fp = len(sv) + 4
        sv += OBJHEADER + blob
        table += write_str(tn) + struct.pack("<II", fp, oi)
    out = bytearray(sf.head)
    out += struct.pack("<i", len(sv) + 4)
    out += sv
    out += struct.pack("<i", len(sf.types))
    out += table
    pad = (-len(out)) % 16
    out += b"\x00" * pad
    return bytes(out)


def encrypt_plain(plain: bytes) -> bytes:
    enc = aes_encrypt(plain)
    return struct.pack("<II", len(enc), crc32(enc)) + enc


def decrypt_file(raw: bytes) -> bytes:
    enc_len = struct.unpack_from("<I", raw, 0)[0]
    return aes_decrypt(raw[8:8 + enc_len])


def verify_noop(plain: bytes, sf: SaveFile) -> None:
    """Remontar sem mudar nada TEM que dar o mesmo plaintext. Senao, nao toca."""
    if build_plain(sf) != plain:
        raise SaveFormatError("remontagem nao e identica ao original — save nao sera tocado")


def load(raw: bytes) -> tuple[SaveFile, bytes]:
    sf = parse_save(raw)
    plain = decrypt_file(raw)
    verify_noop(plain, sf)
    return sf, plain
