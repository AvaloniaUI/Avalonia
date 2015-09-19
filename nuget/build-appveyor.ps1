$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir


sv version $env:APPVEYOR_BUILD_NUMBER
#sv version "1-debug"

sv version 9999.0.$version-nightly
sv key $env:myget_key

sv file Perspex.$version.nupkg

.\build-version.ps1 $version


sv reponame $env:APPVEYOR_REPO_NAME
sv repobranch $env:APPVEYOR_REPO_BRANCH
sv pullreq $env:APPVEYOR_PULL_REQUEST_NUMBER

echo "Checking for publishing"
echo $reponame $repobranch $pullreq
if ($reponame -eq "Perspex/Perspex")
{
    echo Repo name matched
    if($repobranch -eq "master")
    {
        echo Repo branch matched
        nuget.exe push $file $key -Source https://www.myget.org/F/perspex-nightly/api/v2/package
    }
}


