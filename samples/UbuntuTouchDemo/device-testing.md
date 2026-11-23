# Ubuntu Touch integration: device validation

Recorded on 23 November 2026. This example references the Avalonia source projects. It has no UTDotNet assembly, package, adapter or per-control input registration. The original X11 results are preserved below; the native Wayland extension has a separate validation record. The current Click launcher explicitly selects `UseWayland()` with `--wayland` and unsets `DISPLAY`.

## Original X11 reproducible inputs

| Item | Value |
| --- | --- |
| Avalonia base | `51dae7305e4e5de4e906cad8f80453605a171d2a`, branch `ubuntu-touch/native-integration`, plus this working-tree integration |
| Existing D-Bus submodule checkout | `e793254ddaaa51af327a5840d7738de2d18eb865`; this pre-existing change was retained |
| Reference implementation | UTDotNet-Bridge `f7690f6108db05bf8e185e9f7ae533bda0f81462` plus its local Content Hub work |
| Device | Fairphone 5, ARM64, Ubuntu Touch 24.04-2.2 build 3, Lomiri |
| Backend | X11 through XWayland; automatic Ubuntu Touch detection |
| Scaling | `GRID_UNIT_PX=24`, observed Avalonia scale 3 |
| Runtime | Self-contained .NET 10 ARM64; Avalonia assemblies `12.2.999`, Tmds.DBus.Protocol `0.94.1` |
| Installed test package | `avalonia.inputdemo_0.1.4_arm64.click` |
| SHA-256 | `b7fa0923c5a01825ddadc1d7085553eaa8115856861549c932131944c112d6d1` |
| Confinement | `/proc/<pid>/attr/current`: `avalonia.inputdemo_inputdemo_0.1.4 (enforce)`; policy groups `[]` |

The reference working tree, including untracked files, was captured before integration. Local baseline archives are under `/tmp/ubuntu-touch-baseline`. The published `.deps.json` contains the source-built Avalonia components and no `UTDotNet` dependency.

## Automated validation

| Check | Result |
| --- | --- |
| Avalonia.X11, .NET 8 | Build passed, zero warnings/errors |
| ARM64 .NET 10 source publish | Passed; packaged and run on the phone |
| Avalonia.FreeDesktop.UnitTests | 49 passed, zero failures/skips on Linux |
| Avalonia.Controls.UnitTests | 4,301 passed, zero failures; one pre-existing CalendarDatePicker test skipped |
| Whitespace check | `git diff --check` passed |

The backend suite covers handshake buffering, fragmented reads, limits and disposal; native clipboard wire data and an isolated real D-Bus session; rejection/timeouts; conservative platform detection; input hints and sensitive state; Qt key conversion; synchronous callback state; stale clients; preedit/reset; and area intersection after resize/move. The native D-Bus test requires Linux and `dbus-daemon` and skips on other systems.

The TextBox regressions exercise replacement, empty deletion, cursor position, undo/redo, editing constraints, stale clipboard operations, focus restoration versus focus loss, password sensitivity, multiline options, pointer reset and composition before client detachment.

Before submission, the X11 .NET 8 build and Controls suite were repeated using D-Bus sources exported from the upstream-tracked submodule revision `cada3ecfc153a9efbc8d64f530136d737c1a7eec`, selected through `AvDBusSourcePath`. Both builds passed with zero warnings/errors; Controls again reported 4,301 passed and one existing skip. The pre-existing local submodule update is not part of this change.

## Device results

Only test strings were used. The Qt peer was the unconfined `NativeClipboardProbe.qml` launched by `qmlscene`, with native Qt text controls. Avalonia ran as the confined Click application; clipboard calls originated in that application with its actual identity. No clipboard-service impersonation or X11 clipboard fallback was used.

