using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;

namespace Avalonia.Controls
{
    /// <summary>
    /// Label control. Focuses <see cref="Target"/> on pointer click or access key press (Alt + accessKey)
    /// </summary>
    public class Label : ContentControl
    {
        /// <summary>
        /// Defines the <see cref="Target"/> Direct property
        /// </summary>
        public static readonly DirectProperty<Label, IInputElement> TargetProperty =
            AvaloniaProperty.RegisterDirect<Label, IInputElement>(nameof(Target), lbl => lbl.Target, (lbl, inp) => lbl.Target = inp);

        /// <summary>
        /// Label focus target storage field
        /// </summary>
        private IInputElement _target;

        /// <summary>
        /// Flag indicating that custom template was provided to show Access Key
        /// </summary>
        private bool _isContentTemplateProvided;

        /// <summary>
        /// Custom template to provide for Access Key show
        /// </summary>
        private FuncDataTemplate<string> _accessTextTemplate;

        /// <summary>
        /// Label focus Target
        /// </summary>
        public IInputElement Target
        {
            get => _target;
            set => SetAndRaise(TargetProperty, ref _target, value);
        }

        /// <summary>
        /// Initializes instance of <see cref="Label"/> control
        /// </summary>
        public Label()
        {
            this.GetObservable(ContentProperty).Subscribe(ContentChanged);
            _accessTextTemplate = new FuncDataTemplate<string>(
                (val, ns) =>
                {
                    var accessText = new AccessText
                    {
                        [!AccessText.TextProperty] = new Binding(),
                    };
                    accessText.AddHandler(AccessKeyHandler.AccessKeyPressedEvent, (s,a) => LabelActivated());
                    return accessText;
                });

            
        }

        /// <summary>
        /// Method which focuses <see cref="Target"/> input element
        /// </summary>
        private void LabelActivated()
        {
            Target?.Focus();
        }

        /// <summary>
        /// Handler for Content property change event
        /// </summary>
        /// <param name="obj">new value</param>
        private void ContentChanged(object obj)
        {
            if (obj is string)
            {
                if (ContentTemplate == null)
                {
                    _isContentTemplateProvided = true;
                    ContentTemplate = _accessTextTemplate;
                }
            }
            else if (_isContentTemplateProvided)
            {
                ContentTemplate = null;
                _isContentTemplateProvided = false;
            }
        }

        /// <summary>
        /// Handler of <see cref="IInputElement.PointerPressed"/> event
        /// </summary>
        /// <param name="e">Event Arguments</param>
        protected override void OnPointerPressed(PointerPressedEventArgs e)
        {
            if (e.GetCurrentPoint(this).Properties.PointerUpdateKind == PointerUpdateKind.LeftButtonPressed)
            {
                LabelActivated();
                e.Handled = true;
            }
            else
            {
                base.OnPointerPressed(e);
            }
        }
    }
}
