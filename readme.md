<img src='https://avatars2.githubusercontent.com/u/14075148?s=200&v=4' width='100' />

# Avalonia

| Gitter Chat | Windows Build Status | Linux/Mac Build Status |
|---|---|---|
| [![Gitter](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/AvaloniaUI/Avalonia?utm_campaign=pr-badge&utm_content=badge&utm_medium=badge&utm_source=badge) | [![Build status](https://ci.appveyor.com/api/projects/status/hubk3k0w9idyibfg/branch/master?svg=true)](https://ci.appveyor.com/project/AvaloniaUI/Avalonia/branch/master) | [![Build Status](https://travis-ci.org/AvaloniaUI/Avalonia.svg?branch=master)](https://travis-ci.org/AvaloniaUI/Avalonia) |

## About

Avalonia is a WPF-inspired cross-platform XAML-based UI framework providing a flexible styling system and supporting a wide range of OSs: Windows (.NET Framework, .NET Core), Linux (GTK), MacOS, Android and iOS.

<b>Avalonia is now in alpha.</b> This means that framework is now at a stage where you can have a play and hopefully create simple applications. There's still a lot missing, and you *will* find bugs, and the API *will* change, but this represents the first time where we've made it somewhat easy to have a play and experiment with the framework.

| Control catalog | Desktop platforms | Mobile platforms |
|---|---|---|
| <a href='https://youtu.be/wHcB3sGLVYg'><img width='300' src='http://avaloniaui.net/images/screen.png'></a> | <a href='https://www.youtube.com/watch?t=28&v=c_AB_XSILp0' target='_blank'><img width='300' src='http://avaloniaui.net/images/avalonia-video.png'></a> | <a href='https://www.youtube.com/watch?v=NJ9-hnmUbBM' target='_blank'><img width='300' src='https://i.ytimg.com/vi/NJ9-hnmUbBM/hqdefault.jpg'></a> |

## Getting Started

Avalonia [Visual Studio Extension](https://marketplace.visualstudio.com/items?itemName=AvaloniaTeam.AvaloniaforVisualStudio) contains project and control templates that will help you get started. After installing it, open "New Project" dialog in Visual Studio, choose "Avalonia" in "Visual C#" section, select "Avalonia .NET Core Application" and press OK (<a href="http://avaloniaui.net/tutorial/images/add-dialogs.png">screenshot</a>). Now you can write code and markup that will work on multiple platforms!

Avalonia is delivered via <b>NuGet</b> package manager. You can find the packages here: ([stable(ish)](https://www.nuget.org/packages/Avalonia/), [nightly](https://github.com/AvaloniaUI/Avalonia/wiki/Using-nightly-build-feed))

Use these commands in Package Manager console to install Avalonia manually:
```
Install-Package Avalonia
Install-Package Avalonia.Desktop
```

## Bleeding Edge Builds

Try out the latest build of Avalonia available for download here:
https://ci.appveyor.com/project/AvaloniaUI/Avalonia/branch/master/artifacts

## Documentation

As mentioned above, Avalonia is still in alpha and as such there's not much documentation yet. You can take a look at the [getting started page](http://avaloniaui.net/guides/quickstart) for an overview of how to get started but probably the best thing to do for now is to already know a little bit about WPF/Silverlight/UWP/XAML and ask questions in our [Gitter room](https://gitter.im/AvaloniaUI/Avalonia).

There's also a high-level [architecture document](http://avaloniaui.net/architecture/project-structure) that is currently a little bit out of date, and I've also started writing blog posts on Avalonia at http://grokys.github.io/.

Contributions are always welcome!

## Building and Using

See the [build instructions here](http://avaloniaui.net/guidelines/build).

## Contributing

Please read the [contribution guidelines](http://avaloniaui.net/guidelines/contributing) before submitting a pull request.
