# Contributing to Avalonia

PRs are always welcomed from everyone. Following this guide will help us get your PR reviewed and merged as quickly as possible.

For this guide we're going to split PRs into two types: bug fixes and features; the requirements for each are slightly different.

## Bug Fixes

A bug fix will ideally be accompanied by tests. There are a few types of tests:

- Unit tests are for issues that aren't related to platform features. These tests are located in the `tests` directly, categorised by the assembly which they're testing.
- Integration tests are for issues that are related to platform features (for example fixing a bug with Window resizing). These tests are located in the `tests/Avalonia.IntegrationTests.Appium` directory. Integration tests should be run on Windows and macOS. See the readme in that directory for more information
- Render tests are for issues with rendering. These tests are located in `tests/Avalonia.RenderTests` with separate project files for Skia and Direct2D. The Direct2D backend is currently mostly unmaintained so render tests that just run on Skia are acceptable

It's not always feasible to accompany a bug fix with a test, but doing so will speed up the review process.

The commits in a bug fix PR **should follow this pattern**:

- A commit with a failing unit test; followed by
- A commit that fixes the issues

In this way the reviewer can check out the commit with the failing test and confirm the problem. One this is confirmed, they can confirm the fix.

## Features

**Features should be discussed with the core team before opening a PR.** Please open an issue to discuss the feature before starting work, to ensure that the core team are onboard.

Features should always include unit tests or integration tests where possible.

> One exception to this is features related to DevTools which has no tests currently

Features that introduce new controls should consider the following:

- Ideally the control should be exposed to the operating system's automation/accessibility APIs by writing an `AutomationPeer`
- If the control introduces any functionality which is difficult to unit test, an integration test should be written

## General Guidance

## PR description

- The PR template contains sections to fill in. These are discretionary and are intended to provide guidance rather than being prescritive: feel free to delete sections that do not apply, or add additional sections
- **Please** provide a good description of the PR. Not doing so **will** delay review of the PR at a minimum, or may cause it to be closed. If English isn't your first language, consider using ChatGPT or another tool to write the description. If you're looking for a good example of a PR description see https://github.com/AvaloniaUI/Avalonia/pull/12765 for example.
- Link any fixed issues with a `Fixes #1234` comment

## Breaking changes

- During a major release cycle, source or binary breaking changes may not be introduced to the codebase: this is checked by an automated tool and will cause CI to fail
- If something needs addressing in the next major release, you can leave a `TODOXX:` comment, where `XX` is the version number of the next major release, e.g. `TODO12:`
- Carefully consider behavioral breaking changes and point them out in the PR description

### Commits

In addition to the guidance in the `Bug Fixes` section, following these guidelines may help to get your PR reviewed in a timely manner:

- Rebase your changes to remove extraneous commits. Ideally the commit history should tell a clean story of how the PR was implemented (even though the process was probably not clean!)
- Provide meaningful commit comments
- **Do not** change code unrelated to the bug fix/feature
- **Do not** introduce spurious formatting or whitespace changes

While it's tempting to fix style issues you encounter, don't do it:

- It causes the reviewer to get distracted by unrelated changes
- It makes finding the cause of any later issue more difficult (blame/bisect is made more difficult)
- As the code churns, style issues will be resolved anyway

Separate PRs for style issues may be accepted if agreed with the core team in advance.

### Style

- The codebase uses [.net core](https://github.com/dotnet/runtime/blob/master/docs/coding-guidelines/coding-style.md) coding style.
- Try to keep lines of code around 120 characters in length or less, though this is not a hard limit. If you're a few characters over then don't worry too much.
- Public methods should have XML documentation
- Prefer terseness to verbosity but don't try to be too clever.
- **DO NOT USE #REGIONS** full stop

Tests do not follow the usual method naming convention. Instead they should be named in a sentence style, separated by underscores, that describes in English what the test is testing, e.g.

```csharp
    void Calling_Foo_Should_Increment_Bar()
```

Render tests should describe what the produced image is:

```csharp
    void Rectangle_2px_Stroke_Filled()
```

## Code of Conduct

This project has adopted the code of conduct defined by the Contributor Covenant to clarify expected behavior in our community.
For more information see the [Contributor Covenant Code of Conduct](https://dotnetfoundation.org/code-of-conduct)