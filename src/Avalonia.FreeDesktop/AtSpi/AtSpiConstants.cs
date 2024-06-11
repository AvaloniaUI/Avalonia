using System;

namespace Avalonia.FreeDesktop.AtSpi;

/// <summary>
/// Constants used by the AT-SPI subsystem.
/// Enum and RoleName descriptions courtesy of the https://github.com/odilia-app/atspi project
/// Licensed under MIT License
/// Copyright (c) 2022 Tait Hoyem [tait@tait.tech]
/// </summary>
public class AtSpiConstants
{
    public const string AvaloniaPathPrefix = "/net/avaloniaui/accessibles/";

    public static readonly string[] RoleNames =
    [
        "invalid",
        "accelerator label",
        "alert",
        "animation",
        "arrow",
        "calendar",
        "canvas",
        "check box",
        "check menu item",
        "color chooser",
        "column header",
        "combo box",
        "date editor",
        "desktop icon",
        "desktop frame",
        "dial",
        "dialog",
        "directory pane",
        "drawing area",
        "file chooser",
        "filler",
        "focus traversable",
        "font chooser",
        "frame",
        "glass pane",
        "html container",
        "icon",
        "image",
        "internal frame",
        "label",
        "layered pane",
        "list",
        "list item",
        "menu",
        "menu bar",
        "menu item",
        "option pane",
        "page tab",
        "page tab list",
        "panel",
        "password text",
        "popup menu",
        "progress bar",
        "push button",
        "radio button",
        "radio menu item",
        "root pane",
        "row header",
        "scroll bar",
        "scroll pane",
        "separator",
        "slider",
        "spin button",
        "split pane",
        "status bar",
        "table",
        "table cell",
        "table column header",
        "table row header",
        "tearoff menu item",
        "terminal",
        "text",
        "toggle button",
        "tool bar",
        "tool tip",
        "tree",
        "tree table",
        "unknown",
        "viewport",
        "window",
        "extended",
        "header",
        "footer",
        "paragraph",
        "ruler",
        "application",
        "autocomplete",
        "editbar",
        "embedded",
        "entry",
        "chart",
        "caption",
        "document frame",
        "heading",
        "page",
        "section",
        "redundant object",
        "form",
        "link",
        "input method window",
        "table row",
        "tree item",
        "document spreadsheet",
        "document presentation",
        "document text",
        "document web",
        "document email",
        "comment",
        "list box",
        "grouping",
        "image map",
        "notification",
        "info bar",
        "level bar",
        "title bar",
        "block quote",
        "audio",
        "video",
        "definition",
        "article",
        "landmark",
        "log",
        "marquee",
        "math",
        "rating",
        "timer",
        "static",
        "math fraction",
        "math root",
        "subscript",
        "superscript",
        "description list",
        "description term",
        "description value",
        "footnote",
        "content deletion",
        "content insertion",
        "mark",
        "suggestion",
        "push button menu",
    ];

    public enum Role
    {
        /// A role indicating an error condition, such as uninitialized Role data, or an error deserializing.
        Invalid,

        /// Object is a label indicating the keyboard accelerators for the parent.
        AcceleratorLabel,

        /// Object is used to alert the user about something.
        Alert,

        /// Object contains a dynamic or moving image of some kind.
        Animation,

        /// Object is a 2d directional indicator.
        Arrow,

        /// Object contains one or more dates, usually arranged into a 2d list.
        Calendar,

        /// Object that can be drawn into and is used to trap events.
        Canvas,

        /// A choice that can be checked or unchecked and provides a separate indicator for the current state.
        CheckBox,

        /// A menu item that behaves like a check box. See <see cref="CheckBox"/>.
        CheckMenuItem,

        /// A specialized dialog that lets the user choose a color.
        ColorChooser,

        /// The header for a column of data.
        ColumnHeader,

        /// A list of choices the user can select from.
        ComboBox,

        /// An object which allows entry of a date.
        DateEditor,

        /// An inconifed internal frame within a [`Role.DesktopFrame`].
        DesktopIcon,

        /// A pane that supports internal frames and iconified versions of those internal frames.
        DesktopFrame,

        /// An object that allows a value to be changed via rotating a visual element, or which displays a value via such a rotating element.
        Dial,

