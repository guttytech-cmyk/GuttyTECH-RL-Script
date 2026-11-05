"""Prova do reparo de cor/id: nativo, legado e pasta vazia."""
from __future__ import annotations

import struct
import tempfile
from pathlib import Path

import patch_save_video as P
from save_surgical import (
    enc_int,
    enc_object,
    parse_props,
    repair_object,
    write_str,
    _tag,
)


def _legacy_struct(prop: str, sname: str, body: bytes) -> bytes:
    name_b = write_str(sname)
    return _tag(prop, "StructProperty", len(name_b) + len(body)) + name_b + body


def _native_struct(prop: str, sname: str, body: bytes) -> bytes:
    return _tag(prop, "StructProperty", len(body)) + write_str(sname) + body


def _paint_body() -> bytes:
    return (
        enc_int("TeamColorID", 35)
        + enc_int("CustomColorID", 12)
        + write_str("None")
    )


def _object_with(struct_bytes: bytes) -> bytes:
    return struct_bytes + enc_int("After", 7) + write_str("None")


def _colors(blob: bytes) -> dict:
    props, tail = parse_props(blob)
    assert tail == b""
    paint = next(p for p in props if p.name == "TeamPaint")
    inner, itail = parse_props(paint.struct_body)
    assert itail == b""
    after = next(p for p in props if p.name == "After")
    size_off = 0
    _, size_off = __import__("save_surgical").read_str(paint.raw, size_off)
    _, size_off = __import__("save_surgical").read_str(paint.raw, size_off)
    size = struct.unpack_from("<i", paint.raw, size_off)[0]
    return {
        "colors": {p.name: (p.tag, p.value) for p in inner},
        "after": after.value,
        "size": size,
        "body": len(paint.struct_body),
    }


def test_native_and_legacy_keep_the_next_field() -> None:
    body = _paint_body()
    for label, raw in (
        ("native", _object_with(_native_struct("TeamPaint", "LoadoutTeamPaint", body))),
        ("legacy", _object_with(_legacy_struct("TeamPaint", "LoadoutTeamPaint", body))),
    ):
        fixed = repair_object(raw)
        got = _colors(fixed)
        assert got["colors"]["TeamColorID"] == ("ByteProperty", 35), label
        assert got["colors"]["CustomColorID"] == ("ByteProperty", 12), label
        assert got["after"] == 7, label
        assert got["size"] == got["body"], label
        again = repair_object(fixed)
        assert again == fixed, label


def test_qword_inside_array() -> None:
    elem = enc_int("LowerBits", 376) + enc_int("UpperBits", 0) + write_str("None")
    payload = struct.pack("<i", 1) + elem
    raw = _tag("OnlineProducts128", "ArrayProperty", len(payload)) + payload
    obj = raw + write_str("None")
    fixed = repair_object(obj)
    props, tail = parse_props(fixed)
    assert tail == b""
    arr = props[0].raw
    off = 0
    _, off = __import__("save_surgical").read_str(arr, off)
    _, off = __import__("save_surgical").read_str(arr, off)
    off += 8 + 4
    inner, _ = parse_props(arr[off:])
    kinds = {p.name: p.tag for p in inner}
    assert kinds["LowerBits"] == "QWordProperty"
    assert kinds["UpperBits"] == "QWordProperty"


def test_object_property_and_mtime() -> None:
    import os
    import shutil

    from save_surgical import load as load_save

    body = (
        enc_int("EquippedLoadoutSet", 8871)
        + enc_int("GameTags", 12)
        + enc_int("UseCount", 3)
        + write_str("None")
    )
    fixed = repair_object(body)
    props, tail = parse_props(fixed)
    assert tail == b""
    kinds = {p.name: (p.tag, p.value) for p in props}
    assert kinds["EquippedLoadoutSet"][0] == "ObjectProperty"
    assert kinds["UseCount"] == ("IntProperty", 3)
    # indice sobrevive
    raw = next(p.raw for p in props if p.name == "EquippedLoadoutSet")
    off = 0
    _, off = __import__("save_surgical").read_str(raw, off)
    _, off = __import__("save_surgical").read_str(raw, off)
    off += 8
    assert struct.unpack_from("<i", raw, off)[0] == 8871

    original = enc_object("Loadouts", 10) + enc_object("Loadouts", 11) + write_str("None")
    dumped = (
        enc_str("__parse_error", "utf-8")
        + enc_str("__raw_hex", original.hex())
        + write_str("None")
    )
    back = repair_object(dumped)
    assert back.count(b"ObjectProperty") == original.count(b"ObjectProperty") == 2

    intact = Path(r"C:\Users\a\GuttyTECH\RL-Optimizer-v22\Backups\Presets\Best\83cddf70210b453082dfe33b8b11b71a_1.save")
    broken = Path(r"C:\Users\a\Downloads\GUTTYTECH-RL-Optimizer\_diag\roundtrip\roundtrip.save")
    assert intact.is_file() and broken.is_file()
    with tempfile.TemporaryDirectory() as tmp:
        dest = Path(tmp) / "round.save"
        shutil_copy = __import__("shutil").copy2
        shutil_copy(broken, dest)
        stamp = 1_600_000_000
        os_utime = __import__("os").utime
        os_utime(dest, (stamp, stamp))
        assert P.patch_file(dest, "repair", None) == "ok"
        assert abs(dest.stat().st_mtime - stamp) < 1
        good_n = load_save(intact.read_bytes())[1].count(b"ObjectProperty")
        bad_n = load_save(dest.read_bytes())[1].count(b"ObjectProperty")
        assert bad_n == good_n, (bad_n, good_n)


def enc_str(name: str, value: str) -> bytes:
    body = write_str(value)
    return _tag(name, "StrProperty", len(body)) + body


def test_empty_dir_is_failure() -> None:
    with tempfile.TemporaryDirectory() as tmp:
        assert P.main(["patch_save_video.py", "--mode", "repair", tmp]) == 1


if __name__ == "__main__":
    test_native_and_legacy_keep_the_next_field()
    test_qword_inside_array()
    test_object_property_and_mtime()
    test_empty_dir_is_failure()
    print("ok")
