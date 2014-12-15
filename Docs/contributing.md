# Contributing #

## Style ##

The codebase uses StyleCop with default settings[1] to enforce coding style. Yes, some of the 
decisions it makes are downright bizarre, and are certainly not what I would've personally chosen 
but the less time spent debating coding style the more time left for coding.

StyleCop should run on each build and give warnings for any violations. So please, follow the style
- you'll get used to it in the end (I know I have).

If the .NET core team decide on a style and write an automatic checker/tidy tool for that style,
I'll gladly adopt it! I'm certainly not tied to the current style, I'm just tired of endless coding
style debates. Someone decide for me goddammit!

Try to keep lines of code around 100 characters in length or less, though this is not a hard limit.
If you're a few characters over then don't worry too much. 

Documentation comments should also be formatted to a 100 character length to help keep them 
readable.

## Pull requests ##

A single pull request should be submitted for each change. If you're making more than one change,
please submit separate pull requests for each change for easy review. Rebase your changes to make 
sense, so a history that looks like:

* Add class A
* Feature A didn't set Foo when Bar was set
* Fix spacing
* Add class B
* Sort using statements

Should be rebased to read:

* Add class A
* Add class B

Again, this makes review much easier.

Please try not to submit pull requests that don't add new features (e.g. moving stuff around) 
unless you see something that is obviously wrong or that could be written in a more terse or 
idiomatic style. It takes time to review each pull request - time that I'd prefer to spend writing 
new features!

Prefer terseness to verbosity (yes I know that StyleCop will often be working against you here 
:weary:) but don't try to be too clever.

## Tests ##

There are two types of tests currently in the codebase; unit tests and render tests.

Unit tests should be contained in a class name that mirrors the class being tested with the suffix
-Tests, e.g.

    Perspex.Controls.UnitTests.Presenters.TextPresenterTests

Where Perspex.Controls.UnitTests is the name of the project.

Unit test methods should be named in a sentence style, separated by underscores, that describes in
English what the test is testing, e.g.

    void Calling_Foo_Should_Increment_Bar()

Render tests should describe what the produced image is:

    void Rectangle_2px_Stroke_Filled()

----
[1] Documentation rules are disabled because there's currently so much missing documentation, sorry!