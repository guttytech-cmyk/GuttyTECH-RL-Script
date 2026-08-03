"""Patch Rocket League Epic/Steam .save video settings for GUTTYTECH (menu sync)."""
from __future__ import annotations

import argparse
import os
import shutil
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

# CRIADOR: limpa potato do COMPLETO. Sem RenderQuality → UI cai em Alta qualidade.
# TexturesHigher = visual bom; RenderDetail=Custom = toggles avancados ajustaveis.
CRIADOR_OPTIONS = [
    {"Id": "RenderDetail", "Value": "Custom"},
    {"Id": "TextureDetail", "Value": "TexturesHigher"},
    {"Id": "ParticleDetail", "Value": "Performance"},
    {"Id": "WorldDetail", "Value": "Quality"},
    {"Id": "AntiAlias", "Value": "0"},
]

COMPLETO_IDS = {o["Id"] for o in COMPLETO_OPTIONS}
CRIADOR_IDS = {o["Id"] for o in CRIADOR_OPTIONS}

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


def _option_ids(options: list[dict] | None) -> set[str]:
    return {o["Id"] for o in _sanitize_options(options)}


def _options_equal(a: list[dict], b: list[dict]) -> bool:
    return [(o.get("Id"), o.get("Value")) for o in a] == [(o.get("Id"), o.get("Value")) for o in b]


def _set_flag(obj: dict, key: str, val) -> bool:
    if obj.get(key) != val:
        obj[key] = val
        return True
    return False


def _flags_ok(obj: dict, *, completo: bool) -> bool:
    """Flags criticas do menu. Ausencia de flag=False e OK (jogo por vezes omite);
    so falha se estiver explicitamente no valor errado."""
    for key, val in VIDEO_FLAGS_COMMON:
        got = obj.get(key)
        if key == "MaxFPS":
            if got != val:
                return False
            continue
        if val is True:
            if got is not True:
                return False
        else:
            # desejado False: None/ausente/False OK; True e regressao
            if got is True:
                return False
    if completo:
        for key, val in VIDEO_FLAGS_COMPLETO:
            got = obj.get(key)
            if val is False and got is True:
                return False
            if val is True and got is not True and key in obj:
                return False
    return True


def _completo_options_ok(obj: dict) -> bool:
    """True so se VideoOptions esta completo e flags criticas batem.

    O RL grava VideoOptions 'sujo' (so campos alterados). Lista incompleta faz o
    cliente rejeitar o bloco inteiro → Alta qualidade / 60 FPS / particula vazia.
    """
    current = _sanitize_options(obj.get("VideoOptions"))
    if not _options_equal(current, COMPLETO_OPTIONS):
        return False
    if not COMPLETO_IDS.issubset(_option_ids(current)):
        return False
    return _flags_ok(obj, completo=True)


def _criador_options_ok(obj: dict) -> bool:
    current = _sanitize_options(obj.get("VideoOptions"))
    if not _options_equal(current, CRIADOR_OPTIONS):
        return False
    if not CRIADOR_IDS.issubset(_option_ids(current)):
        return False
    return _flags_ok(obj, completo=False)


def _looks_like_completo_options(obj: dict) -> bool:
    """Detecta VideoOptions potato do COMPLETO ainda no save (troca de modo)."""
    ids = {o.get("Id"): o.get("Value") for o in _sanitize_options(obj.get("VideoOptions"))}
    return ids.get("RenderQuality") == "Performance" or ids.get("TextureDetail") == "TexturesLow"


def _is_sparse_or_broken(obj: dict, *, completo: bool) -> bool:
    """VideoOptions parcial / None / flags em falta — precisa regravar sempre."""
    opts = obj.get("VideoOptions")
    if opts is None:
        return True
    current = _sanitize_options(opts)
    if len(current) == 0:
        return True
    needed = COMPLETO_IDS if completo else CRIADOR_IDS
    if not needed.issubset(_option_ids(current)):
        return True
    return not _flags_ok(obj, completo=completo)


