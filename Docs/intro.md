# Perspex #

## Background ##

As everyone who's involved in client-side .NET development knows, the past half decade have been a 
very sad time. Where WPF started off as a game-changer, it now seems to have been all but forgotten.
WinRT came along and took many of the lessons of WPF but it's currently not usable on the desktop.

After a few months of trying to reverse-engineer WPF with the [Avalonia Project](https://github.com/grokys/Avalonia) I began to come to the same conclusion that I imagine Microsoft
came to internally: for all its groundbreaking-ness at the time, WPF at its core is a dated mess,
started on with .NET 1 and barely updated to even bring it up-to-date with .NET 2 features such as
generics.

So I began to think: what if we were to start anew with modern C# features such as (gasp) Generics,
Observables, async, etc etc. The result of that thought is Perspex 
(https://github.com/grokys/Perspex).

**DISCLAIMER**: This is really early development pre-alpha-alpha stuff. Everything is subject to 
change, I'm not even sure if the performance characteristics of Rx make Observables suitable for 
binding through a framework. *I'm writing this only to see if the idea of exploring these ideas 
appeals to anyone else.*

So what can it do so far? Not a whole lot right now. Something like this:

![](screen.png)

Ok, not so impressive visually right now, so lets go for a tour of the technical details instead.

## PerspexProperty ##

PerspexProperty is the equivalent of WPF's DependencyProperty. 

I'm not a big fan of DependencyProperty. My first thought was that I'd rather not have something 
like this at all and just use basic INPC but DPs give you two important features: Inheritance and 
Attached Properties. So the challenge became to improve it.

Delaring a DP in WPF looks something like this:

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

Eww all that just to declare a single property. **A LOT** of boilerplate there. With generics and 
default parameters we can at least make it look a bit nicer:

    public static readonly PerspexProperty<PropertyType> PropertyDeclaration =
        PerspexProperty.Register<OwnerClass, PropertyType>("PropertyName", inherits: true);

	public PropertyType PropertyName
	{
	    get { return this.GetValue(PropertyDeclaration); }
	    set { this.SetValue(PropertyDeclaration, value); }
	}

What can we see here?

- PerpexProperties are typed, so no more having to cast in the getter.
- We pass the property type and owner class as a generic type to Register() so we don't have to 
write typeof() twice.
- We used default parameter values in Rigister() so that defaults don't have to be restated.

*(ASIDE: maybe Roslyn will give us [var for fields](http://blogs.msdn.com/b/ericlippert/archive/2009/01/26/why-no-var-on-fields.aspx)...)? Lets hope...*

## Binding

Binding in Perspex uses Reactive Extensions' IObservable. To bind an IObservable to a property,
use the Bind method:

    control.Bind(BorderProperty, someObject.SomeObservable());

Note that because PerspexProperty is typed, we can check that the observable is of the correct type.

To get the value of a property as an observable, call GetObservable():

    var observable = control.GetObservable(Control.FooProperty);

## Attached Properties and Binding Pt 2

Attached properties are set just like in WPF, using SetValue. But what about the [] operator? C# 6 
will allow us to use [] array subscripts in object initializers. So how does this look?

    var control = new Control
	{
		Property1 = "Foo",
        [Attached.Property] = "Bar",
	}


Nice... Lets take this further:

    var control = new Control
	{
		Property1 = "Foo",
        [Attached.Property] = "Bar",
		[!Property2] = something.SomeObservable,
	}

Yep, by putting a bang in front of the property name you can bind to a property (attached or 
otherwise) from the object initializer.

## That's all for now

If you want to have a play you can get the code here: [https://github.com/grokys/Perspex](https://github.com/grokys/Perspex)

Feedback welcome!