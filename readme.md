# Perspex #

...a next generation WPF?

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

**DISCLAIMER**: This is really early development pre-alpha-alpha stuff. Everything is subject to 
change, I'm not even sure if the performance characteristics of Rx make Observables suitable for 
binding throughout a framework. *I'm writing this only to see if the idea of exploring these ideas 
appeals to anyone else.*

[Take a look at the introduction document here.](Docs/intro.md)

**NOTE**: This uses proposed C#6 features so you'll have to install a Roslyn preview. [If you're using VS2013, try here.]( https://connect.microsoft.com/VisualStudio/Downloads/DownloadDetails.aspx?DownloadID=52793)