        /// A top level window with title bar and a border.
        Dialog,

        /// A pane that allows the user to navigate through and select the contents of a directory.
        DirectoryPane,

        /// An object used for drawing custom user interface elements.
        DrawingArea,

        /// A specialized dialog that displays the files in the directory and lets the user select a file, browse a different directory, or specify a filename.
        FileChooser,

        /// A object that fills up space in a user interface.
        Filler,

        /// Don't use, reserved for future use.
        FocusTraversable,

        /// Allows selection of a display font.
        FontChooser,

        /// A top level window with a title bar, border, menubar, etc.
        Frame,

        /// A pane that is guaranteed to be painted on top of all panes beneath it.
        GlassPane,

        /// A document container for HTML, whose children represent the document content.
        HTMLContainer,

        /// A small fixed size picture, typically used to decorate components.
        Icon,

        /// An image, typically static.
        Image,

        /// A frame-like object that is clipped by a desktop pane.
        InternalFrame,

        /// An object used to present an icon or short string in an interface.
        Label,

        /// A specialized pane that allows its children to be drawn in layers, providing a form of stacking order.
        LayeredPane,

        /// An object that presents a list of objects to the user and * allows the user to select one or more of them.
        List,

        /// An object that represents an element of a list.
        ListItem,

        /// An object usually found inside a menu bar that contains a list of actions the user can choose from.
        Menu,

        /// An object usually drawn at the top of the primary dialog box of an application that contains a list of menus the user can choose from.
        MenuBar,

        /// An object usually contained in a menu that presents an action the user can choose.
        MenuItem,

        /// A specialized pane whose primary use is inside a dialog.
        OptionPane,

        /// An object that is a child of a page tab list.
        PageTab,

        /// An object that presents a series of panels (or page tabs), one at a time,through some mechanism provided by the object.
        PageTabList,

        /// A generic container that is often used to group objects.
        Panel,

        /// A text object uses for passwords, or other places where the text content is not shown visibly to the user.
        PasswordText,

        /// A temporary window that is usually used to offer the user a list of choices, and then hides when the user selects one of those choices.
        PopupMenu,

        /// An object used to indicate how much of a task has been completed.
        ProgressBar,

        /// An object the user can manipulate to tell the application to do something.
        PushButton,

        /// A specialized check box that will cause other radio buttons in the same group to become unchecked when this one is checked.
        RadioButton,

        /// Object is both a menu item and a "radio button". See <see cref="RadioButton"/>.
        RadioMenuItem,

        /// A specialized pane that has a glass pane and a layered pane as its children.
        RootPane,

        /// The header for a row of data.
        RowHeader,

        /// An object usually used to allow a user to incrementally view a large amount of data by moving the bounds of a viewport along a one-dimensional axis.
        ScrollBar,

        /// A scroll pane: the pane in which the scrollable content is contained within.
        /// An object that allows a user to incrementally view a large amount of information.
        /// <see cref="ScrollPane"/> objects are usually accompanied by <see cref="ScrollBar"/> controllers,
        /// on which the <see cref="RelationType.ControllerFor"/> and <see cref="RelationType.ControlledBy"/> reciprocal relations are set.
        ScrollPane,

        /// An object usually contained in a menu to provide a visible and logical separation of the contents in a menu.
        Separator,

        /// An object that allows the user to select from a bounded range.
        /// Unlike <see cref="ScrollBar"/>, <see cref="Slider"/> objects need not control 'viewport'-like objects.
        Slider,

        /// An object which allows one of a set of choices to be selected, and which displays the current choice.
        SpinButton,

        /// A specialized panel that presents two other panels at the same time.
        SplitPane,

        /// Object displays non-quantitative status information (c.f. <see cref="ProgressBar"/>)
        StatusBar,

        /// An object used to represent information in terms of rows and columns.
        Table,

        /// A 'cell' or discrete child within a Table.
        /// Note: Table cells need not have <see cref="TableCell"/>, other <see cref="Role"/> values are valid as well.
        TableCell,

        /// An object which labels a particular column in an <see cref="Table"/>.
        TableColumnHeader,

