#!/usr/bin/env python3
"""Inject one bounded touchscreen tap for developer tests on an unlocked Ubuntu Touch device.

Uses the already-permitted evdev interface; does not change device permissions or create a root helper.
Pass an event device verified with /proc/bus/input/devices. Coordinates use its native absolute range.
"""
import argparse
import fcntl
import os
import struct
import time

parser = argparse.ArgumentParser()
parser.add_argument("device")
parser.add_argument("x", type=int)
parser.add_argument("y", type=int)
parser.add_argument("--swipe-to", type=int, nargs=2, metavar=("X", "Y"))
parser.add_argument("--hold-ms", type=int, default=80)
args = parser.parse_args()
if not 20 <= args.hold_ms <= 2000:
    parser.error("hold duration must be 20..2000 ms")
fd = os.open(args.device, os.O_RDWR)


def absolute(axis):
    return struct.unpack("6i", fcntl.ioctl(fd, 0x80184540 + axis, bytes(24)))


def emit(kind, code, value):
    os.write(fd, struct.pack("llHHi", 0, 0, kind, code, value))


try:
    coordinates = [(53, args.x), (54, args.y)]
    if args.swipe_to:
        coordinates += [(53, args.swipe_to[0]), (54, args.swipe_to[1])]
    for axis, value in coordinates:
        _, lower, upper, *_ = absolute(axis)
        if not lower <= value <= upper:
            raise ValueError("Coordinate outside the touchscreen range")
    _, slot_min, slot_max, *_ = absolute(47)
    slot_count = slot_max - slot_min + 1
    slots = struct.pack(f"{slot_count + 1}i", 57, *([0] * slot_count))
    contacts = struct.unpack(f"{slot_count + 1}i", fcntl.ioctl(fd, 0x8000450a | (len(slots) << 16), slots))[1:]
    if any(contact != -1 for contact in contacts):
        raise RuntimeError("A touch is already active; refusing to overlap it")
    emit(3, 47, 0)  # ABS_MT_SLOT
    emit(3, 57, 199)  # ABS_MT_TRACKING_ID
    emit(3, 53, args.x)
    emit(3, 54, args.y)
    emit(1, 330, 1)  # BTN_TOUCH
    emit(0, 0, 0)
    try:
        time.sleep(args.hold_ms / 1000)
        if args.swipe_to:
            for step in range(1, 21):
                emit(3, 53, round(args.x + (args.swipe_to[0] - args.x) * step / 20))
                emit(3, 54, round(args.y + (args.swipe_to[1] - args.y) * step / 20))
                emit(0, 0, 0)
                time.sleep(0.015)
    finally:
        emit(3, 57, -1)
        emit(1, 330, 0)
        emit(0, 0, 0)
finally:
    os.close(fd)
