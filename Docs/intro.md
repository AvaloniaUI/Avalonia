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
Generics, Observables, async, etc etc. The result of that thought is Perspex 
(https://github.com/grokys/Perspex).

##### DISCLAIMER
This is really **early development pre-alpha-alpha** stuff. Everything is subject to 
change, I'm not even sure if the performance characteristics of Rx make Observables suitable for 
binding throughout a framework. *I'm writing this only to see if the idea of exploring these ideas 
appeals to anyone else.*

So what can it do so far? Not a whole lot right now. Here's the demo application:

![](screen.png)

## PerspexProperty ##

PerspexProperty is the equivalent of WPF's DependencyProperty. 

I'm not a big fan of DependencyProperty. My first thought was that I'd rather not have something 
like this at all and just use basic INPC but DPs give you two important features: Inheritance and 
Attached Properties. So the challenge became to improve it.

Delaring a DP in WPF looks something like this:

```csharp
public static readonly DependencyProperty PropertyDeclaration =
DependencyProperty.Register(
    "PropertyName",
    typeof(PropertyType),
    typeof(OwnerClass),
    new FrameworkPropertyMetadata(
        default(PropertyType),
        FrameworkPropertyMetadataOptions.Inherits));

public PropertyType PropertyName
{
    get { return (PropertyType)this.GetValue(PropertyDeclaration); }
    set { this.SetValue(PropertyDeclaration, value); }
}
```

Eww! All that just to declare a single property. There's **A LOT** of boilerplate there. With 
generics and default parameters we can at least make it look a bit nicer:

```csharp
public static readonly PerspexProperty<PropertyType> PropertyDeclaration =
PerspexProperty.Register<OwnerClass, PropertyType>("PropertyName", inherits: true);

public PropertyType PropertyName
{
    get { return this.GetValue(PropertyDeclaration); }
    set { this.SetValue(PropertyDeclaration, value); }
}
```

What can we see here?

- PerspexProperties are typed, so no more having to cast in the getter.
- We pass the property type and owner class as a generic type to `Register()` so we don't have to 
write `typeof()` twice.
- We used default parameter values in `Register()` so that defaults don't have to be restated.

*(ASIDE: maybe Roslyn will give us [var for fields](http://blogs.msdn.com/b/ericlippert/archive/2009/01/26/why-no-var-on-fields.aspx)...)? Lets hope...*

## Binding

Binding in Perspex uses Reactive Extensions' [IObservable](http://msdn.microsoft.com/library/dd990377.aspx). To bind an IObservable to a property, use the `Bind()` method:

```csharp
control.Bind(BorderProperty, someObject.SomeObservable());
```

Note that because PerspexProperty is typed, we can check that the observable is of the correct type.

To get the value of a property as an observable, call `GetObservable()`:

```csharp
var observable = control.GetObservable(Control.FooProperty);
```

## Attached Properties and Binding Pt 2

Attached properties are set just like in WPF, using `SetValue()`. But what about the `[]` operator, also called [index initializer](https://roslyn.codeplex.com/wikipage?title=Language%20Feature%20Status&referringTitle=Home) (see *C# feature descriptions*)? The upcoming version of C# will provide a new feature that allows us to use `[]` array subscripts in object initializers. An example on how to make use of it will look like this:

```csharp
var control = new Control
{
	Property1 = "Foo",
[Attached.Property] = "Bar",
}
```


Nice... Lets take this further:

```csharp
var control = new Control
{
	Property1 = "Foo",
[Attached.Property] = "Bar",
	[!Property2] = something.SomeObservable,
}
```

Yep, by putting a bang in front of the property name you can **bind** to a property (attached or 
otherwise) from the object initializer.

Binding to a property on another control? Easy:

```csharp
var control = new Control
{
	Property1 = "Foo",
[Attached.Property] = "Bar",
	[!Property2] = anotherControl[!Property1],
}
```

Two way binding? Just add two bangs:

```csharp
var control = new Control
{
	Property1 = "Foo",
[Attached.Property] = "Bar",
	[!!Property2] = anotherControl[!!Property1],
}
```

## Visual and Logical trees

Perspex uses the same visual/logical tree separation that is used by WPF (and to some extent HTML 
is moving in this direction with the Shadow DOM). The manner of accessing the two trees is slightly
different however. Rather than using Visual/LogicalTreeHelper you can cast any control to an 
`IVisual` or `ILogical` to reveal the tree operations. There's also the VisualExtensions class which
provides some useful extension methods such as `GetVisualAncestor<T>(this IVisual visual)` or 
`GetVisualAt(this IVisual visual, Point p)`.

Again like WPF, Controls are lookless with the visual appearance being defined by the Template 
property, which is determined by...

## Styles

Styles in Perspex diverge from styles in WPF quite a lot, and move towards a more CSS-like system.
It's probably easiest to show in an example. Here is the default style for the CheckBox control:

```csharp
new Style(x => x.OfType<CheckBox>())
{
	Setters = new[]
	{
	    new Setter(Button.TemplateProperty, ControlTemplate.Create<CheckBox>(this.Template))
	}
};

new Style(x => x.OfType<CheckBox>().Template().Id("checkMark"))
{
	Setters = new[]
	{
	    new Setter(Shape.IsVisibleProperty, false)
	}
};
	
new Style(x => x.OfType<CheckBox>().Class(":checked").Template().Id("checkMark"))
{
	Setters = new[]
	{
	    new Setter(Shape.IsVisibleProperty, true)
	}
};
```

Let's see what's happening here:

```csharp
new Style(x => x.OfType<CheckBox>())
```

The constructor for the Style class defines the selector. Here we're saying "*this style applies to 
all controls in the the visual tree of type CheckBox*". A more complex selector:

```csharp
new Style(x => x.OfType<CheckBox>().Class(":checked").Template().Id("checkMark"))
```

This selector matches "*all controls with Id == "checkMark" in the template of a CheckBox with the
class `:checked`"*. Each control has an Id property, and Ids in templates are considered to be in a
separate namespace.

Inside the Style class we then have a collection of setters similar to WPF. 

This system means that there's no more need for WPF's Triggers - the styling works with classes 
(which are arbitrary strings) similar to CSS. Similar to CSS, classes with a leading `:` are set
by the control itself in response to events like `mouseover` and `click`.

Similar to WPF, styles can be defined on each control, with a global application style collection
at the root. This means that different subsections of the visual tree can have a completely 
different look-and-feel.

## XAML

As you can see, all of the examples here are defined in code - but a XAML implementation is being
worked on. The current progress can be reviewed at https://github.com/SuperJMN/Perspex/tree/fuent-api.

## That's all for now

There's a lot more to see, and even more to do, so if you want to have a play you can get the code 
here: [https://github.com/grokys/Perspex](https://github.com/grokys/Perspex)

Feedback is always welcome!
