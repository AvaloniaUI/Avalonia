#include "com.h"

#define AVNCOM(name, id) COMINTERFACE(name, 2e2cda0a, 9ae5, 4f1b, 8e, 20, 08, 1a, 04, 27, 9f, id)

struct IAvnWindowEvents;
struct IAvnWindow;
struct IAvnMacOptions;
struct IAvnPlatformThreadingInterface;


struct AvnSize
{
    double Width, Height;
};

struct AvnRect
{
    double X, Y, Width, Height;
};

AVNCOM(IAvaloniaNativeFactory, 01) : virtual IUnknown
{
public:
    virtual HRESULT Initialize() = 0;
    virtual IAvnMacOptions* GetMacOptions() = 0;
    virtual HRESULT CreateWindow(IAvnWindowEvents* cb, IAvnWindow** ppv) = 0;
    virtual HRESULT CreatePlatformThreadingInterface(IAvnPlatformThreadingInterface** ppv) = 0;
};

AVNCOM(IAvnWindowBase, 02) : virtual IUnknown
{
    virtual HRESULT Show() = 0;
    virtual HRESULT Close() = 0;
    virtual HRESULT GetClientSize(AvnSize*ret) = 0;
    virtual HRESULT Resize(double width, double height) = 0;
};

AVNCOM(IAvnWindow, 03) : virtual IAvnWindowBase
{
    
};

AVNCOM(IAvnWindowBaseEvents, 04) : IUnknown
{
    virtual HRESULT SoftwareDraw(void* ptr, int stride, int pixelWidth, int pixelHeight, const AvnSize& logicalSize) = 0;
    virtual void Closed() = 0;
    virtual void Activated() = 0;
    virtual void Deactivated() = 0;
    virtual void Resized(const AvnSize& size) = 0;
};


AVNCOM(IAvnWindowEvents, 05) : IAvnWindowBaseEvents
{

};

AVNCOM(IAvnMacOptions, 06) : virtual IUnknown
{
    virtual HRESULT SetShowInDock(int show) = 0;
};

AVNCOM(IAvnActionCallback, 07) : IUnknown
{
    virtual void Run() = 0;
};

AVNCOM(IAvnSignaledCallback, 08) : IUnknown
{
    virtual void Signaled(int priority, bool priorityContainsMeaningfulValue) = 0;
};


AVNCOM(IAvnLoopCancellation, 09) : virtual IUnknown
{
    virtual void Cancel() = 0;
};

AVNCOM(IAvnPlatformThreadingInterface, 0a) : virtual IUnknown
{
    virtual bool GetCurrentThreadIsLoopThread() = 0;
    virtual void SetSignaledCallback(IAvnSignaledCallback* cb) = 0;
    virtual IAvnLoopCancellation* CreateLoopCancellation() = 0;
    virtual void RunLoop(IAvnLoopCancellation* cancel) = 0;
    // Can't pass int* to sharpgentools for some reason
    virtual void Signal(int priority) = 0;
    virtual IUnknown* StartTimer(int priority, int ms, IAvnActionCallback* callback) = 0;
};

extern "C" IAvaloniaNativeFactory* CreateAvaloniaNative();
