$ErrorActionPreference = "Stop"

. ".\include.ps1"

foreach($pkg in $Packages) 
{
    rm -Force -Recurse .\$pkg -ErrorAction SilentlyContinue
}

rm -Force -Recurse *.nupkg -ErrorAction SilentlyContinue
Copy-Item template Avalonia -Recurse
sv lib "Avalonia\lib\portable-windows8+net45"
sv build "Avalonia.Desktop\lib\net45"

sv skia_root "Avalonia.Skia.Desktop"
sv skia_lib "Avalonia.Skia.Desktop\lib\net45"
sv android "Avalonia.Android\lib\MonoAndroid10"
sv ios "Avalonia.iOS\lib\Xamarin.iOS10"

mkdir $lib -ErrorAction SilentlyContinue
mkdir $build -ErrorAction SilentlyContinue
mkdir $skia_lib
mkdir $android
mkdir $ios


Copy-Item ..\src\Avalonia.Animation\bin\Release\Avalonia.Animation.dll $lib
Copy-Item ..\src\Avalonia.Animation\bin\Release\Avalonia.Animation.xml $lib
Copy-Item ..\src\Avalonia.Base\bin\Release\Avalonia.Base.dll $lib
Copy-Item ..\src\Avalonia.Base\bin\Release\Avalonia.Base.xml $lib
Copy-Item ..\src\Avalonia.Controls\bin\Release\Avalonia.Controls.dll $lib
Copy-Item ..\src\Avalonia.Controls\bin\Release\Avalonia.Controls.xml $lib
Copy-Item ..\src\Avalonia.DesignerSupport\bin\Release\Avalonia.DesignerSupport.dll $lib
Copy-Item ..\src\Avalonia.DesignerSupport\bin\Release\Avalonia.DesignerSupport.xml $lib
Copy-Item ..\src\Avalonia.Diagnostics\bin\Release\\Avalonia.Diagnostics.dll $lib
Copy-Item ..\src\Avalonia.Diagnostics\bin\Release\\Avalonia.Diagnostics.xml $lib
Copy-Item ..\src\Avalonia.Input\bin\Release\Avalonia.Input.dll $lib
Copy-Item ..\src\Avalonia.Input\bin\Release\Avalonia.Input.xml $lib
Copy-Item ..\src\Avalonia.Interactivity\bin\Release\Avalonia.Interactivity.dll $lib
Copy-Item ..\src\Avalonia.Interactivity\bin\Release\Avalonia.Interactivity.xml $lib
Copy-Item ..\src\Avalonia.Layout\bin\Release\Avalonia.Layout.dll $lib
Copy-Item ..\src\Avalonia.Layout\bin\Release\Avalonia.Layout.xml $lib
Copy-Item ..\src\Avalonia.Logging.Serilog\bin\Release\Avalonia.Logging.Serilog.dll $lib
Copy-Item ..\src\Avalonia.Logging.Serilog\bin\Release\Avalonia.Logging.Serilog.xml $lib
Copy-Item ..\src\Avalonia.SceneGraph\bin\Release\Avalonia.SceneGraph.dll $lib
Copy-Item ..\src\Avalonia.SceneGraph\bin\Release\Avalonia.SceneGraph.xml $lib
Copy-Item ..\src\Avalonia.Styling\bin\Release\Avalonia.Styling.dll $lib
Copy-Item ..\src\Avalonia.Styling\bin\Release\Avalonia.Styling.xml $lib
Copy-Item ..\src\Avalonia.Themes.Default\bin\Release\Avalonia.Themes.Default.dll $lib
Copy-Item ..\src\Avalonia.Themes.Default\bin\Release\Avalonia.Themes.Default.xml $lib
Copy-Item ..\src\Markup\Avalonia.Markup\bin\Release\Avalonia.Markup.dll $lib
Copy-Item ..\src\Markup\Avalonia.Markup\bin\Release\Avalonia.Markup.xml $lib
Copy-Item ..\src\Markup\Avalonia.Markup.Xaml\bin\Release\Avalonia.Markup.Xaml.dll $lib
Copy-Item ..\src\Markup\Avalonia.Markup.Xaml\bin\Release\Avalonia.Markup.Xaml.xml $lib
Copy-Item ..\src\Avalonia.HtmlRenderer\bin\Release\Avalonia.HtmlRenderer.dll $lib
Copy-Item ..\src\Avalonia.ReactiveUI\bin\Release\Avalonia.ReactiveUI.dll $lib

Copy-Item ..\src\Windows\Avalonia.Direct2D1\bin\Release\Avalonia.Direct2D1.dll $build
Copy-Item ..\src\Windows\Avalonia.Win32\bin\Release\Avalonia.Win32.dll $build
Copy-Item ..\src\Gtk\Avalonia.Gtk\bin\Release\Avalonia.Gtk.dll $build
Copy-Item ..\src\Gtk\Avalonia.Cairo\bin\Release\Avalonia.Cairo.dll $build

Copy-Item ..\src\Skia\Avalonia.Skia.Desktop\bin\x86\Release\Avalonia.Skia.Desktop.dll $skia_lib

Copy-Item ..\src\Android\Avalonia.Android\bin\Release\Avalonia.Android.dll $android
Copy-Item ..\src\Skia\Avalonia.Skia.Android\bin\Release\Avalonia.Skia.Android.dll $android

Copy-Item ..\src\iOS\Avalonia.iOS\bin\iPhone\Release\Avalonia.iOS.dll $ios
Copy-Item ..\src\Skia\Avalonia.Skia.iOS\bin\iPhone\Release\Avalonia.Skia.iOS.dll $ios

foreach($pkg in $Packages)
{
    (gc Avalonia\$pkg.nuspec).replace('#VERSION#', $args[0]) | sc $pkg\$pkg.nuspec
}

foreach($pkg in $Packages)
{
    nuget.exe pack $pkg\$pkg.nuspec
}

foreach($pkg in $Packages)
{
    rm -Force -Recurse .\$pkg
}