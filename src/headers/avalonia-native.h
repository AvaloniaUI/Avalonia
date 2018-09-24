#include "com.h"

#define AVNCOM(name, id) COMINTERFACE(name, 2e2cda0a, 9ae5, 4f1b, 8e, 20, 08, 1a, 04, 27, 9f, id)

struct IAvnWindowEvents;
struct IAvnWindow;
struct IAvnPopup;
struct IAvnMacOptions;
struct IAvnPlatformThreadingInterface;
struct IAvnSystemDialogEvents;
struct IAvnSystemDialogs;

struct AvnSize
{
    double Width, Height;
};

struct AvnRect
{
    double X, Y, Width, Height;
};

struct AvnVector
{
    double X, Y;
};

struct AvnPoint
{
    double X, Y;
};

enum AvnPixelFormat
{
    kAvnRgb565,
    kAvnRgba8888,
    kAvnBgra8888
};

struct AvnFramebuffer
{
    void* Data;
    int Width;
    int Height;
    int Stride;
    AvnVector Dpi;
    AvnPixelFormat PixelFormat;
};

enum AvnRawMouseEventType
{
    LeaveWindow,
    LeftButtonDown,
    LeftButtonUp,
    RightButtonDown,
    RightButtonUp,
    MiddleButtonDown,
    MiddleButtonUp,
    Move,
    Wheel,
    NonClientLeftButtonDown
};

enum AvnInputModifiers
{
    None = 0,
    Alt = 1,
    Control = 2,
    Shift = 4,
    Windows = 8,
    LeftMouseButton = 16,
    RightMouseButton = 32,
    MiddleMouseButton = 64
};

AVNCOM(IAvaloniaNativeFactory, 01) : virtual IUnknown
{
public:
    virtual HRESULT Initialize() = 0;
    virtual IAvnMacOptions* GetMacOptions() = 0;
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv) = 0;
    virtual HRESULT CreatePopup (IAvnWindowEvents* cb, IAvnPopup** ppv) = 0;
    virtual HRESULT CreatePlatformThreadingInterface(IAvnPlatformThreadingInterface** ppv) = 0;
    virtual HRESULT CreateSystemDialogs (IAvnSystemDialogs** ppv) = 0;
};

AVNCOM(IAvnWindowBase, 02) : virtual IUnknown
{
    virtual HRESULT Show() = 0;
    virtual HRESULT Hide () = 0;
    virtual HRESULT Close() = 0;
    virtual HRESULT GetClientSize(AvnSize*ret) = 0;
    virtual HRESULT GetMaxClientSize(AvnSize* ret) = 0;
    virtual HRESULT GetScaling(double*ret)=0;
    virtual HRESULT Resize(double width, double height) = 0;
    virtual void Invalidate (AvnRect rect) = 0;
    virtual void BeginMoveDrag () = 0;
    virtual HRESULT GetPosition (AvnPoint*ret) = 0;
    virtual void SetPosition (AvnPoint point) = 0;
    virtual HRESULT PointToClient (AvnPoint point, AvnPoint*ret) = 0;
    virtual HRESULT PointToScreen (AvnPoint point, AvnPoint*ret) = 0;
    virtual HRESULT ThreadSafeSetSwRenderedFrame(AvnFramebuffer* fb, IUnknown* dispose) = 0;
    virtual HRESULT SetTopMost (bool value) = 0;
};

AVNCOM(IAvnPopup, 03) : virtual IAvnWindowBase
{
    
};

AVNCOM(IAvnWindow, 04) : virtual IAvnWindowBase
{
    virtual HRESULT SetCanResize(bool value) = 0;
    virtual HRESULT SetHasDecorations(bool value) = 0;
};

AVNCOM(IAvnWindowBaseEvents, 05) : IUnknown
{
    virtual HRESULT SoftwareDraw(AvnFramebuffer* fb) = 0;
    virtual void Closed() = 0;
    virtual void Activated() = 0;
    virtual void Deactivated() = 0;
    virtual void Resized(const AvnSize& size) = 0;
    virtual void RawMouseEvent (AvnRawMouseEventType type,
                                unsigned int timeStamp,
                                AvnInputModifiers modifiers,
                                AvnPoint point,
                                AvnVector delta) = 0;
    virtual void ScalingChanged(double scaling) = 0;
    virtual void RunRenderPriorityJobs() = 0;
};


AVNCOM(IAvnWindowEvents, 06) : IAvnWindowBaseEvents
{

};

AVNCOM(IAvnMacOptions, 07) : virtual IUnknown
{
    virtual HRESULT SetShowInDock(int show) = 0;
};

AVNCOM(IAvnActionCallback, 08) : IUnknown
{
    virtual void Run() = 0;
};

AVNCOM(IAvnSignaledCallback, 09) : IUnknown
{
    virtual void Signaled(int priority, bool priorityContainsMeaningfulValue) = 0;
};


AVNCOM(IAvnLoopCancellation, 0a) : virtual IUnknown
{
    virtual void Cancel() = 0;
};

AVNCOM(IAvnPlatformThreadingInterface, 0b) : virtual IUnknown
{
    virtual bool GetCurrentThreadIsLoopThread() = 0;
    virtual void SetSignaledCallback(IAvnSignaledCallback* cb) = 0;
    virtual IAvnLoopCancellation* CreateLoopCancellation() = 0;
    virtual void RunLoop(IAvnLoopCancellation* cancel) = 0;
    // Can't pass int* to sharpgentools for some reason
    virtual void Signal(int priority) = 0;
    virtual IUnknown* StartTimer(int priority, int ms, IAvnActionCallback* callback) = 0;
};

AVNCOM(IAvnSystemDialogEvents, 0c) : virtual IUnknown
{
    virtual void OnCompleted (int numResults, void* ptrFirstResult) = 0;
};

AVNCOM(IAvnSystemDialogs, 0d) : virtual IUnknown
{
    virtual void SelectFolderDialog (IAvnWindow* parentWindowHandle,
                                     IAvnSystemDialogEvents* events,
                                     const char* title,
                                     const char* initialPath) = 0;
    
    virtual void OpenFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 bool allowMultiple,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) = 0;
    
    virtual void SaveFileDialog (IAvnWindow* parentWindowHandle,
                                 IAvnSystemDialogEvents* events,
                                 const char* title,
                                 const char* initialDirectory,
                                 const char* initialFile,
                                 const char* filters) = 0;
};

extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative();
