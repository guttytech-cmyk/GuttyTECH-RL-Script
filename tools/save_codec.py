"""UE3 save serializer fixes for RL (ByteProperty enums, NameProperty ids)."""
from __future__ import annotations

import struct

from nixwrap.save_file._binary_serializer import write_ue3

BYTE_ENUM_FIELDS: dict[str, str] = {
    "EffectIntensity": "EEffectsIntensity",
    "StatEventDisplayLevel": "EStatEventDisplayLevels",
    "TrainingControlsVisibility": "EControlsVisibilityType",
}

# Id e Value de VideoOptions sao FName (NameProperty), nao StrProperty.
NAME_FIELDS: set[str] = {"Id", "Value"}


def serialize_scalar(name: str, val) -> tuple[str, bytes]:
    if name in BYTE_ENUM_FIELDS and isinstance(val, str):
        enum_type = BYTE_ENUM_FIELDS[name]
        body = write_ue3(enum_type) + write_ue3(val)
        return "ByteProperty", body
    if isinstance(val, bool):
        return "BoolProperty", b"\x01" if val else b"\x00"
    if isinstance(val, int):
        if val > 0x7FFFFFFF or val < -0x80000000:
            return "QWordProperty", struct.pack("<Q", val)
        return "IntProperty", struct.pack("<i", val)
    if isinstance(val, float):
        return "FloatProperty", struct.pack("<f", val)
    if isinstance(val, str):
        if name in NAME_FIELDS:
            return "NameProperty", write_ue3(val)
        return "StrProperty", write_ue3(val)
    if isinstance(val, dict):
        return _serialize_struct(val)
    if isinstance(val, list):
        return _serialize_array(val)
    return "IntProperty", struct.pack("<i", 0)


def serialize_property_stream(props: dict) -> bytes:
    buf = b""
    scalars: dict = {}
    arrays: dict = {}
    for name, val in props.items():
        if name == "__type":
            continue
        if isinstance(val, list):
            arrays[name] = val
        else:
            scalars[name] = val

    for name, val in scalars.items():
        buf += write_ue3(name)
        tag, body = serialize_scalar(name, val)
        buf += write_ue3(tag)
        buf += struct.pack("<i", len(body))
        buf += struct.pack("<i", 0)
        buf += body

    for name, arr in arrays.items():
        payload = struct.pack("<i", len(arr))
        for elem in arr:
            if isinstance(elem, dict):
                _, ebody = _serialize_struct(elem, is_array_elem=True)
            else:
                from nixwrap.save_file._binary_serializer import _serialize_value as orig_serialize_value
                _, ebody = orig_serialize_value(elem, is_array_elem=True)
            payload += ebody
        buf += write_ue3(name)
        buf += write_ue3("ArrayProperty")
        buf += struct.pack("<i", len(payload))
        buf += struct.pack("<i", 0)
        buf += payload

    buf += write_ue3("None")
    return buf


def _serialize_struct(d: dict, is_array_elem: bool = False) -> tuple[str, bytes]:
    from nixwrap.save_file._crypto import OBJHEADER

    tn = d.get("__type", "Unknown")
    props = {k: v for k, v in d.items() if k != "__type"}
    body = b""
    if tn in ("Vector", "Rotator"):
        x = props.get("x", props.get("pitch", 0.0))
        y = props.get("y", props.get("yaw", 0.0))
        z = props.get("z", props.get("roll", 0.0))
        body = struct.pack("<fff", x, y, z)
    elif tn == "Guid":
        body = b"\x00" * 16
    elif tn == "Unknown":
        body = serialize_property_stream(props)
        return "StructProperty", body
    elif "." in tn and is_array_elem:
        body = struct.pack("<I", OBJHEADER) + serialize_property_stream(props)
    else:
        body = serialize_property_stream(props)
    return "StructProperty", write_ue3(tn) + body


def _serialize_array(lst: list) -> tuple[str, bytes]:
    from nixwrap.save_file._binary_serializer import _serialize_value as orig_serialize_value

    payload = struct.pack("<i", len(lst))
    for elem in lst:
        if isinstance(elem, dict):
            _, ebody = _serialize_struct(elem, is_array_elem=True)
        else:
            _, ebody = orig_serialize_value(elem, is_array_elem=True)
        payload += ebody
    return "ArrayProperty", payload
