[CmdletBinding()]
Param(
    #[switch]$CustomParam,
    [Parameter(Position=0,Mandatory=$false,ValueFromRemainingArguments=$true)]
    [string[]]$BuildArguments
)

Write-Output "Windows PowerShell $($Host.Version)"

Set-StrictMode -Version 2.0; $ErrorActionPreference = "Stop"; $ConfirmPreference = "None"; trap { exit 1 }
$PSScriptRoot = Split-Path $MyInvocation.MyCommand.Path -Parent

###########################################################################
# CONFIGURATION
###########################################################################

$BuildProjectFile = "$PSScriptRoot\nukebuild\_build.csproj"
$TempDirectory = "$PSScriptRoot\\.tmp"

$DotNetGlobalFile = "$PSScriptRoot\\global.json"
$DotNetInstallUrl = "https://raw.githubusercontent.com/dotnet/cli/master/scripts/obtain/dotnet-install.ps1"
$DotNetChannel = "Current"

$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = 1
$env:DOTNET_CLI_TELEMETRY_OPTOUT = 1
$env:NUGET_XMLDOC_MODE = "skip"

###########################################################################
# EXECUTION
###########################################################################

function ExecSafe([scriptblock] $cmd) {
    & $cmd
    if ($LASTEXITCODE) { exit $LASTEXITCODE }
}

# If global.json exists, load expected version
if (Test-Path $DotNetGlobalFile) {
    $DotNetGlobal = $(Get-Content $DotNetGlobalFile | Out-String | ConvertFrom-Json)
    if ($DotNetGlobal.PSObject.Properties["sdk"] -and $DotNetGlobal.sdk.PSObject.Properties["version"]) {
        $DotNetVersion = $DotNetGlobal.sdk.version
    }
}

# If dotnet is installed locally, and expected version is not set or installation matches the expected version
if ((Get-Command "dotnet" -ErrorAction SilentlyContinue) -ne $null -and `
     (!(Test-Path variable:DotNetVersion) -or $(& dotnet --version) -eq $DotNetVersion)) {
    $env:DOTNET_EXE = (Get-Command "dotnet").Path
}
else {
    $DotNetDirectory = "$TempDirectory\dotnet-win"
    $env:DOTNET_EXE = "$DotNetDirectory\dotnet.exe"

    # Download install script
    $DotNetInstallFile = "$TempDirectory\dotnet-install.ps1"
    md -force $TempDirectory > $null
    (New-Object System.Net.WebClient).DownloadFile($DotNetInstallUrl, $DotNetInstallFile)

    # Install by channel or version
    if (!(Test-Path variable:DotNetVersion)) {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Channel $DotNetChannel -NoPath }
    } else {
        ExecSafe { & $DotNetInstallFile -InstallDir $DotNetDirectory -Version $DotNetVersion -NoPath }
    }
}

Write-Output "Microsoft (R) .NET Core SDK version $(& $env:DOTNET_EXE --version)"

ExecSafe { & $env:DOTNET_EXE run --project $BuildProjectFile -- $BuildArguments }
