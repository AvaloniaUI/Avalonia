# API Compatibility

Avalonia maintains strict **source and binary compatibility** within major versions. Automated API compatibility checks run on every CI build to enforce this policy—builds will fail if breaking changes are detected.

## When Breaking Changes Are Permitted

Breaking changes are only acceptable under these specific circumstances and **must be approved** by the Avalonia code team:

- **Major version targeting**: When `master` branch is targeting a new major version
- **Accidental public APIs**: When code was unintentionally exposed as a public API and is unlikely to have external dependencies
- **Experimental features**: When the API is explicitly marked as unstable or experimental

## Handling Approved Breaking Changes

When a breaking change is approved, you can bypass CI validation using an API suppression file:

1. **Generate suppression file**: Run `nuke --update-api-suppression true`
2. **Commit changes**: Commit the [updated suppression file](../api/) in a separate commit

> **Note**: The suppression file should only be updated after the breaking change has been reviewed and approved.

## Baseline Version Configuration

API changes are validated against a **baseline version**—the reference point for compatibility checks.

- **Default behavior**: Uses the current major version (e.g., for version 11.0.5, baseline is 11.0.0)
- **Custom baseline**: Override using the [`api-baseline`](https://github.com/AvaloniaUI/Avalonia/blob/56d94d64b9aa6f16200be39b3bcb17f03325b7f9/nukebuild/BuildParameters.cs#L27) parameter with `nuke`
- **Fallback**: When not specified, uses the version defined in [`SharedVersion.props`](https://github.com/AvaloniaUI/Avalonia/blob/56d94d64b9aa6f16200be39b3bcb17f03325b7f9/build/SharedVersion.props#L6)

## Additional Resources

- [API Validation Tool Implementation](https://github.com/AvaloniaUI/Avalonia/pull/12072) - Pull request that introduced this feature
