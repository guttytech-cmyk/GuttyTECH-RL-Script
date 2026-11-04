"""Sincroniza o menu de video do Rocket League no .save (Epic/Steam).

Edicao cirurgica (save_surgical): so os objetos VideoSettingsSavePC_TA,
GameplaySettingsSave_TA e ProfileCameraSave_TA mudam. Garagem, presets,
tinta e inventario ficam byte a byte iguais — verificado antes de gravar.
"""
from __future__ import annotations

import argparse
import json
import os
import sys
from pathlib import Path

_TOOLS_DIR = Path(__file__).resolve().parent
if str(_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(_TOOLS_DIR))

from save_surgical import (  # noqa: E402
    OBJHEADER,
    PropEditor,
    SaveFormatError,
    build_plain,
    decrypt_file,
    encrypt_plain,
    load,
    parse_plain,
)

UNCAPPED_MAX_FPS = 10000
MAX_SAVE_BYTES = 16_000_000
MAX_FILES = 12

VIDEO_T = "TAGame.VideoSettingsSavePC_TA"
GAMEPLAY_T = "TAGame.GameplaySettingsSave_TA"
CAMERA_T = "TAGame.ProfileCameraSave_TA"
TARGET_TYPES = (VIDEO_T, GAMEPLAY_T, CAMERA_T)

# Valores que o cliente aceita (UI PT: Desempenho / Alto desempenho).
# HighPerformance em Particle/Render e ParticleDetail=Low derrubam o menu
# para Alta qualidade / 60 FPS.
COMPLETO_OPTIONS = [
    ("RenderQuality", "Performance"),
    ("RenderDetail", "Performance"),
    ("TextureDetail", "TexturesLow"),
    ("ParticleDetail", "Performance"),
    ("WorldDetail", "Performance"),
    ("AntiAlias", "0"),
]

# Sem RenderQuality → UI fica em Alta qualidade. RenderDetail=Custom quebra o menu.
CRIADOR_OPTIONS = [
    ("RenderDetail", "Quality"),
    ("TextureDetail", "TexturesHigher"),
    ("ParticleDetail", "Performance"),
    ("WorldDetail", "Quality"),
    ("AntiAlias", "0"),
]

VIDEO_BOOLS_COMMON = (
    ("bShowLightShafts", False),
    ("bShowWeatherFX", False),
    ("bShowLensFlares", False),
    ("bUncappedFramerate", True),
    ("bVsync", False),
)
VIDEO_BOOLS_COMPLETO = (("bTranslucentArenaShaders", False),)

GUTTY_VIDEO_KEYS = (
    "VideoOptions",
    "MaxFPS",
    "bShowLightShafts",
    "bShowWeatherFX",
    "bShowLensFlares",
    "bUncappedFramerate",
    "bVsync",
    "bTranslucentArenaShaders",
)

EFFECT_ENUM = "EEffectsIntensity"


def looks_gutty(video: PropEditor) -> bool:
    opts = video.video_options()
    if opts in (COMPLETO_OPTIONS, CRIADOR_OPTIONS):
        return True
    mfps = video.get("MaxFPS")
    return mfps is not None and mfps.value == UNCAPPED_MAX_FPS


def _edit_video(ed: PropEditor, mode: str) -> None:
    ed.set_video_options(COMPLETO_OPTIONS if mode == "completo" else CRIADOR_OPTIONS)
    for k, v in VIDEO_BOOLS_COMMON:
        ed.set_bool(k, v)
    if mode == "completo":
        for k, v in VIDEO_BOOLS_COMPLETO:
            ed.set_bool(k, v)
    ed.set_int("MaxFPS", UNCAPPED_MAX_FPS)


def _edit_gameplay(ed: PropEditor, mode: str) -> None:
    if mode == "completo":
        ed.set_enum("EffectIntensity", EFFECT_ENUM, "EI_Low")


def _edit_camera(ed: PropEditor, mode: str) -> None:
    if mode == "completo":
        ed.set_bool("bUseBallIndicator", True)


_EDITORS = {VIDEO_T: _edit_video, GAMEPLAY_T: _edit_gameplay, CAMERA_T: _edit_camera}

ROOT = -1


