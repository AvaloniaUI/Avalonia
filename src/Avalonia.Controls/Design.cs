using System.Collections.Generic;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Metadata;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    /// <summary>
    /// Provides attached properties and helpers for design-time support.
    /// </summary>
    public static class Design
    {
        private static Dictionary<object, ITemplate<Control>?>? s_previewWith;

        /// <summary>
        /// Gets a value indicating whether the application is running in design mode.
        /// </summary>
        /// <remarks>
        /// This property is typically used to enable or disable features that should only be available
        /// at design-time, such as sample/preview data.
        /// </remarks>
        public static bool IsDesignMode { get; internal set; }

        /// <summary>
        /// Defines the Height attached property.
        /// </summary>
        public static readonly AttachedProperty<double> HeightProperty = AvaloniaProperty
            .RegisterAttached<Control, double>("Height", typeof (Design));

        /// <summary>
        /// Sets the design-time height for a control.
        /// </summary>
        /// <param name="control">The control to set the height for.</param>
        /// <param name="value">The height value.</param>
        public static void SetHeight(Control control, double value)
        {
            control.SetValue(HeightProperty, value);
        }

        /// <summary>
        /// Gets the design-time height for a control.
        /// </summary>
        /// <param name="control">The control to get the height from.</param>
        /// <returns>The height value.</returns>
        public static double GetHeight(Control control)
        {
            return control.GetValue(HeightProperty);
        }

        /// <summary>
        /// Defines the Width attached property.
        /// </summary>
        public static readonly AttachedProperty<double> WidthProperty = AvaloniaProperty
            .RegisterAttached<Control, double>("Width", typeof(Design));

        /// <summary>
        /// Sets the design-time width for a control.
        /// </summary>
        /// <param name="control">The control to set the width for.</param>
        /// <param name="value">The width value.</param>
        public static void SetWidth(Control control, double value)
        {
            control.SetValue(WidthProperty, value);
        }

        /// <summary>
        /// Gets the design-time width for a control.
        /// </summary>
        /// <param name="control">The control to get the width from.</param>
        /// <returns>The width value.</returns>
        public static double GetWidth(Control control)
        {
            return control.GetValue(WidthProperty);
        }

        /// <summary>
        /// Defines the DataContext attached property.
        /// </summary>
        public static readonly AttachedProperty<object?> DataContextProperty = AvaloniaProperty
            .RegisterAttached<Control, object?>("DataContext", typeof (Design));

        /// <summary>
        /// Sets the design-time data context for a control.
        /// </summary>
        /// <param name="control">The control to set the data context for.</param>
        /// <param name="value">The data context value.</param>
        public static void SetDataContext(Control control, object? value)
        {
            control.SetValue(DataContextProperty, value);
        }

        /// <summary>
        /// Gets the design-time data context for a control.
        /// </summary>
        /// <param name="control">The control to get the data context from.</param>
        /// <returns>The data context value.</returns>
        public static object? GetDataContext(Control control)
        {
            return control.GetValue(DataContextProperty);
        }

        /// <summary>
        /// Defines the PreviewWith attached property.
        /// </summary>
        public static readonly AttachedProperty<Control?> PreviewWithProperty = AvaloniaProperty
            .RegisterAttached<AvaloniaObject, Control?>("PreviewWith", typeof (Design));

        /// <summary>
        /// Sets a preview control for the specified <see cref="AvaloniaObject"/> at design-time.
        /// </summary>
        /// <remarks>
        /// This method allows you to specify a substitute control to be rendered in the previewer
        /// for a given object.
        /// </remarks>
        /// <param name="target">The target object.</param>
        /// <param name="control">The preview control.</param>
        public static void SetPreviewWith(AvaloniaObject target, Control? control)
        {
            target.SetValue(PreviewWithProperty, control);
        }

        /// <summary>
        /// Sets a preview template for the specified <see cref="AvaloniaObject"/> at design-time.
        /// </summary>
        /// <remarks>
        /// This method allows you to specify a substitute control template to be rendered in the previewer
        /// for a given object.
        /// </remarks>
        /// <param name="target">The target object.</param>
        /// <param name="template">The preview template.</param>
        public static void SetPreviewWith(AvaloniaObject target, ITemplate<Control>? template)
        {
            s_previewWith ??= [];
            s_previewWith[target] = template;
        }

        /// <summary>
        /// Sets a preview control for the specified <see cref="ResourceDictionary"/> at design-time.
        /// </summary>
        /// <remarks>
        /// This method allows you to specify a substitute control to be rendered in the previewer
        /// for a given object. ResourceDictionary is attached to that control, displaying real time changes on the control. 
        /// </remarks>
        /// <param name="target">The resource dictionary.</param>
        /// <param name="control">The preview control.</param>
        public static void SetPreviewWith(ResourceDictionary target, Control? control)
        {
            s_previewWith ??= [];
            s_previewWith[target] = control is not null ? new FuncTemplate<Control>(() => control) : null;
        }

        /// <summary>
        /// Sets a preview template for the specified <see cref="ResourceDictionary"/> at design-time.
        /// </summary>
        /// <remarks>
        /// This method allows you to specify a substitute control template to be rendered in the previewer.
        /// ResourceDictionary is attached to that control, displaying real time changes on the control. 
        /// </remarks>
        /// <param name="target">The resource dictionary.</param>
        /// <param name="template">The preview template.</param>
        public static void SetPreviewWith(ResourceDictionary target, ITemplate<Control>? template)
        {
            s_previewWith ??= [];
            s_previewWith[target] = template;
        } 

        /// <summary>
        /// Sets a preview template for the specified <see cref="IDataTemplate"/> at design-time.
        /// </summary>
        /// <remarks>
        /// This method allows you to specify a substitute control template to be rendered in the previewer.
        /// Template must return ContentControl, and IDataTemplate will be set assigned to ContentControl.ContentTemplate property.
        /// </remarks>
        /// <param name="target">The data template.</param>
        /// <param name="template">The preview template.</param>
        public static void SetPreviewWith(IDataTemplate target, ITemplate<ContentControl>? template)
        {
            s_previewWith ??= [];
            s_previewWith[target] = template is not null ? new FuncTemplate<Control>(template.Build) : null;
        }

        /// <summary>
        /// Gets the preview control for the specified <see cref="AvaloniaObject"/> at design-time.
        /// </summary>
        /// <param name="target">The target object.</param>
        /// <returns>The preview control, or null.</returns>
        public static Control? GetPreviewWith(AvaloniaObject target)
        {
            return s_previewWith?[target]?.Build();
        }

        /// <summary>
        /// Gets the preview control for the specified <see cref="ResourceDictionary"/> at design-time.
        /// </summary>
        /// <param name="target">The resource dictionary.</param>
        /// <returns>The preview control, or null.</returns>
        public static Control? GetPreviewWith(ResourceDictionary target)
        {
            return s_previewWith?[target]?.Build();
        }

        /// <summary>
        /// Gets the preview control for the specified <see cref="IDataTemplate"/> at design-time.
        /// </summary>
        /// <param name="target">The data template.</param>
        /// <returns>The preview control, or null.</returns>
        public static Control? GetPreviewWith(IDataTemplate target)
        {
            return s_previewWith?[target]?.Build();
        }

        /// <summary>
        /// Identifies the DesignStyle attached property for design-time use.
        /// </summary>
        /// <remarks>
        /// This property allows you to apply a style to a control only at design-time, enabling
        /// custom visualizations or highlighting in the designer without affecting the runtime appearance.
        /// </remarks>
        public static readonly AttachedProperty<IStyle> DesignStyleProperty = AvaloniaProperty
            .RegisterAttached<Control, IStyle>("DesignStyle", typeof(Design));

        /// <summary>
        /// Sets the design-time style for a control.
        /// </summary>
        /// <param name="control">The control to set the style for.</param>
        /// <param name="value">The style value.</param>
        public static void SetDesignStyle(Control control, IStyle value)
        {
            control.SetValue(DesignStyleProperty, value);
        }

        /// <summary>
        /// Gets the design-time style for a control.
        /// </summary>
        /// <param name="control">The control to get the style from.</param>
        /// <returns>The style value.</returns>
        public static IStyle GetDesignStyle(Control control)
        {
            return control.GetValue(DesignStyleProperty);
        }

        [PrivateApi]
        public static void ApplyDesignModeProperties(Control target, Control source)
        {
            if (source.IsSet(WidthProperty))
                target.Bind(Layoutable.WidthProperty, target.GetBindingObservable(WidthProperty));
            if (source.IsSet(HeightProperty))
                target.Bind(Layoutable.HeightProperty, target.GetBindingObservable(HeightProperty));
            if (source.IsSet(DataContextProperty))
                target.Bind(StyledElement.DataContextProperty, target.GetBindingObservable(DataContextProperty));
            if (source.IsSet(DesignStyleProperty))
                target.Styles.Add(source.GetValue(DesignStyleProperty));
        }
    }
}
