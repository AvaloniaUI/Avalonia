sv version $env:APPVEYOR_BUILD_NUMBER
sv version 9999.0.$version-nightly
.\build-version.ps1 $version
