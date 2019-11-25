using System;

namespace Avalonia.Controls
{
    public class NativeMenuItemBase : AvaloniaObject, IDataContextProvider
    {
        private NativeMenu _parent;

        internal NativeMenuItemBase()
        {

        }


        public static readonly DirectProperty<NativeMenuItem, NativeMenu> ParentProperty =
            AvaloniaProperty.RegisterDirect<NativeMenuItem, NativeMenu>("Parent", o => o.Parent, (o, v) => o.Parent = v);

        public NativeMenu Parent
        {
            get => _parent;
            set => SetAndRaise(ParentProperty, ref _parent, value);
        }

        /// <summary>
        /// Defines the <see cref="DataContext"/> property.
        /// </summary>
        public static readonly StyledProperty<object> DataContextProperty =
            AvaloniaProperty.Register<Application, object>(
                nameof(DataContext));

        /// <summary>
        /// Gets or sets the controls's data context.
        /// </summary>
        /// <remarks>
        /// The data context property
        /// specifies the default object that will
        /// be used for data binding.
        /// </remarks>
        public object DataContext
        {
            get { return GetValue(DataContextProperty); }
            set { SetValue(DataContextProperty, value); }
        }
    }
}
