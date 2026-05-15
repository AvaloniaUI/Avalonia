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
                writer.WriteByte(200);
                writer.WriteOpcode(RenderDataOpcode.DrawGeometry);
                writer.WriteInt32(-123456);
                writer.WriteUInt32(4000000000);
                writer.WriteDouble(3.14159);
                writer.WriteBoolean(true);
                writer.WriteBoolean(false);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(200, reader.ReadByte());
                Assert.Equal(RenderDataOpcode.DrawGeometry, reader.ReadOpcode());
                Assert.Equal(-123456, reader.ReadInt32());
                Assert.Equal(4000000000, reader.ReadUInt32());
                Assert.Equal(3.14159, reader.ReadDouble());
                Assert.True(reader.ReadBoolean());
                Assert.False(reader.ReadBoolean());
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
                writer.WritePoint(point);
                writer.WriteVector(vector);
                writer.WriteRect(rect);
                writer.WriteRoundedRect(roundedRect);
                writer.WriteMatrix(matrix);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(point, reader.ReadPoint());
                Assert.Equal(vector, reader.ReadVector());
                Assert.Equal(rect, reader.ReadRect());

                var readRounded = reader.ReadRoundedRect();
                Assert.Equal(roundedRect.Rect, readRounded.Rect);
                Assert.Equal(roundedRect.RadiiTopLeft, readRounded.RadiiTopLeft);
                Assert.Equal(roundedRect.RadiiTopRight, readRounded.RadiiTopRight);
                Assert.Equal(roundedRect.RadiiBottomRight, readRounded.RadiiBottomRight);
                Assert.Equal(roundedRect.RadiiBottomLeft, readRounded.RadiiBottomLeft);

                Assert.Equal(matrix, reader.ReadMatrix());
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
                writer.WriteColor(color);
                writer.WriteBoxShadow(shadow);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(color, reader.ReadColor());

                var readShadow = reader.ReadBoxShadow();
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
                writer.WriteRenderOptions(renderOptions);
                writer.WriteTextOptions(textOptions);

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(renderOptions, reader.ReadRenderOptions());
                Assert.Equal(textOptions, reader.ReadTextOptions());
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
                writer.WriteRenderOptions(new RenderOptions { RequiresFullOpacityHandling = value });

                var reader = new RenderDataReader(writer.Written);
                Assert.Equal(value, reader.ReadRenderOptions().RequiresFullOpacityHandling);
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
                writer.WriteByte(1);
                Assert.Equal(1, writer.Length);
                writer.WriteInt32(2);
                Assert.Equal(5, writer.Length);
                writer.WriteDouble(3);
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
                    writer.WriteInt32(i);

                Assert.Equal(4000, writer.Length);

                var reader = new RenderDataReader(writer.Written);
                for (var i = 0; i < 1000; i++)
                    Assert.Equal(i, reader.ReadInt32());
                Assert.True(reader.IsAtEnd);
            }
            finally
            {
                writer.Dispose();
            }
        }
    }
}
