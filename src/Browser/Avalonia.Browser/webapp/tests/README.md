# Browser text input regression checks

Run `node tests/text-input.test.cjs` from this directory's parent after restoring the webapp dependencies. The tests exercise the real TypeScript event handlers with managed interop and unrelated browser services stubbed. They do not simulate an actual IME.

## Actual IME verification

Use a standalone Browser app with a default `TextBox` configured with `AcceptsReturn = true`, `TextWrapping = Wrap`, and a narrow width (140 logical pixels). Inspect `Text` directly, not the hidden DOM input value. No custom TextBox or application input handlers are required.

Verified on Android 15/API 35 x86_64, Chrome 124.0.6367.219, Gboard 14.2.09 Korean two-set, .NET 10.0.402. Use the actual Gboard keys, not paste or `adb input text`.

- Type `가`, Space, `나`, Enter, `다`, Enter twice, `라`, Space. Expected: `가 나\n다\n\n라 `.
- Type `가나다라마바사아자차카타파하`, backspace six jamo, add ㄴ then ㅏ across the wrap, backspace, add ㅏ and ㅁ, Space, shift-ㄱ and ㅏ, Enter, `나`, Enter twice, `다`, Space. Expected: `가나다라마바사아자차카남 까\n나\n\n다 `.
- Move left twice, type `한`, backspace ㄴ and reinsert it, Space. Expected suffix: `\n\n한 다 `.
- Switch Gboard to English and insert `abc123` at that position. Expected suffix: `\n\n한 abc123다 `.
- Physical-key event regression: `a`, Space, `b`, Enter, `c` produces `a b\nc` in multiline and `a bc` in single-line mode. Read-only mode remains unchanged. These checks used browser key-event injection, not a physical keyboard.

All cases above passed on the candidate. Baseline main reproduced missing spaces/line breaks. Safari, Firefox, other IMEs, multithreaded WASM, native desktop platforms and broader clipboard/autocorrect behavior are not covered by these results. This preserves the legacy input bridge; it does not implement the EditContext work in PR #21347.
