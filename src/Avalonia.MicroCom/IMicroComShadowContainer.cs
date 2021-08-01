namespace Avalonia.MicroCom
{
    public interface IMicroComShadowContainer
    {
        MicroComShadow Shadow { get; set; }
        void OnReferencedFromNative();
        void OnUnreferencedFromNative();
    }
}
