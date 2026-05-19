# Unicode Trie & Codepoint Coverage Plan

This document plans unit test and benchmark coverage for the Unicode property
lookup path in `Avalonia.Base`. It is a planning artifact only — the
implementation will land as a follow-up PR (or PRs).

## Scope

`Avalonia.Media.TextFormatting.Unicode.UnicodeData` is the public entry point
for resolving Unicode properties (general category, script, BiDi class, line
break class, word break class, grapheme break class, East Asian width, paired
bracket). Every property goes through the same path:

```
char[] / ReadOnlySpan<char>
    └─ Codepoint.ReadAt(text, index, out count)       (surrogate decoding)
         └─ new Codepoint(uint value)
              └─ Codepoint.<Property>                  (Category, Script, BiDiClass, …)
                   └─ UnicodeData.Get<Property>(uint)
                        └─ <Name>Trie.Trie.Get(uint)   (4-branch trie walk)
                             └─ Unsafe.Add(ref dataBase, index)
```

Two of the three layers — `UnicodeTrie.Get` and the `UnicodeData.Get*`
wrappers — have **no direct test coverage today**. `Codepoint.ReadAt` is only
exercised indirectly through the layout pipeline. This plan adds direct unit
tests for each layer and adds benchmarks that isolate the trie cost from
shaping/layout noise.

## Current coverage audit

### What is exercised today

- BiDi algorithm, grapheme / word / line break enumerators — covered by
  `[Theory(Skip = ...)]` conformance tests against the Unicode UCD test files.
  These do not run in CI; they require unskipping by hand when Unicode is
  bumped.
- `Utf16Utils.CharacterOffsetToStringOffset` — direct unit tests exist.
- End-to-end through `TextLayout` — covered by `HugeTextLayout` benchmark
  (`tests/Avalonia.Benchmarks/Text/HugeTextLayout.cs`) and integration tests.
  Layout-level coverage hides trie-level regressions: a 30 % slowdown in
  `UnicodeData.GetBiDiClass` is invisible behind shaping cost.

### What is not covered

- `UnicodeTrie.Get` — none of its four code paths (BMP non-surrogate,
  surrogate range via LSCP index, supplementary code points below `HighStart`,
  code points at/above `HighStart`, out-of-range error value) has a direct
  test.
- `UnicodeData.Get*` — all ten static helpers are untested in isolation.
- `Codepoint.ReadAt` — surrogate-pair edge cases (lone high / lone low /
  high at end / low at index 0 / index past length / empty span) are not
  asserted at this entry point.
- `Codepoint.<Property>` accessors — no spot-check tests against well-known
  codepoints. A subtle packing bug (wrong shift, wrong mask) would silently
  return wrong values without any test failing.
- `Codepoint.IsWhiteSpace` — relies on `(1UL << (int)GeneralCategory)`,
  which silently breaks if `GeneralCategory.Control` / `Format` /
  `SpaceSeparator` ever move past int 63. No regression test guards this.
- `Codepoint.IsBreakChar`, `Codepoint.GetCanonicalType`,
  `Codepoint.TryGetPairedBracket` — untested.

### Latent correctness risks that motivate the new tests

1. **`HighStart` correctness.** The trie's `HighStart` is baked into the
   generated `.trie.cs` files via `new(Data, 0x{HighStart:X8}, …)`. If a future
   generator change emits a wrong `HighStart`, code points past it silently
   fold to the last data block — no exception, no warning. The generator's
   self-verification only round-trips codepoints that had explicit
   assignments, so the `>= HighStart` branch is effectively untested.
2. **Bit-packing layout drift.** `UnicodeData.<X>_SHIFT` /
   `<X>_MASK` constants are consumed by both the generator (packs values in)
   and `UnicodeData.Get*` (reads them out). A `*_BITS` change without a
   matching trie regen creates silent corruption rather than a compile error.
3. **`initialValue` semantics for default classes.** The BiDi and
   GraphemeBreak builders rely on `initialValue = 0` matching seeded
   `LeftToRight` / `Other` at enum position 0. The ABI validator in
   `UnicodeEnumsGenerator.cs` catches position shifts, but no test directly
   asserts that *unassigned* code points (e.g. an arbitrary point in an
   unassigned BMP range) resolve to those defaults at runtime.
4. **Surrogate-range lookups.** Code points in `0xD800..0xDFFF` are valid
   trie keys served by a separate LSCP index region. We have no test that
   confirms e.g. `new Codepoint(0xD800).GeneralCategory == Surrogate`.