| Scenario | Observed result |
| --- | --- |
| Standard TextBox focus | Maliit opens; preedit is underlined separately from committed text |
| Change field during preedit | `ciao` is committed in the old field before switching to the numeric layout |
| Long press during preedit | The visible word becomes committed and selectable; the normal selection menu opens as an overlay |
| Numeric input | Numeric layout; entering `123` and Backspace leaves `12` |
| Password | Characters are masked; the prediction row is absent. Omission of surrounding text/offsets is verified in automated tests, not inferred from the screenshot |
| Email | Layout exposes `@` and domain keys |
| Multiline clipboard | Native Qt text `luna caffè 🌙` followed by `seconda riga` arrives intact in the normal Avalonia TextBox |
| Cut from context menu | Source word `ciao` is removed after the asynchronous clipboard write |
| Avalonia to Qt | A fresh, empty Qt control pastes exactly `ciao` from that Cut |
| Dynamic field | Adding a TextBox while Maliit is open focuses it, scrolls it into view and accepts input |
| Owned dialog | A standard TextBox in a newly opened dialog accepts Maliit preedit without registration |
| Hide and retap | Swiping down on Maliit closes it; tapping the already focused field opens it again |
| Background and foreground | A pending `ciao` in the dynamic field survives switching to Qt and returning to the same Avalonia process as committed text |
| InputPane | Open/Closed and logical overlap are reported. In the initial portrait viewport, global keyboard top 1672 px corresponds to logical Y 522.333 after the 105 px window offset, with height 342.667 at scale 3 |

Hardware testing found two issues and drove regressions: TextBox originally detached its input client before confirming composition on focus loss; the asynchronous clipboard guard originally treated focus restored by a closing menu as a reason to cancel Cut. Both were fixed and repeated successfully on the device. The sample also explicitly brings newly added fields into view after layout.

## Dialog layout update (0.1.3)

The sample's owned dialog now uses content-driven height, a centered starting position, a scrollable body and width/height limits with 16 logical pixels of clearance from the available bounds. Opening it with Maliit already visible centers it in the remaining area above the keyboard. Limits and centering are recalculated after owner/dialog resize or screen changes. This is sample layout behavior implemented through ordinary Window, Screens and InputPane APIs.

The updated ARM64 package was rebuilt, installed and tested on the same phone. The framework test results above are from the preceding integration run; this update changes the sample only.

Measured in X11 client pixel coordinates:

| Opening condition | Owner | Dialog | Dialog position |
| --- | --- | --- | --- |
| Keyboard initially closed | 1224 × 2595 | 1020 × 529 | (102, 1033) |
| Keyboard already visible | 1224 × 2595, available height 1567 | 1020 × 529 | (102, 519) |

These positions give equal opposing margins in each available area. The title, text field and Close button are fully visible; Maliit composition and closing the dialog were exercised. The final dialog was left open for inspection. An attempted external resize of the parent was overridden by Lomiri, so reduced viewport behavior and physical rotation are not claimed as device-validated here.

Evidence: `dialog-centered-start.png`, `dialog-centered-keyboard.png`, `dialog-centered-input.png`, `dialog-keyboard-geometry.json`, `dialog-final-geometry.log` and `dialog-build.log` in the local verification directory. These are window captures and geometry measurements, not full compositor captures.

## AXAML and English sample update (0.1.4)

The theme, main window, dialog, control styles and dynamic-field template are now declared in `App.axaml`, `InputWindow.axaml` and `InputDialog.axaml`. Code-behind handles input events, clipboard commands, template instantiation and adaptive window positioning. The sample interface and diagnostics use English. The native Qt test fixture now uses `Café 🌙` followed by `second line`; the earlier multilingual clipboard captures above retain their original test strings.

The dialog's Close button explicitly centers its content horizontally and vertically in AXAML. The package staging directory is recreated for each build so obsolete packaging files cannot survive into a new package.

The ARM64 AXAML build and Click packaging passed. On the Fairphone, the main view rendered in English, entering `123` and Backspace left `12`, the AXAML template produced a focused dynamic field, and `hello` was composed in both the dynamic field and the owned dialog. Close returned to the main window with the dynamic field's committed text preserved. The dialog retained its previous measured position and size. The backend test suite was not rerun for this UI refactor; its earlier results remain listed separately above.

Evidence in the verification directory: `axaml-main.png`, `axaml-dynamic.png`, `axaml-dialog-close.png`, `axaml-dialog-input.png`, `axaml-after-close.png`, `axaml-dialog-geometry.log`, `axaml-app.log`, `axaml-build.log` and `axaml-package-files.txt`.

## Native Wayland extension

The extension starts from X11 integration commit `31272dfdfbd16a4fb777cd45d526be2491e8c977` on branch `ubuntu-touch/wayland-integration`. It uses the existing Avalonia.Wayland backend and shares Maliit editing/transport and Content Hub with X11. The pre-existing D-Bus submodule checkout remains unchanged and excluded from this work.

