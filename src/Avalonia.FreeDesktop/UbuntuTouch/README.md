# Ubuntu Touch on X11 and Wayland

Ubuntu Touch integration is internal to Avalonia.FreeDesktop, Avalonia.X11 and Avalonia.Wayland. Applications use the normal `AppBuilder`, `TextBox`, `TextInputOptions`, `TopLevel.Clipboard` and `TopLevel.InputPane`. No control registration, Qt library, external input adapter or UTDotNet package is required. Both windowing backends share the Maliit transport and editing lifecycle. The standalone example is `samples/UbuntuTouchDemo`.

The sample defines its application theme, main view, dialog and dynamic-field template in AXAML. Code-behind handles input, clipboard commands, dynamic template creation and positioning within the available screen area. Sample UI strings and diagnostics are in English.

## Selection and overrides

Both backends enable Ubuntu Touch defaults for an explicit Touch distribution ID/variant, or a UBports system image corroborated by the Ubuntu distribution or Lomiri session. Lomiri, Maliit, ARM64 and a Wayland display variable alone are insufficient. Select native Wayland with `UseWayland()` and a reference to Avalonia.Wayland; `UsePlatformDetect()` retains its existing backend selection. The Click sample explicitly selects Wayland with `--wayland` and unsets `DISPLAY`.

- `X11PlatformOptions.UseUbuntuTouch = true/false` or `WaylandPlatformOptions.UseUbuntuTouch = true/false` overrides detection for the corresponding backend.
- `AVALONIA_UBUNTU_TOUCH=1/0` overrides automatic detection when that option is unset.
- `AVALONIA_IM_MODULE=maliit` selects just Maliit; `AVALONIA_IM_MODULE=none` disables text input integration. Existing X11 IBus/Fcitx selections and `EnableIme=false` remain available. Wayland otherwise retains its text-input-v3 integration.
- Ubuntu Touch uses Content Hub for the regular clipboard, overlay popups, and an absolute render scale of `GRID_UNIT_PX / 8` for valid grid sizes 8–48. Explicit X11 scale settings take precedence. X11 primary selection remains independent.
- Wayland derives the default `xdg_toplevel` application ID from Click's `APP_ID`. An explicitly assigned `WaylandPlatformOptions.AppId` takes precedence, including null. Outside Ubuntu Touch, the entry assembly name remains the default.

Applications retain responsibility for their Click identity, responsive layout and theme. The example launcher sets `RESOURCE_NAME` from `APP_ID` so deriving WM_CLASS does not require reading `/proc` under confinement. Its AppArmor policy requests no extra policy groups.

Lomiri on the tested phone exposes `xdg_wm_base` v1 and no text-input-v3 manager. Ubuntu Touch mode accepts v1 because overlay popups do not require `xdg_popup.reposition` (v3). Other Wayland sessions retain their v3 requirement and native popups. The phone reports compositor scale 1 despite using grid scale 3. Wayland therefore keeps the compositor scale and Avalonia UI scale distinct: buffers, configure sizes, constraints, pointer/touch and drag positions use the appropriate coordinate space. Fullscreen content is 408 × 900 logical units on the 1224 × 2700 display.

## Input and editing

Each focus context has a private D-Bus discovery connection and a separate local Unix peer connection. Outgoing calls are serialized and bounded to four seconds. Timeout/cancellation disposes only the affected transport. Initialization callbacks are buffered until the peer handler is installed (at most 32 frames and 64 KiB), retaining the workaround required by Tmds.DBus.Protocol 0.94.1.

Callbacks run on the UI thread before acknowledgement. Generic clients can override `TextInputMethodClient.CommitText` to handle relative replacement, empty deletion and explicit cursor positioning synchronously. `TextBox` implements this through its normal input/editing path, including undo, read-only mode, MaxLength and multiline rules. Older custom clients retain ordinary string commits through the input event route; they must implement the new method for replacement operations.

Composition is finalized before pointer selection, focus loss, reset and server hide. An external text replacement invalidates the old composition. Stale callbacks cannot edit a newly focused client. Qt key codes are converted to XKB keysyms and delivered through the selected backend's normal input route; signal-only requests do not edit. Password and PIN state omits surrounding text and offsets; `PasswordChar` also marks the client sensitive even without explicit input options.

`IInputPane.OccludedRect` is the intersection of the reported global pixel rectangle and the client viewport, converted to logical units. Position, size and scale changes recompute it. An already resized window has no overlap, so applications should reserve only the reported overlap rather than subtracting the full keyboard height again. A successful show request alone does not prove that the keyboard is visible.

Wayland does not expose ordinary toplevel global positions. Such windows report Maliit's visibility with an empty occlusion rectangle and omit global cursor geometry and the X11 window ID from Maliit state. A fullscreen window covering the sole entered output at origin (0, 0) can report a measured local overlap. No global coordinates are invented for windowed surfaces. The phone sample uses a fullscreen window and an AXAML dialog within that surface, centered above the keyboard; positioning an ordinary native dialog remains the compositor's responsibility.

## Clipboard

Content Hub is called from the application process using `APP_ID` and an empty surface ID, allowing the service's normal caller-PID focus check. There is no X11 cache fallback. Only UTF-8 plain text is supported; malformed, oversized and non-text payloads are rejected. Clear writes an empty plain-text paste. A denied write throws an expected clipboard exception so Cut retains source text. A timed-out operation closes its own connection. TextBox invalidates pending edits when its text, selection, focus, window activation or attachment changes.

The separate X11 SAVE_TARGETS fix bounds best-effort clipboard-manager persistence to two seconds after ownership has succeeded. It does not replace Content Hub interoperability.

## Build and test

```sh
dotnet build src/Avalonia.X11/Avalonia.X11.csproj -f net8.0
dotnet build src/Avalonia.X11/Avalonia.X11.csproj -f net10.0
dotnet build src/Avalonia.Wayland/Avalonia.Wayland.csproj -f net8.0
dotnet build src/Avalonia.Wayland/Avalonia.Wayland.csproj -f net10.0
dotnet test --project tests/Avalonia.FreeDesktop.UnitTests/Avalonia.FreeDesktop.UnitTests.csproj
dotnet test --project tests/Avalonia.Wayland.UnitTests/Avalonia.Wayland.UnitTests.csproj
dotnet test --project tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj
samples/UbuntuTouchDemo/build-click.sh
```

The native clipboard protocol test starts an isolated `dbus-daemon` and verifies signatures, caller identity, Unicode, rejection and timeout behavior. Backend tests cover stale contexts, reset, preedit, callbacks, key conversion and coordinate conversion. Handshake tests cover partial reads, ordering, limits and disposal. TextBox tests cover replacement/undo and asynchronous clipboard races.

The sample Click package is a developer test artifact, not a store release. Hardware key redirection, individual Maliit preedit formatting spans and non-text Content Hub formats are outside this implementation. Keep real-device evidence separate from unit-test results; see the sample's device-testing record.

## Implementation reference

The transport, codec and lifecycle design were adapted from UTDotNet-Bridge at `f7690f6108db05bf8e185e9f7ae533bda0f81462`, including its local Content Hub work dated 23 November 2026.
