using System;
using System.Collections.Generic;
using Avalonia.Automation.Provider;

namespace Avalonia.Automation.Peers
{
    public enum AutomationControlType
    {
        None,
        Button,
        Calendar,
        CheckBox,
        ComboBox,
        ComboBoxItem,
        Edit,
        Hyperlink,
        Image,
        ListItem,
        List,
        Menu,
        MenuBar,
        MenuItem,
        ProgressBar,
        RadioButton,
        ScrollBar,
        Slider,
        Spinner,
        StatusBar,
        Tab,
        TabItem,
        Text,
        ToolBar,
        ToolTip,
        Tree,
        TreeItem,
        Custom,
        Group,
        Thumb,
        DataGrid,
        DataItem,
        Document,
        SplitButton,
        Window,
        Pane,
        Header,
        HeaderItem,
        Table,
        TitleBar,
        Separator,
    }

    public enum AutomationLandmarkType
    {
        Banner,
        Complementary,
        ContentInfo,
        Region,
        Form,
        Main,
        Navigation,
        Search,
    }

    /// <summary>
    /// Provides a base class that exposes an element to UI Automation.
    /// </summary>
    public abstract class AutomationPeer
    {
        /// <summary>
        /// Attempts to bring the element associated with the automation peer into view.
        /// </summary>
        public void BringIntoView() => BringIntoViewCore();

        /// <summary>
        /// Gets the accelerator key combinations for the element that is associated with the UI
        /// Automation peer.
        /// </summary>
        /// <remarks>
        /// An accelerator key (sometimes called a shortcut key) exposes a key combination for
        /// which can be used to invoke an action, for example, an "Open..." menu item on Windows
        /// often has an accelerator key of "Ctrl+O".
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_AcceleratorKeyPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string? GetAcceleratorKey() => GetAcceleratorKeyCore();

        /// <summary>
        /// Gets the access key for the element that is associated with the automation peer.
        /// </summary>
        /// <remarks>
        /// An access key (sometimes called a mnemonic) is a character in the text of a menu, menu
        /// item, or label of a control such as a button, that activates the associated function.
        /// For example, to open the File menu, for which the access key is typically F, the user
        /// would press ALT+F.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_AccessKeyPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string? GetAccessKey() => GetAccessKeyCore();

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <remarks>
        /// Gets the type of the element.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_ControlTypePropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityRole</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public AutomationControlType GetAutomationControlType() => GetControlTypeOverrideCore();

        /// <summary>
        /// Gets the automation ID of the element that is associated with the UI Automation peer.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_AutomationIdPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityIdentifier</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string? GetAutomationId() => GetAutomationIdCore();

        /// <summary>
        /// Gets the bounding rectangle of the element that is associated with the automation peer
        /// in top-level coordinates.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRawElementProviderFragment.get_BoundingRectangle</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityFrame</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public Rect GetBoundingRectangle() => GetBoundingRectangleCore();

        /// <summary>
        /// Gets the child automation peers.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRawElementProviderFragment.Navigate</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityChildren</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public IReadOnlyList<AutomationPeer> GetChildren() => GetOrCreateChildrenCore();

        /// <summary>
        /// Gets a string that describes the class of the element.
        /// </summary>
        /// <remarks>
        /// A string containing the class name for the automation element as assigned by the
        /// control developer. This is often the C# class name of the control.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_ClassNamePropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string GetClassName() => GetClassNameCore() ?? string.Empty;

        /// <summary>
        /// Gets the automation peer for the label that is targeted to the element.
        /// </summary>
        /// <remarks>
        /// Identifies an automation peer representing an element which that contains the text
        /// label for this element.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_LabeledByPropertyId</c> (not yet implemented)</description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityLabelUIElements</c> (not yet implemented)</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public AutomationPeer? GetLabeledBy() => GetLabeledByCore();

        /// <summary>
        /// Gets a human-readable localized string that represents the type of the control that is
        /// associated with this automation peer.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_LocalizedControlTypePropertyId</c> (not yet implemented)</description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string GetLocalizedControlType() => GetLocalizedControlTypeCore();

        /// <summary>
        /// Gets text that describes the element that is associated with this automation peer.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_NamePropertyId</c> (not yet implemented)</description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>
        ///       When the control type is <see cref="AutomationControlType.Text"/>, this value is
        ///       exposed by both <c>NSAccessibilityProtocol.accessibilityTitle</c> and
        ///       <c>NSAccessibilityProtocol.accessibilityValue</c> .
        ///     </description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string GetName() => GetNameCore() ?? string.Empty;

