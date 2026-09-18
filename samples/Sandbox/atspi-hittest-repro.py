#!/usr/bin/env python3
"""Exercises org.a11y.atspi.Component.GetAccessibleAtPoint against the Sandbox sample.

AT-SPI hit-testing is done one level at a time: GetAccessibleAtPoint on an object
returns the child of that object which contains the point, and a client repeats the
call on each result until it gets the null reference, to find the deepest object at
the point. Avalonia's bridge instead returns the object it was called on whenever
the point is inside it. Called on the window, that means the window itself, so
hit-testing never gets past the window.

For each target control this script reads the control's screen extents, then asks
the window what is at the centre of them, and prints:

  1. what one GetAccessibleAtPoint call on the window returns - expected: the
     window's child that contains the point, got: the window itself
  2. where a client walking down from the window ends up - expected: at or below
     the target control, got: the window

Needs python3-dbus (Fedora) / python3-dbus (Debian) and a running AT-SPI bus.

Usage:
    ./atspi-hittest-repro.py                 # build output is launched automatically
    ./atspi-hittest-repro.py --attach        # use an already-running Sandbox
    ./atspi-hittest-repro.py --tree          # also dump the accessible tree

Exits 0 when every target is found by hit-testing, 1 while the bug is present.
"""

import argparse
import os
import subprocess
import sys
import time

try:
    import dbus
except ImportError:
    sys.exit("python3-dbus is required: dnf install python3-dbus / apt install python3-dbus")

ACCESSIBLE = "org.a11y.atspi.Accessible"
COMPONENT = "org.a11y.atspi.Component"
PROPERTIES = "org.freedesktop.DBus.Properties"

A11Y_BUS_NAME = "org.a11y.Bus"
A11Y_BUS_PATH = "/org/a11y/bus"
A11Y_STATUS = "org.a11y.Status"

REGISTRY_NAME = "org.a11y.atspi.Registry"
REGISTRY_ROOT = "/org/a11y/atspi/accessible/root"
NULL_PATH = "/org/a11y/atspi/null"

# AtspiCoordType
COORD_SCREEN = 0

APP_NAME = "Avalonia Application"
WINDOW_TITLE = "AtSpi Hit Test Repro"

# (role, name) of each control to hit-test. None matches any name.
TARGETS = [
    ("push button", "Button"),
    ("entry", None),
    ("list item", "Item 1"),
]

# A walk that never reaches the null reference would otherwise loop forever.
MAX_DEPTH = 32

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_BINARY = os.path.join(HERE, "bin", "Debug", "net10.0", "Sandbox")


class Node:
    """An accessible, addressed the way AT-SPI addresses one: a (bus name, path) pair."""

    def __init__(self, bus, service, path):
        self.bus = bus
        self.service = str(service)
        self.path = str(path)
        self.proxy = bus.get_object(self.service, self.path)

    def __eq__(self, other):
        return isinstance(other, Node) and (self.service, self.path) == (other.service, other.path)

    def __hash__(self):
        return hash((self.service, self.path))

    @property
    def is_null(self):
        return self.path == NULL_PATH

    def get(self, interface, prop):
        return self.proxy.Get(interface, prop, dbus_interface=PROPERTIES)

    @property
    def name(self):
        return str(self.get(ACCESSIBLE, "Name"))

    @property
    def role(self):
        return str(self.proxy.GetRoleName(dbus_interface=ACCESSIBLE))

    @property
    def parent(self):
        ref = self.get(ACCESSIBLE, "Parent")
        return Node(self.bus, ref[0] or self.service, ref[1])

    @property
    def children(self):
        return [Node(self.bus, ref[0] or self.service, ref[1])
                for ref in self.proxy.GetChildren(dbus_interface=ACCESSIBLE)]

    def label(self):
        return f"{self.role} {self.name!r} {self.path}"

    # --- Component ---

    def extents(self):
        x, y, w, h = self.proxy.GetExtents(dbus.UInt32(COORD_SCREEN), dbus_interface=COMPONENT)
        return int(x), int(y), int(w), int(h)

    def contains(self, x, y):
        return bool(self.proxy.Contains(dbus.Int32(x), dbus.Int32(y), dbus.UInt32(COORD_SCREEN),
                                        dbus_interface=COMPONENT))

    def accessible_at_point(self, x, y):
        ref = self.proxy.GetAccessibleAtPoint(dbus.Int32(x), dbus.Int32(y), dbus.UInt32(COORD_SCREEN),
                                              dbus_interface=COMPONENT)
        return Node(self.bus, ref[0] or self.service, ref[1])

    # --- Traversal ---

    def descendants(self):
        for child in self.children:
            yield child
            yield from child.descendants()

    def find(self, role, name=None):
        for node in self.descendants():
            if node.role == role and (name is None or node.name == name):
                return node
        raise LookupError(f"no {role!r} named {name!r} in the accessible tree")

    def path_from(self, ancestor):
        """The chain of objects from ancestor down to this one, both included."""
        chain = [self]
        node = self
        while node != ancestor:
            node = node.parent
            if node.is_null:
                raise LookupError(f"{self.label()} is not below {ancestor.label()}")
            chain.append(node)
        return list(reversed(chain))

    def dump(self, depth=0, out=sys.stdout):
        print("  " * depth + self.label(), file=out)
        for child in self.children:
            child.dump(depth + 1, out)