        /// An object which labels a particular row in a <see cref="Table"/>.
        /// `TableProxy` rows and columns may also be labelled via the
        /// <see cref="RelationType.LabelFor"/>/<see cref="RelationType.LabelledBy"/> relationships.
        /// See: `AccessibleProxy.get_relation_type`.
        TableRowHeader,

        /// Object allows menu to be removed from menubar and shown in its own window.
        TearoffMenuItem,

        /// An object that emulates a terminal.
        Terminal,

        /// An interactive widget that supports multiple lines of text and optionally accepts user input,
        /// but whose purpose is not to solicit user input.
        /// Thus <see cref="Text"/> is appropriate for the text view in a plain text editor but inappropriate for an input field in a dialog box or web form.
        /// For widgets whose purpose is to solicit input from the user, see <see cref="Entry"/> and <see cref="PasswordText"/>.
        /// For generic objects which display a brief amount of textual information, see <see cref="Static"/>.
        Text,

        /// A specialized push button that can be checked or unchecked, but does not provide a separate indicator for the current state.
        ToggleButton,

        /// A bar or palette usually composed of push buttons or toggle buttons.
        ToolBar,

        /// An object that provides information about another object.
        ToolTip,

        /// An object used to repsent hierarchical information to the user.
        Tree,

        /// An object that presents both tabular and hierarchical info to the user.
        TreeTable,

        /// When the role cannot be accurately reported, this role will be set.
        Unknown,

        /// An object usually used in a scroll pane, or to otherwise clip a larger object or content renderer to a specific onscreen viewport.
        Viewport,

        /// A top level window with no title or border.
        Window,

        /// means that the role for this item is known, but not included in the core enumeration.
        Extended,

        /// An object that serves as a document header.
        Header,

        /// An object that serves as a document footer.
        Footer,

        /// An object which is contains a single paragraph of text content. See also <see cref="Text"/>.
        Paragraph,

        /// An object which describes margins and tab stops, etc. for text objects which it controls (should have <see cref="RelationType.ControllerFor"/> relation to such).
        Ruler,

        /// An object corresponding to the toplevel accessible of an application, which may contain <see cref="Frame"/> objects or other accessible objects.
        /// Children of objects with the <see cref="DesktopFrame"/> role are generally <see cref="Application"/> objects.
        Application,

        /// The object is a dialog or list containing items for insertion into an entry widget, for instance a list of words for completion of a text entry.
        Autocomplete,

        /// The object is an editable text object in a toolbar.
        Editbar,

        /// The object is an embedded component container.
        /// This role is a "grouping" hint that the contained objects share a context which is different from the container in which this accessible is embedded.
        /// In particular, it is used for some kinds of document embedding, and for embedding of out-of-process component, "panel applets", etc.
        Embedded,

        /// The object is a component whose textual content may be entered or modified by the user, provided <see cref="State.Editable"/> is present.
        /// A readonly <see cref="Entry"/> object (i.e. where <see cref="State.Editable"/> is not present) implies a read-only 'text field' in a form, as opposed to a title, label, or caption.
        Entry,

        /// The object is a graphical depiction of quantitative data.
        /// It may contain multiple subelements whose attributes and/or description may be queried to obtain both the  quantitative data and information about how the data is being presented.
        /// The <see cref="RelationType.LabelledBy"/> relation is particularly important in interpreting objects of this type, as is the accessible description property.
        /// See <see cref="Caption"/>.
        Chart,

        /// The object contains descriptive information, usually textual, about another user interface element such as a table, chart, or image.
        Caption,

        /// The object is a visual frame or container which
        /// contains a view of document content. <see cref="DocumentFrame"/>s may occur within
        /// another `DocumentProxy` instance, in which case the second  document may be
        /// said to be embedded in the containing instance.
        /// HTML frames are often <see cref="DocumentFrame"/>:  Either this object, or a singleton descendant,
        /// should implement the <see cref="Interface.Document"/> interface.
        DocumentFrame,

        /// Heading: this is a heading with a level (usually 1-6). This is represented by `<h1>` through `<h6>` in HTML.
        /// The object serves as a heading for content which follows it in a document.
        /// The 'heading level' of the heading, if available, may be obtained by querying the object's attributes.
        Heading,

        /// The object is a containing instance which encapsulates a page of information.
        /// <see cref="Page"/> is used in documents and content which support a paginated navigation model.
        Page,

