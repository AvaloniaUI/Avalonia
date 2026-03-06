using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace ControlCatalog.Pages.OpenGl;

public partial class GlPageKnobs : UserControl
{
    public GlPageKnobs()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private float _yaw;

    public static readonly DirectProperty<GlPageKnobs, float> YawProperty =
        AvaloniaProperty.RegisterDirect<GlPageKnobs, float>("Yaw", o => o.Yaw, (o, v) => o.Yaw = v);

    public float Yaw
    {
        get => _yaw;
        set => SetAndRaise(YawProperty, ref _yaw, value);
    }

    private float _pitch;

    public static readonly DirectProperty<GlPageKnobs, float> PitchProperty =
        AvaloniaProperty.RegisterDirect<GlPageKnobs, float>("Pitch", o => o.Pitch, (o, v) => o.Pitch = v);

    public float Pitch
    {
        get => _pitch;
        set => SetAndRaise(PitchProperty, ref _pitch, value);
    }


    private float _roll;

    public static readonly DirectProperty<GlPageKnobs, float> RollProperty =
        AvaloniaProperty.RegisterDirect<GlPageKnobs, float>("Roll", o => o.Roll, (o, v) => o.Roll = v);

    public float Roll
    {
        get => _roll;
        set => SetAndRaise(RollProperty, ref _roll, value);
    }


    private float _disco;

    public static readonly DirectProperty<GlPageKnobs, float> DiscoProperty =
        AvaloniaProperty.RegisterDirect<GlPageKnobs, float>("Disco", o => o.Disco, (o, v) => o.Disco = v);

    public float Disco
    {
        get => _disco;
        set => SetAndRaise(DiscoProperty, ref _disco, value);
    }

    private string _info = string.Empty;

    public static readonly DirectProperty<GlPageKnobs, string> InfoProperty =
        AvaloniaProperty.RegisterDirect<GlPageKnobs, string>("Info", o => o.Info, (o, v) => o.Info = v);

    public string Info
    {
        get => _info;
        private set => SetAndRaise(InfoProperty, ref _info, value);
    }
}