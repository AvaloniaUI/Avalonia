$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir

Add-Type -AssemblyName System.IO.Compression.FileSystem

rm -Force -Recurse .\native -ErrorAction SilentlyContinue
mkdir native
$url = cat native.url

$nativedir = join-path $dir "native"
$nativezip = join-path $nativedir "native.zip"

Invoke-WebRequest $url -OutFile $nativezip
[System.IO.Compression.ZipFile]::ExtractToDirectory(($nativezip), ($nativedir))
Pop-Location