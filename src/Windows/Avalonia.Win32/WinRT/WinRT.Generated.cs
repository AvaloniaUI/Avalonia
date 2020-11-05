#pragma warning disable 108
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using Avalonia.MicroCom;

namespace Avalonia.Win32.WinRT
{
    internal enum TrustLevel
    {
        BaseTrust,
        PartialTrust,
        FullTrust
    }

    internal enum DirectXAlphaMode
    {
        Unspecified,
        Premultiplied,
        Straight,
        Ignore
    }

    internal enum DirectXPixelFormat
    {
        Unknown = 0,
        R32G32B32A32Typeless = 1,
        R32G32B32A32Float = 2,
        R32G32B32A32UInt = 3,
        R32G32B32A32Int = 4,
        R32G32B32Typeless = 5,
        R32G32B32Float = 6,
        R32G32B32UInt = 7,
        R32G32B32Int = 8,
        R16G16B16A16Typeless = 9,
        R16G16B16A16Float = 10,
        R16G16B16A16UIntNormalized = 11,
        R16G16B16A16UInt = 12,
        R16G16B16A16IntNormalized = 13,
        R16G16B16A16Int = 14,
        R32G32Typeless = 15,
        R32G32Float = 16,
        R32G32UInt = 17,
        R32G32Int = 18,
        R32G8X24Typeless = 19,
        D32FloatS8X24UInt = 20,
        R32FloatX8X24Typeless = 21,
        X32TypelessG8X24UInt = 22,
        R10G10B10A2Typeless = 23,
        R10G10B10A2UIntNormalized = 24,
        R10G10B10A2UInt = 25,
        R11G11B10Float = 26,
        R8G8B8A8Typeless = 27,
        R8G8B8A8UIntNormalized = 28,
        R8G8B8A8UIntNormalizedSrgb = 29,
        R8G8B8A8UInt = 30,
        R8G8B8A8IntNormalized = 31,
        R8G8B8A8Int = 32,
        R16G16Typeless = 33,
        R16G16Float = 34,
        R16G16UIntNormalized = 35,
        R16G16UInt = 36,
        R16G16IntNormalized = 37,
        R16G16Int = 38,
        R32Typeless = 39,
        D32Float = 40,
        R32Float = 41,
        R32UInt = 42,
        R32Int = 43,
        R24G8Typeless = 44,
        D24UIntNormalizedS8UInt = 45,
        R24UIntNormalizedX8Typeless = 46,
        X24TypelessG8UInt = 47,
        R8G8Typeless = 48,
        R8G8UIntNormalized = 49,
        R8G8UInt = 50,
        R8G8IntNormalized = 51,
        R8G8Int = 52,
        R16Typeless = 53,
        R16Float = 54,
        D16UIntNormalized = 55,
        R16UIntNormalized = 56,
        R16UInt = 57,
        R16IntNormalized = 58,
        R16Int = 59,
        R8Typeless = 60,
        R8UIntNormalized = 61,
        R8UInt = 62,
        R8IntNormalized = 63,
        R8Int = 64,
        A8UIntNormalized = 65,
        R1UIntNormalized = 66,
        R9G9B9E5SharedExponent = 67,
        R8G8B8G8UIntNormalized = 68,
        G8R8G8B8UIntNormalized = 69,
        BC1Typeless = 70,
        BC1UIntNormalized = 71,
        BC1UIntNormalizedSrgb = 72,
        BC2Typeless = 73,
        BC2UIntNormalized = 74,
        BC2UIntNormalizedSrgb = 75,
        BC3Typeless = 76,
        BC3UIntNormalized = 77,
        BC3UIntNormalizedSrgb = 78,
        BC4Typeless = 79,
        BC4UIntNormalized = 80,
        BC4IntNormalized = 81,
        BC5Typeless = 82,
        BC5UIntNormalized = 83,
        BC5IntNormalized = 84,
        B5G6R5UIntNormalized = 85,
        B5G5R5A1UIntNormalized = 86,
        B8G8R8A8UIntNormalized = 87,
        B8G8R8X8UIntNormalized = 88,
        R10G10B10XRBiasA2UIntNormalized = 89,
        B8G8R8A8Typeless = 90,
        B8G8R8A8UIntNormalizedSrgb = 91,
        B8G8R8X8Typeless = 92,
        B8G8R8X8UIntNormalizedSrgb = 93,
        BC6HTypeless = 94,
        BC6H16UnsignedFloat = 95,
        BC6H16Float = 96,
        BC7Typeless = 97,
        BC7UIntNormalized = 98,
        BC7UIntNormalizedSrgb = 99,
        Ayuv = 100,
        Y410 = 101,
        Y416 = 102,
        NV12 = 103,
        P010 = 104,
        P016 = 105,
        Opaque420 = 106,
        Yuy2 = 107,
        Y210 = 108,
        Y216 = 109,
        NV11 = 110,
        AI44 = 111,
        IA44 = 112,
        P8 = 113,
        A8P8 = 114,
        B4G4R4A4UIntNormalized = 115,
        P208 = 130,
        V208 = 131,
        V408 = 132
    }

    internal enum PropertyType
    {
        Empty = 0,
        UInt8 = 1,
        Int16 = 2,
        UInt16 = 3,
        Int32 = 4,
        UInt32 = 5,
        Int64 = 6,
        UInt64 = 7,
        Single = 8,
        Double = 9,
        Char16 = 10,
        Boolean = 11,
        String = 12,
        Inspectable = 13,
        DateTime = 14,
        TimeSpan = 15,
        Guid = 16,
        Point = 17,
        Size = 18,
        Rect = 19,
        OtherType = 20,
        UInt8Array = 1025,
        Int16Array = 1026,
        UInt16Array = 1027,
        Int32Array = 1028,
        UInt32Array = 1029,
        Int64Array = 1030,
        UInt64Array = 1031,
        SingleArray = 1032,
        DoubleArray = 1033,
        Char16Array = 1034,
        BooleanArray = 1035,
        StringArray = 1036,
        InspectableArray = 1037,
        DateTimeArray = 1038,
        TimeSpanArray = 1039,
        GuidArray = 1040,
        PointArray = 1041,
        SizeArray = 1042,
        RectArray = 1043,
        OtherTypeArray = 1044
    }

    internal enum AsyncStatus
    {
        Started = 0,
        Completed,
        Canceled,
        Error
    }

    internal enum CompositionBatchTypes
    {
        None = 0x0,
        Animation = 0x1,
        Effect = 0x2,
        InfiniteAnimation = 0x4,
        AllAnimations = 0x5
    }

    internal enum CompositionBackfaceVisibility
    {
        Inherit,
        Visible,
        Hidden
    }

    internal enum CompositionBorderMode
    {
        Inherit,
        Soft,
        Hard
    }

    internal enum CompositionCompositeMode
    {
        Inherit,
        SourceOver,
        DestinationInvert,
        MinBlend
    }

    internal enum GRAPHICS_EFFECT_PROPERTY_MAPPING
    {
        GRAPHICS_EFFECT_PROPERTY_MAPPING_UNKNOWN,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_DIRECT,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORX,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORY,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORZ,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_VECTORW,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_RECT_TO_VECTOR4,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_RADIANS_TO_DEGREES,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLORMATRIX_ALPHA_MODE,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLOR_TO_VECTOR3,
        GRAPHICS_EFFECT_PROPERTY_MAPPING_COLOR_TO_VECTOR4
    }

    internal enum CompositionEffectFactoryLoadStatus
    {
        Success = 0,
        EffectTooComplex = 1,
        Pending = 2,
        Other = -1
    }

    internal unsafe partial interface IInspectable : Avalonia.MicroCom.IUnknown
    {
        void GetIids(ulong* iidCount, Guid** iids);
        IntPtr RuntimeClassName
        {
            get;
        }

        TrustLevel TrustLevel
        {
            get;
        }
    }

    internal unsafe partial interface IPropertyValue : IInspectable
    {
        PropertyType Type
        {
            get;
        }

        int IsNumericScalar
        {
            get;
        }

        byte UInt8
        {
            get;
        }

        short Int16
        {
            get;
        }

        ushort UInt16
        {
            get;
        }

        int Int32
        {
            get;
        }

        uint UInt32
        {
            get;
        }

        long Int64
        {
            get;
        }

        ulong UInt64
        {
            get;
        }

        float Single
        {
            get;
        }

        double Double
        {
            get;
        }

        System.Char Char16
        {
            get;
        }

        int Boolean
        {
            get;
        }

        IntPtr String
        {
            get;
        }

        System.Guid Guid
        {
            get;
        }

        void GetDateTime(void* value);
        void GetTimeSpan(void* value);
        void GetPoint(void* value);
        void GetSize(void* value);
        void GetRect(void* value);
        byte* GetUInt8Array(uint* __valueSize);
        short* GetInt16Array(uint* __valueSize);
        ushort* GetUInt16Array(uint* __valueSize);
        int* GetInt32Array(uint* __valueSize);
        uint* GetUInt32Array(uint* __valueSize);
        long* GetInt64Array(uint* __valueSize);
        ulong* GetUInt64Array(uint* __valueSize);
        float* GetSingleArray(uint* __valueSize);
        double* GetDoubleArray(uint* __valueSize);
        System.Char* GetChar16Array(uint* __valueSize);
        int* GetBooleanArray(uint* __valueSize);
        IntPtr* GetStringArray(uint* __valueSize);
        void** GetInspectableArray(uint* __valueSize);
        System.Guid* GetGuidArray(uint* __valueSize);
        void* GetDateTimeArray(uint* __valueSize);
        void* GetTimeSpanArray(uint* __valueSize);
        void* GetPointArray(uint* __valueSize);
        void* GetSizeArray(uint* __valueSize);
        void* GetRectArray(uint* __valueSize);
    }