The same Fairphone exposes `/run/user/32011/wayland-0`, `xdg_wm_base` v1, `wl_compositor` v4, `wl_seat` v6 and `wl_output` v3. No text-input manager was advertised. The Click process runs with `--wayland`, `WAYLAND_DISPLAY=wayland-0`, no `DISPLAY`, its actual `APP_ID` and the existing enforced AppArmor profile with no extra policy groups. This is a native Wayland connection.

Initial hardware testing found that Lomiri reports output scale 1 while `GRID_UNIT_PX=24` requires render scale 3. The corrected backend distinguishes compositor coordinates from Avalonia logical coordinates for configuration, rendering, size constraints and input. The fullscreen sample now occupies 1224 × 2700 pixels, or 408 × 900 Avalonia units. A measured keyboard top of 1635 pixels yields `InputPane` overlap `(0, 545, 408, 355)`.

Automated validation: FreeDesktop **51 passed**, Wayland **51 passed**, no failures or skips. The new shared tests cover a Maliit context without an X11 window ID, visibility without fabricated global geometry, and the transition to/from a known window position. The Wayland identity test verifies Click identity selection, explicit application-ID overrides (including null), and unchanged desktop defaults. Wayland and X11 .NET 8 builds passed with zero warnings/errors; the ARM64 .NET 10 publish includes both backends. The earlier Controls results above were not rerun because this extension does not change Controls.

| Native Wayland scenario | Observed result |
| --- | --- |
| Text input | `hello` entered through Maliit and committed on field change |
| Numeric input and key forwarding | Entering `123` and Backspace leaves `12` |
| Qt to Avalonia | `Café 🌙` followed by `second line` arrives intact in the multiline field |
| Avalonia to Qt | Cut removes selected `hello`; a fresh, empty native Qt editor pastes exactly `hello` |
| Dynamic field | A field created while the keyboard is open accepts text and scrolls above the measured overlap |
| Dialog | AXAML content is centered within the available viewport; input and the centered Close button work |
| Return from dialog | The form and focused field are restored within the same fullscreen surface |
| Application switching | Returning from the Qt probe reactivates Maliit in the same Avalonia process |

An intermediate build also accepted text in a separate native owned window. Lomiri placed a windowed dialog partly outside the phone. Making that child fullscreen exposed a second compositor behavior: after closing it, the owner retained fullscreen state but received 1224 × 2595 rather than full-display bounds. The backend correctly withheld global keyboard coordinates in that state. The phone sample therefore presents its dialog within the same fullscreen surface, as an AXAML `UserControl`, and hides the form until Close restores it. It uses ordinary focus and `IInputPane` events, with no per-control IME registration. Native toplevel placement remains controlled by the compositor.

The keyboard was confirmed visible on the physical phone. Current `mirscreencast` captures show application content but omit the keyboard layer; a blank lower area in these captures is not evidence that the keyboard is absent. Input and clipboard checks used only test strings. The native Qt fixture was the English `NativeClipboardProbe.qml`, with a local capture-only wrapper using `grabToImage`.

Evidence under the ignored verification directory includes `wayland-scaled.png`, `wayland-input.png`, `wayland-number.png`, `wayland-qt-to-avalonia.png`, `wayland-avalonia-to-qt.png`, `wayland-dynamic-typed.png`, `wayland-overlay-input.png` and `wayland-overlay-return.png`. Earlier captures document intermediate builds, including the native-window layout issue.

Final installed package: `avalonia.inputdemo_0.2.6_arm64.click`, SHA-256 `bd0a49a43bc7ac36fda264d16dfad35775e6151cd632011bf9dced9268102355`. Its process profile was `avalonia.inputdemo_inputdemo_0.2.6 (enforce)`. The published dependency manifest contains no UTDotNet dependency. In this exact build, opening Dialog automatically focused its text field and opened Maliit; entering `hello`, closing, entering `123` followed by Backspace in Number, and adding a dynamic field all succeeded. The dialog and dynamic field stayed inside the area above the keyboard. Captures: `wayland-final-dialog.png`, `wayland-final-dialog-typed.png`, `wayland-final-number.png` and `wayland-final-dynamic.png`; process diagnostics: `wayland-final-device.log`.

## Repeat the checks

From the repository root:

```sh
dotnet build src/Avalonia.X11/Avalonia.X11.csproj -f net8.0
dotnet build src/Avalonia.Wayland/Avalonia.Wayland.csproj -f net8.0
dotnet test --project tests/Avalonia.FreeDesktop.UnitTests/Avalonia.FreeDesktop.UnitTests.csproj
dotnet test --project tests/Avalonia.Wayland.UnitTests/Avalonia.Wayland.UnitTests.csproj
dotnet test --project tests/Avalonia.Controls.UnitTests/Avalonia.Controls.UnitTests.csproj
samples/UbuntuTouchDemo/build-click.sh
```

Packaging uses Docker and the installed `clickable/amd64-ut24.04-2.x-arm64:latest` image. `CLICK_BUILD_IMAGE` can select another compatible image. `desktop-file-validate` currently reports a nonfatal warning for the relative Click icon filename. Store review is not part of this build.

Install and launch the resulting package on a connected Ubuntu Touch device:

```sh
adb shell mkdir -p /home/phablet/avalonia-input-verification
adb push artifacts/ubuntu-touch/avalonia.inputdemo_0.2.6_arm64.click /home/phablet/avalonia-input-verification/
adb shell 'gdbus call --system --dest com.lomiri.click --object-path /com/lomiri/click --method com.lomiri.click.Install /home/phablet/avalonia-input-verification/avalonia.inputdemo_0.2.6_arm64.click'
adb shell 'XDG_RUNTIME_DIR=/run/user/32011 DBUS_SESSION_BUS_ADDRESS=unix:path=/run/user/32011/bus lomiri-app-launch avalonia.inputdemo_inputdemo_0.2.6'
```

The runtime directory above belongs to the tested device; use the target user's actual runtime directory elsewhere. Stop any earlier test instance before replacing its package. The launcher occasionally reports a registry disconnect even when the application starts; check the application process and UI independently.

Copy `NativeClipboardProbe.qml` to the same device directory and run it with the user's runtime/session bus, `QT_QPA_PLATFORM=ubuntumirclient` and `QT_IM_MODULE=maliit`. Use **Test text**, **Copy**, and **Paste** to compare against the standard Avalonia controls. Start a fresh native control before the reverse-direction test to rule out leftover editor content.

`device-tap.py` can inject one bounded gesture on an unlocked test device. First identify the correct touchscreen from `/proc/bus/input/devices` and verify its coordinate range. On this phone it was `/dev/input/event4`; no device permissions were changed. The helper refuses to overlap an existing touch.

## Evidence and limits

Local, ignored evidence is under `artifacts/ubuntu-touch/verification/`. In particular:

- `focus-commit-0.1.1.png`, `password-0.1.1.png`, `email-0.1.1.png`, `dialog-input-0.1.1.png`, `keyboard-dismissed-0.1.1.png` and `retap-0.1.1.png` are compositor captures from the intermediate build.
- `final-cut-window.png`, `final-dynamic-window.png`, `final-resumed-window.png` and `final-qt-to-avalonia.png` capture the final Avalonia X11 client surface.
- `final-avalonia-to-qt.png` captures the new Qt control after Paste.
- `final-device.log`, build/test logs and the capture helpers document the final run without recording clipboard or password contents in application logs.

After repeated captures, `mirscreencast` stopped providing a buffer. Final screenshots therefore use X11 `XGetImage` for the Avalonia window and QML `grabToImage` for the Qt test control. These captures show each application's content, not the compositor or keyboard. Earlier compositor captures establish the actual keyboard layouts. This capture limitation was not treated as an input or clipboard failure.

Portrait, application background/foreground and multiple windows were exercised. Physical rotation, screen-off/deep suspend, external hardware keyboards, a real IBus/Fcitx session, accessibility and GPU acceleration were not validated in this run. Geometry resize/move behavior is covered by automated tests. The device rendered using the existing fallback after GLX initialization failed; no graphics-backend changes were made.

Non-text clipboard formats and individual Maliit preedit formatting spans remain unsupported. On native Wayland, ordinary window positions are unavailable, so keyboard visibility may be reported with an empty overlap rectangle. Fullscreen overlap was validated only on the single origin output described above. Rotation, external displays, physical keyboards, deep suspend and GPU acceleration remain unverified for the Wayland extension. See [backend notes](../../src/Avalonia.FreeDesktop/UbuntuTouch/README.md) for supported options and contracts.