5. **Per-property allocation behavior.** Each
   `Codepoint.<Property>` access constructs a new `UnicodeTrie` struct and
   wraps the static data in a fresh `ReadOnlySpan<uint>`. Aggressive inlining
   should fold these away in Release, but no benchmark observes this — a
   future refactor that hides the property body behind a non-inlined call
   would regress every layout pass.

## Plan: unit tests

All new test files live under
`tests/Avalonia.Base.UnitTests/Media/TextFormatting/`. They run on every CI
build (no `[Skip]`). They consume `Avalonia.Media.TextFormatting.Unicode`
internals via the existing `InternalsVisibleTo` declaration.

### `UnicodeDataTests.cs` (new)

Lock the trie data against drift by pinning property values for representative
well-known codepoints. Each `[Theory]` rows out roughly 6–30 codepoints,
covering ASCII letters / digits / whitespace / punctuation, common scripts
(Latin, Cyrillic, Han, Hebrew, Arabic), supplementary-plane emoji, the
surrogate range, the private-use area, and the upper edge of the assigned
range.

- `GeneralCategory_KnownCodepoints`
- `Script_KnownCodepoints`
- `BiDiClass_KnownCodepoints`
- `BiDiPairedBracket_RoundTrip` — also covers `GetBiDiPairedBracketType` and
  `Codepoint.TryGetPairedBracket`.
- `LineBreakClass_KnownCodepoints`
- `WordBreakClass_KnownCodepoints`
- `GraphemeBreakClass_KnownCodepoints` — including the
  `ExtendedPictographic` path for a known emoji.
- `EastAsianWidth_KnownCodepoints`
- `UnassignedCodepoints_FallBackToDefaults` — regression test for risk
  #3 above. Picks code points in known-unassigned BMP ranges (e.g. `0x0378`)
  and asserts the default class for each enum (`LineBreakClass.Unknown`,
  `WordBreakClass.Other`, `BidiClass.LeftToRight`, `GraphemeBreakClass.Other`).

### `CodepointTests.cs` (new)

- `ReadAt_*` — exhaustive surrogate-pair edge cases at the `Codepoint.ReadAt`
  entry point:
  - BMP scalar at start / middle / end (`count == 1`).
  - High + low surrogate pair → `count == 2`, value `>= 0x10000`.
  - High surrogate at end of string → `ReplacementCodepoint`.
  - Lone low surrogate at index 0 → `ReplacementCodepoint`.
  - Low surrogate following a non-high char → `ReplacementCodepoint`.
  - Index past length → `ReplacementCodepoint`, `count == 1`.
  - Empty span at index 0 → `ReplacementCodepoint`.
- `CodepointEnumerator_DecodesMixedText` — iterate a string containing BMP +
  supplementary code points and assert the enumerated sequence matches the
  expected codepoint values.
- `IsWhiteSpace_AllUsedCategoriesFitInBitmask` — assert
  `(int)GeneralCategory.Control < 64`, same for `Format` and `SpaceSeparator`.
  Guards risk #4 above. Add spot-check rows for `' '`, `'\t'`, `' '`,
  `'a'`, `'0'`.
- `IsBreakChar_KnownCodepoints` — every entry in the switch (`
`,
  ``, ``, ``, ``, ` `, ` `) and a couple of
  negatives.
- `GetCanonicalType_HandlesAngleBrackets` — `0x3008 → 0x2329`, `0x3009 →
  0x232A`, anything else passes through unchanged.
- `Codepoint_ImplicitConversions` — `(int)` and `(uint)`.

### `UnicodeTrieTests.cs` (new, uses internals)

Cover all four branches of `UnicodeTrie.Get`. Tests use the production
`UnicodeDataTrie.Trie` for realistic coverage and a small synthetic trie
built via `UnicodeTrieBuilder` for the error-value path.

