"""Patch Rocket League Epic .save video settings for GUTTYTECH COMPLETO (potato)."""
from __future__ import annotations

import sys
from pathlib import Path

_TOOLS_DIR = Path(__file__).resolve().parent
if str(_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(_TOOLS_DIR))

# Fix nixwrap experimental save(): ByteProperty enums + NameProperty ids.
import nixwrap.save_file._file_io as _fio
from save_codec import serialize_property_stream as _codec_serialize

_fio.serialize_property_stream = _codec_serialize
from nixwrap.save_file import load_raw  # noqa: E402
from nixwrap.save_file._file_io import assemble_savedata  # noqa: E402

# Menu PT: textura=Alto desempenho, mundo/particula=Desempenho, efeito=Baixa intensidade
OPTION_VALUES: dict[str, str] = {
    "RenderQuality": "Performance",
    "RenderDetail": "Performance",
    "TextureDetail": "TexturesLow",
    "ParticleDetail": "Low",
    "WorldDetail": "Quality",
    "AntiAlias": "0",
}

UNCAPPED_MAX_FPS = 10000


def _upsert_option(options: list[dict], option_id: str, value: str) -> list[dict]:
    opts = list(options or [])
    for opt in opts:
        if opt.get("Id") == option_id:
            if opt.get("Value") != value:
                opt["Value"] = value
            return opts
    opts.append({"Id": option_id, "Value": value})
    return opts


def _patch_video(obj: dict) -> bool:
    changed = False
    for key, val in (
        ("bShowLightShafts", False),
        ("bShowWeatherFX", False),
        ("bShowLensFlares", False),
        ("bUncappedFramerate", True),
        ("bVsync", False),
        ("MaxFPS", UNCAPPED_MAX_FPS),
    ):
        if obj.get(key) != val:
            obj[key] = val
            changed = True

    opts = list(obj.get("VideoOptions") or [])
    before = [(o.get("Id"), o.get("Value")) for o in opts]
    for option_id, value in OPTION_VALUES.items():
        opts = _upsert_option(opts, option_id, value)
    after = [(o.get("Id"), o.get("Value")) for o in opts]
    if before != after:
        obj["VideoOptions"] = opts
        changed = True
    return changed


def _patch_gameplay(obj: dict) -> bool:
    if obj.get("EffectIntensity") == "EI_Low":
        return False
    obj["EffectIntensity"] = "EI_Low"
    return True


def _patch_camera(obj: dict) -> bool:
    if obj.get("bUseBallIndicator") is True:
        return False
    obj["bUseBallIndicator"] = True
    return True


def _patch_raw(raw: dict) -> bool:
    changed = False
    for obj in raw.get("objects", []):
        t = obj.get("__type")
        if t == "TAGame.VideoSettingsSavePC_TA":
            changed |= _patch_video(obj)
        elif t == "TAGame.GameplaySettingsSave_TA":
            changed |= _patch_gameplay(obj)
        elif t == "TAGame.ProfileCameraSave_TA":
            changed |= _patch_camera(obj)
    return changed


def patch_file(path: Path) -> bool:
    raw = load_raw(path)
    if not _patch_raw(raw):
        return False
    assemble_savedata(raw, path)
    return True


def main(argv: list[str]) -> int:
    if len(argv) < 2:
        print("usage: patch_save_video.py <save_dir|file.save>", file=sys.stderr)
        return 2

    target = Path(argv[1])
    if target.is_file() and target.suffix.lower() == ".save":
        files = [target]
    elif target.is_dir():
        files = sorted(target.glob("*.save"))
    else:
        print(f"alvo invalido: {target}", file=sys.stderr)
        return 2

    if not files:
        print("nenhum .save encontrado", file=sys.stderr)
        return 1

    errors = 0
    for f in files:
        try:
            if patch_file(f):
                print(f"OK {f}")
            else:
                print(f"SKIP {f} (ja sincronizado)")
        except Exception as ex:
            print(f"FAIL {f}: {ex}", file=sys.stderr)
            errors += 1
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
