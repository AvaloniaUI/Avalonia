
When running code review, please refer to the following documents if they are relevant to the changed parts of the codebase:
- `native/Avalonia.Native/**` -> `native/Avalonia.Native/README.md`
- `src/Avalonia.Wayland/**` -> `src/Avalonia.Wayland/README.md`

## SBOM (EU Cyber Resilience Act)

We generate a CycloneDX SBOM per published NuGet package (`nukebuild/SbomGenerator.cs`, `CreateSbom` target) so that every shipped package carries an accurate bill of materials, as required for EU CRA compliance. When reviewing, flag the following:

- **New shipped package:** if a change adds a new NuGet package that is published to end users (a new packable project, or a new final package in `nukebuild/numerge.json`), it must be covered by SBOM generation. Confirm `CreateSbom` produces an SBOM for it, and call it out if the package would ship without one.
- **New kinds of shipped dependencies:** if a change alters what dependencies are delivered with a component in a way the existing scan doesn't already understand, `nukebuild/SbomGenerator.cs` must be updated so those dependencies appear in the SBOM. In particular:
  - Adding npm/JS dependencies to a component that was previously .NET-only (e.g. a new `webapp` bundled into a package), or changing how existing bundled JS is built.
  - Bundling third-party binaries directly into a package (copy-local/ILRepack/embedded assemblies) rather than referencing them as normal NuGet dependencies.
  - New Numerge merge groups, or any other mechanism that folds a project's shipped output into a package other than via ordinary `PackageReference`/`ProjectReference`.

  The scope is what is *delivered* with the package (BSI TR-03183-2 "scope of delivery"), not build/test-only tooling. Purely development-time dependencies that are never shipped do not need to be in the SBOM.
