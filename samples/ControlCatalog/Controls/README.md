# Catalog sample format

Every page in the ControlCatalog uses one of two shapes. Both are built from the same controls and
the same palette, so they read as one app.

## Choosing a tier

Write a **sectioned-scroll** page by default: one scrolling page whose examples are all visible at
once. Reach for a **gallery** only when one of these is true:

- a sample needs the whole pane to be usable (a navigation stack, a drawer, a large table, a GL
  surface, a gesture surface, a full app showcase);
- two samples on one page would fight over something scoped to the window: focus and tab order,
  access keys, drop targets, pointer capture, the clipboard;
- building every sample at once is measurably expensive;
- there are eight or more substantial samples, each with its own options.

When the lesson is *comparison* ("here are the six Stretch modes"), stay sectioned even at high
counts. A gallery replaces adjacency with a memory test.

## Sectioned-scroll page

```xml
<controls:SamplePage xmlns="https://github.com/avaloniaui"
                     xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                     xmlns:controls="using:ControlCatalog.Controls"
                     x:Class="ControlCatalog.Pages.MyControlPage"
                     Header="MyControl"
                     Description="One or two sentences on what the control is for.">
  <StackPanel Spacing="24">
    <controls:SampleGroup Header="Overview">
      <controls:SampleSection Header="First Look"
                              Description="What this example shows, in one sentence."
                              Code="&lt;MyControl Value=&quot;{Binding Thing}&quot; /&gt;">
        <MyControl x:Name="Demo" />
        <controls:SampleSection.Options>
          <StackPanel Spacing="8">
            <CheckBox Content="IsEnabled" IsChecked="{Binding #Demo.IsEnabled}" />
          </StackPanel>
        </controls:SampleSection.Options>
      </controls:SampleSection>
    </controls:SampleGroup>

    <controls:SampleGroup Header="Appearance">
      <controls:SampleSection Header="..." Description="..."> ... </controls:SampleSection>
    </controls:SampleGroup>
  </StackPanel>
</controls:SamplePage>
```

The code-behind derives from `SamplePage`.

The page scrolls by default. A page whose content scrolls on its own (a `ListBox`, a `DataGrid`) sets
`ScrollViewer.VerticalScrollBarVisibility="Disabled"` on the `SamplePage`, so the content gets the page height and
scrolls on its own instead of growing to its full extent.

Sections are always wrapped in a `SampleGroup` whose `Header` is one of the group names below. This is
what makes the two tiers one format: a scrolling page and a gallery show the **same groups in the same
order**, and differ only in whether a group's samples are expanded inline or collapsed into cards.

`SampleSection` gives you:

- `Header` — the example's title.
- `Description` — one sentence saying what it shows.
- `Content` — the live example, on a stage.
- `Options` — controls that configure it. Docked to the right on wide layouts, dropped below the
  stage under 620px. Leave it unset when there is nothing to configure.
- `Code` / `CodeLanguage` — a short snippet, syntax-highlighted, with a copy button. Short ones start
  open; anything over 12 lines starts collapsed. Use the attribute form for one line and
  `<controls:SampleSection.Code>` with a `CDATA` block for several. Show the lines that matter, not the
  whole example. Keep rendered lines under about 100 columns, and never put an element and its children
  on one line — the snippet is teaching markup, so it should look like markup. Wrapped attributes are
  re-aligned under the first attribute automatically, so you do not need to count spaces.
- `Stage` — `Padded` (default), `Flush` for content that paints its own edges, `None` for content
  that must sit directly on the card. This is the only sanctioned way to change the stage; never set
  a literal `Background` on it.
- `StageMinHeight` — when an example needs room to be understood.
- `StageHeight` — a *fixed* stage, for content that is a frame rather than a document: a whole demo
  app, a `Carousel`, a GL surface, anything rooted in its own `ScrollViewer` or with no natural
  height. The stage sits inside the page's `ScrollViewer`, so it offers unbounded height and such
  content would otherwise stretch to its entire scroll extent. Set this on the section and set both
  `*ContentAlignment`s to `Stretch`; never put a literal `Height` on the example to work around it.

## Gallery page

```xml
<controls:SampleGalleryPage xmlns="https://github.com/avaloniaui"
                            xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml"
                            xmlns:controls="using:ControlCatalog.Controls"
                            x:Class="ControlCatalog.Pages.MyControlPage"
                            Header="MyControl"
                            Description="One or two sentences on what the control is for." />
```

