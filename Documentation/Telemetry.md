From versions 11.0.0-preview 7 and onwards, anonymous telemetry data is collected at build time:

All data that could be used to identify users is hashed so that the information is protected and anonymous.

The reason for the telemetry is to allow the team to understand and then focus on the needs of the community. We also plan to use the telemetry system for anonymously sharing crashes or problems with the build tooling.

Telemetry does not affect applications built with Avalonia it is only at build time that this occurs.

You may opt out by setting the environment variable `AVALONIA_TELEMETRY_OPTOUT` to `1`.


The following information is collected:
* where Hash of means the data is anonymised using SHA256.

- Timestamp of the build
- Hash of Project Name.
- Output type of the project.
- Target Framework of the project.
- Runtime identifier of the project.
- Hash of git user email.
- A unique but anonymous GUID for machine id.
- Which IDE is being used: VS, VS4Mac, Rider or Cli
- The version of Avalonia that is being used.
- The operating system and version and architecture
- If running on CI the CI Provider, i.e. Azure Devops or Github Actions


The telemetry system is inline with other popular .NET OSS frameworks like Uno Platform and WinUI.

The purpose of this telemetry is to help us better understand how Avalonia UI is being used in the real world so that we can make data-driven decisions to improve the framework.

I want to reassure everyone that the data collected through this telemetry is entirely anonymous, and no personal identifiable information will be collected. The information collected will only pertain to how the Avalonia UI is being used and will not include any sensitive data.

We understand that privacy is a top concern, and we want to assure you that we take this matter seriously. The data collected is hashed, and is intended to give us an understanding of usage numbers and target platforms.

We believe that the addition of telemetry will be a positive step for the Avalonia UI community, as it will provide us with valuable insights that will help us improve the framework and enhance the developer experience.


All data is stored in the EU. The data will be summarised every 90 days and then destroyed.