        /// <summary>
        /// Gets text that provides help for the element that is associated with this automation peer.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_HelpTextPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityHelp</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public string GetHelpText() => GetHelpTextCore() ?? string.Empty;

        /// <summary>
        /// Gets the control type for the element that is associated with the UI Automation peer.
        /// </summary>
        /// <remarks>
        /// Gets the type of the element.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_LandmarkTypePropertyId</c>, <c>UIA_LocalizedLandmarkTypePropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityRole</c>, <c>NSAccessibilityProtocol.accessibilitySubrole</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public AutomationLandmarkType? GetLandmarkType() => GetLandmarkTypeCore();

        /// <summary>
        /// Gets the heading level that is associated with this automation peer.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_HeadingLevelPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityValue</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public int GetHeadingLevel() => GetHeadingLevelCore();

        /// <summary>
        /// Gets the <see cref="AutomationPeer"/> that is the parent of this <see cref="AutomationPeer"/>.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>IRawElementProviderFragment.Navigate</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityParent</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public AutomationPeer? GetParent() => GetParentCore();

        /// <summary>
        /// Gets the <see cref="AutomationPeer"/> that is the root of this <see cref="AutomationPeer"/>'s
        /// visual tree.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description>No mapping, but used internally to translate coordinates.</description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.accessibilityTopLevelUIElement</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public AutomationPeer? GetVisualRoot() => GetVisualRootCore();

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation
        /// peer currently has keyboard focus.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_HasKeyboardFocusPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.isAccessibilityFocused</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool HasKeyboardFocus() => HasKeyboardFocusCore();

        /// <summary>
        /// Gets a value that indicates whether the element that is associated with this automation
        /// peer contains data that is presented to the user.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_IsContentElementPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool IsContentElement() => IsContentElementOverrideCore();

        /// <summary>
        /// Gets a value that indicates whether the element is understood by the user as
        /// interactive or as contributing to the logical structure of the control in the GUI.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_IsControlElementPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.isAccessibilityElement</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool IsControlElement() => IsControlElementOverrideCore();

        /// <summary>
        /// Gets a value indicating whether the control is enabled for user interaction.
        /// </summary>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_IsEnabledPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description><c>NSAccessibilityProtocol.isAccessibilityEnabled</c></description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool IsEnabled() => IsEnabledCore();

        /// <summary>
        /// Gets a value that indicates whether the element can accept keyboard focus.
        /// </summary>
        /// <returns></returns>
        /// <remarks>
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_IsKeyboardFocusablePropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool IsKeyboardFocusable() => IsKeyboardFocusableCore();

        /// <summary>
        /// Gets a value that indicates whether an element is off the screen.
        /// </summary>
        /// <remarks>
        /// This property does not indicate whether the element is visible. In some circumstances,
        /// an element is on the screen but is still not visible. For example, if the element is
        /// on the screen but obscured by other elements, it might not be visible. In this case,
        /// the method returns false.
        /// 
        /// <list type="table">
        ///   <item>
        ///     <term>Windows</term>
        ///     <description><c>UIA_IsOffscreenPropertyId</c></description>
        ///   </item>
        ///   <item>
        ///     <term>macOS</term>
        ///     <description>No mapping.</description>
        ///   </item>
        /// </list>
        /// </remarks>
        public bool IsOffscreen() => IsOffscreenCore();

        /// <summary>
        /// Sets the keyboard focus on the element that is associated with this automation peer.
        /// </summary>
        public void SetFocus() => SetFocusCore();

        /// <summary>
        /// Shows the context menu for the element that is associated with this automation peer.
        /// </summary>
        /// <returns>true if a context menu is present for the element; otherwise false.</returns>
        public bool ShowContextMenu() => ShowContextMenuCore();

        /// <summary>
        /// Tries to get a provider of the specified type from the peer.
        /// </summary>
        /// <typeparam name="T">The provider type.</typeparam>
        /// <returns>The provider, or null if not implemented on this peer.</returns>
        public T? GetProvider<T>() => (T?)GetProviderCore(typeof(T));

        /// <summary>
        /// Occurs when the children of the automation peer have changed.
        /// </summary>
        public event EventHandler? ChildrenChanged;

