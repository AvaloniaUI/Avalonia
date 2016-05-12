function Get-NewDirectoryName {
    param ([System.IO.DirectoryInfo]$item)

    $name = $item.Name.Replace("perspex", "avalonia")
    $name = $name.Replace("Perspex", "Avalonia")
    Join-Path $item.Parent.FullName $name
}

function Get-NewFileName {
    param ([System.IO.FileInfo]$item)

    $name = $item.Name.Replace("perspex", "avalonia")
    $name = $name.Replace("Perspex", "Avalonia")
    Join-Path $item.DirectoryName $name
}

function Rename-Contents {
    param ([System.IO.FileInfo] $file)

    $extensions = @(".cs",".xaml",".csproj",".sln",".md",".json",".yml",".partial",".ps1",".nuspec",".htm",".html",".gitmodules".".xml",".plist",".targets",".projitems",".shproj",".xib")

    if ($extensions.Contains($file.Extension)) {
        $text = [IO.File]::ReadAllText($file.FullName)
        $text = $text.Replace("github.com/perspex", "github.com/avaloniaui")
        $text = $text.Replace("github.com/Perspex", "github.com/AvaloniaUI")
        $text = $text.Replace("perspex", "avalonia")
        $text = $text.Replace("Perspex", "Avalonia")
        $text = $text.Replace("PERSPEX", "AVALONIA")
        [IO.File]::WriteAllText($file.FullName, $text)
    }
}

function Process-Files {
    param ([System.IO.DirectoryInfo] $item)

    $dirs = Get-ChildItem -Path $item.FullName -Directory
    $files = Get-ChildItem -Path $item.FullName -File

    foreach ($dir in $dirs) {
        Process-Files $dir.FullName
    }

    foreach ($file in $files) {
        Rename-Contents $file

        $renamed = Get-NewFileName $file

        if ($file.FullName -ne $renamed) {
            Write-Host git mv $file.FullName $renamed
            & git mv $file.FullName $renamed
        }
    }

    $renamed = Get-NewDirectoryName $item

    if ($item.FullName -ne $renamed) {
        Write-Host git mv $item.FullName $renamed
        & git mv $item.FullName $renamed
    }
}

& git submodule deinit .
& git clean -xdf
Process-Files .
