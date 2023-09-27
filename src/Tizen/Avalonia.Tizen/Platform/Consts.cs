namespace Avalonia.Tizen.Platform;
internal static class Consts
{
    public const int DpiX = 96;
    public const int DpiY = 96;
    public static readonly Vector Dpi = new Vector(DpiX, DpiY);

    public const string VertexShader =
           "attribute mediump vec2 aPosition;\n" +
           "varying mediump vec2 vTexCoord;\n" +
           "uniform highp mat4 uMvpMatrix;\n" +
           "uniform mediump vec3 uSize;\n" +
           "varying mediump vec2 sTexCoordRect;\n" +
           "void main()\n" +
           "{\n" +
           "   gl_Position = uMvpMatrix * vec4(aPosition * uSize.xy, 0.0, 1.0);\n" +
           "   vTexCoord = aPosition + vec2(0.5);\n" +
           "}\n";

    public const string FragmentShader =
        "#extension GL_OES_EGL_image_external:require\n" +
        "uniform lowp vec4 uColor;\n" +
        "varying mediump vec2 vTexCoord;\n" +
        "uniform samplerExternalOES sTexture;\n" +
        "void main()\n" +
        "{\n" +
        "   gl_FragColor = texture2D(sTexture, vTexCoord) * uColor;\n" +
        "}\n";
}