        /// The object is a containing instance of document content which constitutes a particular 'logical' section of the document.
        /// The type of content within a section, and the nature of the section division itself, may be obtained by querying the object's attributes.
        /// Sections may be nested.
        Section,

        /// The object is redundant with another object in the hierarchy, and is exposed for purely technical reasons.
        /// Objects of this role should be ignored by clients, if they are encountered at all.
        RedundantObject,

        /// The object is a containing instance of document content which has within it components with which the user can interact in order to input information;
        /// i.e. the object is a container for pushbuttons, comboboxes, text input fields, and other 'GUI' components.
        /// <see cref="Form"/> should not, in general, be used for toplevel GUI containers or dialogs, but should be reserved for 'GUI' containers which occur within document content, for instance within Web documents, presentations, or text documents.
        /// Unlike other GUI containers and dialogs which occur inside application instances, <see cref="Form"/> containers' components are associated with the current document, rather than the current foreground application or viewer instance.
        Form,

        /// The object is a hypertext anchor, i.e. a "link" in a hypertext document.
        /// Such objects are distinct from 'inline' content which may also use the <see cref="Interface.Hypertext"/>/<see cref="Interface.Hyperlink"/> interfaces to indicate the range/location within a text object where an inline or embedded object lies.
        Link,

        /// The object is a window or similar viewport which is used to allow composition or input of a 'complex character', in other words it is an "input method window".
        InputMethodWindow,

        /// A row in a table.
        TableRow,

        /// An object that represents an element of a tree.
        TreeItem,

        /// A document frame which contains a spreadsheet.
        DocumentSpreadsheet,

        /// A document frame which contains a presentation or slide content.
        DocumentPresentation,

        /// A document frame which contains textual content, such as found in a word processing application.
        DocumentText,

        /// A document frame which contains HTML or other markup suitable for display in a web browser.
        DocumentWeb,

        /// A document frame which contains email content to be displayed or composed either in plain text or HTML.
        DocumentEmail,

        /// An object found within a document and designed to present a comment, note, or other annotation.
        /// In some cases, this object might not be visible until activated.
        Comment,

        /// A non-collapsible list of choices the user can select from.
        ListBox,

        /// A group of related widgets. This group typically has a label.
        Grouping,

        /// An image map object. Usually a graphic with multiple hotspots, where each hotspot can be activated resulting in the loading of another document or section of a document.
        ImageMap,

        /// A transitory object designed to present a message to the user, typically at the desktop level rather than inside a particular application.
        Notification,

        /// An object designed to present a message to the user within an existing window.
        InfoBar,

        /// A bar that serves as a level indicator to, for instance, show the strength of a password or the state of a battery.
        LevelBar,

        /// A bar that serves as the title of a window or a dialog.
        TitleBar,

        /// An object which contains a text section that is quoted from another source.
        BlockQuote,

        /// An object which represents an audio element.
        Audio,

        /// An object which represents a video element.
        Video,

        /// A definition of a term or concept.
        Definition,

        /// A section of a page that consists of a composition that forms an independent part of a document, page, or site.
        /// Examples: A blog entry, a news story, a forum post.
        Article,

        /// A region of a web page intended as a navigational landmark. This is designed to allow Assistive Technologies to provide quick navigation among key regions within a document.
        Landmark,

        /// A text widget or container holding log content, such as chat history and error logs. In this role there is a relationship between the arrival of new items in the log and the reading order.
        /// The log contains a meaningful sequence and new information is added only to the end of the log, not at arbitrary points.
        Log,

        /// A container where non-essential information changes frequently.
        /// Common usages of marquee include stock tickers and ad banners.
        /// The primary difference between a marquee and a log is that logs usually have a meaningful order or sequence of important content changes.
        Marquee,

        /// A text widget or container that holds a mathematical expression.
        Math,

        /// A rating system, generally out of five stars, but it does not need to be that way. There is no tag nor role for this in HTML, however.
        /// A widget whose purpose is to display a rating, such as the number of stars associated with a song in a media player.
        /// Objects of this role should also implement <see cref="Interface.Value"/>.
        Rating,

