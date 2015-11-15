using Perspex.Android.Platform.Specific;
using System;

namespace Perspex.Android.Platform.CanvasPlatform
{
    public enum PointUnit
    {
        Pixel,
        DP, //device independent pixel
        Custom    // CustomScale
    }

    public interface IPointUnitService
    {
        double ScaleX { get; }
        double ScaleY { get; }
        double FontScale { get; }

        double NativeToPerspexX(double x);

        double NativeToPerspexY(double y);

        double PerspexToNativeX(double x);

        double PerspexToNativeY(double y);

        float PerspexToNativeFontSize(double fontSize);
    }

    public class PointUnitService : IPointUnitService
    {
        public static IPointUnitService Instance => PerspexLocator.Current.GetService<IPointUnitService>() ?? new PointUnitService();

        private PointUnit _pointUnit;

        public PointUnit PointUnit
        {
            get { return _pointUnit; }
            set
            {
                _pointUnit = value;
                OnPointUnitChanged();
            }
        }

        public double ScaleX { get; set; }

        public double ScaleY { get; set; }

        public double FontScale { get; set; }

        public PointUnitService()
        {
            PointUnit = AndroidPlatform.Instance.DefaultPointUnit;
            //ScaleX = 1;
            //ScaleY = 1;
            ////FontScale = 1;
            //ScaleX = 2;
            //ScaleY = 2;
            //FontScale = 2;
        }

        private void OnPointUnitChanged()
        {
            switch (PointUnit)
            {
                case PointUnit.Pixel:
                    ScaleX = 1;
                    ScaleY = 1;
                    FontScale = 1;
                    break;

                case PointUnit.DP:
                    {
                        //get android metrics for device pixel format
                        var activity = PerspexLocator.Current.GetService<IAndroidActivity>().Activity;
                        var dm = activity.Resources.DisplayMetrics;
                        double scale = Math.Round((double)dm.ScaledDensity, 2);
                        ScaleX = scale;
                        ScaleY = scale;
                        FontScale = scale;
                    }
                    break;

                case PointUnit.Custom:
                    {
                        double scale = 2;
                        ScaleX = scale;
                        ScaleY = scale;
                        FontScale = scale;
                    }
                    break;
            }
        }

        public double NativeToPerspexX(double x) => ScaleX == 1 ? x : RoundToPerspex(x / ScaleX);

        public double NativeToPerspexY(double y) => ScaleY == 1 ? y : RoundToPerspex(y / ScaleY);

        public double PerspexToNativeX(double x) => ScaleX == 1 ? x : RoundToNative(ScaleX * x);

        public double PerspexToNativeY(double y) => ScaleY == 1 ? y : RoundToNative(ScaleY * y);

        public float PerspexToNativeFontSize(double fontSize)
        {
            if (FontScale == 1) return (float)fontSize;
            return (float)Math.Round(FontScale * fontSize, 1);
        }

        private static double RoundToPerspex(double v)
        {
           //  return Math.Ceiling(v);
            return Math.Round(v, 5);
        }

        private static double RoundToNative(double v)
        {
            //return Math.Ceiling(v);
             return Math.Round(v, 2);
        }
    }

    public static class PointServiceExtensions
    {
        //public static Matrix PerspexToNative(this IPointService ps, Matrix m)
        //{
        //    if (ps.ScaleX == ps.ScaleY && ps.ScaleX == 1) return m;

        //    return new Matrix(m.M11, m.M12, m.M21, m.M22, m.M31 * ps.ScaleX, m.M32 * ps.ScaleY);
        //}

        //public static double PerspexToNative(this IPointService service, double x)
        //{
        //    if (service.ScaleX == 1) return x;
        //    return x * service.ScaleX;
        //}

        private static int RoundToInt(double v)
        {
            return (int)Math.Round(v, 0);
        }

        public static double NativeToPerspex(this IPointUnitService ps, double x) => ps.NativeToPerspexX(x);

        public static float PerspexToNativeXF(this IPointUnitService ps, double x) => (float)ps.PerspexToNativeX(x);

        public static float PerspexToNativeYF(this IPointUnitService ps, double y) => (float)ps.PerspexToNativeY(y);

        public static int PerspexToNativeXInt(this IPointUnitService ps, double x) => RoundToInt(ps.PerspexToNativeX(x));

        public static int PerspexToNativeYInt(this IPointUnitService ps, double y) => RoundToInt(ps.PerspexToNativeX(y));

        public static Point NativeToPerspex(this IPointUnitService ps, Point point) => (ps.ScaleY == 1 && ps.ScaleX == 1) ? point : new Point(ps.NativeToPerspexX(point.X), ps.NativeToPerspexY(point.Y));

        public static Point PerspexToNative(this IPointUnitService ps, Point point) => (ps.ScaleY == 1 && ps.ScaleX == 1) ? point : new Point(ps.PerspexToNativeX(point.X), ps.PerspexToNativeY(point.Y));

        public static Size NativeToPerspex(this IPointUnitService service, Size size)
        {
            if (service.ScaleX == service.ScaleY && service.ScaleX == 1) return size;

            return new Size(size.Width / service.ScaleX, size.Height / service.ScaleY);
        }

        public static Size PerspexToNative(this IPointUnitService service, Size size)
        {
            if (service.ScaleX == service.ScaleY && service.ScaleX == 1) return size;
            return new Size(size.Width * service.ScaleX, size.Height * service.ScaleY);
        }

        public static Rect NativeToPerspex(this IPointUnitService service, Rect rect)
        {
            if (service.ScaleX == service.ScaleY && service.ScaleX == 1) return rect;

            return new Rect(rect.X / service.ScaleX, rect.Y / service.ScaleY, rect.Width / service.ScaleX, rect.Height / service.ScaleY);
        }

        public static Rect PerspexToNative(this IPointUnitService service, Rect rect)
        {
            if (service.ScaleX == service.ScaleY && service.ScaleX == 1) return rect;

            return new Rect(rect.X * service.ScaleX, rect.Y * service.ScaleY, rect.Width * service.ScaleX, rect.Height * service.ScaleY);
        }
    }
}