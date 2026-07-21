"""Patch Rocket League Epic .save video settings for GUTTYTECH (menu Epic sync)."""
from __future__ import annotations

import argparse
import sys
from pathlib import Path

_TOOLS_DIR = Path(__file__).resolve().parent
if str(_TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(_TOOLS_DIR))

import nixwrap.save_file._file_io as _fio
from save_codec import serialize_property_stream as _codec_serialize

_fio.serialize_property_stream = _codec_serialize
from nixwrap.save_file import load_raw  # noqa: E402
from nixwrap.save_file._file_io import assemble_savedata  # noqa: E402

UNCAPPED_MAX_FPS = 10000

# Menu Epic (EN): High Performance / Performance / Quality / High Quality.
# TextureDetail usa buckets do INI (TexturesLow = High Performance no menu).
COMPLETO_OPTIONS = [
    {"Id": "RenderQuality", "Value": "HighPerformance"},
    {"Id": "RenderDetail", "Value": "Performance"},
    {"Id": "TextureDetail", "Value": "TexturesLow"},
    {"Id": "ParticleDetail", "Value": "HighPerformance"},
    {"Id": "WorldDetail", "Value": "HighPerformance"},
    {"Id": "AntiAlias", "Value": "0"},
]


def _upsert_option(options: list[dict], option_id: str, value: str) -> list[dict]:
    opts = list(options or [])
    for opt in opts:
        if opt.get("Id") == option_id:
            if opt.get("Value") != value:
                opt["Value"] = value
            return opts
    opts.append({"Id": option_id, "Value": value})
    return opts


def _sanitize_options(options: list[dict] | None) -> list[dict]:
    """Remove entradas corrompidas (bug antigo: dict unpack → Id='Id', Value='Value')."""
    clean: list[dict] = []
    for opt in options or []:
        oid = opt.get("Id")
        val = opt.get("Value")
        if not isinstance(oid, str) or not isinstance(val, str):
            continue
        if oid in ("", "Id") or val in ("", "Value"):
            continue
        clean.append(opt)
    return clean


def _patch_video_flags(obj: dict, *, completo: bool) -> bool:
    changed = False
    for key, val in (
        ("bShowLightShafts", False),
        ("bShowWeatherFX", False),
        ("bUncappedFramerate", True),
        ("bVsync", False),
        ("MaxFPS", UNCAPPED_MAX_FPS),
    ):
        if obj.get(key) != val:
            obj[key] = val
            changed = True

    if completo:
        opts = _sanitize_options(obj.get("VideoOptions"))
        before = [(o.get("Id"), o.get("Value")) for o in (obj.get("VideoOptions") or [])]
        for item in COMPLETO_OPTIONS:
            opts = _upsert_option(opts, item["Id"], item["Value"])
        after = [(o.get("Id"), o.get("Value")) for o in opts]
        if before != after:
            obj["VideoOptions"] = opts
            changed = True

    return changed


def _patch_gameplay(obj: dict, *, completo: bool) -> bool:
    if not completo:
        return False
    if obj.get("EffectIntensity") == "EI_Low":
        return False
    obj["EffectIntensity"] = "EI_Low"
    return True


def _patch_camera(obj: dict) -> bool:
    if obj.get("bUseBallIndicator") is True:
        return False
    obj["bUseBallIndicator"] = True
    return True


def _patch_raw(raw: dict, *, completo: bool) -> bool:
    changed = False
    for obj in raw.get("objects", []):
        t = obj.get("__type")
        if t == "TAGame.VideoSettingsSavePC_TA":
            changed |= _patch_video_flags(obj, completo=completo)
        elif t == "TAGame.GameplaySettingsSave_TA":
            changed |= _patch_gameplay(obj, completo=completo)
        elif t == "TAGame.ProfileCameraSave_TA" and completo:
            changed |= _patch_camera(obj)
    return changed


def patch_file(path: Path, *, completo: bool) -> bool:
    raw = load_raw(path)
    if not _patch_raw(raw, completo=completo):
        return False
    assemble_savedata(raw, path)
    return True


def main(argv: list[str]) -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--mode", choices=("completo", "criador"), default="completo")
    parser.add_argument("target")
    args = parser.parse_args(argv[1:])

    completo = args.mode == "completo"
    target = Path(args.target)
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
            if patch_file(f, completo=completo):
                print(f"OK {f}")
            else:
                print(f"SKIP {f} (ja sincronizado)")
        except Exception as ex:
            print(f"FAIL {f}: {ex}", file=sys.stderr)
            errors += 1
    return 0 if errors == 0 else 1


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
