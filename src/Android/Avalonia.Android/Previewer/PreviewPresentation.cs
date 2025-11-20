using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Avalonia.Media;

namespace Avalonia.Android.Previewer
{
    internal class PreviewPresentation : Presentation
    {
        private readonly Context? _outerContext;
        private readonly int _port;
        private readonly Assembly? _assembly;
        private PreviewerConnection? _connection;
        private Preview? _preview;
        private PreviewImageReader? _reader;
        private AvaloniaView? _view;
        private float _renderScaling = 1;

        public AvaloniaView? View { get => _view; set => _view = value; }

        public float RenderScaling
        {
            get => _renderScaling;
            internal set
            {
                _renderScaling = value;
                if(PreviewDisplay.Instance?.Surface is { } surface)
                {
                    surface.Scaling = _renderScaling;
                }

                _preview?.Invalidate();
            }
        }

        public PreviewPresentation(Context? outerContext, Display? display, int port, Assembly? assembly) : base(outerContext, display)
        {
            _outerContext = outerContext;
            _port = port;
            _assembly = assembly;
        }

        [UnconditionalSuppressMessage("Trimming", "IL2026:Members annotated with 'RequiresUnreferencedCodeAttribute' require dynamic access otherwise can break functionality when trimming application code", 
            Justification = "<Pending>")]
        protected override void OnCreate(Bundle? savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            var display = PreviewDisplay.Instance!;

            View = new AvaloniaView(Context);
            _connection = new PreviewerConnection(this);
            _preview = new Preview(this, View.TopLevel, _assembly);
            _reader = new PreviewImageReader(display.Surface!, _connection);
            _connection.Listen(_port);
            SetContentView(View);
        }

        public async Task UpdateXaml(string xaml)
        {
            if (_preview != null)
                await _preview.UpdateXamlAsync(xaml);
        }
    }
}