```csharp
public partial class MyControlPage : SampleGalleryPage
{
    private static readonly SampleInfo[] Demos =
    {
        new(SampleGroups.Overview, "First Look", "The minimal demo.", () => new MyControlFirstLookPage()),
    };

    public MyControlPage()
    {
        InitializeComponent();
        Samples = Demos;
    }
}
```

Sub-samples are `UserControl`s under `Pages/<Control>/`, named `<Control><Topic>Page`. The factory
runs only when the card is clicked, so a gallery costs nothing until it is used. A sub-sample that
throws shows the exception on its own page rather than taking the app down.

A card pushes its sample on the host `NavigationPage` rather than on a navigation host of its own, so the
shell keeps a single navigation bar: the page title and the drawer toggle on the gallery, the sample title
and the back button once a sample is open.

The registry must be reachable without constructing the page, so declare it as an `internal static readonly`
field (the convention is `Demos`) and pass it when the page is registered in `MainWindowViewModel_PageList`:

```csharp
s.Add<CalendarPage>("Calendar", Icons.Calendar, "A month calendar for selecting dates", CalendarPage.Demos);
```

The drawer search reads the registry without building the page, so it matches a page by its samples
("BlackoutDates" finds Calendar). A page whose samples do not show up in search is usually missing this argument.

## Groups

`SampleGroups` defines the order, and the gallery enforces it regardless of the array order:

**Overview → Populate → Appearance → Features → Events → Performance → Showcases**

Every page gets exactly one Overview sample: the minimal, no-options demo. Leave a group out rather
than inventing filler. A group name outside this list is allowed but renders after all of them.

Both tiers use these names: a single page wraps its sections in `<controls:SampleGroup Header="Appearance">`,
a gallery sets `new(SampleGroups.Appearance, ...)` in its registry. Same vocabulary, same order, two densities.
Use the `SampleGroups` constants, never a literal: a misspelled group renders after all known ones with no warning.

## Colours

Prefer the theme's own defaults: leave `Background` and `Foreground` unset wherever the control already looks
right. Pages have no background of their own, so window transparency shows through; never give one a background.

When a colour is needed, use only `Catalog*` keys. They are defined for `Default` and `Dark` in `CustomThemes.xaml`
(`CatalogBaseLowColor` in `App.xaml`) and are independent of the theme flavour. Keep this list short: add a key only
when several pages need it.

| Key | Use |
|---|---|
| `CatalogCardBackground` / `CatalogCardBorderBrush` | cards |
| `CatalogStageBackground` | the area behind a live example, and code snippets |
| `CatalogSubtleForeground` | secondary text |
| `CatalogAccentBackground` | a tinted accent surface, for text in `CatalogAccentForeground` |
| `CatalogAccentForeground` | accent text |
| `CatalogBaseLowColor` | hairlines and borders |

**Never reference a key starting `SystemControl`, `SystemAccent` or `Theme`.** Those exist in only
one of the two theme flavours, so they silently resolve to nothing under the other. A hardcoded hex
is acceptable only when the colour *is* the demo (a swatch, a deliberately red shape), and then set
the foreground alongside it.

Text roles live in `App.xaml`: `sample-description`, `sample-card-title`,
`sample-card-description`, `sample-section-description`, `sample-options-title`, `sample-caption`.

## Lifetime

Every `Loaded` or `AttachedToVisualTree` subscription in a sample must be symmetric and idempotent.
`Loaded` fires on every attach, so a handler that adds items or subscribes must remove or guard on
the way out. `DrawerPageEventsPage` is the reference.

## Responsiveness

Do not hardcode a width on an options panel or a container; `SampleSection` and `CardGrid` handle
reflow. A fixed width on the example itself is fine when the control needs one (a slider track, a
numeric box).

The same goes for height: if an example grows to fill the page, it has no natural height, and the
fix is `StageHeight` on the section, not a `Height` on the example.

## Leave the page cleaner than you found it

Converting a page is the moment to remove what it accumulated:

- dead code: handlers wired to nothing, fields never read, `x:Name`s nobody references, commented-out blocks;
- unused `using` directives, unused XAML namespace prefixes, and resources declared but never referenced;
- duplicated markup: the same demo repeated with one property changed belongs in one section with an option,
  not in five near-identical copies;
- a view model used only by this page that has drifted from what the page shows;
- files and folders left behind by a sample that no longer exists.

Do not delete anything reachable from another page without saying so.
