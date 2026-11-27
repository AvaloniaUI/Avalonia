# Ubuntu Touch on X11/XWayland

Ubuntu Touch integration is internal to Avalonia.FreeDesktop and Avalonia.X11. Applications use the normal `AppBuilder`, `TextBox`, `TextInputOptions`, `TopLevel.Clipboard` and `TopLevel.InputPane`. No control registration, Qt library, external input adapter or UTDotNet package is required. The standalone example is `samples/UbuntuTouchDemo`.

The sample defines its application theme, main view, dialog and dynamic-field template in AXAML. Code-behind handles input, clipboard commands, dynamic template creation and positioning within the available screen area. Sample UI strings and diagnostics are in English.

## Selection and overrides

The X11 backend enables Ubuntu Touch defaults for an explicit Touch distribution ID/variant, or a UBports system image corroborated by the Ubuntu distribution or Lomiri session. Lomiri, Maliit, ARM64 and a Wayland display variable alone are insufficient.

- `X11PlatformOptions.UseUbuntuTouch = true/false` overrides detection for the X11 backend.
- `AVALONIA_UBUNTU_TOUCH=1/0` overrides automatic detection when that option is unset.
- `AVALONIA_IM_MODULE=maliit` selects just Maliit. Existing IBus/Fcitx selections and `AVALONIA_IM_MODULE=none` remain available. `EnableIme=false` disables the input method.
- Ubuntu Touch uses Content Hub for the regular clipboard, overlay popups, and an absolute scale of `GRID_UNIT_PX / 8` for valid grid sizes 8–48. Explicit Avalonia scale settings take precedence. X11 primary selection remains independent.

Applications retain responsibility for their Click identity, responsive layout and theme. The example launcher sets `RESOURCE_NAME` from `APP_ID` so deriving WM_CLASS does not require reading `/proc` under confinement. Its AppArmor policy requests no extra policy groups.

## Input and editing

Each focus context has a private D-Bus discovery connection and a separate local Unix peer connection. Outgoing calls are serialized and bounded to four seconds. Timeout/cancellation disposes only the affected transport. Initialization callbacks are buffered until the peer handler is installed (at most 32 frames and 64 KiB), retaining the workaround required by Tmds.DBus.Protocol 0.94.1.

Callbacks run on the UI thread before acknowledgement. Generic clients can override `TextInputMethodClient.CommitText` to handle relative replacement, empty deletion and explicit cursor positioning synchronously. `TextBox` implements this through its normal input/editing path, including undo, read-only mode, MaxLength and multiline rules. Older custom clients retain ordinary string commits through the input event route; they must implement the new method for replacement operations.

Composition is finalized before pointer selection, focus loss, reset and server hide. An external text replacement invalidates the old composition. Stale callbacks cannot edit a newly focused client. Qt key codes are converted to X11 keysyms and signal-only requests do not edit. Password and PIN state omits surrounding text and offsets; `PasswordChar` also marks the client sensitive even without explicit input options.

`IInputPane.OccludedRect` is the intersection of the reported global pixel rectangle and the client viewport, converted to logical units. Position, size and scale changes recompute it. An already resized window has no overlap, so applications should reserve only the reported overlap rather than subtracting the full keyboard height again. A successful show request alone does not prove that the keyboard is visible.

## Clipboard

Content Hub is called from the application process using `APP_ID` and an empty surface ID, allowing the service's normal caller-PID focus check. There is no X11 cache fallback. Only UTF-8 plain text is supported; malformed, oversized and non-text payloads are rejected. Clear writes an empty plain-text paste. A denied write throws an expected clipboard exception so Cut retains source text. A timed-out operation closes its own connection. TextBox invalidates pending edits when its text, selection, focus, window activation or attachment changes.

The separate X11 SAVE_TARGETS fix bounds best-effort clipboard-manager persistence to two seconds after ownership has succeeded. It does not replace Content Hub interoperability.

## Build and test

```sh
dotnet build src/Avalonia.X11/Avalonia.X11.csproj -f net8.0
dotnet build src/Avalonia.X11/Avalonia.X11.csproj -f net10.0
dotnet test --project tests/Avalonia.FreeDesktop.UnitTests/Avalonia.FreeDesktop.UnitTests.csproj
dotnet test --project tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj
samples/UbuntuTouchDemo/build-click.sh
```

The native clipboard protocol test starts an isolated `dbus-daemon` and verifies signatures, caller identity, Unicode, rejection and timeout behavior. Backend tests cover stale contexts, reset, preedit, callbacks, key conversion and coordinate conversion. Handshake tests cover partial reads, ordering, limits and disposal. TextBox tests cover replacement/undo and asynchronous clipboard races.

The sample Click package is a developer test artifact, not a store release. Hardware key redirection, individual Maliit preedit formatting spans, non-text Content Hub formats and native Wayland support are outside this implementation. Keep real-device evidence separate from unit-test results; see the sample's device-testing record.

## Implementation reference

The transport, codec and lifecycle design were adapted from UTDotNet-Bridge at `f7690f6108db05bf8e185e9f7ae533bda0f81462`, including its local Content Hub work dated 23 November 2026.