def a11y_status(session_bus):
    proxy = session_bus.get_object(A11Y_BUS_NAME, A11Y_BUS_PATH)
    return bool(proxy.Get(A11Y_STATUS, "IsEnabled", dbus_interface=PROPERTIES))


def set_a11y_status(session_bus, enabled):
    proxy = session_bus.get_object(A11Y_BUS_NAME, A11Y_BUS_PATH)
    proxy.Set(A11Y_STATUS, "IsEnabled", dbus.Boolean(enabled), dbus_interface=PROPERTIES)


def a11y_bus_address(session_bus):
    proxy = session_bus.get_object(A11Y_BUS_NAME, A11Y_BUS_PATH)
    return str(proxy.GetAddress(dbus_interface=A11Y_BUS_NAME))


def find_app(bus, timeout=15.0):
    """Waits for the Sandbox window to show up on the accessibility bus."""
    registry = Node(bus, REGISTRY_NAME, REGISTRY_ROOT)
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        for app in registry.children:
            try:
                if app.name != APP_NAME:
                    continue
                for window in app.children:
                    if window.name == WINDOW_TITLE:
                        return app, window
            except dbus.DBusException:
                continue
        time.sleep(0.25)
    raise LookupError(f"no {APP_NAME!r} window titled {WINDOW_TITLE!r} on the accessibility bus")


def hit_test(window, x, y):
    """Walks down from the window the way an AT-SPI client does, returning every step."""
    steps = [window]
    node = window
    for _ in range(MAX_DEPTH):
        hit = node.accessible_at_point(x, y)
        if hit.is_null or hit == node:
            break
        steps.append(hit)
        node = hit
    return steps


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--attach", action="store_true",
                        help="use an already-running Sandbox instead of launching one")
    parser.add_argument("--binary", default=DEFAULT_BINARY,
                        help=f"Sandbox binary to launch (default: {DEFAULT_BINARY})")
    parser.add_argument("--tree", action="store_true",
                        help="dump the accessible tree before hit-testing")
    args = parser.parse_args()

    session_bus = dbus.SessionBus()

    # The bridge only starts when a client has announced itself, so turn accessibility
    # on before launching the app and put the setting back on the way out.
    restore_a11y = None
    if not a11y_status(session_bus):
        print("Accessibility is off; enabling org.a11y.Status.IsEnabled for this run.")
        set_a11y_status(session_bus, True)
        restore_a11y = False

    app_process = None
    try:
        if not args.attach:
            if not os.path.exists(args.binary):
                sys.exit(f"{args.binary} not found - build the Sandbox sample first")
            app_process = subprocess.Popen([args.binary],
                                           stdout=subprocess.DEVNULL,
                                           stderr=subprocess.DEVNULL)

        bus = dbus.bus.BusConnection(a11y_bus_address(session_bus))
        app, window = find_app(bus)
        # Give the window time to be laid out and placed, so the extents are final.
        time.sleep(1.0)
        print(f"Found {app.name!r} at {app.service}, window {window.name!r} "
              f"at {window.extents()}\n")

        if args.tree:
            print("--- accessible tree ---")
            window.dump()
            print()

        failures = []
        for role, name in TARGETS:
            target = window.find(role, name)
            expected = target.path_from(window)
            x, y, w, h = target.extents()
            cx, cy = x + w // 2, y + h // 2

            print(f"{target.label()}")
            print(f"  extents ({x}, {y}, {w}, {h}), hit-testing its centre ({cx}, {cy})")
            print(f"  expected walk: {' -> '.join(n.path for n in expected)}")

            # The point really is inside the target, as far as the bridge is concerned.
            if not target.contains(cx, cy):
                failures.append(f"{target.label()}: Contains is false for its own centre")
                print("  Contains(centre) = false - the extents are not usable, skipping\n")
                continue

            # 1. One call on the window should name the window's child on the way to the
            #    target, not the window.
            first = window.accessible_at_point(cx, cy)
            first_label = "<null reference>" if first.is_null else first.label()
            print(f"  window.GetAccessibleAtPoint = {first_label}")
            if first != expected[1]:
                failures.append(f"{target.label()}: window.GetAccessibleAtPoint returned "
                                f"{first_label}, expected {expected[1].label()}")

            # 2. Walking down should pass through the target. It may carry on below it
            #    into the control's template parts (the text inside a button, say):
            #    those are the deepest objects at the point, which is correct.
            steps = hit_test(window, cx, cy)
            print(f"  actual walk:   {' -> '.join(n.path for n in steps)}")
            if steps[:len(expected)] != expected:
                failures.append(f"{target.label()}: hit-testing stopped at {steps[-1].label()}")
            print()

        if failures:
            print(f"FAIL: {len(failures)} hit test(s) did not reach the target")
            for failure in failures:
                print(f"  - {failure}")
            return 1

        print("PASS: every target was found by hit-testing")
        return 0
    finally:
        if app_process is not None:
            app_process.terminate()
            try:
                app_process.wait(timeout=5)
            except subprocess.TimeoutExpired:
                app_process.kill()
        if restore_a11y is not None:
            set_a11y_status(session_bus, restore_a11y)


if __name__ == "__main__":
    sys.exit(main())
