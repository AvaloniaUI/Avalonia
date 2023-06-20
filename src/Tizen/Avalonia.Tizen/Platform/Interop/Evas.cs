using System;
using System.Runtime.InteropServices;

namespace Avalonia.Tizen.Platform.Interop
{
	internal static class Evas
	{
		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_object_evas_get(IntPtr obj);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_new(IntPtr evas);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_gl_free(IntPtr evas_gl);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_context_create(IntPtr evas_gl, IntPtr share_ctx);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_context_version_create(IntPtr evas_gl, IntPtr share_ctx, GLContextVersion version);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_gl_context_destroy(IntPtr evas_gl, IntPtr ctx);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_surface_create(IntPtr evas_gl, IntPtr config, int width, int height);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_gl_surface_destroy(IntPtr evas_gl, IntPtr surf);

		[DllImport(Libraries.Evas)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool evas_gl_native_surface_get(IntPtr evas_gl, IntPtr surf, out NativeSurfaceOpenGL ns);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_proc_address_get(IntPtr evas_gl, string name);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_gl_api_get(IntPtr evas_gl);

		[DllImport(Libraries.Evas)]
		[return: MarshalAs(UnmanagedType.U1)]
		internal static extern bool evas_gl_make_current(IntPtr evas_gl, IntPtr surf, IntPtr ctx);

		internal enum GLContextVersion
		{
			EVAS_GL_GLES_1_X = 1,  // OpenGL-ES 1.x
			EVAS_GL_GLES_2_X = 2,  // OpenGL-ES 2.x (default)
			EVAS_GL_GLES_3_X = 3,  // OpenGL-ES 3.x (since 2.4)
			EVAS_GL_DEBUG = 0x1000 // Enable debug mode on this context (see GL_KHR_debug) (since 4.0)
		}

		internal struct Config
		{
			public ColorFormat color_format;
			public DepthBits depth_bits;
			public StencilBits stencil_bits;
			public OptionsBits options_bits;
			public MultisampleBits multisample_bits;
			public ContextVersion gles_version;
		}

		// This structure is used to move data from one entity into another.
		internal struct NativeSurfaceOpenGL
		{
			public uint texture_id;
			public uint framebuffer_id;
			public uint internal_format;
			public uint format;
			public uint x;
			public uint y;
			public uint w;
			public uint h;
		}

		internal enum ColorFormat
		{
			RGB_888 = 0,
			RGBA_8888 = 1,
			NO_FBO = 2
		}

		internal enum DepthBits
		{
			NONE = 0,
			BIT_8 = 1,
			BIT_16 = 2,
			BIT_24 = 3,
			BIT_32 = 4
		}

		internal enum StencilBits
		{
			NONE = 0,
			BIT_1 = 1,
			BIT_2 = 2,
			BIT_4 = 3,
			BIT_8 = 4,
			BIT_16 = 5
		}

		internal enum OptionsBits
		{
			NONE = 0,
			DIRECT = (1 << 0),
			CLIENT_SIDE_ROTATION = (1 << 1),
			THREAD = (1 << 2)
		}

		internal enum MultisampleBits
		{
			NONE = 0,
			LOW = 1,
			MED = 2,
			HIGH = 3
		}

		internal enum ContextVersion
		{
			GLES_1_X = 1,
			GLES_2_X = 2,
			GLES_3_X = 3,
			DEBUG = 0x1000
		}

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_object_image_add(IntPtr obj);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_object_image_filled_add(IntPtr obj);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_size_get(IntPtr obj, IntPtr x, out int y);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_size_get(IntPtr obj, out int x, IntPtr y);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_size_get(IntPtr obj, out int x, out int y);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_size_set(IntPtr obj, int w, int h);

		[DllImport(Libraries.Evas)]
		internal static extern IntPtr evas_object_image_data_get(IntPtr obj, bool for_writing);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_data_set(IntPtr obj, IntPtr data);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_data_update_add(IntPtr obj, int x, int y, int w, int h);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_colorspace_set(IntPtr obj, Colorspace cspace);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_fill_set(IntPtr obj, int x, int y, int w, int h);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_native_surface_set(IntPtr obj, ref NativeSurfaceOpenGL surf);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_native_surface_set(IntPtr obj, IntPtr zero);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_pixels_dirty_set(IntPtr obj, bool dirty);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_pixels_get_callback_set(IntPtr obj, ImagePixelsSetCallback func, IntPtr data);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_pixels_get_callback_set(IntPtr obj, IntPtr zero, IntPtr data);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_smooth_scale_set(IntPtr obj, bool smooth_scale);

		[DllImport(Libraries.Evas)]
		internal static extern void evas_object_image_alpha_set(IntPtr obj, bool has_alpha);

		public delegate void ImagePixelsSetCallback(IntPtr data, IntPtr o);

		internal enum Colorspace
		{
			ARGB8888,
			YCBCR422P601_PL,
			YCBCR422P709_PL,
			RGB565_A5P,
			GRY8 = 4,
			YCBCR422601_PL,
			YCBCR420NV12601_PL,
			YCBCR420TM12601_PL,
			AGRY88 = 8,
			ETC1 = 9,
			RGB8_ETC2 = 10,
			RGBA8_ETC2_EAC = 11,
			ETC1_ALPHA = 12,
			RGB_S3TC_DXT1 = 13,
			RGBA_S3TC_DXT1 = 14,
			RGBA_S3TC_DXT2 = 15,
			RGBA_S3TC_DXT3 = 16,
			RGBA_S3TC_DXT4 = 17,
			RGBA_S3TC_DXT5 = 18,
		}
	}
}