def _force_video_profile(obj: dict, *, completo: bool) -> None:
    # Preserva janela/resolucao — o botao APLICAR do modo de exibicao no RL
    # reescreve VideoOptions para Alta qualidade; nao queremos resetar WindowMode.
    window = obj.get("WindowMode")
    resolution = obj.get("Resolution")

    if completo:
        obj["VideoOptions"] = [dict(x) for x in COMPLETO_OPTIONS]
        for key, val in VIDEO_FLAGS_COMMON:
            obj[key] = val
        for key, val in VIDEO_FLAGS_COMPLETO:
            if key in obj:
                obj[key] = val
    else:
        obj["VideoOptions"] = [dict(x) for x in CRIADOR_OPTIONS]
        for key, val in VIDEO_FLAGS_COMMON:
            obj[key] = val

    if window is not None:
        obj["WindowMode"] = window
    if isinstance(resolution, str) and resolution:
        obj["Resolution"] = resolution


def _patch_video_flags(obj: dict, *, completo: bool) -> bool:
    # Regrava se errado/esparso/vazio OU se RenderDetail=Custom (APLICAR em
    # Sem bordas deixa Custom e o menu explode em Alta qualidade / efeitos ON).
    ids = {o.get("Id"): o.get("Value") for o in _sanitize_options(obj.get("VideoOptions"))}
    if ids.get("RenderDetail") == "Custom" and completo:
        _force_video_profile(obj, completo=True)
        return True

    if completo:
        if _completo_options_ok(obj):
            return False
        _force_video_profile(obj, completo=True)
        return True

    if _criador_options_ok(obj) and not _looks_like_completo_options(obj):
        return False
    _force_video_profile(obj, completo=False)
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

    backup = path.with_suffix(path.suffix + ".guttybak")
    tmp = path.with_suffix(path.suffix + ".guttytmp")
    try:
        shutil.copy2(path, backup)
    except OSError:
        backup = None

    try:
        assemble_savedata(raw, tmp)
        # Valida em memoria (evita 2o load_raw — o mais lento do pipeline).
        if completo:
            for obj in raw.get("objects", []):
                if obj.get("__type") == "TAGame.VideoSettingsSavePC_TA":
                    if not _completo_options_ok(obj):
                        raise RuntimeError(f"patch nao persistiu VideoOptions completos em {path.name}")
        os.replace(tmp, path)
    except Exception:
        # Rollback atomico — nunca deixar save a meio.
        try:
            if tmp.exists():
                tmp.unlink()
        except OSError:
            pass
        if backup is not None and backup.exists():
            try:
                os.replace(backup, path)
            except OSError:
                pass
        raise
    finally:
        if backup is not None and backup.exists():
            try:
                backup.unlink()
            except OSError:
                pass
    return True


def _select_files(files: list[Path]) -> list[Path]:
    """So perfis recentes e leves — saves de 2MB+ demoram minutos no decrypt UE3."""
    max_bytes = 1_200_000
    max_files = 6
    ranked = sorted(files, key=lambda p: p.stat().st_mtime, reverse=True)
    chosen: list[Path] = []
    for f in ranked:
        try:
            sz = f.stat().st_size
        except OSError:
            continue
        if sz > max_bytes:
            print(f"SKIP {f.name} (grande demais: {sz // 1024}KB)", flush=True)
            continue
        chosen.append(f)
        if len(chosen) >= max_files:
            break
    return chosen


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
        files = _select_files(list(target.glob("*.save")))
    else:
        print(f"alvo invalido: {target}", file=sys.stderr)
        return 2

    if not files:
        print("nenhum .save elegivel (recentes <1.2MB)", file=sys.stderr)
        return 1

    errors = 0
    patched = 0
    total = len(files)
    for i, f in enumerate(files, 1):
        try:
            pct = int(i * 100 / total)
            print(f"BAR {i} {total} {pct} {f.name}", flush=True)
            if patch_file(f, completo=completo):
                print(f"OK {f.name}", flush=True)
                patched += 1
            else:
                print(f"SKIP {f.name} (ja sincronizado)", flush=True)
                patched += 1
        except Exception as ex:
            print(f"SKIP {f.name}: {ex}", file=sys.stderr, flush=True)
            errors += 1
    print(f"BAR {total} {total} 100 done", flush=True)
    # Pasta so com .save lixo/corrompido (ex. Steam stub) nao deve falhar o heal Epic.
    if patched == 0 and errors > 0:
        print(f"WARN: {errors} save(s) invalidos ignorados", file=sys.stderr, flush=True)
        return 0
    return 0


if __name__ == "__main__":
    raise SystemExit(main(sys.argv))
