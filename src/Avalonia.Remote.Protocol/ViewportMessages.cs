namespace Avalonia.Remote.Protocol.Viewport
{
    public enum PixelFormat
    {
        Rgb565,
        Rgba8888,
        Bgra8888,
        MaxValue = Bgra8888
    }

    [AvaloniaRemoteMessageGuid("6E3C5310-E2B1-4C3D-8688-01183AA48C5B")]
    public class MeasureViewportMessage
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    [AvaloniaRemoteMessageGuid("BD7A8DE6-3DB8-4A13-8583-D6D4AB189A31")]
    public class ClientViewportAllocatedMessage
    {
        public double Width { get; set; }
        public double Height { get; set; }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
    }

    [AvaloniaRemoteMessageGuid("9B47B3D8-61DF-4C38-ACD4-8C1BB72554AC")]
    public class RequestViewportResizeMessage
    {
        public double Width { get; set; }
        public double Height { get; set; }
    }

    [AvaloniaRemoteMessageGuid("63481025-7016-43FE-BADC-F2FD0F88609E")]
    public class ClientSupportedPixelFormatsMessage
    {
        public PixelFormat[] Formats { get; set; }
    }

    [AvaloniaRemoteMessageGuid("7A3c25d3-3652-438D-8EF1-86E942CC96C0")]
    public class ClientRenderInfoMessage
    {
        public double DpiX { get; set; }
        public double DpiY { get; set; }
    }

    [AvaloniaRemoteMessageGuid("68014F8A-289D-4851-8D34-5367EDA7F827")]
    public class FrameReceivedMessage
    {
        public long SequenceId { get; set; }
    }


    [AvaloniaRemoteMessageGuid("F58313EE-FE69-4536-819D-F52EDF201A0E")]
    public class FrameMessage
    {
        public long SequenceId { get; set; }
        public PixelFormat Format { get; set; }
        public byte[] Data { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Stride { get; set; }
        public double DpiX { get; set; }
        public double DpiY { get; set; }
    }

}
