# Perspex #
[![Gitter](https://badges.gitter.im/Join Chat.svg)](https://gitter.im/grokys/Perspex?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

[![Build status](https://ci.appveyor.com/api/projects/status/hubk3k0w9idyibfg/branch/master?svg=true)](https://ci.appveyor.com/project/grokys/perspex/branch/master)

...a next generation WPF?

![](Docs/screen.png)

## Background ##

As everyone who's involved in client-side .NET development knows, the past half decade have been a 
very sad time. Where WPF started off as a game-changer, it now seems to have been all but forgotten.
WinRT came along and took many of the lessons of WPF but it's currently not usable on the desktop.

After a few months of trying to reverse-engineer WPF with the [Avalonia Project](https://github.com/grokys/Avalonia) I began to come to the same conclusion that I imagine Microsoft
came to internally: for all its groundbreaking-ness at the time, WPF at its core is a dated mess,
written for .NET 1 and barely updated to even bring it up-to-date with .NET 2 features such as
generics.

So I began to think: what if we were to start anew with modern C# features such as *(gasp)* 
Generics, Observables, async, etc etc. The result of that thought is Perspex.

##### DISCLAIMER
This is really early development pre-alpha-alpha stuff. Everything is subject to 
change, I'm not even sure if the performance characteristics of Rx make Observables suitable for 
binding throughout a framework. *I'm writing this only to see if the idea of exploring these ideas 
appeals to anyone else.*

## Documentation
As mentioned above this is really an early version of Perspex and we're working hard on improving the code base and the documentation at the same time. Please feel free to have a look at our [introduction document](Docs/intro.md) to get things started real quick. 

There's also a high-level [architecture document](Docs/architecture.md).

I've also started writing blog posts on Perspex at http://grokys.github.io/.

Contributions are always welcome!

## Building and Using
In order to build and use Perpex you need a compiler that supports the upcoming C# 6 features.

- **Visual Studio 2015 RC**: The recommended way to compile C# 6 code is to use the Visual Studio 2015 RC.It comes with the new Roslyn compiler and features like the new upcoming JIT (RyuJIT) and other improvements like extensible code analysis right out of the box. It can be downloaded [here](http://www.visualstudio.com/en-us/downloads/visual-studio-2015-downloads-vs)

## Contributing ##

Please read the [contribution guidelines](Docs/contributing.md) before submitting a pull request.
