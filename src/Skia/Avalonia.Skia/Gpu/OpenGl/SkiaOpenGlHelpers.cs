using System;
using System.IO;
using Avalonia.OpenGL;
using SkiaSharp;
using static Avalonia.OpenGL.GlConsts;

namespace Avalonia.Skia
{
    public class SkiaOpenGlHelpers
    {
        public static void SaveCurrentFramebufferTo(GlInterface gl, string path, int width, int height)
        {
            using(var f = File.Create(path))
            using (var bmp = ReadCurrentFramebufferAsBitmap(gl, width, height))
                bmp.Encode(f, SKEncodedImageFormat.Png, 1);
        }

        public static SKBitmap ReadCurrentFramebufferAsBitmap(GlInterface gl, int width, int height)
        {
            var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            gl.ReadPixels(0, 0, width, height, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_INT_8_8_8_8, bmp.GetPixels());
            return bmp;
        }
        
        public static void SaveTextureTo(GlInterface gl, string path, int target, int level, int width, int height)
        {
            using(var f = File.Create(path))
            using (var bmp = ReadTextureAsBitmap(gl, target, level, width, height))
                bmp.Encode(f, SKEncodedImageFormat.Png, 1);
        }

        public static SKBitmap ReadTextureAsBitmap(GlInterface gl, int target, int level, int width, int height)
        {
            var bmp = new SKBitmap(width, height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            gl.GetTexImage(target, level, GlConsts.GL_RGBA, GlConsts.GL_UNSIGNED_INT_8_8_8_8, bmp.GetPixels());
            return bmp;
        }
    }
}