        /// An object containing a numerical counter which indicates an amount of elapsed time from a start point, or the time remaining until an end point.
        Timer,

        /// A generic non-container object whose purpose is to display a brief amount of information to the user and whose role is known by the implementor but lacks semantic value for the user.
        /// Examples in which <see cref="Static"/> is appropriate include the message displayed in a message box and an image used as an alternative means to display text.
        /// <see cref="Static"/> should not be applied to widgets which are traditionally interactive, objects which display a significant amount of content, or any object which has an accessible relation pointing to another object.
        /// The displayed information, as a general rule, should be exposed through the accessible name of the object.
        /// For labels which describe another widget, see <see cref="Label"/>.
        /// For text views, see <see cref="Text"/>.
        /// For generic containers, see <see cref="Panel"/>. For objects whose role is not known by the implementor, see <see cref="Unknown"/>.
        Static,

        /// An object that represents a mathematical fraction.
        MathFraction,

        /// An object that represents a mathematical expression displayed with a radical.
        MathRoot,

        /// An object that contains text that is displayed as a subscript.
        Subscript,

        /// An object that contains text that is displayed as a superscript.
        Superscript,

        /// An object that represents a list of term-value groups.
        /// A term-value group represents an individual description and consist of one or more names (<see cref="DescriptionTerm"/>) followed by one or more values (<see cref="DescriptionValue"/>).
        /// For each list, there should not be more than one group with the same term name.
        DescriptionList,

        /// An object that represents a term or phrase with a corresponding definition.
        DescriptionTerm,

        /// An object that represents the description, definition, or value of a term.
        DescriptionValue,

        /// An object that contains the text of a footnote.
        Footnote,

        /// Content previously deleted or proposed to be deleted, e.g. in revision history or a content view providing suggestions from reviewers.
        ContentDeletion,

        /// Content previously inserted or proposed to be inserted, e.g. in revision history or a content view providing suggestions from reviewers.
        ContentInsertion,

        /// A run of content that is marked or highlighted, such as for reference purposes, or to call it out as having a special purpose.
        /// If the marked content has an associated section in the document elaborating on the reason for the mark, then <see cref="RelationType.Details"/> should be used on the mark to point to that associated section.
        /// In addition, the reciprocal relation <see cref="RelationType.DetailsFor"/> should be used on the associated content section to point back to the mark.
        Mark,

        /// A container for content that is called out as a proposed change from the current version of the document, such as by a reviewer of the content.
        /// An object with this role should include children with <see cref="ContentDeletion"/> and/or <see cref="ContentInsertion"/>, in any order, to indicate what the actual change is.
        Suggestion,

        /// A specialized push button to open a menu.
        PushButtonMenu,
    }

    public enum RelationType
    {
        /// Not a meaningful relationship; clients should not normally encounter this value.
        Null = 0,

        /// Object is a label for one or more other objects.
        LabelFor,

        /// Object is labelled by one or more other objects.
        LabelledBy,

        /// Object is an interactive object which modifies the state,
        /// onscreen location, or other attributes of one or more target objects.
        ControllerFor,

        /// Object state, position, etc. is modified/controlled by user interaction
        /// with one or more other objects.
        /// For instance a viewport or scroll pane may be <see cref="ControlledBy"/> scrollbars.
        ControlledBy,

        /// Object has a grouping relationship (e.g. 'same group as') to one or more other objects.
        MemberOf,

        /// Object is a tooltip associated with another object.
        TooltipFor,

        /// Object is a child of the target.
        NodeChildOf,

        /// Object is a parent of the target.
        NodeParentOf,

        /// Used to indicate that a relationship exists, but its type is not
        /// specified in the enumeration.
        Extended,

        /// Object renders content which flows logically to another object.
        /// For instance, text in a paragraph may flow to another object
        /// which is not the 'next sibling' in the accessibility hierarchy.
        FlowsTo,

        /// Reciprocal of <see cref="FlowsTo"/>.
        FlowsFrom,

        /// Object is visually and semantically considered a subwindow of another object,
        /// even though it is not the object's child.
        /// Useful when dealing with embedded applications and other cases where the
        /// widget hierarchy does not map cleanly to the onscreen presentation.
        SubwindowOf,

