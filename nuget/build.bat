SET lib="Perspex\lib\portable-windows8+net45"
SET build="Perspex\build\net45"

mkdir %lib%
mkdir %build%

copy ..\src\Perspex.Animation\bin\Release\Perspex.Animation.dll %lib%
copy ..\src\Perspex.Animation\bin\Release\Perspex.Animation.xml %lib%
copy ..\src\Perspex.Application\bin\Release\Perspex.Application.dll %lib%
copy ..\src\Perspex.Application\bin\Release\Perspex.Application.xml %lib%
copy ..\src\Perspex.Base\bin\Release\Perspex.Base.dll %lib%
copy ..\src\Perspex.Base\bin\Release\Perspex.Base.xml %lib%
copy ..\src\Perspex.Controls\bin\Release\Perspex.Controls.dll %lib%
copy ..\src\Perspex.Controls\bin\Release\Perspex.Controls.xml %lib%
copy ..\src\Perspex.Diagnostics\bin\Release\\Perspex.Diagnostics.dll %lib%
copy ..\src\Perspex.Diagnostics\bin\Release\\Perspex.Diagnostics.xml %lib%
copy ..\src\Perspex.Input\bin\Release\Perspex.Input.dll %lib%
copy ..\src\Perspex.Input\bin\Release\Perspex.Input.xml %lib%
copy ..\src\Perspex.Interactivity\bin\Release\Perspex.Interactivity.dll %lib%
copy ..\src\Perspex.Interactivity\bin\Release\Perspex.Interactivity.xml %lib%
copy ..\src\Perspex.Layout\bin\Release\Perspex.Layout.dll %lib%
copy ..\src\Perspex.Layout\bin\Release\Perspex.Layout.xml %lib%
copy ..\src\Perspex.SceneGraph\bin\Release\Perspex.SceneGraph.dll %lib%
copy ..\src\Perspex.SceneGraph\bin\Release\Perspex.SceneGraph.xml %lib%
copy ..\src\Perspex.Styling\bin\Release\Perspex.Styling.dll %lib%
copy ..\src\Perspex.Styling\bin\Release\Perspex.Styling.xml %lib%
copy ..\src\Perspex.Themes.Default\bin\Release\Perspex.Themes.Default.dll %lib%
copy ..\src\Perspex.Themes.Default\bin\Release\Perspex.Themes.Default.xml %lib%
copy ..\src\Markup\Perspex.Markup.Xaml\bin\Release\Perspex.Markup.Xaml.dll %lib%
copy ..\src\Markup\Perspex.Markup.Xaml\bin\Release\Perspex.Markup.Xaml.xml %lib%
copy ..\src\NGenerics\bin\Release\NGenerics.dll %lib%

copy ..\src\Windows\Perspex.Direct2D1\bin\Release\Perspex.Direct2D1.dll %build%
copy ..\src\Windows\Perspex.Direct2D1\bin\Release\SharpDX.dll %build%
copy ..\src\Windows\Perspex.Direct2D1\bin\Release\SharpDX.Direct2D1.dll %build%
copy ..\src\Windows\Perspex.Direct2D1\bin\Release\SharpDX.DXGI.dll %build%
copy ..\src\Windows\Perspex.Win32\bin\Release\Perspex.Win32.dll %build%

nuget.exe pack Perspex\Perspex.nuspec