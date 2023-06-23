## GTK
On linux window resize is *async*. For now we have some workarounds for that, but autosize is still wonky

## OSX

- OS X has a concept of "live resize" (animated window resize). Application is supposed to draw something of low quality if it can't keep proper FPS
- OS X has application-modal and window-modal dialogs. We currently support only application modal ones
- OS X has a complex protocol for interacting with text input engine, we need to somehow support it. See https://developer.apple.com/reference/appkit/nstextinputcontext and
https://developer.apple.com/library/content/documentation/TextFonts/Conceptual/CocoaTextArchitecture/TextEditing/TextEditing.html
- OSX uses `Command` key (`Win` key on regular keyboards) for most of keyboard shortcuts like copy/paste. We have them hardcoded to use `Ctrl`.
- You must not use `await` inside Main before Avalonia has started, as on OSX this will hijack the first thread in the program, and OSX assumes UIThread is the first thread. If an await statement occurs before Avalonia has started, then your application will not run on OSX.