        /// Similar to <see cref="SubwindowOf"/>, but specifically used for cross-process embedding.
        Embeds,

        /// Reciprocal of <see cref="Embeds"/>. Used to denote content rendered by embedded renderers
        /// that live in a separate process space from the embedding context.
        EmbeddedBy,

        ///Denotes that the object is a transient window or frame associated with another
        /// onscreen object. Similar to <see cref="TooltipFor"/>, but more general.
        /// Useful for windows which are technically toplevels but which, for one or more reasons,
        /// do not explicitly cause their associated window to lose 'window focus'.
        /// Creation of an <see cref="Role.Window"/> object with the <see cref="PopupFor"/> relation usually requires
        /// some presentation action on the part of assistive technology clients,
        /// even though the previous toplevel <see cref="Role.Frame"/> object may still be the active window.
        PopupFor,

        /// This is the reciprocal relation to  <see cref="PopupFor"/> .
        ParentWindowOf,

        /// Reciprocal of <see cref="DescribedBy"/>. Indicates that this object provides descriptive information
        /// about the target object(s). See also <see cref="DetailsFor"/> and <see cref="ErrorFor"/>.
        DescriptionFor,

        /// Reciprocal of <see cref="DescriptionFor"/>.
        /// Indicates that one or more target objects provide descriptive information
        /// about this object. This relation type is most appropriate for information
        /// that is not essential as its presentation may be user-configurable and/or
        /// limited to an on-demand mechanism such as an assistive technology command.
        /// For brief, essential information such as can be found in a widget's on-screen
        /// label, use <see cref="LabelledBy"/>. For an on-screen error message, use <see cref="ErrorMessage"/>.
        /// For lengthy extended descriptive information contained in an on-screen object,
        /// consider using <see cref="Details"/> as assistive technologies may provide a means
        /// for the user to navigate to objects containing detailed descriptions so that
        /// their content can be more closely reviewed.
        DescribedBy,

        /// Reciprocal of <see cref="DetailsFor"/>.
        /// Indicates that this object has a detailed or extended description,
        /// the contents of which can be found in the target object(s).
        /// This relation type is most appropriate for information that is sufficiently lengthy
        /// as to make navigation to the container of that information desirable.
        /// For less verbose information suitable for announcement only, see <see cref="DescribedBy"/>.
        /// If the detailed information describes an error condition, <see cref="ErrorFor"/> should be used instead.
        /// Included in upstream [AT-SPI2-CORE](https://gitlab.gnome.org/GNOME/at-spi2-core) since 2.26.
        Details,

        /// Reciprocal of <see cref="Details"/>.
        /// Indicates that this object provides a detailed or extended description about the target
        /// object(s). See also <see cref="DescriptionFor"/>`` and <see cref="ErrorFor"/>``.
        /// Included in upstream [AT-SPI2-CORE](https://gitlab.gnome.org/GNOME/at-spi2-core) since 2.26.
        DetailsFor,

        /// Reciprocal of <see cref="ErrorFor"/>``.
        /// Indicates that this object has one or more errors, the nature of which is
        /// described in the contents of the target object(s). Objects that have this
        /// relation type should also contain <see cref="State.InvalidEntry"/><see cref="state.State.InvalidEntry"/> when their
        /// `GetState` method is called.  
        /// Included in upstream [AT-SPI2-CORE](https://gitlab.gnome.org/GNOME/at-spi2-core) since 2.26.
        ErrorMessage,

        ///  Reciprocal of `ErrorMessage`.
        /// Indicates that this object contains an error message describing an invalid condition
        /// in the target object(s).
        /// Included in upstream [AT-SPI2-CORE](https://gitlab.gnome.org/GNOME/at-spi2-core) since 2.26.
        ErrorFor,
    }

    [Flags]
    public enum State 
    {
        /// Indicates an invalid state - probably an error condition.
        Invalid,

        /// Indicates a window is currently the active window, or
        /// an object is the active subelement within a container or table.
        ///
        /// `Active` should not be used for objects which have
        /// <see cref="State.Focusable"/> or <see cref="State.Selectable"/>: Those objects should use
        /// <see cref="State.Focused"/> and <see cref="State.Selected"/> respectively.
        ///
        /// `Active` is a means to indicate that an object which is not
        /// focusable and not selectable is the currently-active item within its
        /// parent container.
        Active,

