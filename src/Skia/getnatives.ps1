$scriptpath = $MyInvocation.MyCommand.Path
$dir = Split-Path $scriptpath
Push-Location $dir

Add-Type -AssemblyName System.IO.Compression.FileSystem
rm -Force -Recurse .\native -ErrorAction SilentlyContinue
mkdir native
$url = cat native.url
Invoke-WebRequest $url -OutFile native\native.zip
[System.IO.Compression.ZipFile]::ExtractToDirectory("native\native.zip", "native")