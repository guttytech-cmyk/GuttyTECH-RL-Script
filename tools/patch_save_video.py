"""Patch Rocket League Epic/Steam .save video settings for GUTTYTECH (menu sync)."""
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

# Valores FName/Str que o cliente Epic ACEITA (UI PT: Desempenho / Alto desempenho).
# HighPerformance em Particle/Render quebra o menu (branco / Alta qualidade).
# ParticleDetail=Low TAMBEM quebra (dropdown vazio → menu cai em Alta qualidade / 60 FPS).
# TextureDetail=TexturesLow = "Alto desempenho" / High Performance no menu.
COMPLETO_OPTIONS = [
    {"Id": "RenderQuality", "Value": "Performance"},
    {"Id": "RenderDetail", "Value": "Performance"},
    {"Id": "TextureDetail", "Value": "TexturesLow"},
    {"Id": "ParticleDetail", "Value": "Performance"},
    {"Id": "WorldDetail", "Value": "Performance"},
    {"Id": "AntiAlias", "Value": "0"},
]

# Flags do menu Video (BakkesMod VideoSettings + saves reais).
VIDEO_FLAGS_COMMON = (
    ("bShowLightShafts", False),
    ("bShowWeatherFX", False),
    ("bShowLensFlares", False),
    ("bUncappedFramerate", True),
    ("bVsync", False),
    ("MaxFPS", UNCAPPED_MAX_FPS),
)

# So COMPLETO: High Quality Shaders off.
VIDEO_FLAGS_COMPLETO = (
    ("bTranslucentArenaShaders", False),
)

REQUIRED_COMPLETO_OPTION_IDS = {o["Id"] for o in COMPLETO_OPTIONS}


def _sanitize_options(options: list[dict] | None) -> list[dict]:
    """Remove entradas corrompidas (bug antigo: Id='Id', Value='Value')."""
    clean: list[dict] = []
    for opt in options or []:
        oid = opt.get("Id")
        val = opt.get("Value")
        if not isinstance(oid, str) or not isinstance(val, str):
            continue
        if oid in ("", "Id") or val in ("", "Value"):
            continue
        clean.append({"Id": oid, "Value": val})
    return clean


def _options_equal(a: list[dict], b: list[dict]) -> bool:
    return [(o.get("Id"), o.get("Value")) for o in a] == [(o.get("Id"), o.get("Value")) for o in b]


def _set_flag(obj: dict, key: str, val) -> bool:
    if obj.get(key) != val:
        obj[key] = val
        return True
    return False


def _patch_video_flags(obj: dict, *, completo: bool) -> bool:
    changed = False
    for key, val in VIDEO_FLAGS_COMMON:
        changed |= _set_flag(obj, key, val)

    if completo:
        # So grava se ja existir no save — adicionar campo novo em alguns
        # perfis faz o jogo rejeitar o bloco Video e cair em Alta qualidade.
        for key, val in VIDEO_FLAGS_COMPLETO:
            if key in obj:
                changed |= _set_flag(obj, key, val)

        desired = [dict(x) for x in COMPLETO_OPTIONS]
        if not _completo_options_ok(obj):
            obj["VideoOptions"] = desired
            for key, val in VIDEO_FLAGS_COMMON:
                obj[key] = val
            for key, val in VIDEO_FLAGS_COMPLETO:
                if key in obj:
                    obj[key] = val
            changed = True

    return changed


def _completo_options_ok(obj: dict) -> bool:
    """True so se VideoOptions esta completo e flags criticas batem."""
    current = _sanitize_options(obj.get("VideoOptions"))
    if not _options_equal(current, COMPLETO_OPTIONS):
        return False
    for key, val in VIDEO_FLAGS_COMMON:
        if obj.get(key) != val:
            return False
    for key, val in VIDEO_FLAGS_COMPLETO:
        if key in obj and obj.get(key) != val:
            return False
    return True


def _patch_gameplay(obj: dict, *, completo: bool) -> bool:
    if not completo:
        return False
    changed = False
    if obj.get("EffectIntensity") != "EI_Low":
        obj["EffectIntensity"] = "EI_Low"
        changed = True
    return changed


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
    # Verifica round-trip: se o jogo/perfil ativo perder campos, falha visivel no log.
    if completo:
        verify = load_raw(path)
        for obj in verify.get("objects", []):
            if obj.get("__type") == "TAGame.VideoSettingsSavePC_TA":
                if not _completo_options_ok(obj):
                    raise RuntimeError(f"patch nao persistiu VideoOptions completos em {path.name}")
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
    patched = 0
    for f in files:
        try:
            if patch_file(f, completo=completo):
                print(f"OK {f}")
                patched += 1
            else:
                print(f"SKIP {f} (ja sincronizado)")
                patched += 1
        except Exception as ex:
            print(f"SKIP {f}: {ex}", file=sys.stderr)
            errors += 1
    if patched == 0 and errors > 0:
        return 1
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
