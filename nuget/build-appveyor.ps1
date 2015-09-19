$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir

sv version $env:APPVEYOR_BUILD_NUMBER
sv version 9999.0.$version-nightly
sv key $env:myget_key

sv file Perspex.$version.nupkg

.\build-version.ps1 $version


nuget.exe push $file $key -Source https://www.myget.org/F/perspex-nightly/api/v2/package