def _keys(sf, t: str) -> list[int]:
    """Indices dos objetos do tipo. Save Steam guarda o video no stream raiz (ROOT)."""
    idx = sf.index_of(t)
    if idx or t != VIDEO_T:
        return idx
    if sf.root[:4] == OBJHEADER:
        try:
            if PropEditor(sf.root[4:]).get("VideoOptions") is not None:
                return [ROOT]
        except SaveFormatError:
            pass
    return []


def _get(sf, k: int) -> bytes:
    return sf.root[4:] if k == ROOT else sf.objects[k]


def _set(sf, k: int, blob: bytes) -> None:
    if k == ROOT:
        sf.root = OBJHEADER + blob
    else:
        sf.objects[k] = blob


# ------------------------------------------------------------ sidecar (stock)

def _sidecar_path(sidecar_dir: Path | None, save: Path) -> Path | None:
    if sidecar_dir is None:
        return None
    return sidecar_dir / f"{save.stem}.settings.json"


def _write_sidecar(path: Path | None, sf) -> None:
    """Guarda os blocos originais (pre-Gutty) uma unica vez."""
    if path is None or path.exists():
        return
    vids = _keys(sf, VIDEO_T)
    if not vids:
        return
    try:
        if looks_gutty(PropEditor(_get(sf, vids[0]))):
            return
    except SaveFormatError:
        return
    blobs = {t: [_get(sf, k).hex() for k in _keys(sf, t)] for t in TARGET_TYPES}
    path.parent.mkdir(parents=True, exist_ok=True)
    tmp = path.with_suffix(".tmp")
    tmp.write_text(json.dumps(blobs), encoding="utf-8")
    os.replace(tmp, path)


def _read_sidecar(path: Path | None) -> dict[str, list[bytes]] | None:
    if path is None or not path.exists():
        return None
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
        return {t: [bytes.fromhex(h) for h in data.get(t, [])] for t in TARGET_TYPES}
    except (OSError, ValueError):
        return None


GUTTY_KEYS = {
    VIDEO_T: GUTTY_VIDEO_KEYS,
    GAMEPLAY_T: ("EffectIntensity",),
    CAMERA_T: ("bUseBallIndicator",),
}


def _stock(sf, sidecar: dict[str, list[bytes]] | None) -> str:
    vids = _keys(sf, VIDEO_T)
    if sidecar and sidecar.get(VIDEO_T):
        # So as chaves que o Gutty mexe voltam ao valor pre-Gutty; o que o
        # jogador mudou depois (camera, FOV, resolucao) fica.
        for t in TARGET_TYPES:
            keys = _keys(sf, t)
            orig = sidecar.get(t) or []
            if len(keys) != len(orig):
                continue
            for k, blob in zip(keys, orig):
                cur = PropEditor(_get(sf, k))
                was = PropEditor(blob)
                for name in GUTTY_KEYS[t]:
                    p = was.get(name)
                    if p is None:
                        cur.remove(name)
                    else:
                        cur.put(p)
                _set(sf, k, cur.build())
        return "original"
    changed = False
    for k in vids:
        ed = PropEditor(_get(sf, k))
        if not looks_gutty(ed):
            continue
        for key in GUTTY_VIDEO_KEYS:
            ed.remove(key)
        _set(sf, k, ed.build())
        changed = True
        for g in sf.index_of(GAMEPLAY_T):
            ged = PropEditor(sf.objects[g])
            eff = ged.get("EffectIntensity")
            if eff is not None and eff.value == "EI_Low":
                ged.remove("EffectIntensity")
                sf.objects[g] = ged.build()
    return "padrao" if changed else ""


# ------------------------------------------------------------------- patch

