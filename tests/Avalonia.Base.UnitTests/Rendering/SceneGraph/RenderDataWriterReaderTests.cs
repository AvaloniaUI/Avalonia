using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Rendering.Composition.Drawing;
using Xunit;

namespace Avalonia.Base.UnitTests.Rendering.SceneGraph
{
    public class RenderDataWriterReaderTests
    {
        [Fact]
        public void Primitives_Round_Trip()
        {
            var writer = new RenderDataWriter();
            try
            {
                writer.Write<byte>(200);
                writer.WriteOpcode(RenderDataOpcode.DrawGeometry);
                writer.Write(-123456);
                writer.Write(4000000000u);
                writer.Write(3.14159);
                writer.Write(true);
                writer.Write(false);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(200, reader.Read<byte>());
                Assert.Equal(RenderDataOpcode.DrawGeometry, reader.Read<RenderDataOpcode>());
                Assert.Equal(-123456, reader.Read<int>());
                Assert.Equal(4000000000u, reader.Read<uint>());
                Assert.Equal(3.14159, reader.Read<double>());
                Assert.True(reader.Read<bool>());
                Assert.False(reader.Read<bool>());
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void Geometric_Structs_Round_Trip()
        {
            var point = new Point(1, 2);
            var vector = new Vector(3, 4);
            var rect = new Rect(5, 6, 7, 8);
            var roundedRect = new RoundedRect(new Rect(1, 2, 30, 40),
                new Vector(1, 1), new Vector(2, 2), new Vector(3, 3), new Vector(4, 4));
            var matrix = new Matrix(1, 2, 3, 4, 5, 6, 7, 8, 9);

            var writer = new RenderDataWriter();
            try
            {
                writer.Write(point);
                writer.Write(vector);
                writer.Write(rect);
                writer.Write(roundedRect);
                writer.Write(matrix);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(point, reader.Read<Point>());
                Assert.Equal(vector, reader.Read<Vector>());
                Assert.Equal(rect, reader.Read<Rect>());

                var readRounded = reader.Read<RoundedRect>();
                Assert.Equal(roundedRect.Rect, readRounded.Rect);
                Assert.Equal(roundedRect.RadiiTopLeft, readRounded.RadiiTopLeft);
                Assert.Equal(roundedRect.RadiiTopRight, readRounded.RadiiTopRight);
                Assert.Equal(roundedRect.RadiiBottomRight, readRounded.RadiiBottomRight);
                Assert.Equal(roundedRect.RadiiBottomLeft, readRounded.RadiiBottomLeft);

                Assert.Equal(matrix, reader.Read<Matrix>());
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void Color_And_BoxShadow_Round_Trip()
        {
            var color = Color.FromArgb(10, 20, 30, 40);
            var shadow = new BoxShadow
            {
                OffsetX = 1.5,
                OffsetY = -2.5,
                Blur = 3,
                Spread = 4,
                Color = Color.FromArgb(255, 1, 2, 3),
                IsInset = true
            };

            var writer = new RenderDataWriter();
            try
            {
                writer.Write(color);
                writer.Write(shadow);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(color, reader.Read<Color>());

                var readShadow = reader.Read<BoxShadow>();
                Assert.Equal(shadow.OffsetX, readShadow.OffsetX);
                Assert.Equal(shadow.OffsetY, readShadow.OffsetY);
                Assert.Equal(shadow.Blur, readShadow.Blur);
                Assert.Equal(shadow.Spread, readShadow.Spread);
                Assert.Equal(shadow.Color, readShadow.Color);
                Assert.Equal(shadow.IsInset, readShadow.IsInset);
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void RenderOptions_And_TextOptions_Round_Trip()
        {
            var renderOptions = new RenderOptions
            {
#pragma warning disable CS0618
                TextRenderingMode = TextRenderingMode.Antialias,
#pragma warning restore CS0618
                BitmapInterpolationMode = BitmapInterpolationMode.HighQuality,
                EdgeMode = EdgeMode.Aliased,
                BitmapBlendingMode = BitmapBlendingMode.Plus,
                RequiresFullOpacityHandling = true
            };
            var textOptions = new TextOptions
            {
                TextRenderingMode = TextRenderingMode.SubpixelAntialias,
                TextHintingMode = TextHintingMode.Light,
                BaselinePixelAlignment = BaselinePixelAlignment.Aligned
            };

            var writer = new RenderDataWriter();
            try
            {
                writer.Write(renderOptions);
                writer.Write(textOptions);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(renderOptions, reader.Read<RenderOptions>());
                Assert.Equal(textOptions, reader.Read<TextOptions>());
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Theory]
        [InlineData(null)]
        [InlineData(true)]
        [InlineData(false)]
        public void Nullable_Boolean_Round_Trips_All_Three_States(bool? value)
        {
            var writer = new RenderDataWriter();
            try
            {
                writer.Write(new RenderOptions { RequiresFullOpacityHandling = value });

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(value, reader.Read<RenderOptions>().RequiresFullOpacityHandling);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void Length_Tracks_Written_Bytes()
        {
            var writer = new RenderDataWriter();
            try
            {
                Assert.Equal(0, writer.Length);
                writer.Write<byte>(1);
                Assert.Equal(1, writer.Length);
                writer.Write(2);
                Assert.Equal(5, writer.Length);
                writer.Write(3d);
                Assert.Equal(13, writer.Length);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void Writer_Grows_Buffer_To_Fit_Large_Payloads()
        {
            var writer = new RenderDataWriter();
            try
            {
                for (var i = 0; i < 1000; i++)
                    writer.Write(i);

                Assert.Equal(4000, writer.Length);

                var reader = new RenderDataReader(writer.Written);
                for (var i = 0; i < 1000; i++)
                    Assert.Equal(i, reader.Read<int>());
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }

        [Fact]
        public void Payload_Auto_Prepends_Opcode()
        {
            var writer = new RenderDataWriter();
            try
            {
                writer.WritePayload(new PushOpacityPayload { Opacity = 0.5 });

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(RenderDataOpcode.PushOpacity, reader.Read<RenderDataOpcode>());
                var payload = reader.Read<PushOpacityPayload>();
                Assert.Equal(0.5, payload.Opacity);
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