        /// Indicates that the object is armed.
        Armed,

        /// Indicates the current object is busy, i.e. onscreen
        /// representation is in the process of changing, or       the object is
        /// temporarily unavailable for interaction due to activity already in progress.
        Busy,

        /// Indicates this object is currently checked.
        Checked,

        /// Indicates this object is collapsed.
        Collapsed,

        /// Indicates that this object no longer has a valid
        /// backing widget        (for instance, if its peer object has been destroyed).
        Defunct,

        /// Indicates the user can change the contents of this object.
        Editable,

        /// Indicates that this object is enabled, i.e. that it
        /// currently reflects some application state. Objects that are "greyed out"
        /// may lack this state, and may lack the <see cref="State.Sensitive"/> if direct
        /// user interaction cannot cause them to acquire `Enabled`.
        ///
        /// See <see cref="State.Sensitive"/>.
        Enabled,

        /// Indicates this object allows progressive
        /// disclosure of its children.
        Expandable,

        /// Indicates this object is expanded.
        Expanded,

        /// Indicates this object can accept keyboard focus,
        /// which means all events resulting from typing on the keyboard will
        /// normally be passed to it when it has focus.
        Focusable,

        /// Indicates this object currently has the keyboard focus.
        Focused,

        /// Indicates that the object has an associated tooltip.
        HasTooltip,

        /// Indicates the orientation of this object is horizontal.
        Horizontal,

        /// Indicates this object is minimized and is
        /// represented only by an icon.
        Iconified,

        /// Indicates something must be done with this object
        /// before the user can interact with an object in a different window.
        Modal,

        /// Indicates this (text) object can contain multiple
        /// lines of text.
        MultiLine,

        /// Indicates this object allows more than one of
        /// its children to be selected at the same time, or in the case of text
        /// objects, that the object supports non-contiguous text selections.
        Multiselectable,

        /// Indicates this object paints every pixel within its
        /// rectangular region. It also indicates an alpha value of unity, if it
        /// supports alpha blending.
        Opaque,

        /// Indicates this object is currently pressed.
        Pressed,

        /// Indicates the size of this object's size is not fixed.
        Resizable,

        /// Indicates this object is the child of an object
        /// that allows its children to be selected and that this child is one of
        /// those children       that can be selected.
        Selectable,

        /// Indicates this object is the child of an object that
        /// allows its children to be selected and that this child is one of those
        /// children that has been selected.
        Selected,

        /// Indicates this object is sensitive, e.g. to user
        /// interaction. `Sensitive` usually accompanies.
        /// <see cref="State.Enabled"/> for user-actionable controls, but may be found in the
        /// absence of <see cref="State.Enabled"/> if the current visible state of the control
        /// is "disconnected" from the application state.  In such cases, direct user
        /// interaction can often result in the object gaining `Sensitive`,
        /// for instance if a user makes an explicit selection using an object whose
        /// current state is ambiguous or undefined.
        ///
        /// See <see cref="State.Enabled"/>, <see cref="State.Indeterminate"/>.
        Sensitive,

        /// Indicates this object, the object's parent, the
        /// object's parent's parent, and so on, are all 'shown' to the end-user,
        /// i.e. subject to "exposure" if blocking or obscuring objects do not
        /// interpose between this object and the top of the window stack.
        Showing,

        /// Indicates this (text) object can contain only a
        /// single line of text.
        SingleLine,

        /// Indicates that the information returned for this object
        /// may no longer be synchronized with the application state.  This can occur
        /// if the object has <see cref="State.Transient"/>, and can also occur towards the
        /// end of the object peer's lifecycle.
        Stale,

        /// Indicates this object is transient.
        Transient,

        /// Indicates the orientation of this object is vertical;
        /// for example this state may appear on such objects as scrollbars, text
        /// objects (with vertical text flow), separators, etc.
        Vertical,

        /// Indicates this object is visible, e.g. has been
        /// explicitly marked for exposure to the user. `Visible` is no
        /// guarantee that the object is actually unobscured on the screen, only that
        /// it is 'potentially' visible, barring obstruction, being scrolled or clipped
        /// out of the field of view, or having an ancestor container that has not yet
        /// made visible. A widget is potentially onscreen if it has both
        /// `Visible` and <see cref="State.Showing"/>. The absence of
        /// `Visible` and <see cref="State.Showing"/> is
        /// semantically equivalent to saying that an object is 'hidden'.
        Visible,