def _verify(orig_sf, new_plain: bytes, mode: str, root_target: bool) -> None:
    nsf = parse_plain(new_plain)
    if nsf.types != orig_sf.types or nsf.head != orig_sf.head:
        raise SaveFormatError("tabela de objetos mudou")
    if root_target:
        PropEditor(nsf.root[4:])
    elif nsf.root != orig_sf.root:
        raise SaveFormatError("stream raiz mudou — abortado")
    for i, (t, _) in enumerate(orig_sf.types):
        if t in TARGET_TYPES:
            PropEditor(nsf.objects[i])
            continue
        if nsf.objects[i] != orig_sf.objects[i]:
            raise SaveFormatError(f"objeto {t} mudou — abortado")
    if mode in ("completo", "criador"):
        want = COMPLETO_OPTIONS if mode == "completo" else CRIADOR_OPTIONS
        for k in _keys(nsf, VIDEO_T):
            if PropEditor(_get(nsf, k)).video_options() != want:
                raise SaveFormatError("VideoOptions nao persistiu")


def patch_file(path: Path, mode: str, sidecar_dir: Path | None = None) -> str:
    """Retorna 'ok', 'same' ou levanta erro. Nunca deixa save pela metade."""
    raw = path.read_bytes()
    sf, plain = load(raw)
    vids = _keys(sf, VIDEO_T)
    if not vids:
        raise SaveFormatError("sem bloco de video")
    root_target = ROOT in vids

    import copy
    orig = copy.copy(sf)
    orig.objects = list(sf.objects)

    side = _sidecar_path(sidecar_dir, path)
    if mode == "stock":
        what = _stock(sf, _read_sidecar(side))
        if not what:
            return "same"
    else:
        _write_sidecar(side, sf)
        for t in TARGET_TYPES:
            for k in _keys(sf, t):
                ed = PropEditor(_get(sf, k))
                _EDITORS[t](ed, mode)
                _set(sf, k, ed.build())

    new_plain = build_plain(sf)
    if new_plain == plain:
        return "same"
    _verify(orig, new_plain, mode, root_target)
    out = encrypt_plain(new_plain)
    if decrypt_file(out)[: len(new_plain)] != new_plain:
        raise SaveFormatError("cifra nao confere")

    tmp = path.with_suffix(path.suffix + ".guttytmp")
    try:
        with open(tmp, "wb") as f:
            f.write(out)
            f.flush()
            os.fsync(f.fileno())
        os.replace(tmp, path)
    finally:
        try:
            if tmp.exists():
                tmp.unlink()
        except OSError:
            pass
    if path.read_bytes() != out:
        path.write_bytes(raw)
        raise SaveFormatError("leitura pos-gravacao divergiu — original restaurado")
    return "ok"


def _select_files(files: list[Path]) -> list[Path]:
    ranked = []
    for f in files:
        try:
            st = f.stat()
        except OSError:
            continue
        if st.st_size <= 0 or st.st_size > MAX_SAVE_BYTES:
            print(f"SKIP {f.name} (fora da faixa: {st.st_size // 1024}KB)", flush=True)
            continue
        ranked.append((st.st_mtime, f))
    ranked.sort(key=lambda x: x[0], reverse=True)
    return [f for _, f in ranked[:MAX_FILES]]


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=("completo", "criador", "stock"), default="completo")
    parser.add_argument("--sidecar", default=None)
    parser.add_argument("target")
    args = parser.parse_args(argv[1:])

    target = Path(args.target)
    sidecar_dir = Path(args.sidecar) if args.sidecar else None
    if target.is_file() and target.suffix.lower() == ".save":
        files = [target]
    elif target.is_dir():
        files = _select_files(list(target.glob("*.save")))
    else:
        print(f"alvo invalido: {target}", file=sys.stderr)
        return 2

    if not files:
        print("nenhum .save na pasta", flush=True)
        return 0

    errors = 0
    done = 0
    total = len(files)
    for i, f in enumerate(files, 1):
        print(f"BAR {i} {total} {int(i * 100 / total)} {f.name}", flush=True)
        try:
            r = patch_file(f, args.mode, sidecar_dir)
            print(f"{'OK' if r == 'ok' else 'SKIP'} {f.name}{'' if r == 'ok' else ' (ja sincronizado)'}", flush=True)
            done += 1
        except Exception as ex:  # noqa: BLE001
            print(f"SKIP {f.name}: {ex}", file=sys.stderr, flush=True)
            errors += 1
    print(f"BAR {total} {total} 100 done", flush=True)
    if done == 0 and errors > 0:
        print(f"WARN: {errors} save(s) invalidos ignorados", file=sys.stderr, flush=True)
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