        /// <summary>
        /// Occurs when a property value of the automation peer has changed.
        /// </summary>
        public event EventHandler<AutomationPropertyChangedEventArgs>? PropertyChanged;

        /// <summary>
        /// Raises an event to notify the automation client the children of the peer have changed.
        /// </summary>
        protected void RaiseChildrenChangedEvent() => ChildrenChanged?.Invoke(this, EventArgs.Empty);

        /// <summary>
        /// Raises an event to notify the automation client of a changed property value.
        /// </summary>
        /// <param name="property">The property that changed.</param>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        public void RaisePropertyChangedEvent(
            AutomationProperty property,
            object? oldValue,
            object? newValue)
        {
            PropertyChanged?.Invoke(this, new AutomationPropertyChangedEventArgs(property, oldValue, newValue));
        }

        protected virtual string GetLocalizedControlTypeCore()
        {
            var controlType = GetAutomationControlType();

            return controlType switch
            {
                AutomationControlType.CheckBox => "check box",
                AutomationControlType.ComboBox => "combo box",
                AutomationControlType.ListItem => "list item",
                AutomationControlType.MenuBar => "menu bar",
                AutomationControlType.MenuItem => "menu item",
                AutomationControlType.ProgressBar => "progress bar",
                AutomationControlType.RadioButton => "radio button",
                AutomationControlType.ScrollBar => "scroll bar",
                AutomationControlType.StatusBar => "status bar",
                AutomationControlType.TabItem => "tab item",
                AutomationControlType.ToolBar => "toolbar",
                AutomationControlType.ToolTip => "tooltip",
                AutomationControlType.TreeItem => "tree item",
                AutomationControlType.Custom => "custom",
                AutomationControlType.DataGrid => "data grid",
                AutomationControlType.DataItem => "data item",
                AutomationControlType.SplitButton => "split button",
                AutomationControlType.HeaderItem => "header item",
                AutomationControlType.TitleBar => "title bar",
                AutomationControlType.None => (GetLandmarkType()?.ToString() ?? controlType.ToString()).ToLowerInvariant(),
                _ => controlType.ToString().ToLowerInvariant(),
            };
        }

        protected abstract void BringIntoViewCore();
        protected abstract string? GetAcceleratorKeyCore();
        protected abstract string? GetAccessKeyCore();
        protected abstract AutomationControlType GetAutomationControlTypeCore();
        protected abstract string? GetAutomationIdCore();
        protected abstract Rect GetBoundingRectangleCore();
        protected abstract IReadOnlyList<AutomationPeer> GetOrCreateChildrenCore();
        protected abstract string GetClassNameCore();
        protected abstract AutomationPeer? GetLabeledByCore();
        protected abstract string? GetNameCore();
        protected virtual string? GetHelpTextCore() => null;
        protected virtual AutomationLandmarkType? GetLandmarkTypeCore() => null;
        protected virtual int GetHeadingLevelCore() => 0;
        protected abstract AutomationPeer? GetParentCore();
        protected abstract bool HasKeyboardFocusCore();
        protected abstract bool IsContentElementCore();
        protected abstract bool IsControlElementCore();
        protected abstract bool IsEnabledCore();
        protected abstract bool IsKeyboardFocusableCore();
        protected virtual bool IsOffscreenCore() => false;
        protected abstract void SetFocusCore();
        protected abstract bool ShowContextMenuCore();

        protected virtual AutomationControlType GetControlTypeOverrideCore()
        {
            return GetAutomationControlTypeCore();
        }

        protected virtual AutomationPeer? GetVisualRootCore()
        {
            var peer = this;
            var parent = peer.GetParent();

            while (peer.GetProvider<IRootProvider>() is null && parent is not null)
            {
                peer = parent;
                parent = peer.GetParent();
            }

            return peer;
        }


        protected virtual bool IsContentElementOverrideCore()
        {
            return IsControlElement() && IsContentElementCore();
        }

        protected virtual bool IsControlElementOverrideCore()
        {
            return IsControlElementCore();
        }

        protected virtual object? GetProviderCore(Type providerType)
        {
            return providerType.IsAssignableFrom(this.GetType()) ? this : null;
        }

        protected internal abstract bool TrySetParent(AutomationPeer? parent);

        protected void EnsureEnabled()
        {
            if (!IsEnabled())
                throw new ElementNotEnabledException();
        }
    }
}
