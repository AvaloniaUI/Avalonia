using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Controls.Primitives;
using Avalonia.Logging;
using Avalonia.Styling;

namespace Avalonia.Controls
{
    public class HyperlinkButton: Button, IStyleable
    {
        public const string pcVisited = ":visited";

        Type IStyleable.StyleKey => typeof(HyperlinkButton);

        public static readonly DirectProperty<HyperlinkButton, Uri?> NavigateUriProperty =
            AvaloniaProperty.RegisterDirect<HyperlinkButton, Uri?>(nameof(NavigateUri), x => x.NavigateUri, (x, v) => x.NavigateUri = v);

        private Uri? _navigateUri;
        public Uri? NavigateUri
        {
            get => _navigateUri;
            set => SetAndRaise(NavigateUriProperty, ref _navigateUri, value);
        }

        public static readonly StyledProperty<bool> IsVisitedProperty =
            AvaloniaProperty.Register<HyperlinkButton, bool>(nameof(IsVisited));
        public bool IsVisited
        {
            get => GetValue(IsVisitedProperty);
            set => SetValue(IsVisitedProperty, value);
        }

        public static readonly DirectProperty<HyperlinkButton, bool> SetVisitedOnClickProperty =
            AvaloniaProperty.RegisterDirect<HyperlinkButton, bool>(nameof(SetVisitedOnClick), x => x.SetVisitedOnClick, (x, v) => x.SetVisitedOnClick = v);
        private bool _setVisitedOnClick;
        public bool SetVisitedOnClick
        {
            get => _setVisitedOnClick;
            set => SetAndRaise(SetVisitedOnClickProperty, ref _setVisitedOnClick, value);
        }

        static HyperlinkButton()
        {
            IsVisitedProperty.Changed.AddClassHandler<HyperlinkButton>((x,e)=>x.OnIsVisitedChanged(e));
        }

        protected override void OnClick()
        {
            base.OnClick();
            if (NavigateUri is not null)
            {
                try
                {
                    Process.Start(new ProcessStartInfo(NavigateUri.ToString()) { UseShellExecute = true });
                    if (SetVisitedOnClick)
                    {
                        IsVisited = true;
                    }
                }
                catch
                {
                    Logger.TryGet(LogEventLevel.Error, $"Unable to open Uri {NavigateUri}");
                }
            }
        }

        private void OnIsVisitedChanged(AvaloniaPropertyChangedEventArgs args)
        {
            bool newValue = args.GetNewValue<bool>();
            PseudoClasses.Set(pcVisited, newValue);
        }
    }
}
