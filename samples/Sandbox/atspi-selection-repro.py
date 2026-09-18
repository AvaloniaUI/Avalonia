#!/usr/bin/env python3
"""Exercises org.a11y.atspi.Selection.GetSelectedChild against the Sandbox sample.

The AT-SPI Selection interface on an Avalonia ComboBox reports NSelectedChildren = 1
but answers GetSelectedChild(0) with the null reference, so an assistive technology
can see that something is selected but never find out what. This script drives the
Sandbox window over the accessibility bus and prints what the bridge answers for:

  1. a closed ComboBox   - broken: the selected item is an UnrealizedSelectionPeer
                           that is never a child of anything, so it has no AT-SPI node
  2. a ListBox           - works:  the selected item is a realized child
  3. an open ComboBox    - broken until a client happens to walk the popup subtree,
                           which is what creates the node for the selected item

Needs python3-dbus (Fedora) / python3-dbus (Debian) and a running AT-SPI bus.

Usage:
    ./atspi-selection-repro.py                 # build output is launched automatically
    ./atspi-selection-repro.py --attach        # use an already-running Sandbox
    ./atspi-selection-repro.py --tree          # also dump the accessible tree

Exits 0 when the bridge resolves every selected child, 1 while the bug is present.
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
ACTION = "org.a11y.atspi.Action"
SELECTION = "org.a11y.atspi.Selection"
PROPERTIES = "org.freedesktop.DBus.Properties"

A11Y_BUS_NAME = "org.a11y.Bus"
A11Y_BUS_PATH = "/org/a11y/bus"
A11Y_STATUS = "org.a11y.Status"

REGISTRY_NAME = "org.a11y.atspi.Registry"
REGISTRY_ROOT = "/org/a11y/atspi/accessible/root"
NULL_PATH = "/org/a11y/atspi/null"

APP_NAME = "Avalonia Application"
WINDOW_TITLE = "AtSpi Selection Repro"

HERE = os.path.dirname(os.path.abspath(__file__))
DEFAULT_BINARY = os.path.join(HERE, "bin", "Debug", "net10.0", "Sandbox")


class Node:
    """An accessible, addressed the way AT-SPI addresses one: a (bus name, path) pair."""

    def __init__(self, bus, service, path):
        self.bus = bus
        self.service = str(service)
        self.path = str(path)
        self.proxy = bus.get_object(self.service, self.path)

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
    def interfaces(self):
        return sorted(str(i) for i in self.proxy.GetInterfaces(dbus_interface=ACCESSIBLE))

    @property
    def children(self):
        return [Node(self.bus, ref[0] or self.service, ref[1])
                for ref in self.proxy.GetChildren(dbus_interface=ACCESSIBLE)]

    # --- Selection ---

    @property
    def n_selected_children(self):
        return int(self.get(SELECTION, "NSelectedChildren"))

    def selected_child(self, index):
        ref = self.proxy.GetSelectedChild(dbus.Int32(index), dbus_interface=SELECTION)
        return Node(self.bus, ref[0] or self.service, ref[1])

    # --- Action ---

    def do_action(self, index=0):
        self.proxy.DoAction(dbus.Int32(index), dbus_interface=ACTION)

    # --- Traversal ---

    def descendants(self):
        for child in self.children:
            yield child
            yield from child.descendants()

    def find(self, role, required=True):
        for node in self.descendants():
            if node.role == role:
                return node
        if required:
            raise LookupError(f"no {role!r} in the accessible tree")
        return None

    def dump(self, depth=0, out=sys.stdout):
        print("  " * depth + f"{self.role} {self.name!r} {self.path}", file=out)
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


def wait_for_popup(app, timeout=5.0):
    """Waits for the drop-down's own top level to appear beside the main window."""
    deadline = time.monotonic() + timeout
    while time.monotonic() < deadline:
        for child in app.children:
            if child.name == "PopupRoot":
                # The items are added a moment after the popup root itself.
                time.sleep(0.5)
                return child
        time.sleep(0.1)
    return None


def describe(node, index=0):
    """Reads NSelectedChildren and GetSelectedChild, and says whether they agree."""
    count = node.n_selected_children
    selected = node.selected_child(index)
    if selected.is_null:
        return count, None, f"NSelectedChildren={count}, GetSelectedChild({index})=<null reference>"
    return (count, selected,
            f"NSelectedChildren={count}, GetSelectedChild({index})={selected.path} {selected.name!r}")


def main():
    parser = argparse.ArgumentParser(description=__doc__,
                                     formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--attach", action="store_true",
                        help="use an already-running Sandbox instead of launching one")
    parser.add_argument("--binary", default=DEFAULT_BINARY,
                        help=f"Sandbox binary to launch (default: {DEFAULT_BINARY})")
    parser.add_argument("--tree", action="store_true",
                        help="dump the accessible tree at each step")
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
        print(f"Found {app.name!r} at {app.service}, window {window.name!r}\n")

        if args.tree:
            print("--- accessible tree ---")
            app.dump()
            print()

        combo = window.find("combo box")
        listbox = window.find("list")
        failures = []

        # 1. Closed ComboBox: the selected item is an UnrealizedSelectionPeer, which is
        #    not a child of the combo box, so the bridge never made a node for it.
        _, selected, line = describe(combo)
        print(f"closed ComboBox   {line}")
        if selected is None:
            failures.append("closed ComboBox does not resolve its selected child")

        # 2. ListBox: the control case. Its selected item is a realized child that the
        #    bridge attached while building the tree, so this one resolves.
        _, selected, line = describe(listbox)
        print(f"ListBox           {line}")
        if selected is None:
            failures.append("ListBox does not resolve its selected child")

        # 3. Open ComboBox: the items now exist, but they live in the popup's own
        #    subtree. GetSelectedChild only looks up peers that already have a node,
        #    and opening the drop-down does not create one.
        combo.do_action(0)
        popup = wait_for_popup(app)
        _, selected, line = describe(combo)
        print(f"open ComboBox     {line}")
        if selected is None:
            failures.append("open ComboBox does not resolve its selected child")

        # 3b. Walking the popup is what creates the nodes. Afterwards the very same
        #     call answers correctly, which is what makes this order-dependent.
        if popup is not None:
            for _ in popup.descendants():
                pass
            if args.tree:
                print("\n--- popup subtree ---")
                popup.dump()
                print()
            _, selected, line = describe(combo)
            print(f"  after walking the popup subtree: {line}")

        combo.do_action(0)

        print()
        if failures:
            print(f"FAIL: {len(failures)} selection(s) unresolved")
            for failure in failures:
                print(f"  - {failure}")
            return 1

        print("PASS: every selected child resolved")
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