- `Get_BmpAscii` — sample ~32 evenly-spaced BMP codepoints in
  `0x20..0xFFFF` and assert the unpacked properties match what
  `UnicodeData.Get*` returns. This is a round-trip test on shifts/masks
  (guards risk #2).
- `Get_SurrogateRange` — sample codepoints in `0xD800..0xDFFF`; assert
  `GetGeneralCategory(cp) == Surrogate` for each.
- `Get_Supplementary_BelowHighStart` — sample codepoints in
  `0x10000..(HighStart - 1)` (math symbols, emoji block) and round-trip
  packed values.
- `Get_AtAndAboveHighStart` — for the boundary code points (`HighStart`,
  `HighStart + 0x100`, `0x10FFFE`, `0x10FFFF`), assert the trie returns the
  same fallback value for all of them. Guards risk #1.
- `Get_OutOfRange_ReturnsErrorValue` — build a tiny `UnicodeTrieBuilder` with
  a known `errorValue`, freeze, call `Get(0x110000)`, assert
  `errorValue`. This is the only feasible test of the error path because the
  production tries don't expose it through `UnicodeData.Get*`.
- `Builder_RoundTripsAssignedValues` — build a small trie, set a handful of
  codepoints to specific values, freeze, and assert `Get` returns those
  exact values; assert unassigned codepoints return the builder's
  `initialValue`. This is the only direct test of `UnicodeTrieBuilder` /
  `Freeze` round-tripping in isolation.

### `PropertyValueAliasHelperTests.cs` (new)

Round-trip `Get<Enum>(tag)` and `GetTag(<enum>)` for a couple of values per
generated enum (Script, GeneralCategory, LineBreakClass, WordBreakClass,
BidiClass, BiDiPairedBracketType). Lightweight regression net for the
generated alias helper.

## Plan: benchmarks

All new benchmarks live in `tests/Avalonia.Benchmarks/Text/`, next to the
existing `HugeTextLayout`. Every benchmark class uses `[MemoryDiagnoser]` so
allocations on what should be a zero-alloc path surface as regressions.

### `UnicodeTrieBenchmark.cs` (new)

Goal: isolate raw trie lookup cost from any string decoding.

- Setup: three pre-built `uint[]` of 4 096 codepoints each — ASCII (`< 0x80`),
  BMP (`< 0xFFFF`), supplementary (`0x10000..0x10FFFF`). Generated once with a
  fixed seed so numbers are reproducible.
- Benchmarks (one per trie × three distributions):
  - `Get_Ascii_UnicodeData` / `Get_Bmp_UnicodeData` / `Get_Supplementary_UnicodeData`
  - Same for `BiDiTrie`, `GraphemeBreakTrie`, `EastAsianWidthTrie`.
- `Get_Mixed_AllTriesPerCodepoint` — for each codepoint in the BMP fixture,
  call `Get` on all four tries. Represents the "what a layout pass would
  see" cost and establishes a baseline for any future "merge packed
  properties into one read" experiment.

### `CodepointBenchmark.cs` (new)

Goal: measure the full path from `string` / `ReadOnlySpan<char>` to specific
properties, including surrogate decoding and the per-property
`UnicodeTrie` construction.

- Setup: three strings of equal codepoint count — ASCII-only, BMP-mixed
  (Latin + CJK), supplementary-heavy (emoji + math).
- Benchmarks:
  - `ReadAt_Sequence` — drive `Codepoint.ReadAt` across the whole string in a
    `for` loop, summing codepoint values into a sink.
  - `CodepointEnumerator_Sequence` — same content via `MoveNext`.
  - `Sequence_ReadGeneralCategory` / `_ReadScript` / `_ReadBiDiClass` — read
    one property per codepoint across the whole string.
  - `Sequence_ReadAllProperties` — read Category + Script + BiDi + LB + WB +
    GCB + EAW per codepoint. Worst-case representative of a layout pipeline
    pass; this is the number to watch when discussing batched-lookup
    experiments.
  - `TryGetPairedBracket_Sequence` — useful regression target because it
    exercises the bracket-mask path of `BiDiTrie`.

### `UnicodeBreakEnumeratorBenchmark.cs` (optional)

Direct benchmarks for `LineBreakEnumerator`, `WordBreakEnumerator`, and
`GraphemeEnumerator` over the same three fixture strings. Currently these
are exercised only inside `TextLayout`, so a regression in their inner loops
is hard to attribute. Cheap to add; defer if the trie/codepoint benchmarks
already provide enough signal.

## Implementation order

1. **Land the new unit-test files first.** They are fast, run in CI on every
   build, and lock in current behavior so subsequent refactors / benchmark
   changes cannot drift correctness silently.
2. **Add the trie and codepoint benchmarks.** Run them once after landing to
   capture baseline numbers in the PR description; from then on they exist
   as a regression detector.
3. **(Optional follow-up.)** Use the benchmarks to evaluate whether exposing
   batched lookups (`UnicodeData.GetUnicodeData(uint cp, out GeneralCategory,
   out Script, out LineBreakClass, out WordBreakClass)` returning all four
   from a single `UnicodeDataTrie.Trie.Get` call) is worth it. The benchmarks
   in step 2 are the evidence base for or against this change. No need to
   commit to it in the same PR.

## Non-goals

- Replacing the existing UCD conformance theories. Those remain the
  authoritative algorithmic checks; the new tests are spot-checks and
  unit-level guards, not algorithm conformance.
- Re-running UCD downloads from the test project. The conformance tests
  already do this; the new tests are pure in-memory.
- Reorganising or merging the trie classes. Any structural changes are
  deferred to a follow-up PR once the benchmarks provide a baseline.
