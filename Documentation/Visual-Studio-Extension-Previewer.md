# Visual Studio Extension Previewer


## Previewer does not work on Windows 7 without the platform update

You need [this](https://www.microsoft.com/en-us/download/details.aspx?id=36805)

## Previewer does not work for .NET Framework

The previewer currently works *only* .NET Core apps. If you want to use the previewer with .NET Framework, you should use multitargeting (hint: you can have everything targeting `netstandard`, it's only final executable that needs to target .NET Core).