        /// Indicates that "active-descendant-changed"
        /// event is sent when children become 'active' (i.e. are selected or
        /// navigated to onscreen).  Used to prevent need to enumerate all children
        /// in very large containers, like tables. The presence of
        /// `ManagesDescendants` is an indication to the client that the
        /// children should not, and need not, be enumerated by the client.
        /// Objects implementing this state are expected to provide relevant state      
        /// notifications to listening clients, for instance notifications of
        /// visibility changes and activation of their contained child objects, without
        /// the client having previously requested references to those children.
        ManagesDescendants,

        /// Indicates that a check box or other boolean
        /// indicator is in a state other than checked or not checked.
        ///
        /// This usually means that the boolean value reflected or controlled by the
        /// object does not apply consistently to the entire current context.      
        /// For example, a checkbox for the "Bold" attribute of text may have
        /// `Indeterminate` if the currently selected text contains a mixture
        /// of weight attributes. In many cases interacting with a
        /// `Indeterminate` object will cause the context's corresponding
        /// boolean attribute to be homogenized, whereupon the object will lose
        /// `Indeterminate` and a corresponding state-changed event will be
        /// fired.
        Indeterminate,

        /// Indicates that user interaction with this object is
        /// 'required' from the user, for instance before completing the
        /// processing of a form.
        Required,

        /// Indicates that an object's onscreen content
        /// is truncated, e.g. a text value in a spreadsheet cell.
        Truncated,

        /// Indicates this object's visual representation is
        /// dynamic, not static. This state may be applied to an object during an
        /// animated 'effect' and be removed from the object once its visual
        /// representation becomes static. Some applications, notably content viewers,
        /// may not be able to detect all kinds of animated content.  Therefore the
        /// absence of this state should not be taken as
        /// definitive evidence that the object's visual representation is      
        /// static; this state is advisory.
        Animated,

        /// This object has indicated an error condition
        /// due to failure of input validation.  For instance, a form control may
        /// acquire this state in response to invalid or malformed user input.
        InvalidEntry,

        /// This state indicates that the object
        /// in question implements some form of typeahead or       
        /// pre-selection behavior whereby entering the first character of one or more
        /// sub-elements causes those elements to scroll into view or become
        /// selected. Subsequent character input may narrow the selection further as
        /// long as one or more sub-elements match the string. This state is normally
        /// only useful and encountered on objects that implement <see cref="interface.Interface.Selection"/>.
        /// In some cases the typeahead behavior may result in full or partial
        /// completion of the data in the input field, in which case
        /// these input events may trigger text-changed events from the source.
        SupportsAutocompletion,

        /// Indicates that the object in
        /// question supports text selection. It should only be exposed on objects
        /// which implement the <see cref="Interface.Text"/> interface, in order to distinguish this state
        /// from <see cref="State.Selectable"/>, which infers that the object in question is a
        /// selectable child of an object which implements <see cref="interface.Interface.Selection"/>. While
        /// similar, text selection and subelement selection are distinct operations.
        SelectableText,

        /// Indicates that the object in question is
        /// the 'default' interaction object in a dialog, i.e. the one that gets
        /// activated if the user presses "Enter" when the dialog is initially
        /// posted.
        IsDefault,

        /// Indicates that the object (typically a
        /// hyperlink) has already been activated or invoked, with the result that
        /// some backing data has been downloaded or rendered.
        Visited,

        /// Indicates this object has the potential to
        /// be checked, such as a checkbox or toggle-able table cell.
        Checkable,

        /// Indicates that the object has a popup
        /// context menu or sub-level menu which may or may not be
        /// showing. This means that activation renders conditional content.
        /// Note that ordinary tooltips are not considered popups in this
        /// context.
        HasPopup,

        /// Indicates that an object which is <see cref="State.Enabled"/> and
        /// <see cref="State.Sensitive"/> has a value which can be read, but not modified, by the
        /// user.
        ReadOnly,
    }
}
