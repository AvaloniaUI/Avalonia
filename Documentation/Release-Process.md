1) Create a branch named `release/0.10.1` for example.
2) Update the version number in the file `SharedVersion.props` i.e. `<Version>0.10.1</Version>`.
3) Push the release branch.
4) wait for azure pipelines to finish the build.
5) using the nightly build run a due diligence test to make sure you're happy with the package.
6) On azure pipelines, click on "Releases" then select "Avalonia - publish myget"
7) on the release for your release branch `release/0.10.1` click on the badge for "Nuget Release"
8) Click deploy
9) Make a release on Github releases, this will set a tag named: `0.10.1` for you. You can add release notes here.
10) Update the dotnet templates, rider plugin templates, visual studio templates.
11) announce on gitter, twitter, etc