    internal unsafe partial interface IAsyncActionCompletedHandler : Avalonia.MicroCom.IUnknown
    {
        void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus);
    }

    internal unsafe partial interface IAsyncAction : IInspectable
    {
        void SetCompleted(IAsyncActionCompletedHandler handler);
        IAsyncActionCompletedHandler Completed
        {
            get;
        }

        void GetResults();
    }

    internal unsafe partial interface IDispatcherQueue : IInspectable
    {
    }

    internal unsafe partial interface IDispatcherQueueController : IInspectable
    {
        IDispatcherQueue DispatcherQueue
        {
            get;
        }

        IAsyncAction ShutdownQueueAsync();
    }

    internal unsafe partial interface IActivationFactory : IInspectable
    {
        IntPtr ActivateInstance();
    }

    internal unsafe partial interface ICompositor : IInspectable
    {
        void* CreateColorKeyFrameAnimation();
        void* CreateColorBrush();
        ICompositionColorBrush CreateColorBrushWithColor(Avalonia.Win32.WinRT.WinRTColor* color);
        IContainerVisual CreateContainerVisual();
        void* CreateCubicBezierEasingFunction(System.Numerics.Vector2 controlPoint1, System.Numerics.Vector2 controlPoint2);
        ICompositionEffectFactory CreateEffectFactory(IGraphicsEffect graphicsEffect);
        void* CreateEffectFactoryWithProperties(void* graphicsEffect, void* animatableProperties);
        void* CreateExpressionAnimation();
        void* CreateExpressionAnimationWithExpression(IntPtr expression);
        void* CreateInsetClip();
        void* CreateInsetClipWithInsets(float leftInset, float topInset, float rightInset, float bottomInset);
        void* CreateLinearEasingFunction();
        void* CreatePropertySet();
        void* CreateQuaternionKeyFrameAnimation();
        void* CreateScalarKeyFrameAnimation();
        void* CreateScopedBatch(CompositionBatchTypes batchType);
        ISpriteVisual CreateSpriteVisual();
        ICompositionSurfaceBrush CreateSurfaceBrush();
        ICompositionSurfaceBrush CreateSurfaceBrushWithSurface(ICompositionSurface surface);
        void* CreateTargetForCurrentView();
        void* CreateVector2KeyFrameAnimation();
        void* CreateVector3KeyFrameAnimation();
        void* CreateVector4KeyFrameAnimation();
        void* GetCommitBatch(CompositionBatchTypes batchType);
    }

    internal unsafe partial interface ICompositor2 : IInspectable
    {
        void* CreateAmbientLight();
        void* CreateAnimationGroup();
        ICompositionBackdropBrush CreateBackdropBrush();
        void* CreateDistantLight();
        void* CreateDropShadow();
        void* CreateImplicitAnimationCollection();
        void* CreateLayerVisual();
        void* CreateMaskBrush();
        void* CreateNineGridBrush();
        void* CreatePointLight();
        void* CreateSpotLight();
        void* CreateStepEasingFunction();
        void* CreateStepEasingFunctionWithStepCount(int stepCount);
    }

    internal unsafe partial interface ISpriteVisual : IInspectable
    {
        ICompositionBrush Brush
        {
            get;
        }

        void SetBrush(ICompositionBrush value);
    }

    internal unsafe partial interface ICompositionDrawingSurfaceInterop : Avalonia.MicroCom.IUnknown
    {
        Avalonia.Win32.Interop.UnmanagedMethods.POINT BeginDraw(Avalonia.Win32.Interop.UnmanagedMethods.RECT* updateRect, Guid* iid, void** updateObject);
        void EndDraw();
        void Resize(Avalonia.Win32.Interop.UnmanagedMethods.POINT sizePixels);
        void Scroll(Avalonia.Win32.Interop.UnmanagedMethods.RECT* scrollRect, Avalonia.Win32.Interop.UnmanagedMethods.RECT* clipRect, int offsetX, int offsetY);
        void ResumeDraw();
        void SuspendDraw();
    }

    internal unsafe partial interface ICompositionGraphicsDeviceInterop : Avalonia.MicroCom.IUnknown
    {
        IUnknown RenderingDevice
        {
            get;
        }

        void SetRenderingDevice(IUnknown value);
    }

    internal unsafe partial interface ICompositorInterop : Avalonia.MicroCom.IUnknown
    {
        ICompositionSurface CreateCompositionSurfaceForHandle(IntPtr swapChain);
        ICompositionSurface CreateCompositionSurfaceForSwapChain(IUnknown swapChain);
        ICompositionGraphicsDevice CreateGraphicsDevice(IUnknown renderingDevice);
    }

    internal unsafe partial interface ISwapChainInterop : Avalonia.MicroCom.IUnknown
    {
        void SetSwapChain(IUnknown swapChain);
    }

    internal unsafe partial interface ICompositorDesktopInterop : Avalonia.MicroCom.IUnknown
    {
        IDesktopWindowTarget CreateDesktopWindowTarget(IntPtr hwndTarget, int isTopmost);
        void EnsureOnThread(int threadId);
    }

    internal unsafe partial interface IDesktopWindowTargetInterop : Avalonia.MicroCom.IUnknown
    {
        IntPtr HWnd
        {
            get;
        }
    }

    internal unsafe partial interface IDesktopWindowContentBridgeInterop : Avalonia.MicroCom.IUnknown
    {
        void Initialize(ICompositor compositor, IntPtr parentHwnd);
        IntPtr HWnd
        {
            get;
        }

        float AppliedScaleFactor
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionGraphicsDevice : IInspectable
    {
        ICompositionDrawingSurface CreateDrawingSurface(Avalonia.Win32.Interop.UnmanagedMethods.SIZE sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode);
        void AddRenderingDeviceReplaced(void* handler, void* token);
        void RemoveRenderingDeviceReplaced(int token);
    }

    internal unsafe partial interface ICompositionSurface : IInspectable
    {
    }

    internal unsafe partial interface IDesktopWindowTarget : IInspectable
    {
        int IsTopmost
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionDrawingSurface : IInspectable
    {
        DirectXAlphaMode AlphaMode
        {
            get;
        }

        DirectXPixelFormat PixelFormat
        {
            get;
        }

        Avalonia.Win32.Interop.UnmanagedMethods.POINT Size
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionSurfaceBrush : IInspectable
    {
    }

    internal unsafe partial interface ICompositionBrush : IInspectable
    {
    }

    internal unsafe partial interface IVisual : IInspectable
    {
        System.Numerics.Vector2 AnchorPoint
        {
            get;
        }

        void SetAnchorPoint(System.Numerics.Vector2 value);
        CompositionBackfaceVisibility BackfaceVisibility
        {
            get;
        }

        void SetBackfaceVisibility(CompositionBackfaceVisibility value);
        CompositionBorderMode BorderMode
        {
            get;
        }

        void SetBorderMode(CompositionBorderMode value);
        System.Numerics.Vector3 CenterPoint
        {
            get;
        }

        void SetCenterPoint(System.Numerics.Vector3 value);
        void* Clip
        {
            get;
        }

        void SetClip(void* value);
        CompositionCompositeMode CompositeMode
        {
            get;
        }

        void SetCompositeMode(CompositionCompositeMode value);
        int IsVisible
        {
            get;
        }

        void SetIsVisible(int value);
        System.Numerics.Vector3 Offset
        {
            get;
        }

        void SetOffset(System.Numerics.Vector3 value);
        float Opacity
        {
            get;
        }

        void SetOpacity(float value);
        System.Numerics.Quaternion Orientation
        {
            get;
        }

        void SetOrientation(System.Numerics.Quaternion value);
        IContainerVisual Parent
        {
            get;
        }

        float RotationAngle
        {
            get;
        }

        void SetRotationAngle(float value);
        float RotationAngleInDegrees
        {
            get;
        }

        void SetRotationAngleInDegrees(float value);
        System.Numerics.Vector3 RotationAxis
        {
            get;
        }

        void SetRotationAxis(System.Numerics.Vector3 value);
        System.Numerics.Vector3 Scale
        {
            get;
        }

        void SetScale(System.Numerics.Vector3 value);
        System.Numerics.Vector2 Size
        {
            get;
        }

        void SetSize(System.Numerics.Vector2 value);
        System.Numerics.Matrix4x4 TransformMatrix
        {
            get;
        }

        void SetTransformMatrix(System.Numerics.Matrix4x4 value);
    }

    internal unsafe partial interface IVisual2 : IInspectable
    {
        IVisual ParentForTransform
        {
            get;
        }

        void SetParentForTransform(IVisual value);
        System.Numerics.Vector3 RelativeOffsetAdjustment
        {
            get;
        }

        void SetRelativeOffsetAdjustment(System.Numerics.Vector3 value);
        System.Numerics.Vector2 RelativeSizeAdjustment
        {
            get;
        }

        void SetRelativeSizeAdjustment(System.Numerics.Vector2 value);
    }

    internal unsafe partial interface IContainerVisual : IInspectable
    {
        IVisualCollection Children
        {
            get;
        }
    }

    internal unsafe partial interface IVisualCollection : IInspectable
    {
        int Count
        {
            get;
        }

        void InsertAbove(IVisual newChild, IVisual sibling);
        void InsertAtBottom(IVisual newChild);
        void InsertAtTop(IVisual newChild);
        void InsertBelow(IVisual newChild, IVisual sibling);
        void Remove(IVisual child);
        void RemoveAll();
    }

    internal unsafe partial interface ICompositionTarget : IInspectable
    {
        IVisual Root
        {
            get;
        }

        void SetRoot(IVisual value);
    }

    internal unsafe partial interface IGraphicsEffect : IInspectable
    {
        IntPtr Name
        {
            get;
        }

        void SetName(IntPtr name);
    }

    internal unsafe partial interface IGraphicsEffectSource : IInspectable
    {
    }

    internal unsafe partial interface IGraphicsEffectD2D1Interop : Avalonia.MicroCom.IUnknown
    {
        Guid EffectId
        {
            get;
        }

        void GetNamedPropertyMapping(IntPtr name, uint* index, GRAPHICS_EFFECT_PROPERTY_MAPPING* mapping);
        uint PropertyCount
        {
            get;
        }

        IPropertyValue GetProperty(uint index);
        IGraphicsEffectSource GetSource(uint index);
        uint SourceCount
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionEffectSourceParameter : IInspectable
    {
        IntPtr Name
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionEffectSourceParameterFactory : IInspectable
    {
        ICompositionEffectSourceParameter Create(IntPtr name);
    }

    internal unsafe partial interface ICompositionEffectFactory : IInspectable
    {
        ICompositionEffectBrush CreateBrush();
        int ExtendedError
        {
            get;
        }

        CompositionEffectFactoryLoadStatus LoadStatus
        {
            get;
        }
    }

    internal unsafe partial interface ICompositionEffectBrush : IInspectable
    {
        ICompositionBrush GetSourceParameter(IntPtr name);
        void SetSourceParameter(IntPtr name, ICompositionBrush source);
    }

    internal unsafe partial interface ICompositionBackdropBrush : IInspectable
    {
    }

    internal unsafe partial interface ICompositionColorBrush : IInspectable
    {
        Avalonia.Win32.WinRT.WinRTColor Color
        {
            get;
        }

        void SetColor(Avalonia.Win32.WinRT.WinRTColor value);
    }
}

namespace Avalonia.Win32.WinRT.Impl
{
    unsafe internal partial class __MicroComIInspectableProxy : Avalonia.MicroCom.MicroComProxyBase, IInspectable
    {
        public void GetIids(ulong* iidCount, Guid** iids)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, iidCount, iids, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetIids failed", __result);
        }

        public IntPtr RuntimeClassName
        {
            get
            {
                int __result;
                IntPtr className = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &className, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRuntimeClassName failed", __result);
                return className;
            }
        }

        public TrustLevel TrustLevel
        {
            get
            {
                int __result;
                TrustLevel trustLevel = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &trustLevel, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetTrustLevel failed", __result);
                return trustLevel;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IInspectable), new Guid("AF86E2E0-B12D-4c6a-9C5A-D7AA65101E90"), (p, owns) => new __MicroComIInspectableProxy(p, owns));
        }

        public __MicroComIInspectableProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComIInspectableVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIidsDelegate(IntPtr @this, ulong* iidCount, Guid** iids);
        static int GetIids(IntPtr @this, ulong* iidCount, Guid** iids)
        {
            IInspectable __target = null;
            try
            {
                {
                    __target = (IInspectable)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetIids(iidCount, iids);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRuntimeClassNameDelegate(IntPtr @this, IntPtr* className);
        static int GetRuntimeClassName(IntPtr @this, IntPtr* className)
        {
            IInspectable __target = null;
            try
            {
                {
                    __target = (IInspectable)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RuntimeClassName;
                        *className = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetTrustLevelDelegate(IntPtr @this, TrustLevel* trustLevel);
        static int GetTrustLevel(IntPtr @this, TrustLevel* trustLevel)
        {
            IInspectable __target = null;
            try
            {
                {
                    __target = (IInspectable)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.TrustLevel;
                        *trustLevel = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIInspectableVTable()
        {
            base.AddMethod((GetIidsDelegate)GetIids);
            base.AddMethod((GetRuntimeClassNameDelegate)GetRuntimeClassName);
            base.AddMethod((GetTrustLevelDelegate)GetTrustLevel);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IInspectable), new __MicroComIInspectableVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIPropertyValueProxy : __MicroComIInspectableProxy, IPropertyValue
    {
        public PropertyType Type
        {
            get
            {
                int __result;
                PropertyType value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetType failed", __result);
                return value;
            }
        }

        public int IsNumericScalar
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetIsNumericScalar failed", __result);
                return value;
            }
        }

        public byte UInt8
        {
            get
            {
                int __result;
                byte value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetUInt8 failed", __result);
                return value;
            }
        }

        public short Int16
        {
            get
            {
                int __result;
                short value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 3]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetInt16 failed", __result);
                return value;
            }
        }

        public ushort UInt16
        {
            get
            {
                int __result;
                ushort value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 4]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetUInt16 failed", __result);
                return value;
            }
        }

        public int Int32
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 5]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetInt32 failed", __result);
                return value;
            }
        }

        public uint UInt32
        {
            get
            {
                int __result;
                uint value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 6]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetUInt32 failed", __result);
                return value;
            }
        }

        public long Int64
        {
            get
            {
                int __result;
                long value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 7]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetInt64 failed", __result);
                return value;
            }
        }

        public ulong UInt64
        {
            get
            {
                int __result;
                ulong value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 8]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetUInt64 failed", __result);
                return value;
            }
        }

        public float Single
        {
            get
            {
                int __result;
                float value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 9]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetSingle failed", __result);
                return value;
            }
        }

        public double Double
        {
            get
            {
                int __result;
                double value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 10]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetDouble failed", __result);
                return value;
            }
        }

        public System.Char Char16
        {
            get
            {
                int __result;
                System.Char value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 11]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetChar16 failed", __result);
                return value;
            }
        }

        public int Boolean
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 12]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetBoolean failed", __result);
                return value;
            }
        }

        public IntPtr String
        {
            get
            {
                int __result;
                IntPtr value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 13]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetString failed", __result);
                return value;
            }
        }

        public System.Guid Guid
        {
            get
            {
                int __result;
                System.Guid value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 14]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetGuid failed", __result);
                return value;
            }
        }

        public void GetDateTime(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 15]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetDateTime failed", __result);
        }

        public void GetTimeSpan(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 16]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetTimeSpan failed", __result);
        }

        public void GetPoint(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 17]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetPoint failed", __result);
        }

        public void GetSize(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 18]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetSize failed", __result);
        }

        public void GetRect(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 19]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetRect failed", __result);
        }

        public byte* GetUInt8Array(uint* __valueSize)
        {
            int __result;
            byte* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 20]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetUInt8Array failed", __result);
            return value;
        }

        public short* GetInt16Array(uint* __valueSize)
        {
            int __result;
            short* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 21]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetInt16Array failed", __result);
            return value;
        }

        public ushort* GetUInt16Array(uint* __valueSize)
        {
            int __result;
            ushort* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 22]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetUInt16Array failed", __result);
            return value;
        }

        public int* GetInt32Array(uint* __valueSize)
        {
            int __result;
            int* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 23]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetInt32Array failed", __result);
            return value;
        }

        public uint* GetUInt32Array(uint* __valueSize)
        {
            int __result;
            uint* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 24]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetUInt32Array failed", __result);
            return value;
        }

        public long* GetInt64Array(uint* __valueSize)
        {
            int __result;
            long* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 25]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetInt64Array failed", __result);
            return value;
        }

        public ulong* GetUInt64Array(uint* __valueSize)
        {
            int __result;
            ulong* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 26]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetUInt64Array failed", __result);
            return value;
        }

        public float* GetSingleArray(uint* __valueSize)
        {
            int __result;
            float* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 27]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetSingleArray failed", __result);
            return value;
        }

        public double* GetDoubleArray(uint* __valueSize)
        {
            int __result;
            double* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 28]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetDoubleArray failed", __result);
            return value;
        }

        public System.Char* GetChar16Array(uint* __valueSize)
        {
            int __result;
            System.Char* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 29]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetChar16Array failed", __result);
            return value;
        }

        public int* GetBooleanArray(uint* __valueSize)
        {
            int __result;
            int* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 30]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetBooleanArray failed", __result);
            return value;
        }

        public IntPtr* GetStringArray(uint* __valueSize)
        {
            int __result;
            IntPtr* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 31]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetStringArray failed", __result);
            return value;
        }

        public void** GetInspectableArray(uint* __valueSize)
        {
            int __result;
            void** value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 32]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetInspectableArray failed", __result);
            return value;
        }

        public System.Guid* GetGuidArray(uint* __valueSize)
        {
            int __result;
            System.Guid* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 33]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetGuidArray failed", __result);
            return value;
        }

        public void* GetDateTimeArray(uint* __valueSize)
        {
            int __result;
            void* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 34]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetDateTimeArray failed", __result);
            return value;
        }

        public void* GetTimeSpanArray(uint* __valueSize)
        {
            int __result;
            void* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 35]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetTimeSpanArray failed", __result);
            return value;
        }

        public void* GetPointArray(uint* __valueSize)
        {
            int __result;
            void* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 36]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetPointArray failed", __result);
            return value;
        }

        public void* GetSizeArray(uint* __valueSize)
        {
            int __result;
            void* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 37]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetSizeArray failed", __result);
            return value;
        }

        public void* GetRectArray(uint* __valueSize)
        {
            int __result;
            void* value = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, __valueSize, &value, (*PPV)[base.VTableSize + 38]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetRectArray failed", __result);
            return value;
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IPropertyValue), new Guid("4BD682DD-7554-40E9-9A9B-82654EDE7E62"), (p, owns) => new __MicroComIPropertyValueProxy(p, owns));
        }

        public __MicroComIPropertyValueProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 39;
    }

    unsafe class __MicroComIPropertyValueVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetTypeDelegate(IntPtr @this, PropertyType* value);
        static int GetType(IntPtr @this, PropertyType* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Type;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsNumericScalarDelegate(IntPtr @this, int* value);
        static int GetIsNumericScalar(IntPtr @this, int* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.IsNumericScalar;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt8Delegate(IntPtr @this, byte* value);
        static int GetUInt8(IntPtr @this, byte* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.UInt8;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt16Delegate(IntPtr @this, short* value);
        static int GetInt16(IntPtr @this, short* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Int16;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt16Delegate(IntPtr @this, ushort* value);
        static int GetUInt16(IntPtr @this, ushort* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.UInt16;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt32Delegate(IntPtr @this, int* value);
        static int GetInt32(IntPtr @this, int* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Int32;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt32Delegate(IntPtr @this, uint* value);
        static int GetUInt32(IntPtr @this, uint* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.UInt32;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt64Delegate(IntPtr @this, long* value);
        static int GetInt64(IntPtr @this, long* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Int64;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt64Delegate(IntPtr @this, ulong* value);
        static int GetUInt64(IntPtr @this, ulong* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.UInt64;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSingleDelegate(IntPtr @this, float* value);
        static int GetSingle(IntPtr @this, float* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Single;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDoubleDelegate(IntPtr @this, double* value);
        static int GetDouble(IntPtr @this, double* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Double;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetChar16Delegate(IntPtr @this, System.Char* value);
        static int GetChar16(IntPtr @this, System.Char* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Char16;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetBooleanDelegate(IntPtr @this, int* value);
        static int GetBoolean(IntPtr @this, int* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Boolean;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetStringDelegate(IntPtr @this, IntPtr* value);
        static int GetString(IntPtr @this, IntPtr* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.String;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetGuidDelegate(IntPtr @this, System.Guid* value);
        static int GetGuid(IntPtr @this, System.Guid* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Guid;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDateTimeDelegate(IntPtr @this, void* value);
        static int GetDateTime(IntPtr @this, void* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetDateTime(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetTimeSpanDelegate(IntPtr @this, void* value);
        static int GetTimeSpan(IntPtr @this, void* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetTimeSpan(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetPointDelegate(IntPtr @this, void* value);
        static int GetPoint(IntPtr @this, void* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetPoint(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSizeDelegate(IntPtr @this, void* value);
        static int GetSize(IntPtr @this, void* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetSize(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRectDelegate(IntPtr @this, void* value);
        static int GetRect(IntPtr @this, void* value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetRect(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt8ArrayDelegate(IntPtr @this, uint* __valueSize, byte** value);
        static int GetUInt8Array(IntPtr @this, uint* __valueSize, byte** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetUInt8Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt16ArrayDelegate(IntPtr @this, uint* __valueSize, short** value);
        static int GetInt16Array(IntPtr @this, uint* __valueSize, short** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetInt16Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt16ArrayDelegate(IntPtr @this, uint* __valueSize, ushort** value);
        static int GetUInt16Array(IntPtr @this, uint* __valueSize, ushort** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetUInt16Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt32ArrayDelegate(IntPtr @this, uint* __valueSize, int** value);
        static int GetInt32Array(IntPtr @this, uint* __valueSize, int** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetInt32Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt32ArrayDelegate(IntPtr @this, uint* __valueSize, uint** value);
        static int GetUInt32Array(IntPtr @this, uint* __valueSize, uint** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetUInt32Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInt64ArrayDelegate(IntPtr @this, uint* __valueSize, long** value);
        static int GetInt64Array(IntPtr @this, uint* __valueSize, long** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetInt64Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetUInt64ArrayDelegate(IntPtr @this, uint* __valueSize, ulong** value);
        static int GetUInt64Array(IntPtr @this, uint* __valueSize, ulong** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetUInt64Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSingleArrayDelegate(IntPtr @this, uint* __valueSize, float** value);
        static int GetSingleArray(IntPtr @this, uint* __valueSize, float** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetSingleArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDoubleArrayDelegate(IntPtr @this, uint* __valueSize, double** value);
        static int GetDoubleArray(IntPtr @this, uint* __valueSize, double** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetDoubleArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetChar16ArrayDelegate(IntPtr @this, uint* __valueSize, System.Char** value);
        static int GetChar16Array(IntPtr @this, uint* __valueSize, System.Char** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetChar16Array(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetBooleanArrayDelegate(IntPtr @this, uint* __valueSize, int** value);
        static int GetBooleanArray(IntPtr @this, uint* __valueSize, int** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetBooleanArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetStringArrayDelegate(IntPtr @this, uint* __valueSize, IntPtr** value);
        static int GetStringArray(IntPtr @this, uint* __valueSize, IntPtr** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetStringArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetInspectableArrayDelegate(IntPtr @this, uint* __valueSize, void*** value);
        static int GetInspectableArray(IntPtr @this, uint* __valueSize, void*** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetInspectableArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetGuidArrayDelegate(IntPtr @this, uint* __valueSize, System.Guid** value);
        static int GetGuidArray(IntPtr @this, uint* __valueSize, System.Guid** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetGuidArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDateTimeArrayDelegate(IntPtr @this, uint* __valueSize, void** value);
        static int GetDateTimeArray(IntPtr @this, uint* __valueSize, void** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetDateTimeArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetTimeSpanArrayDelegate(IntPtr @this, uint* __valueSize, void** value);
        static int GetTimeSpanArray(IntPtr @this, uint* __valueSize, void** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetTimeSpanArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetPointArrayDelegate(IntPtr @this, uint* __valueSize, void** value);
        static int GetPointArray(IntPtr @this, uint* __valueSize, void** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetPointArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSizeArrayDelegate(IntPtr @this, uint* __valueSize, void** value);
        static int GetSizeArray(IntPtr @this, uint* __valueSize, void** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetSizeArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRectArrayDelegate(IntPtr @this, uint* __valueSize, void** value);
        static int GetRectArray(IntPtr @this, uint* __valueSize, void** value)
        {
            IPropertyValue __target = null;
            try
            {
                {
                    __target = (IPropertyValue)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetRectArray(__valueSize);
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIPropertyValueVTable()
        {
            base.AddMethod((GetTypeDelegate)GetType);
            base.AddMethod((GetIsNumericScalarDelegate)GetIsNumericScalar);
            base.AddMethod((GetUInt8Delegate)GetUInt8);
            base.AddMethod((GetInt16Delegate)GetInt16);
            base.AddMethod((GetUInt16Delegate)GetUInt16);
            base.AddMethod((GetInt32Delegate)GetInt32);
            base.AddMethod((GetUInt32Delegate)GetUInt32);
            base.AddMethod((GetInt64Delegate)GetInt64);
            base.AddMethod((GetUInt64Delegate)GetUInt64);
            base.AddMethod((GetSingleDelegate)GetSingle);
            base.AddMethod((GetDoubleDelegate)GetDouble);
            base.AddMethod((GetChar16Delegate)GetChar16);
            base.AddMethod((GetBooleanDelegate)GetBoolean);
            base.AddMethod((GetStringDelegate)GetString);
            base.AddMethod((GetGuidDelegate)GetGuid);
            base.AddMethod((GetDateTimeDelegate)GetDateTime);
            base.AddMethod((GetTimeSpanDelegate)GetTimeSpan);
            base.AddMethod((GetPointDelegate)GetPoint);
            base.AddMethod((GetSizeDelegate)GetSize);
            base.AddMethod((GetRectDelegate)GetRect);
            base.AddMethod((GetUInt8ArrayDelegate)GetUInt8Array);
            base.AddMethod((GetInt16ArrayDelegate)GetInt16Array);
            base.AddMethod((GetUInt16ArrayDelegate)GetUInt16Array);
            base.AddMethod((GetInt32ArrayDelegate)GetInt32Array);
            base.AddMethod((GetUInt32ArrayDelegate)GetUInt32Array);
            base.AddMethod((GetInt64ArrayDelegate)GetInt64Array);
            base.AddMethod((GetUInt64ArrayDelegate)GetUInt64Array);
            base.AddMethod((GetSingleArrayDelegate)GetSingleArray);
            base.AddMethod((GetDoubleArrayDelegate)GetDoubleArray);
            base.AddMethod((GetChar16ArrayDelegate)GetChar16Array);
            base.AddMethod((GetBooleanArrayDelegate)GetBooleanArray);
            base.AddMethod((GetStringArrayDelegate)GetStringArray);
            base.AddMethod((GetInspectableArrayDelegate)GetInspectableArray);
            base.AddMethod((GetGuidArrayDelegate)GetGuidArray);
            base.AddMethod((GetDateTimeArrayDelegate)GetDateTimeArray);
            base.AddMethod((GetTimeSpanArrayDelegate)GetTimeSpanArray);
            base.AddMethod((GetPointArrayDelegate)GetPointArray);
            base.AddMethod((GetSizeArrayDelegate)GetSizeArray);
            base.AddMethod((GetRectArrayDelegate)GetRectArray);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IPropertyValue), new __MicroComIPropertyValueVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIAsyncActionCompletedHandlerProxy : Avalonia.MicroCom.MicroComProxyBase, IAsyncActionCompletedHandler
    {
        public void Invoke(IAsyncAction asyncInfo, AsyncStatus asyncStatus)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(asyncInfo), asyncStatus, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Invoke failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IAsyncActionCompletedHandler), new Guid("A4ED5C81-76C9-40BD-8BE6-B1D90FB20AE7"), (p, owns) => new __MicroComIAsyncActionCompletedHandlerProxy(p, owns));
        }

        public __MicroComIAsyncActionCompletedHandlerProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIAsyncActionCompletedHandlerVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InvokeDelegate(IntPtr @this, void* asyncInfo, AsyncStatus asyncStatus);
        static int Invoke(IntPtr @this, void* asyncInfo, AsyncStatus asyncStatus)
        {
            IAsyncActionCompletedHandler __target = null;
            try
            {
                {
                    __target = (IAsyncActionCompletedHandler)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.Invoke(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IAsyncAction>(asyncInfo, false), asyncStatus);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIAsyncActionCompletedHandlerVTable()
        {
            base.AddMethod((InvokeDelegate)Invoke);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IAsyncActionCompletedHandler), new __MicroComIAsyncActionCompletedHandlerVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIAsyncActionProxy : __MicroComIInspectableProxy, IAsyncAction
    {
        public void SetCompleted(IAsyncActionCompletedHandler handler)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(handler), (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetCompleted failed", __result);
        }

        public IAsyncActionCompletedHandler Completed
        {
            get
            {
                int __result;
                void* __marshal_ppv = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_ppv, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetCompleted failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IAsyncActionCompletedHandler>(__marshal_ppv, true);
            }
        }

        public void GetResults()
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetResults failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IAsyncAction), new Guid("5A648006-843A-4DA9-865B-9D26E5DFAD7B"), (p, owns) => new __MicroComIAsyncActionProxy(p, owns));
        }

        public __MicroComIAsyncActionProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComIAsyncActionVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetCompletedDelegate(IntPtr @this, void* handler);
        static int SetCompleted(IntPtr @this, void* handler)
        {
            IAsyncAction __target = null;
            try
            {
                {
                    __target = (IAsyncAction)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetCompleted(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IAsyncActionCompletedHandler>(handler, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCompletedDelegate(IntPtr @this, void** ppv);
        static int GetCompleted(IntPtr @this, void** ppv)
        {
            IAsyncAction __target = null;
            try
            {
                {
                    __target = (IAsyncAction)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Completed;
                        *ppv = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetResultsDelegate(IntPtr @this);
        static int GetResults(IntPtr @this)
        {
            IAsyncAction __target = null;
            try
            {
                {
                    __target = (IAsyncAction)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetResults();
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIAsyncActionVTable()
        {
            base.AddMethod((SetCompletedDelegate)SetCompleted);
            base.AddMethod((GetCompletedDelegate)GetCompleted);
            base.AddMethod((GetResultsDelegate)GetResults);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IAsyncAction), new __MicroComIAsyncActionVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIDispatcherQueueProxy : __MicroComIInspectableProxy, IDispatcherQueue
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IDispatcherQueue), new Guid("603E88E4-A338-4FFE-A457-A5CFB9CEB899"), (p, owns) => new __MicroComIDispatcherQueueProxy(p, owns));
        }

        public __MicroComIDispatcherQueueProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComIDispatcherQueueVTable : __MicroComIInspectableVTable
    {
        public __MicroComIDispatcherQueueVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IDispatcherQueue), new __MicroComIDispatcherQueueVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIDispatcherQueueControllerProxy : __MicroComIInspectableProxy, IDispatcherQueueController
    {
        public IDispatcherQueue DispatcherQueue
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetDispatcherQueue failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IDispatcherQueue>(__marshal_value, true);
            }
        }

        public IAsyncAction ShutdownQueueAsync()
        {
            int __result;
            void* __marshal_operation = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_operation, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("ShutdownQueueAsync failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IAsyncAction>(__marshal_operation, true);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IDispatcherQueueController), new Guid("22F34E66-50DB-4E36-A98D-61C01B384D20"), (p, owns) => new __MicroComIDispatcherQueueControllerProxy(p, owns));
        }

        public __MicroComIDispatcherQueueControllerProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComIDispatcherQueueControllerVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetDispatcherQueueDelegate(IntPtr @this, void** value);
        static int GetDispatcherQueue(IntPtr @this, void** value)
        {
            IDispatcherQueueController __target = null;
            try
            {
                {
                    __target = (IDispatcherQueueController)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.DispatcherQueue;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int ShutdownQueueAsyncDelegate(IntPtr @this, void** operation);
        static int ShutdownQueueAsync(IntPtr @this, void** operation)
        {
            IDispatcherQueueController __target = null;
            try
            {
                {
                    __target = (IDispatcherQueueController)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.ShutdownQueueAsync();
                        *operation = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIDispatcherQueueControllerVTable()
        {
            base.AddMethod((GetDispatcherQueueDelegate)GetDispatcherQueue);
            base.AddMethod((ShutdownQueueAsyncDelegate)ShutdownQueueAsync);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IDispatcherQueueController), new __MicroComIDispatcherQueueControllerVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIActivationFactoryProxy : __MicroComIInspectableProxy, IActivationFactory
    {
        public IntPtr ActivateInstance()
        {
            int __result;
            IntPtr instance = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &instance, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("ActivateInstance failed", __result);
            return instance;
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IActivationFactory), new Guid("00000035-0000-0000-C000-000000000046"), (p, owns) => new __MicroComIActivationFactoryProxy(p, owns));
        }

        public __MicroComIActivationFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIActivationFactoryVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int ActivateInstanceDelegate(IntPtr @this, IntPtr* instance);
        static int ActivateInstance(IntPtr @this, IntPtr* instance)
        {
            IActivationFactory __target = null;
            try
            {
                {
                    __target = (IActivationFactory)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.ActivateInstance();
                        *instance = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIActivationFactoryVTable()
        {
            base.AddMethod((ActivateInstanceDelegate)ActivateInstance);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IActivationFactory), new __MicroComIActivationFactoryVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositorProxy : __MicroComIInspectableProxy, ICompositor
    {
        public void* CreateColorKeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateColorKeyFrameAnimation failed", __result);
            return result;
        }

        public void* CreateColorBrush()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateColorBrush failed", __result);
            return result;
        }

        public ICompositionColorBrush CreateColorBrushWithColor(Avalonia.Win32.WinRT.WinRTColor* color)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, color, &__marshal_result, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateColorBrushWithColor failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionColorBrush>(__marshal_result, true);
        }

        public IContainerVisual CreateContainerVisual()
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_result, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateContainerVisual failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IContainerVisual>(__marshal_result, true);
        }

        public void* CreateCubicBezierEasingFunction(System.Numerics.Vector2 controlPoint1, System.Numerics.Vector2 controlPoint2)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, controlPoint1, controlPoint2, &result, (*PPV)[base.VTableSize + 4]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateCubicBezierEasingFunction failed", __result);
            return result;
        }

        public ICompositionEffectFactory CreateEffectFactory(IGraphicsEffect graphicsEffect)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(graphicsEffect), &__marshal_result, (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateEffectFactory failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionEffectFactory>(__marshal_result, true);
        }

        public void* CreateEffectFactoryWithProperties(void* graphicsEffect, void* animatableProperties)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, graphicsEffect, animatableProperties, &result, (*PPV)[base.VTableSize + 6]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateEffectFactoryWithProperties failed", __result);
            return result;
        }

        public void* CreateExpressionAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 7]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateExpressionAnimation failed", __result);
            return result;
        }

        public void* CreateExpressionAnimationWithExpression(IntPtr expression)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, expression, &result, (*PPV)[base.VTableSize + 8]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateExpressionAnimationWithExpression failed", __result);
            return result;
        }

        public void* CreateInsetClip()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 9]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateInsetClip failed", __result);
            return result;
        }

        public void* CreateInsetClipWithInsets(float leftInset, float topInset, float rightInset, float bottomInset)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, leftInset, topInset, rightInset, bottomInset, &result, (*PPV)[base.VTableSize + 10]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateInsetClipWithInsets failed", __result);
            return result;
        }

        public void* CreateLinearEasingFunction()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 11]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateLinearEasingFunction failed", __result);
            return result;
        }

        public void* CreatePropertySet()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 12]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreatePropertySet failed", __result);
            return result;
        }

        public void* CreateQuaternionKeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 13]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateQuaternionKeyFrameAnimation failed", __result);
            return result;
        }

        public void* CreateScalarKeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 14]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateScalarKeyFrameAnimation failed", __result);
            return result;
        }

        public void* CreateScopedBatch(CompositionBatchTypes batchType)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, batchType, &result, (*PPV)[base.VTableSize + 15]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateScopedBatch failed", __result);
            return result;
        }

        public ISpriteVisual CreateSpriteVisual()
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_result, (*PPV)[base.VTableSize + 16]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateSpriteVisual failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ISpriteVisual>(__marshal_result, true);
        }

        public ICompositionSurfaceBrush CreateSurfaceBrush()
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_result, (*PPV)[base.VTableSize + 17]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateSurfaceBrush failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionSurfaceBrush>(__marshal_result, true);
        }

        public ICompositionSurfaceBrush CreateSurfaceBrushWithSurface(ICompositionSurface surface)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(surface), &__marshal_result, (*PPV)[base.VTableSize + 18]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateSurfaceBrushWithSurface failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionSurfaceBrush>(__marshal_result, true);
        }

        public void* CreateTargetForCurrentView()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 19]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateTargetForCurrentView failed", __result);
            return result;
        }

        public void* CreateVector2KeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 20]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateVector2KeyFrameAnimation failed", __result);
            return result;
        }

        public void* CreateVector3KeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 21]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateVector3KeyFrameAnimation failed", __result);
            return result;
        }

        public void* CreateVector4KeyFrameAnimation()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 22]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateVector4KeyFrameAnimation failed", __result);
            return result;
        }

        public void* GetCommitBatch(CompositionBatchTypes batchType)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, batchType, &result, (*PPV)[base.VTableSize + 23]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetCommitBatch failed", __result);
            return result;
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositor), new Guid("B403CA50-7F8C-4E83-985F-CC45060036D8"), (p, owns) => new __MicroComICompositorProxy(p, owns));
        }

        public __MicroComICompositorProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 24;
    }

    unsafe class __MicroComICompositorVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateColorKeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateColorKeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateColorKeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateColorBrushDelegate(IntPtr @this, void** result);
        static int CreateColorBrush(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateColorBrush();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateColorBrushWithColorDelegate(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor* color, void** result);
        static int CreateColorBrushWithColor(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor* color, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateColorBrushWithColor(color);
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateContainerVisualDelegate(IntPtr @this, void** result);
        static int CreateContainerVisual(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateContainerVisual();
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateCubicBezierEasingFunctionDelegate(IntPtr @this, System.Numerics.Vector2 controlPoint1, System.Numerics.Vector2 controlPoint2, void** result);
        static int CreateCubicBezierEasingFunction(IntPtr @this, System.Numerics.Vector2 controlPoint1, System.Numerics.Vector2 controlPoint2, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateCubicBezierEasingFunction(controlPoint1, controlPoint2);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateEffectFactoryDelegate(IntPtr @this, void* graphicsEffect, void** result);
        static int CreateEffectFactory(IntPtr @this, void* graphicsEffect, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateEffectFactory(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IGraphicsEffect>(graphicsEffect, false));
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateEffectFactoryWithPropertiesDelegate(IntPtr @this, void* graphicsEffect, void* animatableProperties, void** result);
        static int CreateEffectFactoryWithProperties(IntPtr @this, void* graphicsEffect, void* animatableProperties, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateEffectFactoryWithProperties(graphicsEffect, animatableProperties);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateExpressionAnimationDelegate(IntPtr @this, void** result);
        static int CreateExpressionAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateExpressionAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateExpressionAnimationWithExpressionDelegate(IntPtr @this, IntPtr expression, void** result);
        static int CreateExpressionAnimationWithExpression(IntPtr @this, IntPtr expression, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateExpressionAnimationWithExpression(expression);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateInsetClipDelegate(IntPtr @this, void** result);
        static int CreateInsetClip(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateInsetClip();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateInsetClipWithInsetsDelegate(IntPtr @this, float leftInset, float topInset, float rightInset, float bottomInset, void** result);
        static int CreateInsetClipWithInsets(IntPtr @this, float leftInset, float topInset, float rightInset, float bottomInset, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateInsetClipWithInsets(leftInset, topInset, rightInset, bottomInset);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateLinearEasingFunctionDelegate(IntPtr @this, void** result);
        static int CreateLinearEasingFunction(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateLinearEasingFunction();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreatePropertySetDelegate(IntPtr @this, void** result);
        static int CreatePropertySet(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreatePropertySet();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateQuaternionKeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateQuaternionKeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateQuaternionKeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateScalarKeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateScalarKeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateScalarKeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateScopedBatchDelegate(IntPtr @this, CompositionBatchTypes batchType, void** result);
        static int CreateScopedBatch(IntPtr @this, CompositionBatchTypes batchType, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateScopedBatch(batchType);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateSpriteVisualDelegate(IntPtr @this, void** result);
        static int CreateSpriteVisual(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateSpriteVisual();
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateSurfaceBrushDelegate(IntPtr @this, void** result);
        static int CreateSurfaceBrush(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateSurfaceBrush();
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateSurfaceBrushWithSurfaceDelegate(IntPtr @this, void* surface, void** result);
        static int CreateSurfaceBrushWithSurface(IntPtr @this, void* surface, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateSurfaceBrushWithSurface(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionSurface>(surface, false));
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateTargetForCurrentViewDelegate(IntPtr @this, void** result);
        static int CreateTargetForCurrentView(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateTargetForCurrentView();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateVector2KeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateVector2KeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateVector2KeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateVector3KeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateVector3KeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateVector3KeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateVector4KeyFrameAnimationDelegate(IntPtr @this, void** result);
        static int CreateVector4KeyFrameAnimation(IntPtr @this, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateVector4KeyFrameAnimation();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCommitBatchDelegate(IntPtr @this, CompositionBatchTypes batchType, void** result);
        static int GetCommitBatch(IntPtr @this, CompositionBatchTypes batchType, void** result)
        {
            ICompositor __target = null;
            try
            {
                {
                    __target = (ICompositor)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetCommitBatch(batchType);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositorVTable()
        {
            base.AddMethod((CreateColorKeyFrameAnimationDelegate)CreateColorKeyFrameAnimation);
            base.AddMethod((CreateColorBrushDelegate)CreateColorBrush);
            base.AddMethod((CreateColorBrushWithColorDelegate)CreateColorBrushWithColor);
            base.AddMethod((CreateContainerVisualDelegate)CreateContainerVisual);
            base.AddMethod((CreateCubicBezierEasingFunctionDelegate)CreateCubicBezierEasingFunction);
            base.AddMethod((CreateEffectFactoryDelegate)CreateEffectFactory);
            base.AddMethod((CreateEffectFactoryWithPropertiesDelegate)CreateEffectFactoryWithProperties);
            base.AddMethod((CreateExpressionAnimationDelegate)CreateExpressionAnimation);
            base.AddMethod((CreateExpressionAnimationWithExpressionDelegate)CreateExpressionAnimationWithExpression);
            base.AddMethod((CreateInsetClipDelegate)CreateInsetClip);
            base.AddMethod((CreateInsetClipWithInsetsDelegate)CreateInsetClipWithInsets);
            base.AddMethod((CreateLinearEasingFunctionDelegate)CreateLinearEasingFunction);
            base.AddMethod((CreatePropertySetDelegate)CreatePropertySet);
            base.AddMethod((CreateQuaternionKeyFrameAnimationDelegate)CreateQuaternionKeyFrameAnimation);
            base.AddMethod((CreateScalarKeyFrameAnimationDelegate)CreateScalarKeyFrameAnimation);
            base.AddMethod((CreateScopedBatchDelegate)CreateScopedBatch);
            base.AddMethod((CreateSpriteVisualDelegate)CreateSpriteVisual);
            base.AddMethod((CreateSurfaceBrushDelegate)CreateSurfaceBrush);
            base.AddMethod((CreateSurfaceBrushWithSurfaceDelegate)CreateSurfaceBrushWithSurface);
            base.AddMethod((CreateTargetForCurrentViewDelegate)CreateTargetForCurrentView);
            base.AddMethod((CreateVector2KeyFrameAnimationDelegate)CreateVector2KeyFrameAnimation);
            base.AddMethod((CreateVector3KeyFrameAnimationDelegate)CreateVector3KeyFrameAnimation);
            base.AddMethod((CreateVector4KeyFrameAnimationDelegate)CreateVector4KeyFrameAnimation);
            base.AddMethod((GetCommitBatchDelegate)GetCommitBatch);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositor), new __MicroComICompositorVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositor2Proxy : __MicroComIInspectableProxy, ICompositor2
    {
        public void* CreateAmbientLight()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAmbientLight failed", __result);
            return result;
        }

        public void* CreateAnimationGroup()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateAnimationGroup failed", __result);
            return result;
        }

        public ICompositionBackdropBrush CreateBackdropBrush()
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_result, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateBackdropBrush failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionBackdropBrush>(__marshal_result, true);
        }

        public void* CreateDistantLight()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateDistantLight failed", __result);
            return result;
        }

        public void* CreateDropShadow()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 4]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateDropShadow failed", __result);
            return result;
        }

        public void* CreateImplicitAnimationCollection()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateImplicitAnimationCollection failed", __result);
            return result;
        }

        public void* CreateLayerVisual()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 6]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateLayerVisual failed", __result);
            return result;
        }

        public void* CreateMaskBrush()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 7]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateMaskBrush failed", __result);
            return result;
        }

        public void* CreateNineGridBrush()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 8]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateNineGridBrush failed", __result);
            return result;
        }

        public void* CreatePointLight()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 9]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreatePointLight failed", __result);
            return result;
        }

        public void* CreateSpotLight()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 10]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateSpotLight failed", __result);
            return result;
        }

        public void* CreateStepEasingFunction()
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &result, (*PPV)[base.VTableSize + 11]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateStepEasingFunction failed", __result);
            return result;
        }

        public void* CreateStepEasingFunctionWithStepCount(int stepCount)
        {
            int __result;
            void* result = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, stepCount, &result, (*PPV)[base.VTableSize + 12]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateStepEasingFunctionWithStepCount failed", __result);
            return result;
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositor2), new Guid("735081DC-5E24-45DA-A38F-E32CC349A9A0"), (p, owns) => new __MicroComICompositor2Proxy(p, owns));
        }

        public __MicroComICompositor2Proxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 13;
    }

    unsafe class __MicroComICompositor2VTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAmbientLightDelegate(IntPtr @this, void** result);
        static int CreateAmbientLight(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateAmbientLight();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateAnimationGroupDelegate(IntPtr @this, void** result);
        static int CreateAnimationGroup(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateAnimationGroup();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateBackdropBrushDelegate(IntPtr @this, void** result);
        static int CreateBackdropBrush(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateBackdropBrush();
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDistantLightDelegate(IntPtr @this, void** result);
        static int CreateDistantLight(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateDistantLight();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDropShadowDelegate(IntPtr @this, void** result);
        static int CreateDropShadow(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateDropShadow();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateImplicitAnimationCollectionDelegate(IntPtr @this, void** result);
        static int CreateImplicitAnimationCollection(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateImplicitAnimationCollection();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateLayerVisualDelegate(IntPtr @this, void** result);
        static int CreateLayerVisual(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateLayerVisual();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateMaskBrushDelegate(IntPtr @this, void** result);
        static int CreateMaskBrush(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateMaskBrush();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateNineGridBrushDelegate(IntPtr @this, void** result);
        static int CreateNineGridBrush(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateNineGridBrush();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreatePointLightDelegate(IntPtr @this, void** result);
        static int CreatePointLight(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreatePointLight();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateSpotLightDelegate(IntPtr @this, void** result);
        static int CreateSpotLight(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateSpotLight();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateStepEasingFunctionDelegate(IntPtr @this, void** result);
        static int CreateStepEasingFunction(IntPtr @this, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateStepEasingFunction();
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateStepEasingFunctionWithStepCountDelegate(IntPtr @this, int stepCount, void** result);
        static int CreateStepEasingFunctionWithStepCount(IntPtr @this, int stepCount, void** result)
        {
            ICompositor2 __target = null;
            try
            {
                {
                    __target = (ICompositor2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateStepEasingFunctionWithStepCount(stepCount);
                        *result = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositor2VTable()
        {
            base.AddMethod((CreateAmbientLightDelegate)CreateAmbientLight);
            base.AddMethod((CreateAnimationGroupDelegate)CreateAnimationGroup);
            base.AddMethod((CreateBackdropBrushDelegate)CreateBackdropBrush);
            base.AddMethod((CreateDistantLightDelegate)CreateDistantLight);
            base.AddMethod((CreateDropShadowDelegate)CreateDropShadow);
            base.AddMethod((CreateImplicitAnimationCollectionDelegate)CreateImplicitAnimationCollection);
            base.AddMethod((CreateLayerVisualDelegate)CreateLayerVisual);
            base.AddMethod((CreateMaskBrushDelegate)CreateMaskBrush);
            base.AddMethod((CreateNineGridBrushDelegate)CreateNineGridBrush);
            base.AddMethod((CreatePointLightDelegate)CreatePointLight);
            base.AddMethod((CreateSpotLightDelegate)CreateSpotLight);
            base.AddMethod((CreateStepEasingFunctionDelegate)CreateStepEasingFunction);
            base.AddMethod((CreateStepEasingFunctionWithStepCountDelegate)CreateStepEasingFunctionWithStepCount);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositor2), new __MicroComICompositor2VTable().CreateVTable());
    }

    unsafe internal partial class __MicroComISpriteVisualProxy : __MicroComIInspectableProxy, ISpriteVisual
    {
        public ICompositionBrush Brush
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetBrush failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionBrush>(__marshal_value, true);
            }
        }

        public void SetBrush(ICompositionBrush value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(value), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetBrush failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ISpriteVisual), new Guid("08E05581-1AD1-4F97-9757-402D76E4233B"), (p, owns) => new __MicroComISpriteVisualProxy(p, owns));
        }

        public __MicroComISpriteVisualProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComISpriteVisualVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetBrushDelegate(IntPtr @this, void** value);
        static int GetBrush(IntPtr @this, void** value)
        {
            ISpriteVisual __target = null;
            try
            {
                {
                    __target = (ISpriteVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Brush;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetBrushDelegate(IntPtr @this, void* value);
        static int SetBrush(IntPtr @this, void* value)
        {
            ISpriteVisual __target = null;
            try
            {
                {
                    __target = (ISpriteVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetBrush(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionBrush>(value, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComISpriteVisualVTable()
        {
            base.AddMethod((GetBrushDelegate)GetBrush);
            base.AddMethod((SetBrushDelegate)SetBrush);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ISpriteVisual), new __MicroComISpriteVisualVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionDrawingSurfaceInteropProxy : Avalonia.MicroCom.MicroComProxyBase, ICompositionDrawingSurfaceInterop
    {
        public Avalonia.Win32.Interop.UnmanagedMethods.POINT BeginDraw(Avalonia.Win32.Interop.UnmanagedMethods.RECT* updateRect, Guid* iid, void** updateObject)
        {
            int __result;
            Avalonia.Win32.Interop.UnmanagedMethods.POINT updateOffset = default;
            __result = (int)LocalInterop.CalliStdCallint(PPV, updateRect, iid, updateObject, &updateOffset, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("BeginDraw failed", __result);
            return updateOffset;
        }

        public void EndDraw()
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("EndDraw failed", __result);
        }

        public void Resize(Avalonia.Win32.Interop.UnmanagedMethods.POINT sizePixels)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, sizePixels, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Resize failed", __result);
        }

        public void Scroll(Avalonia.Win32.Interop.UnmanagedMethods.RECT* scrollRect, Avalonia.Win32.Interop.UnmanagedMethods.RECT* clipRect, int offsetX, int offsetY)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, scrollRect, clipRect, offsetX, offsetY, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Scroll failed", __result);
        }

        public void ResumeDraw()
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, (*PPV)[base.VTableSize + 4]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("ResumeDraw failed", __result);
        }

        public void SuspendDraw()
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SuspendDraw failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionDrawingSurfaceInterop), new Guid("FD04E6E3-FE0C-4C3C-AB19-A07601A576EE"), (p, owns) => new __MicroComICompositionDrawingSurfaceInteropProxy(p, owns));
        }

        public __MicroComICompositionDrawingSurfaceInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 6;
    }

    unsafe class __MicroComICompositionDrawingSurfaceInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int BeginDrawDelegate(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.RECT* updateRect, Guid* iid, void** updateObject, Avalonia.Win32.Interop.UnmanagedMethods.POINT* updateOffset);
        static int BeginDraw(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.RECT* updateRect, Guid* iid, void** updateObject, Avalonia.Win32.Interop.UnmanagedMethods.POINT* updateOffset)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.BeginDraw(updateRect, iid, updateObject);
                        *updateOffset = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int EndDrawDelegate(IntPtr @this);
        static int EndDraw(IntPtr @this)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.EndDraw();
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int ResizeDelegate(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.POINT sizePixels);
        static int Resize(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.POINT sizePixels)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.Resize(sizePixels);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int ScrollDelegate(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.RECT* scrollRect, Avalonia.Win32.Interop.UnmanagedMethods.RECT* clipRect, int offsetX, int offsetY);
        static int Scroll(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.RECT* scrollRect, Avalonia.Win32.Interop.UnmanagedMethods.RECT* clipRect, int offsetX, int offsetY)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.Scroll(scrollRect, clipRect, offsetX, offsetY);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int ResumeDrawDelegate(IntPtr @this);
        static int ResumeDraw(IntPtr @this)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.ResumeDraw();
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SuspendDrawDelegate(IntPtr @this);
        static int SuspendDraw(IntPtr @this)
        {
            ICompositionDrawingSurfaceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurfaceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SuspendDraw();
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionDrawingSurfaceInteropVTable()
        {
            base.AddMethod((BeginDrawDelegate)BeginDraw);
            base.AddMethod((EndDrawDelegate)EndDraw);
            base.AddMethod((ResizeDelegate)Resize);
            base.AddMethod((ScrollDelegate)Scroll);
            base.AddMethod((ResumeDrawDelegate)ResumeDraw);
            base.AddMethod((SuspendDrawDelegate)SuspendDraw);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionDrawingSurfaceInterop), new __MicroComICompositionDrawingSurfaceInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionGraphicsDeviceInteropProxy : Avalonia.MicroCom.MicroComProxyBase, ICompositionGraphicsDeviceInterop
    {
        public IUnknown RenderingDevice
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRenderingDevice failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IUnknown>(__marshal_value, true);
            }
        }

        public void SetRenderingDevice(IUnknown value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(value), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRenderingDevice failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionGraphicsDeviceInterop), new Guid("A116FF71-F8BF-4C8A-9C98-70779A32A9C8"), (p, owns) => new __MicroComICompositionGraphicsDeviceInteropProxy(p, owns));
        }

        public __MicroComICompositionGraphicsDeviceInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComICompositionGraphicsDeviceInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRenderingDeviceDelegate(IntPtr @this, void** value);
        static int GetRenderingDevice(IntPtr @this, void** value)
        {
            ICompositionGraphicsDeviceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionGraphicsDeviceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RenderingDevice;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRenderingDeviceDelegate(IntPtr @this, void* value);
        static int SetRenderingDevice(IntPtr @this, void* value)
        {
            ICompositionGraphicsDeviceInterop __target = null;
            try
            {
                {
                    __target = (ICompositionGraphicsDeviceInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRenderingDevice(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IUnknown>(value, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionGraphicsDeviceInteropVTable()
        {
            base.AddMethod((GetRenderingDeviceDelegate)GetRenderingDevice);
            base.AddMethod((SetRenderingDeviceDelegate)SetRenderingDevice);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionGraphicsDeviceInterop), new __MicroComICompositionGraphicsDeviceInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositorInteropProxy : Avalonia.MicroCom.MicroComProxyBase, ICompositorInterop
    {
        public ICompositionSurface CreateCompositionSurfaceForHandle(IntPtr swapChain)
        {
            int __result;
            void* __marshal_res = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, swapChain, &__marshal_res, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateCompositionSurfaceForHandle failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionSurface>(__marshal_res, true);
        }

        public ICompositionSurface CreateCompositionSurfaceForSwapChain(IUnknown swapChain)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(swapChain), &__marshal_result, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateCompositionSurfaceForSwapChain failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionSurface>(__marshal_result, true);
        }

        public ICompositionGraphicsDevice CreateGraphicsDevice(IUnknown renderingDevice)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(renderingDevice), &__marshal_result, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateGraphicsDevice failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionGraphicsDevice>(__marshal_result, true);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositorInterop), new Guid("25297D5C-3AD4-4C9C-B5CF-E36A38512330"), (p, owns) => new __MicroComICompositorInteropProxy(p, owns));
        }

        public __MicroComICompositorInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComICompositorInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateCompositionSurfaceForHandleDelegate(IntPtr @this, IntPtr swapChain, void** res);
        static int CreateCompositionSurfaceForHandle(IntPtr @this, IntPtr swapChain, void** res)
        {
            ICompositorInterop __target = null;
            try
            {
                {
                    __target = (ICompositorInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateCompositionSurfaceForHandle(swapChain);
                        *res = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateCompositionSurfaceForSwapChainDelegate(IntPtr @this, void* swapChain, void** result);
        static int CreateCompositionSurfaceForSwapChain(IntPtr @this, void* swapChain, void** result)
        {
            ICompositorInterop __target = null;
            try
            {
                {
                    __target = (ICompositorInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateCompositionSurfaceForSwapChain(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IUnknown>(swapChain, false));
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateGraphicsDeviceDelegate(IntPtr @this, void* renderingDevice, void** result);
        static int CreateGraphicsDevice(IntPtr @this, void* renderingDevice, void** result)
        {
            ICompositorInterop __target = null;
            try
            {
                {
                    __target = (ICompositorInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateGraphicsDevice(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IUnknown>(renderingDevice, false));
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositorInteropVTable()
        {
            base.AddMethod((CreateCompositionSurfaceForHandleDelegate)CreateCompositionSurfaceForHandle);
            base.AddMethod((CreateCompositionSurfaceForSwapChainDelegate)CreateCompositionSurfaceForSwapChain);
            base.AddMethod((CreateGraphicsDeviceDelegate)CreateGraphicsDevice);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositorInterop), new __MicroComICompositorInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComISwapChainInteropProxy : Avalonia.MicroCom.MicroComProxyBase, ISwapChainInterop
    {
        public void SetSwapChain(IUnknown swapChain)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(swapChain), (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetSwapChain failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ISwapChainInterop), new Guid("26f496a0-7f38-45fb-88f7-faaabe67dd59"), (p, owns) => new __MicroComISwapChainInteropProxy(p, owns));
        }

        public __MicroComISwapChainInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComISwapChainInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetSwapChainDelegate(IntPtr @this, void* swapChain);
        static int SetSwapChain(IntPtr @this, void* swapChain)
        {
            ISwapChainInterop __target = null;
            try
            {
                {
                    __target = (ISwapChainInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetSwapChain(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IUnknown>(swapChain, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComISwapChainInteropVTable()
        {
            base.AddMethod((SetSwapChainDelegate)SetSwapChain);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ISwapChainInterop), new __MicroComISwapChainInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositorDesktopInteropProxy : Avalonia.MicroCom.MicroComProxyBase, ICompositorDesktopInterop
    {
        public IDesktopWindowTarget CreateDesktopWindowTarget(IntPtr hwndTarget, int isTopmost)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, hwndTarget, isTopmost, &__marshal_result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateDesktopWindowTarget failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IDesktopWindowTarget>(__marshal_result, true);
        }

        public void EnsureOnThread(int threadId)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, threadId, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("EnsureOnThread failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositorDesktopInterop), new Guid("29E691FA-4567-4DCA-B319-D0F207EB6807"), (p, owns) => new __MicroComICompositorDesktopInteropProxy(p, owns));
        }

        public __MicroComICompositorDesktopInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComICompositorDesktopInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDesktopWindowTargetDelegate(IntPtr @this, IntPtr hwndTarget, int isTopmost, void** result);
        static int CreateDesktopWindowTarget(IntPtr @this, IntPtr hwndTarget, int isTopmost, void** result)
        {
            ICompositorDesktopInterop __target = null;
            try
            {
                {
                    __target = (ICompositorDesktopInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateDesktopWindowTarget(hwndTarget, isTopmost);
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int EnsureOnThreadDelegate(IntPtr @this, int threadId);
        static int EnsureOnThread(IntPtr @this, int threadId)
        {
            ICompositorDesktopInterop __target = null;
            try
            {
                {
                    __target = (ICompositorDesktopInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.EnsureOnThread(threadId);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositorDesktopInteropVTable()
        {
            base.AddMethod((CreateDesktopWindowTargetDelegate)CreateDesktopWindowTarget);
            base.AddMethod((EnsureOnThreadDelegate)EnsureOnThread);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositorDesktopInterop), new __MicroComICompositorDesktopInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIDesktopWindowTargetInteropProxy : Avalonia.MicroCom.MicroComProxyBase, IDesktopWindowTargetInterop
    {
        public IntPtr HWnd
        {
            get
            {
                int __result;
                IntPtr value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetHWnd failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IDesktopWindowTargetInterop), new Guid("35DBF59E-E3F9-45B0-81E7-FE75F4145DC9"), (p, owns) => new __MicroComIDesktopWindowTargetInteropProxy(p, owns));
        }

        public __MicroComIDesktopWindowTargetInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIDesktopWindowTargetInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetHWndDelegate(IntPtr @this, IntPtr* value);
        static int GetHWnd(IntPtr @this, IntPtr* value)
        {
            IDesktopWindowTargetInterop __target = null;
            try
            {
                {
                    __target = (IDesktopWindowTargetInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.HWnd;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIDesktopWindowTargetInteropVTable()
        {
            base.AddMethod((GetHWndDelegate)GetHWnd);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IDesktopWindowTargetInterop), new __MicroComIDesktopWindowTargetInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIDesktopWindowContentBridgeInteropProxy : Avalonia.MicroCom.MicroComProxyBase, IDesktopWindowContentBridgeInterop
    {
        public void Initialize(ICompositor compositor, IntPtr parentHwnd)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(compositor), parentHwnd, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Initialize failed", __result);
        }

        public IntPtr HWnd
        {
            get
            {
                int __result;
                IntPtr value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetHWnd failed", __result);
                return value;
            }
        }

        public float AppliedScaleFactor
        {
            get
            {
                int __result;
                float value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetAppliedScaleFactor failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IDesktopWindowContentBridgeInterop), new Guid("37642806-F421-4FD0-9F82-23AE7C776182"), (p, owns) => new __MicroComIDesktopWindowContentBridgeInteropProxy(p, owns));
        }

        public __MicroComIDesktopWindowContentBridgeInteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComIDesktopWindowContentBridgeInteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InitializeDelegate(IntPtr @this, void* compositor, IntPtr parentHwnd);
        static int Initialize(IntPtr @this, void* compositor, IntPtr parentHwnd)
        {
            IDesktopWindowContentBridgeInterop __target = null;
            try
            {
                {
                    __target = (IDesktopWindowContentBridgeInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.Initialize(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositor>(compositor, false), parentHwnd);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetHWndDelegate(IntPtr @this, IntPtr* value);
        static int GetHWnd(IntPtr @this, IntPtr* value)
        {
            IDesktopWindowContentBridgeInterop __target = null;
            try
            {
                {
                    __target = (IDesktopWindowContentBridgeInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.HWnd;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetAppliedScaleFactorDelegate(IntPtr @this, float* value);
        static int GetAppliedScaleFactor(IntPtr @this, float* value)
        {
            IDesktopWindowContentBridgeInterop __target = null;
            try
            {
                {
                    __target = (IDesktopWindowContentBridgeInterop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.AppliedScaleFactor;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIDesktopWindowContentBridgeInteropVTable()
        {
            base.AddMethod((InitializeDelegate)Initialize);
            base.AddMethod((GetHWndDelegate)GetHWnd);
            base.AddMethod((GetAppliedScaleFactorDelegate)GetAppliedScaleFactor);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IDesktopWindowContentBridgeInterop), new __MicroComIDesktopWindowContentBridgeInteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionGraphicsDeviceProxy : __MicroComIInspectableProxy, ICompositionGraphicsDevice
    {
        public ICompositionDrawingSurface CreateDrawingSurface(Avalonia.Win32.Interop.UnmanagedMethods.SIZE sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, sizePixels, pixelFormat, alphaMode, &__marshal_result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateDrawingSurface failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionDrawingSurface>(__marshal_result, true);
        }

        public void AddRenderingDeviceReplaced(void* handler, void* token)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, handler, token, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("AddRenderingDeviceReplaced failed", __result);
        }

        public void RemoveRenderingDeviceReplaced(int token)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, token, (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("RemoveRenderingDeviceReplaced failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionGraphicsDevice), new Guid("FB22C6E1-80A2-4667-9936-DBEAF6EEFE95"), (p, owns) => new __MicroComICompositionGraphicsDeviceProxy(p, owns));
        }

        public __MicroComICompositionGraphicsDeviceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComICompositionGraphicsDeviceVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDrawingSurfaceDelegate(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.SIZE sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode, void** result);
        static int CreateDrawingSurface(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.SIZE sizePixels, DirectXPixelFormat pixelFormat, DirectXAlphaMode alphaMode, void** result)
        {
            ICompositionGraphicsDevice __target = null;
            try
            {
                {
                    __target = (ICompositionGraphicsDevice)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateDrawingSurface(sizePixels, pixelFormat, alphaMode);
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int AddRenderingDeviceReplacedDelegate(IntPtr @this, void* handler, void* token);
        static int AddRenderingDeviceReplaced(IntPtr @this, void* handler, void* token)
        {
            ICompositionGraphicsDevice __target = null;
            try
            {
                {
                    __target = (ICompositionGraphicsDevice)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.AddRenderingDeviceReplaced(handler, token);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int RemoveRenderingDeviceReplacedDelegate(IntPtr @this, int token);
        static int RemoveRenderingDeviceReplaced(IntPtr @this, int token)
        {
            ICompositionGraphicsDevice __target = null;
            try
            {
                {
                    __target = (ICompositionGraphicsDevice)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.RemoveRenderingDeviceReplaced(token);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionGraphicsDeviceVTable()
        {
            base.AddMethod((CreateDrawingSurfaceDelegate)CreateDrawingSurface);
            base.AddMethod((AddRenderingDeviceReplacedDelegate)AddRenderingDeviceReplaced);
            base.AddMethod((RemoveRenderingDeviceReplacedDelegate)RemoveRenderingDeviceReplaced);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionGraphicsDevice), new __MicroComICompositionGraphicsDeviceVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionSurfaceProxy : __MicroComIInspectableProxy, ICompositionSurface
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionSurface), new Guid("1527540D-42C7-47A6-A408-668F79A90DFB"), (p, owns) => new __MicroComICompositionSurfaceProxy(p, owns));
        }

        public __MicroComICompositionSurfaceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComICompositionSurfaceVTable : __MicroComIInspectableVTable
    {
        public __MicroComICompositionSurfaceVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionSurface), new __MicroComICompositionSurfaceVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIDesktopWindowTargetProxy : __MicroComIInspectableProxy, IDesktopWindowTarget
    {
        public int IsTopmost
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetIsTopmost failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IDesktopWindowTarget), new Guid("6329D6CA-3366-490E-9DB3-25312929AC51"), (p, owns) => new __MicroComIDesktopWindowTargetProxy(p, owns));
        }

        public __MicroComIDesktopWindowTargetProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIDesktopWindowTargetVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsTopmostDelegate(IntPtr @this, int* value);
        static int GetIsTopmost(IntPtr @this, int* value)
        {
            IDesktopWindowTarget __target = null;
            try
            {
                {
                    __target = (IDesktopWindowTarget)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.IsTopmost;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIDesktopWindowTargetVTable()
        {
            base.AddMethod((GetIsTopmostDelegate)GetIsTopmost);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IDesktopWindowTarget), new __MicroComIDesktopWindowTargetVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionDrawingSurfaceProxy : __MicroComIInspectableProxy, ICompositionDrawingSurface
    {
        public DirectXAlphaMode AlphaMode
        {
            get
            {
                int __result;
                DirectXAlphaMode value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetAlphaMode failed", __result);
                return value;
            }
        }

        public DirectXPixelFormat PixelFormat
        {
            get
            {
                int __result;
                DirectXPixelFormat value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetPixelFormat failed", __result);
                return value;
            }
        }

        public Avalonia.Win32.Interop.UnmanagedMethods.POINT Size
        {
            get
            {
                int __result;
                Avalonia.Win32.Interop.UnmanagedMethods.POINT value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetSize failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionDrawingSurface), new Guid("A166C300-FAD0-4D11-9E67-E433162FF49E"), (p, owns) => new __MicroComICompositionDrawingSurfaceProxy(p, owns));
        }

        public __MicroComICompositionDrawingSurfaceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComICompositionDrawingSurfaceVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetAlphaModeDelegate(IntPtr @this, DirectXAlphaMode* value);
        static int GetAlphaMode(IntPtr @this, DirectXAlphaMode* value)
        {
            ICompositionDrawingSurface __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurface)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.AlphaMode;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetPixelFormatDelegate(IntPtr @this, DirectXPixelFormat* value);
        static int GetPixelFormat(IntPtr @this, DirectXPixelFormat* value)
        {
            ICompositionDrawingSurface __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurface)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.PixelFormat;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSizeDelegate(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.POINT* value);
        static int GetSize(IntPtr @this, Avalonia.Win32.Interop.UnmanagedMethods.POINT* value)
        {
            ICompositionDrawingSurface __target = null;
            try
            {
                {
                    __target = (ICompositionDrawingSurface)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Size;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionDrawingSurfaceVTable()
        {
            base.AddMethod((GetAlphaModeDelegate)GetAlphaMode);
            base.AddMethod((GetPixelFormatDelegate)GetPixelFormat);
            base.AddMethod((GetSizeDelegate)GetSize);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionDrawingSurface), new __MicroComICompositionDrawingSurfaceVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionSurfaceBrushProxy : __MicroComIInspectableProxy, ICompositionSurfaceBrush
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionSurfaceBrush), new Guid("AD016D79-1E4C-4C0D-9C29-83338C87C162"), (p, owns) => new __MicroComICompositionSurfaceBrushProxy(p, owns));
        }

        public __MicroComICompositionSurfaceBrushProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComICompositionSurfaceBrushVTable : __MicroComIInspectableVTable
    {
        public __MicroComICompositionSurfaceBrushVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionSurfaceBrush), new __MicroComICompositionSurfaceBrushVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionBrushProxy : __MicroComIInspectableProxy, ICompositionBrush
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionBrush), new Guid("AB0D7608-30C0-40E9-B568-B60A6BD1FB46"), (p, owns) => new __MicroComICompositionBrushProxy(p, owns));
        }

        public __MicroComICompositionBrushProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComICompositionBrushVTable : __MicroComIInspectableVTable
    {
        public __MicroComICompositionBrushVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionBrush), new __MicroComICompositionBrushVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIVisualProxy : __MicroComIInspectableProxy, IVisual
    {
        public System.Numerics.Vector2 AnchorPoint
        {
            get
            {
                int __result;
                System.Numerics.Vector2 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetAnchorPoint failed", __result);
                return value;
            }
        }

        public void SetAnchorPoint(System.Numerics.Vector2 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetAnchorPoint failed", __result);
        }

        public CompositionBackfaceVisibility BackfaceVisibility
        {
            get
            {
                int __result;
                CompositionBackfaceVisibility value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetBackfaceVisibility failed", __result);
                return value;
            }
        }

        public void SetBackfaceVisibility(CompositionBackfaceVisibility value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetBackfaceVisibility failed", __result);
        }

        public CompositionBorderMode BorderMode
        {
            get
            {
                int __result;
                CompositionBorderMode value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 4]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetBorderMode failed", __result);
                return value;
            }
        }

        public void SetBorderMode(CompositionBorderMode value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetBorderMode failed", __result);
        }

        public System.Numerics.Vector3 CenterPoint
        {
            get
            {
                int __result;
                System.Numerics.Vector3 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 6]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetCenterPoint failed", __result);
                return value;
            }
        }

        public void SetCenterPoint(System.Numerics.Vector3 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 7]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetCenterPoint failed", __result);
        }

        public void* Clip
        {
            get
            {
                int __result;
                void* value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 8]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetClip failed", __result);
                return value;
            }
        }

        public void SetClip(void* value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 9]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetClip failed", __result);
        }

        public CompositionCompositeMode CompositeMode
        {
            get
            {
                int __result;
                CompositionCompositeMode value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 10]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetCompositeMode failed", __result);
                return value;
            }
        }

        public void SetCompositeMode(CompositionCompositeMode value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 11]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetCompositeMode failed", __result);
        }

        public int IsVisible
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 12]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetIsVisible failed", __result);
                return value;
            }
        }

        public void SetIsVisible(int value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 13]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetIsVisible failed", __result);
        }

        public System.Numerics.Vector3 Offset
        {
            get
            {
                int __result;
                System.Numerics.Vector3 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 14]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetOffset failed", __result);
                return value;
            }
        }

        public void SetOffset(System.Numerics.Vector3 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 15]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetOffset failed", __result);
        }

        public float Opacity
        {
            get
            {
                int __result;
                float value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 16]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetOpacity failed", __result);
                return value;
            }
        }

        public void SetOpacity(float value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 17]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetOpacity failed", __result);
        }

        public System.Numerics.Quaternion Orientation
        {
            get
            {
                int __result;
                System.Numerics.Quaternion value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 18]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetOrientation failed", __result);
                return value;
            }
        }

        public void SetOrientation(System.Numerics.Quaternion value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 19]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetOrientation failed", __result);
        }

        public IContainerVisual Parent
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 20]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetParent failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IContainerVisual>(__marshal_value, true);
            }
        }

        public float RotationAngle
        {
            get
            {
                int __result;
                float value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 21]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRotationAngle failed", __result);
                return value;
            }
        }

        public void SetRotationAngle(float value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 22]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRotationAngle failed", __result);
        }

        public float RotationAngleInDegrees
        {
            get
            {
                int __result;
                float value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 23]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRotationAngleInDegrees failed", __result);
                return value;
            }
        }

        public void SetRotationAngleInDegrees(float value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 24]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRotationAngleInDegrees failed", __result);
        }

        public System.Numerics.Vector3 RotationAxis
        {
            get
            {
                int __result;
                System.Numerics.Vector3 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 25]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRotationAxis failed", __result);
                return value;
            }
        }

        public void SetRotationAxis(System.Numerics.Vector3 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 26]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRotationAxis failed", __result);
        }

        public System.Numerics.Vector3 Scale
        {
            get
            {
                int __result;
                System.Numerics.Vector3 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 27]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetScale failed", __result);
                return value;
            }
        }

        public void SetScale(System.Numerics.Vector3 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 28]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetScale failed", __result);
        }

        public System.Numerics.Vector2 Size
        {
            get
            {
                int __result;
                System.Numerics.Vector2 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 29]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetSize failed", __result);
                return value;
            }
        }

        public void SetSize(System.Numerics.Vector2 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 30]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetSize failed", __result);
        }

        public System.Numerics.Matrix4x4 TransformMatrix
        {
            get
            {
                int __result;
                System.Numerics.Matrix4x4 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 31]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetTransformMatrix failed", __result);
                return value;
            }
        }

        public void SetTransformMatrix(System.Numerics.Matrix4x4 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 32]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetTransformMatrix failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IVisual), new Guid("117E202D-A859-4C89-873B-C2AA566788E3"), (p, owns) => new __MicroComIVisualProxy(p, owns));
        }

        public __MicroComIVisualProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 33;
    }

    unsafe class __MicroComIVisualVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetAnchorPointDelegate(IntPtr @this, System.Numerics.Vector2* value);
        static int GetAnchorPoint(IntPtr @this, System.Numerics.Vector2* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.AnchorPoint;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetAnchorPointDelegate(IntPtr @this, System.Numerics.Vector2 value);
        static int SetAnchorPoint(IntPtr @this, System.Numerics.Vector2 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetAnchorPoint(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetBackfaceVisibilityDelegate(IntPtr @this, CompositionBackfaceVisibility* value);
        static int GetBackfaceVisibility(IntPtr @this, CompositionBackfaceVisibility* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.BackfaceVisibility;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetBackfaceVisibilityDelegate(IntPtr @this, CompositionBackfaceVisibility value);
        static int SetBackfaceVisibility(IntPtr @this, CompositionBackfaceVisibility value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetBackfaceVisibility(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetBorderModeDelegate(IntPtr @this, CompositionBorderMode* value);
        static int GetBorderMode(IntPtr @this, CompositionBorderMode* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.BorderMode;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetBorderModeDelegate(IntPtr @this, CompositionBorderMode value);
        static int SetBorderMode(IntPtr @this, CompositionBorderMode value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetBorderMode(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCenterPointDelegate(IntPtr @this, System.Numerics.Vector3* value);
        static int GetCenterPoint(IntPtr @this, System.Numerics.Vector3* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CenterPoint;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetCenterPointDelegate(IntPtr @this, System.Numerics.Vector3 value);
        static int SetCenterPoint(IntPtr @this, System.Numerics.Vector3 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetCenterPoint(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetClipDelegate(IntPtr @this, void** value);
        static int GetClip(IntPtr @this, void** value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Clip;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetClipDelegate(IntPtr @this, void* value);
        static int SetClip(IntPtr @this, void* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetClip(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCompositeModeDelegate(IntPtr @this, CompositionCompositeMode* value);
        static int GetCompositeMode(IntPtr @this, CompositionCompositeMode* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CompositeMode;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetCompositeModeDelegate(IntPtr @this, CompositionCompositeMode value);
        static int SetCompositeMode(IntPtr @this, CompositionCompositeMode value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetCompositeMode(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetIsVisibleDelegate(IntPtr @this, int* value);
        static int GetIsVisible(IntPtr @this, int* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.IsVisible;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetIsVisibleDelegate(IntPtr @this, int value);
        static int SetIsVisible(IntPtr @this, int value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetIsVisible(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetOffsetDelegate(IntPtr @this, System.Numerics.Vector3* value);
        static int GetOffset(IntPtr @this, System.Numerics.Vector3* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Offset;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetOffsetDelegate(IntPtr @this, System.Numerics.Vector3 value);
        static int SetOffset(IntPtr @this, System.Numerics.Vector3 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetOffset(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetOpacityDelegate(IntPtr @this, float* value);
        static int GetOpacity(IntPtr @this, float* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Opacity;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetOpacityDelegate(IntPtr @this, float value);
        static int SetOpacity(IntPtr @this, float value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetOpacity(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetOrientationDelegate(IntPtr @this, System.Numerics.Quaternion* value);
        static int GetOrientation(IntPtr @this, System.Numerics.Quaternion* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Orientation;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetOrientationDelegate(IntPtr @this, System.Numerics.Quaternion value);
        static int SetOrientation(IntPtr @this, System.Numerics.Quaternion value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetOrientation(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetParentDelegate(IntPtr @this, void** value);
        static int GetParent(IntPtr @this, void** value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Parent;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRotationAngleDelegate(IntPtr @this, float* value);
        static int GetRotationAngle(IntPtr @this, float* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RotationAngle;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRotationAngleDelegate(IntPtr @this, float value);
        static int SetRotationAngle(IntPtr @this, float value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRotationAngle(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRotationAngleInDegreesDelegate(IntPtr @this, float* value);
        static int GetRotationAngleInDegrees(IntPtr @this, float* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RotationAngleInDegrees;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRotationAngleInDegreesDelegate(IntPtr @this, float value);
        static int SetRotationAngleInDegrees(IntPtr @this, float value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRotationAngleInDegrees(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRotationAxisDelegate(IntPtr @this, System.Numerics.Vector3* value);
        static int GetRotationAxis(IntPtr @this, System.Numerics.Vector3* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RotationAxis;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRotationAxisDelegate(IntPtr @this, System.Numerics.Vector3 value);
        static int SetRotationAxis(IntPtr @this, System.Numerics.Vector3 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRotationAxis(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetScaleDelegate(IntPtr @this, System.Numerics.Vector3* value);
        static int GetScale(IntPtr @this, System.Numerics.Vector3* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Scale;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetScaleDelegate(IntPtr @this, System.Numerics.Vector3 value);
        static int SetScale(IntPtr @this, System.Numerics.Vector3 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetScale(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSizeDelegate(IntPtr @this, System.Numerics.Vector2* value);
        static int GetSize(IntPtr @this, System.Numerics.Vector2* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Size;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetSizeDelegate(IntPtr @this, System.Numerics.Vector2 value);
        static int SetSize(IntPtr @this, System.Numerics.Vector2 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetSize(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetTransformMatrixDelegate(IntPtr @this, System.Numerics.Matrix4x4* value);
        static int GetTransformMatrix(IntPtr @this, System.Numerics.Matrix4x4* value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.TransformMatrix;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetTransformMatrixDelegate(IntPtr @this, System.Numerics.Matrix4x4 value);
        static int SetTransformMatrix(IntPtr @this, System.Numerics.Matrix4x4 value)
        {
            IVisual __target = null;
            try
            {
                {
                    __target = (IVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetTransformMatrix(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIVisualVTable()
        {
            base.AddMethod((GetAnchorPointDelegate)GetAnchorPoint);
            base.AddMethod((SetAnchorPointDelegate)SetAnchorPoint);
            base.AddMethod((GetBackfaceVisibilityDelegate)GetBackfaceVisibility);
            base.AddMethod((SetBackfaceVisibilityDelegate)SetBackfaceVisibility);
            base.AddMethod((GetBorderModeDelegate)GetBorderMode);
            base.AddMethod((SetBorderModeDelegate)SetBorderMode);
            base.AddMethod((GetCenterPointDelegate)GetCenterPoint);
            base.AddMethod((SetCenterPointDelegate)SetCenterPoint);
            base.AddMethod((GetClipDelegate)GetClip);
            base.AddMethod((SetClipDelegate)SetClip);
            base.AddMethod((GetCompositeModeDelegate)GetCompositeMode);
            base.AddMethod((SetCompositeModeDelegate)SetCompositeMode);
            base.AddMethod((GetIsVisibleDelegate)GetIsVisible);
            base.AddMethod((SetIsVisibleDelegate)SetIsVisible);
            base.AddMethod((GetOffsetDelegate)GetOffset);
            base.AddMethod((SetOffsetDelegate)SetOffset);
            base.AddMethod((GetOpacityDelegate)GetOpacity);
            base.AddMethod((SetOpacityDelegate)SetOpacity);
            base.AddMethod((GetOrientationDelegate)GetOrientation);
            base.AddMethod((SetOrientationDelegate)SetOrientation);
            base.AddMethod((GetParentDelegate)GetParent);
            base.AddMethod((GetRotationAngleDelegate)GetRotationAngle);
            base.AddMethod((SetRotationAngleDelegate)SetRotationAngle);
            base.AddMethod((GetRotationAngleInDegreesDelegate)GetRotationAngleInDegrees);
            base.AddMethod((SetRotationAngleInDegreesDelegate)SetRotationAngleInDegrees);
            base.AddMethod((GetRotationAxisDelegate)GetRotationAxis);
            base.AddMethod((SetRotationAxisDelegate)SetRotationAxis);
            base.AddMethod((GetScaleDelegate)GetScale);
            base.AddMethod((SetScaleDelegate)SetScale);
            base.AddMethod((GetSizeDelegate)GetSize);
            base.AddMethod((SetSizeDelegate)SetSize);
            base.AddMethod((GetTransformMatrixDelegate)GetTransformMatrix);
            base.AddMethod((SetTransformMatrixDelegate)SetTransformMatrix);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IVisual), new __MicroComIVisualVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIVisual2Proxy : __MicroComIInspectableProxy, IVisual2
    {
        public IVisual ParentForTransform
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetParentForTransform failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(__marshal_value, true);
            }
        }

        public void SetParentForTransform(IVisual value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(value), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetParentForTransform failed", __result);
        }

        public System.Numerics.Vector3 RelativeOffsetAdjustment
        {
            get
            {
                int __result;
                System.Numerics.Vector3 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRelativeOffsetAdjustment failed", __result);
                return value;
            }
        }

        public void SetRelativeOffsetAdjustment(System.Numerics.Vector3 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRelativeOffsetAdjustment failed", __result);
        }

        public System.Numerics.Vector2 RelativeSizeAdjustment
        {
            get
            {
                int __result;
                System.Numerics.Vector2 value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 4]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRelativeSizeAdjustment failed", __result);
                return value;
            }
        }

        public void SetRelativeSizeAdjustment(System.Numerics.Vector2 value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRelativeSizeAdjustment failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IVisual2), new Guid("3052B611-56C3-4C3E-8BF3-F6E1AD473F06"), (p, owns) => new __MicroComIVisual2Proxy(p, owns));
        }

        public __MicroComIVisual2Proxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 6;
    }

    unsafe class __MicroComIVisual2VTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetParentForTransformDelegate(IntPtr @this, void** value);
        static int GetParentForTransform(IntPtr @this, void** value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.ParentForTransform;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetParentForTransformDelegate(IntPtr @this, void* value);
        static int SetParentForTransform(IntPtr @this, void* value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetParentForTransform(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(value, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRelativeOffsetAdjustmentDelegate(IntPtr @this, System.Numerics.Vector3* value);
        static int GetRelativeOffsetAdjustment(IntPtr @this, System.Numerics.Vector3* value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RelativeOffsetAdjustment;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRelativeOffsetAdjustmentDelegate(IntPtr @this, System.Numerics.Vector3 value);
        static int SetRelativeOffsetAdjustment(IntPtr @this, System.Numerics.Vector3 value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRelativeOffsetAdjustment(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRelativeSizeAdjustmentDelegate(IntPtr @this, System.Numerics.Vector2* value);
        static int GetRelativeSizeAdjustment(IntPtr @this, System.Numerics.Vector2* value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.RelativeSizeAdjustment;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRelativeSizeAdjustmentDelegate(IntPtr @this, System.Numerics.Vector2 value);
        static int SetRelativeSizeAdjustment(IntPtr @this, System.Numerics.Vector2 value)
        {
            IVisual2 __target = null;
            try
            {
                {
                    __target = (IVisual2)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRelativeSizeAdjustment(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIVisual2VTable()
        {
            base.AddMethod((GetParentForTransformDelegate)GetParentForTransform);
            base.AddMethod((SetParentForTransformDelegate)SetParentForTransform);
            base.AddMethod((GetRelativeOffsetAdjustmentDelegate)GetRelativeOffsetAdjustment);
            base.AddMethod((SetRelativeOffsetAdjustmentDelegate)SetRelativeOffsetAdjustment);
            base.AddMethod((GetRelativeSizeAdjustmentDelegate)GetRelativeSizeAdjustment);
            base.AddMethod((SetRelativeSizeAdjustmentDelegate)SetRelativeSizeAdjustment);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IVisual2), new __MicroComIVisual2VTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIContainerVisualProxy : __MicroComIInspectableProxy, IContainerVisual
    {
        public IVisualCollection Children
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetChildren failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisualCollection>(__marshal_value, true);
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IContainerVisual), new Guid("02F6BC74-ED20-4773-AFE6-D49B4A93DB32"), (p, owns) => new __MicroComIContainerVisualProxy(p, owns));
        }

        public __MicroComIContainerVisualProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComIContainerVisualVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetChildrenDelegate(IntPtr @this, void** value);
        static int GetChildren(IntPtr @this, void** value)
        {
            IContainerVisual __target = null;
            try
            {
                {
                    __target = (IContainerVisual)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Children;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIContainerVisualVTable()
        {
            base.AddMethod((GetChildrenDelegate)GetChildren);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IContainerVisual), new __MicroComIContainerVisualVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIVisualCollectionProxy : __MicroComIInspectableProxy, IVisualCollection
    {
        public int Count
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetCount failed", __result);
                return value;
            }
        }

        public void InsertAbove(IVisual newChild, IVisual sibling)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(newChild), Avalonia.MicroCom.MicroComRuntime.GetNativePointer(sibling), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("InsertAbove failed", __result);
        }

        public void InsertAtBottom(IVisual newChild)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(newChild), (*PPV)[base.VTableSize + 2]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("InsertAtBottom failed", __result);
        }

        public void InsertAtTop(IVisual newChild)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(newChild), (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("InsertAtTop failed", __result);
        }

        public void InsertBelow(IVisual newChild, IVisual sibling)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(newChild), Avalonia.MicroCom.MicroComRuntime.GetNativePointer(sibling), (*PPV)[base.VTableSize + 4]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("InsertBelow failed", __result);
        }

        public void Remove(IVisual child)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(child), (*PPV)[base.VTableSize + 5]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Remove failed", __result);
        }

        public void RemoveAll()
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, (*PPV)[base.VTableSize + 6]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("RemoveAll failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IVisualCollection), new Guid("8B745505-FD3E-4A98-84A8-E949468C6BCB"), (p, owns) => new __MicroComIVisualCollectionProxy(p, owns));
        }

        public __MicroComIVisualCollectionProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 7;
    }

    unsafe class __MicroComIVisualCollectionVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetCountDelegate(IntPtr @this, int* value);
        static int GetCount(IntPtr @this, int* value)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Count;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InsertAboveDelegate(IntPtr @this, void* newChild, void* sibling);
        static int InsertAbove(IntPtr @this, void* newChild, void* sibling)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.InsertAbove(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(newChild, false), Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(sibling, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InsertAtBottomDelegate(IntPtr @this, void* newChild);
        static int InsertAtBottom(IntPtr @this, void* newChild)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.InsertAtBottom(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(newChild, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InsertAtTopDelegate(IntPtr @this, void* newChild);
        static int InsertAtTop(IntPtr @this, void* newChild)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.InsertAtTop(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(newChild, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int InsertBelowDelegate(IntPtr @this, void* newChild, void* sibling);
        static int InsertBelow(IntPtr @this, void* newChild, void* sibling)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.InsertBelow(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(newChild, false), Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(sibling, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int RemoveDelegate(IntPtr @this, void* child);
        static int Remove(IntPtr @this, void* child)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.Remove(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(child, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int RemoveAllDelegate(IntPtr @this);
        static int RemoveAll(IntPtr @this)
        {
            IVisualCollection __target = null;
            try
            {
                {
                    __target = (IVisualCollection)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.RemoveAll();
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIVisualCollectionVTable()
        {
            base.AddMethod((GetCountDelegate)GetCount);
            base.AddMethod((InsertAboveDelegate)InsertAbove);
            base.AddMethod((InsertAtBottomDelegate)InsertAtBottom);
            base.AddMethod((InsertAtTopDelegate)InsertAtTop);
            base.AddMethod((InsertBelowDelegate)InsertBelow);
            base.AddMethod((RemoveDelegate)Remove);
            base.AddMethod((RemoveAllDelegate)RemoveAll);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IVisualCollection), new __MicroComIVisualCollectionVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionTargetProxy : __MicroComIInspectableProxy, ICompositionTarget
    {
        public IVisual Root
        {
            get
            {
                int __result;
                void* __marshal_value = null;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetRoot failed", __result);
                return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(__marshal_value, true);
            }
        }

        public void SetRoot(IVisual value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(value), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetRoot failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionTarget), new Guid("A1BEA8BA-D726-4663-8129-6B5E7927FFA6"), (p, owns) => new __MicroComICompositionTargetProxy(p, owns));
        }

        public __MicroComICompositionTargetProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComICompositionTargetVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetRootDelegate(IntPtr @this, void** value);
        static int GetRoot(IntPtr @this, void** value)
        {
            ICompositionTarget __target = null;
            try
            {
                {
                    __target = (ICompositionTarget)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Root;
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetRootDelegate(IntPtr @this, void* value);
        static int SetRoot(IntPtr @this, void* value)
        {
            ICompositionTarget __target = null;
            try
            {
                {
                    __target = (ICompositionTarget)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetRoot(Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IVisual>(value, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionTargetVTable()
        {
            base.AddMethod((GetRootDelegate)GetRoot);
            base.AddMethod((SetRootDelegate)SetRoot);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionTarget), new __MicroComICompositionTargetVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIGraphicsEffectProxy : __MicroComIInspectableProxy, IGraphicsEffect
    {
        public IntPtr Name
        {
            get
            {
                int __result;
                IntPtr name = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &name, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetName failed", __result);
                return name;
            }
        }

        public void SetName(IntPtr name)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, name, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetName failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IGraphicsEffect), new Guid("CB51C0CE-8FE6-4636-B202-861FAA07D8F3"), (p, owns) => new __MicroComIGraphicsEffectProxy(p, owns));
        }

        public __MicroComIGraphicsEffectProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComIGraphicsEffectVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetNameDelegate(IntPtr @this, IntPtr* name);
        static int GetName(IntPtr @this, IntPtr* name)
        {
            IGraphicsEffect __target = null;
            try
            {
                {
                    __target = (IGraphicsEffect)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Name;
                        *name = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetNameDelegate(IntPtr @this, IntPtr name);
        static int SetName(IntPtr @this, IntPtr name)
        {
            IGraphicsEffect __target = null;
            try
            {
                {
                    __target = (IGraphicsEffect)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetName(name);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIGraphicsEffectVTable()
        {
            base.AddMethod((GetNameDelegate)GetName);
            base.AddMethod((SetNameDelegate)SetName);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IGraphicsEffect), new __MicroComIGraphicsEffectVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIGraphicsEffectSourceProxy : __MicroComIInspectableProxy, IGraphicsEffectSource
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IGraphicsEffectSource), new Guid("2D8F9DDC-4339-4EB9-9216-F9DEB75658A2"), (p, owns) => new __MicroComIGraphicsEffectSourceProxy(p, owns));
        }

        public __MicroComIGraphicsEffectSourceProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComIGraphicsEffectSourceVTable : __MicroComIInspectableVTable
    {
        public __MicroComIGraphicsEffectSourceVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IGraphicsEffectSource), new __MicroComIGraphicsEffectSourceVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComIGraphicsEffectD2D1InteropProxy : Avalonia.MicroCom.MicroComProxyBase, IGraphicsEffectD2D1Interop
    {
        public Guid EffectId
        {
            get
            {
                int __result;
                Guid id = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &id, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetEffectId failed", __result);
                return id;
            }
        }

        public void GetNamedPropertyMapping(IntPtr name, uint* index, GRAPHICS_EFFECT_PROPERTY_MAPPING* mapping)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, name, index, mapping, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetNamedPropertyMapping failed", __result);
        }

        public uint PropertyCount
        {
            get
            {
                int __result;
                uint count = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &count, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetPropertyCount failed", __result);
                return count;
            }
        }

        public IPropertyValue GetProperty(uint index)
        {
            int __result;
            void* __marshal_value = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, index, &__marshal_value, (*PPV)[base.VTableSize + 3]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetProperty failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IPropertyValue>(__marshal_value, true);
        }

        public IGraphicsEffectSource GetSource(uint index)
        {
            int __result;
            void* __marshal_source = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, index, &__marshal_source, (*PPV)[base.VTableSize + 4]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetSource failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<IGraphicsEffectSource>(__marshal_source, true);
        }

        public uint SourceCount
        {
            get
            {
                int __result;
                uint count = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &count, (*PPV)[base.VTableSize + 5]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetSourceCount failed", __result);
                return count;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(IGraphicsEffectD2D1Interop), new Guid("2FC57384-A068-44D7-A331-30982FCF7177"), (p, owns) => new __MicroComIGraphicsEffectD2D1InteropProxy(p, owns));
        }

        public __MicroComIGraphicsEffectD2D1InteropProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 6;
    }

    unsafe class __MicroComIGraphicsEffectD2D1InteropVTable : Avalonia.MicroCom.MicroComVtblBase
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetEffectIdDelegate(IntPtr @this, Guid* id);
        static int GetEffectId(IntPtr @this, Guid* id)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.EffectId;
                        *id = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetNamedPropertyMappingDelegate(IntPtr @this, IntPtr name, uint* index, GRAPHICS_EFFECT_PROPERTY_MAPPING* mapping);
        static int GetNamedPropertyMapping(IntPtr @this, IntPtr name, uint* index, GRAPHICS_EFFECT_PROPERTY_MAPPING* mapping)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.GetNamedPropertyMapping(name, index, mapping);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetPropertyCountDelegate(IntPtr @this, uint* count);
        static int GetPropertyCount(IntPtr @this, uint* count)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.PropertyCount;
                        *count = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetPropertyDelegate(IntPtr @this, uint index, void** value);
        static int GetProperty(IntPtr @this, uint index, void** value)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetProperty(index);
                        *value = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSourceDelegate(IntPtr @this, uint index, void** source);
        static int GetSource(IntPtr @this, uint index, void** source)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetSource(index);
                        *source = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSourceCountDelegate(IntPtr @this, uint* count);
        static int GetSourceCount(IntPtr @this, uint* count)
        {
            IGraphicsEffectD2D1Interop __target = null;
            try
            {
                {
                    __target = (IGraphicsEffectD2D1Interop)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.SourceCount;
                        *count = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComIGraphicsEffectD2D1InteropVTable()
        {
            base.AddMethod((GetEffectIdDelegate)GetEffectId);
            base.AddMethod((GetNamedPropertyMappingDelegate)GetNamedPropertyMapping);
            base.AddMethod((GetPropertyCountDelegate)GetPropertyCount);
            base.AddMethod((GetPropertyDelegate)GetProperty);
            base.AddMethod((GetSourceDelegate)GetSource);
            base.AddMethod((GetSourceCountDelegate)GetSourceCount);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(IGraphicsEffectD2D1Interop), new __MicroComIGraphicsEffectD2D1InteropVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionEffectSourceParameterProxy : __MicroComIInspectableProxy, ICompositionEffectSourceParameter
    {
        public IntPtr Name
        {
            get
            {
                int __result;
                IntPtr value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetName failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionEffectSourceParameter), new Guid("858AB13A-3292-4E4E-B3BB-2B6C6544A6EE"), (p, owns) => new __MicroComICompositionEffectSourceParameterProxy(p, owns));
        }

        public __MicroComICompositionEffectSourceParameterProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComICompositionEffectSourceParameterVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetNameDelegate(IntPtr @this, IntPtr* value);
        static int GetName(IntPtr @this, IntPtr* value)
        {
            ICompositionEffectSourceParameter __target = null;
            try
            {
                {
                    __target = (ICompositionEffectSourceParameter)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Name;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionEffectSourceParameterVTable()
        {
            base.AddMethod((GetNameDelegate)GetName);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionEffectSourceParameter), new __MicroComICompositionEffectSourceParameterVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionEffectSourceParameterFactoryProxy : __MicroComIInspectableProxy, ICompositionEffectSourceParameterFactory
    {
        public ICompositionEffectSourceParameter Create(IntPtr name)
        {
            int __result;
            void* __marshal_instance = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, name, &__marshal_instance, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("Create failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionEffectSourceParameter>(__marshal_instance, true);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionEffectSourceParameterFactory), new Guid("B3D9F276-ABA3-4724-ACF3-D0397464DB1C"), (p, owns) => new __MicroComICompositionEffectSourceParameterFactoryProxy(p, owns));
        }

        public __MicroComICompositionEffectSourceParameterFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 1;
    }

    unsafe class __MicroComICompositionEffectSourceParameterFactoryVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateDelegate(IntPtr @this, IntPtr name, void** instance);
        static int Create(IntPtr @this, IntPtr name, void** instance)
        {
            ICompositionEffectSourceParameterFactory __target = null;
            try
            {
                {
                    __target = (ICompositionEffectSourceParameterFactory)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Create(name);
                        *instance = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionEffectSourceParameterFactoryVTable()
        {
            base.AddMethod((CreateDelegate)Create);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionEffectSourceParameterFactory), new __MicroComICompositionEffectSourceParameterFactoryVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionEffectFactoryProxy : __MicroComIInspectableProxy, ICompositionEffectFactory
    {
        public ICompositionEffectBrush CreateBrush()
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, &__marshal_result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("CreateBrush failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionEffectBrush>(__marshal_result, true);
        }

        public int ExtendedError
        {
            get
            {
                int __result;
                int value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 1]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetExtendedError failed", __result);
                return value;
            }
        }

        public CompositionEffectFactoryLoadStatus LoadStatus
        {
            get
            {
                int __result;
                CompositionEffectFactoryLoadStatus value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 2]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetLoadStatus failed", __result);
                return value;
            }
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionEffectFactory), new Guid("BE5624AF-BA7E-4510-9850-41C0B4FF74DF"), (p, owns) => new __MicroComICompositionEffectFactoryProxy(p, owns));
        }

        public __MicroComICompositionEffectFactoryProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 3;
    }

    unsafe class __MicroComICompositionEffectFactoryVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int CreateBrushDelegate(IntPtr @this, void** result);
        static int CreateBrush(IntPtr @this, void** result)
        {
            ICompositionEffectFactory __target = null;
            try
            {
                {
                    __target = (ICompositionEffectFactory)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.CreateBrush();
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetExtendedErrorDelegate(IntPtr @this, int* value);
        static int GetExtendedError(IntPtr @this, int* value)
        {
            ICompositionEffectFactory __target = null;
            try
            {
                {
                    __target = (ICompositionEffectFactory)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.ExtendedError;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetLoadStatusDelegate(IntPtr @this, CompositionEffectFactoryLoadStatus* value);
        static int GetLoadStatus(IntPtr @this, CompositionEffectFactoryLoadStatus* value)
        {
            ICompositionEffectFactory __target = null;
            try
            {
                {
                    __target = (ICompositionEffectFactory)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.LoadStatus;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionEffectFactoryVTable()
        {
            base.AddMethod((CreateBrushDelegate)CreateBrush);
            base.AddMethod((GetExtendedErrorDelegate)GetExtendedError);
            base.AddMethod((GetLoadStatusDelegate)GetLoadStatus);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionEffectFactory), new __MicroComICompositionEffectFactoryVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionEffectBrushProxy : __MicroComIInspectableProxy, ICompositionEffectBrush
    {
        public ICompositionBrush GetSourceParameter(IntPtr name)
        {
            int __result;
            void* __marshal_result = null;
            __result = (int)LocalInterop.CalliStdCallint(PPV, name, &__marshal_result, (*PPV)[base.VTableSize + 0]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("GetSourceParameter failed", __result);
            return Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionBrush>(__marshal_result, true);
        }

        public void SetSourceParameter(IntPtr name, ICompositionBrush source)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, name, Avalonia.MicroCom.MicroComRuntime.GetNativePointer(source), (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetSourceParameter failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionEffectBrush), new Guid("BF7F795E-83CC-44BF-A447-3E3C071789EC"), (p, owns) => new __MicroComICompositionEffectBrushProxy(p, owns));
        }

        public __MicroComICompositionEffectBrushProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComICompositionEffectBrushVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetSourceParameterDelegate(IntPtr @this, IntPtr name, void** result);
        static int GetSourceParameter(IntPtr @this, IntPtr name, void** result)
        {
            ICompositionEffectBrush __target = null;
            try
            {
                {
                    __target = (ICompositionEffectBrush)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.GetSourceParameter(name);
                        *result = Avalonia.MicroCom.MicroComRuntime.GetNativePointer(__result, true);
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetSourceParameterDelegate(IntPtr @this, IntPtr name, void* source);
        static int SetSourceParameter(IntPtr @this, IntPtr name, void* source)
        {
            ICompositionEffectBrush __target = null;
            try
            {
                {
                    __target = (ICompositionEffectBrush)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetSourceParameter(name, Avalonia.MicroCom.MicroComRuntime.CreateProxyFor<ICompositionBrush>(source, false));
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionEffectBrushVTable()
        {
            base.AddMethod((GetSourceParameterDelegate)GetSourceParameter);
            base.AddMethod((SetSourceParameterDelegate)SetSourceParameter);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionEffectBrush), new __MicroComICompositionEffectBrushVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionBackdropBrushProxy : __MicroComIInspectableProxy, ICompositionBackdropBrush
    {
        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionBackdropBrush), new Guid("C5ACAE58-3898-499E-8D7F-224E91286A5D"), (p, owns) => new __MicroComICompositionBackdropBrushProxy(p, owns));
        }

        public __MicroComICompositionBackdropBrushProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 0;
    }

    unsafe class __MicroComICompositionBackdropBrushVTable : __MicroComIInspectableVTable
    {
        public __MicroComICompositionBackdropBrushVTable()
        {
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionBackdropBrush), new __MicroComICompositionBackdropBrushVTable().CreateVTable());
    }

    unsafe internal partial class __MicroComICompositionColorBrushProxy : __MicroComIInspectableProxy, ICompositionColorBrush
    {
        public Avalonia.Win32.WinRT.WinRTColor Color
        {
            get
            {
                int __result;
                Avalonia.Win32.WinRT.WinRTColor value = default;
                __result = (int)LocalInterop.CalliStdCallint(PPV, &value, (*PPV)[base.VTableSize + 0]);
                if (__result != 0)
                    throw new System.Runtime.InteropServices.COMException("GetColor failed", __result);
                return value;
            }
        }

        public void SetColor(Avalonia.Win32.WinRT.WinRTColor value)
        {
            int __result;
            __result = (int)LocalInterop.CalliStdCallint(PPV, value, (*PPV)[base.VTableSize + 1]);
            if (__result != 0)
                throw new System.Runtime.InteropServices.COMException("SetColor failed", __result);
        }

        static internal void __MicroComModuleInit()
        {
            Avalonia.MicroCom.MicroComRuntime.Register(typeof(ICompositionColorBrush), new Guid("2B264C5E-BF35-4831-8642-CF70C20FFF2F"), (p, owns) => new __MicroComICompositionColorBrushProxy(p, owns));
        }

        public __MicroComICompositionColorBrushProxy(IntPtr nativePointer, bool ownsHandle) : base(nativePointer, ownsHandle)
        {
        }

        protected override int VTableSize => base.VTableSize + 2;
    }

    unsafe class __MicroComICompositionColorBrushVTable : __MicroComIInspectableVTable
    {
        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int GetColorDelegate(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor* value);
        static int GetColor(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor* value)
        {
            ICompositionColorBrush __target = null;
            try
            {
                {
                    __target = (ICompositionColorBrush)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    {
                        var __result = __target.Color;
                        *value = __result;
                    }
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        [System.Runtime.InteropServices.UnmanagedFunctionPointer(System.Runtime.InteropServices.CallingConvention.StdCall)]
        delegate int SetColorDelegate(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor value);
        static int SetColor(IntPtr @this, Avalonia.Win32.WinRT.WinRTColor value)
        {
            ICompositionColorBrush __target = null;
            try
            {
                {
                    __target = (ICompositionColorBrush)Avalonia.MicroCom.MicroComRuntime.GetObjectFromCcw(@this);
                    __target.SetColor(value);
                }
            }
            catch (System.Runtime.InteropServices.COMException __com_exception__)
            {
                return __com_exception__.ErrorCode;
            }
            catch (System.Exception __exception__)
            {
                Avalonia.MicroCom.MicroComRuntime.UnhandledException(__target, __exception__);
                return unchecked((int)0x80004005u);
            }

            return 0;
        }

        public __MicroComICompositionColorBrushVTable()
        {
            base.AddMethod((GetColorDelegate)GetColor);
            base.AddMethod((SetColorDelegate)SetColor);
        }

        static internal void __MicroComModuleInit() => Avalonia.MicroCom.MicroComRuntime.RegisterVTable(typeof(ICompositionColorBrush), new __MicroComICompositionColorBrushVTable().CreateVTable());
    }

    class LocalInterop
    {
        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, void* arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, AsyncStatus arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, System.Numerics.Vector2 arg0, System.Numerics.Vector2 arg1, void* arg2, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, void* arg1, void* arg2, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, IntPtr arg0, void* arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, float arg0, float arg1, float arg2, float arg3, void* arg4, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, CompositionBatchTypes arg0, void* arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, int arg0, void* arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, void* arg1, void* arg2, void* arg3, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, Avalonia.Win32.Interop.UnmanagedMethods.POINT arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, void* arg1, int arg2, int arg3, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, IntPtr arg0, int arg1, void* arg2, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, int arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, void* arg0, IntPtr arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, Avalonia.Win32.Interop.UnmanagedMethods.SIZE arg0, DirectXPixelFormat arg1, DirectXAlphaMode arg2, void* arg3, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, System.Numerics.Vector2 arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, CompositionBackfaceVisibility arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, CompositionBorderMode arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, System.Numerics.Vector3 arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, CompositionCompositeMode arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, float arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, System.Numerics.Quaternion arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, System.Numerics.Matrix4x4 arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, IntPtr arg0, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, IntPtr arg0, void* arg1, void* arg2, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, uint arg0, void* arg1, void* methodPtr)
        {
            throw null;
        }

        static unsafe public int CalliStdCallint(void* thisObj, Avalonia.Win32.WinRT.WinRTColor arg0, void* methodPtr)
        {
            throw null;
        }
    }
}