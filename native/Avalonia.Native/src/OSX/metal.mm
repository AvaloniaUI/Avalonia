#import <AppKit/AppKit.h>
#import <Metal/Metal.h>
#import <QuartzCore/QuartzCore.h>
#include <memory>
#include <mutex>
#include "common.h"
#include "rendertarget.h"
#import "crapium.h"


struct AvnMetalLayout
{
    AvnPixelSize Size;
    double Scaling;

    AvnMetalLayout() : Size{1, 1}, Scaling(1) {}
    AvnMetalLayout(const AvnPixelSize& size, double scaling) : Size(size), Scaling(scaling) {}

    bool operator==(const AvnMetalLayout& other) const
    {
        return Size.Width == other.Size.Width
            && Size.Height == other.Size.Height
            && Scaling == other.Scaling;
    }

    bool operator!=(const AvnMetalLayout& other) const { return !(*this == other); }
};

struct AvnMetalLayoutState
{
    std::mutex Mutex;
    AvnMetalLayout Pending;   // last value reported by resize:withScale:
    AvnMetalLayout Applied;   // value currently set on the CAMetalLayer
};


class API_AVAILABLE(macos(12.0)) AvnMTLSharedEvent : public ComSingleObject<IAvnMTLSharedEvent, &IID_IAvnMTLSharedEvent>
{
    id<MTLSharedEvent> _event;
public:
    
    AvnMTLSharedEvent(id<MTLSharedEvent> ev) : _event(ev)
    {
        
    }
    
    FORWARD_IUNKNOWN()
    
    id<MTLSharedEvent> GetEvent()
    {
        return _event;
    }
    
    void *GetNativeHandle() override {
        return (__bridge void*)_event;
    }
    
    bool Wait(uint64_t value, uint64_t timeoutMS) override {
        return MtlSharedEventWaitUntilSignaledValueHack(_event, value, timeoutMS);
    }
    
    void SetSignaledValue(uint64_t value) override {
        _event.signaledValue = value;
    }
    
    uint64_t GetSignaledValue() override {
        return _event.signaledValue;
    }
};


class AvnMetalTexture : public ComSingleObject<IAvnMetalTexture, &IID_IAvnMetalTexture>
{
    id<MTLTexture> _texture;
public:
    FORWARD_IUNKNOWN()
    AvnMetalTexture(id<MTLTexture> texture) : _texture(texture)
    {
        
    }
    void *GetNativeHandle() override
    {
        return (__bridge void*)_texture;
    }
    
    int GetWidth() override
    {
        return (int)_texture.width;
    }
    
    int GetHeight() override
    {
        return (int)_texture.height;
    }
    
    int GetSampleCount() override
    {
        return (int)_texture.sampleCount;
    }
    
};

class AvnMetalDevice : public ComSingleObject<IAvnMetalDevice, &IID_IAvnMetalDevice>
{
public:
    id<MTLDevice> device;
    id<MTLCommandQueue> queue;
    FORWARD_IUNKNOWN()

    void *GetDevice() override {
        return (__bridge void*) device;
    }

    void *GetQueue() override {
        return (__bridge void*) queue;
    }
    
    HRESULT ImportIOSurface(void *handle, AvnPixelFormat pixelFormat, IAvnMetalTexture **ppv) override {
        START_COM_ARP_CALL;
        auto surf = (IOSurfaceRef)handle;
        auto width = IOSurfaceGetWidth(surf);
        auto height = IOSurfaceGetHeight(surf);

        auto desc = [MTLTextureDescriptor new];
        if(pixelFormat == kAvnRgba8888)
            desc.pixelFormat = MTLPixelFormatRGBA8Unorm;
        else if(pixelFormat == kAvnBgra8888)
            desc.pixelFormat = MTLPixelFormatBGRA8Unorm;
        else
            return E_INVALIDARG;
        desc.textureType = MTLTextureType2D;
        desc.width = width;
        desc.height = height;
        desc.depth = 1;
        desc.mipmapLevelCount = 1;
        desc.sampleCount = 1;
        desc.usage = MTLTextureUsageShaderRead | MTLTextureUsageRenderTarget;

        auto texture = [device newTextureWithDescriptor:desc iosurface:surf plane:0];
        if(texture == nullptr)
            return E_FAIL;
        *ppv = new AvnMetalTexture(texture);
        return S_OK;
    }
    
    HRESULT ImportSharedEvent(void *mtlSharedEventInstance, IAvnMTLSharedEvent**ppv) override {
        if (@available(macOS 12.0, *)) {
            auto external = (__bridge id<MTLSharedEvent>)mtlSharedEventInstance;
            auto handle = external.newSharedEventHandle;
            auto imported = [device newSharedEventWithHandle: handle];
            *ppv = new AvnMTLSharedEvent(imported);
            return S_OK;
        } 
        else
        {
            return E_NOTIMPL;
        }
    }
    
    
    HRESULT SignalOrWait(IAvnMTLSharedEvent *ev, uint64_t value, bool wait)
    {
        START_ARP_CALL;
        if (@available(macOS 12.0, *))
        {
            auto e = dynamic_cast<AvnMTLSharedEvent*>(ev);
            if(e == nullptr)
                return E_FAIL;
            auto buf = [queue commandBuffer];
            if(wait)
                [buf encodeWaitForEvent:e->GetEvent() value:value];
            else
                [buf encodeSignalEvent:e->GetEvent() value:value];
            [buf commit];
            return S_OK;
        }
        else
            return E_FAIL;
    }
    
    HRESULT SubmitWait(IAvnMTLSharedEvent *ev, uint64_t value) override {
        return SignalOrWait(ev, value, true);
    }
    
    HRESULT SubmitSignal(IAvnMTLSharedEvent *ev, uint64_t value) override { 
        return SignalOrWait(ev, value, false);
    }
    
    bool GetIOKitRegistryId(uint64_t *value) override { 
        if (@available(macOS 10.13, *)) {
            *value = [device registryID];
            return true;
        } else {
            return false;
        }
    }
    
    AvnMetalDevice(id <MTLDevice> device, id <MTLCommandQueue> queue) : device(device), queue(queue) {
    }

};


class AvnMetalRenderSession : public ComSingleObject<IAvnMetalRenderingSession, &IID_IAvnMetalRenderingSession>
{
    id<CAMetalDrawable> _drawable;
    id<MTLCommandQueue> _queue;
    id<MTLTexture> _texture;
    CAMetalLayer* _layer;
    AvnPixelSize _size;
    double _scaling;
    bool _presentWithTransaction;
    AvnMetalLayout _renderedLayout;
    std::shared_ptr<AvnMetalLayoutState> _layoutState;   // session can outlive the target
public:
    FORWARD_IUNKNOWN()

    AvnMetalRenderSession(AvnMetalDevice* device, CAMetalLayer* layer, id <CAMetalDrawable> drawable,
                          const AvnPixelSize &size, double scaling, bool presentWithTransaction,
                          const AvnMetalLayout& renderedLayout,
                          std::shared_ptr<AvnMetalLayoutState> layoutState)
            : _drawable(drawable), _size(size), _scaling(scaling), _queue(device->queue),
            _texture([drawable texture]), _presentWithTransaction(presentWithTransaction),
            _renderedLayout(renderedLayout), _layoutState(std::move(layoutState)) {
        _layer = layer;
    }

    HRESULT GetPixelSize(AvnPixelSize *ret) override {
        *ret = _size;
        return 0;
    }

    double GetScaling() override {
        return _scaling;
    }

    void *GetTexture() override {
        return (__bridge void*) _texture;
    }

    ~AvnMetalRenderSession()
    {
        START_ARP_CALL;

        // Presenting this stale frame would stretch it; the resize schedules another.
        bool layoutChanged = false;
        if(_layoutState)
        {
            std::lock_guard<std::mutex> lock(_layoutState->Mutex);
            layoutChanged = _layoutState->Applied != _renderedLayout;
        }

        auto buffer = [_queue commandBuffer];
        if(layoutChanged)
        {
            [buffer commit];
        }
        else if(_presentWithTransaction)
        {
            [buffer commit];
            [buffer waitUntilScheduled];
            [_drawable present];
        }
        else
        {
            [buffer presentDrawable: _drawable];
            [buffer commit];
        }

        if(_presentWithTransaction)
        {
            // Required for dropped frames too, else later frames stay serialized.
            _layer.presentsWithTransaction = NO;
        }
    }
};

class AvnMetalRenderTarget : public ComSingleObject<IAvnMetalRenderTarget, &IID_IAvnMetalRenderTarget>
{
    CAMetalLayer* _layer;
    ComPtr<AvnMetalDevice> _device;
    std::shared_ptr<AvnMetalLayoutState> _layoutState = std::make_shared<AvnMetalLayoutState>();

    void ApplyLayoutLocked(const AvnMetalLayout& layout)
    {
        CGSize layerSize = {(CGFloat)layout.Size.Width, (CGFloat)layout.Size.Height};

        // Without this Core Animation animates the layer to the new size.
        [CATransaction begin];
        [CATransaction setDisableActions:YES];
        [_layer setDrawableSize: layerSize];
        [CATransaction commit];

        _layoutState->Applied = layout;
    }

    // A zero-sized drawable cannot be acquired.
    static AvnMetalLayout Sanitize(const AvnMetalLayout& layout)
    {
        AvnMetalLayout result = layout;
        if(result.Size.Width < 1) result.Size.Width = 1;
        if(result.Size.Height < 1) result.Size.Height = 1;
        if(!(result.Scaling > 0)) result.Scaling = 1;
        return result;
    }

    // Safe to call from any thread.
    AvnMetalLayout SyncLayout()
    {
        std::lock_guard<std::mutex> lock(_layoutState->Mutex);
        auto layout = Sanitize(_layoutState->Pending);
        if(_layoutState->Applied != layout)
            ApplyLayoutLocked(layout);
        return layout;
    }

public:
    FORWARD_IUNKNOWN()
    AvnMetalRenderTarget(CAMetalLayer* layer, ComPtr<AvnMetalDevice> device)
    {
        _layer = layer;
        _device = device;
    }

    // UI thread, on view resize. Applied eagerly so the layer is already correct
    // when the render thread wakes up.
    void SetPendingLayout(const AvnMetalLayout& layout)
    {
        {
            std::lock_guard<std::mutex> lock(_layoutState->Mutex);
            _layoutState->Pending = Sanitize(layout);
        }

        SyncLayout();
    }

    HRESULT BeginDrawing(IAvnMetalRenderingSession **ret) override {
        START_COM_ARP_CALL;
        bool onMainThread = [NSThread isMainThread];
        if(onMainThread)
        {
            // Flush before touching the layer geometry.
            auto buffer = [_device->queue commandBuffer];
            [buffer commit];
            [buffer waitUntilCompleted];
        }

        // Sync on any thread, so a resize is never rendered into a stale drawable.
        auto layout = SyncLayout();

        if(onMainThread)
        {
            // Present synchronously so a live resize cannot tear.
            _layer.presentsWithTransaction = YES;
        }

        auto drawable = [_layer nextDrawable];
        if(drawable == nil)
        {
            if(onMainThread)
                _layer.presentsWithTransaction = NO;
            *ret = nullptr;
            return E_FAIL;
        }

        // Skia requires this to match the Metal texture exactly.
        AvnPixelSize drawableSize = {
            (int)[drawable texture].width,
            (int)[drawable texture].height
        };
        if(drawableSize.Width < 1) drawableSize.Width = 1;
        if(drawableSize.Height < 1) drawableSize.Height = 1;

        *ret = new AvnMetalRenderSession(_device, _layer, drawable,
                                         drawableSize, layout.Scaling, onMainThread,
                                         layout, _layoutState);
        return 0;
    }
};

@implementation MetalRenderTarget
{
    ComPtr<AvnMetalDevice> _device;
    CAMetalLayer* _layer;
    ComPtr<AvnMetalRenderTarget> _target;
}
- (MetalRenderTarget *)initWithDevice:(IAvnMetalDevice *)device {
    _device = dynamic_cast<AvnMetalDevice*>(device);
    _layer = [CAMetalLayer new];
    _layer.opaque = false;
    _layer.device = _device->device;
    _target.setNoAddRef(new AvnMetalRenderTarget(_layer, _device));
    return self;
}


-(void) getRenderTarget: (IAvnMetalRenderTarget**) ppv
{
    *ppv = static_cast<IAvnMetalRenderTarget*>(_target.getRetainedReference());
}

- (void)resize:(AvnPixelSize)size withScale:(float)scale {
    _target->SetPendingLayout(AvnMetalLayout(size, scale));
    [_layer setNeedsDisplay];
}

- (CALayer *)layer {
    return _layer;
}
@end


class AvnMetalDisplay : public ComSingleObject<IAvnMetalDisplay, &IID_IAvnMetalDisplay>
{
public:
    FORWARD_IUNKNOWN()
    HRESULT CreateDevice(IAvnMetalDevice **ret) override {
        START_COM_ARP_CALL;
        auto device = MTLCreateSystemDefaultDevice();
        if(device == nil) {
            ret = nil;
            return E_FAIL;
        }
        auto queue = [device newCommandQueue];
        *ret = new AvnMetalDevice(device, queue);
        return S_OK;
    }
};

static ComStaticPtr<AvnMetalDisplay> _display(comnew<AvnMetalDisplay>());

extern IAvnMetalDisplay* GetMetalDisplay()
{
    return _display;
}


extern IAvnMTLSharedEvent* ImportMTLSharedEvent(void* object)
{
    if (@available(macOS 12.0, *)) {
    if(object == nullptr)
        return nil;
    auto evId = (__bridge id<MTLSharedEvent>)object;
    
    if(evId == nil)
        return nil;
    
    
    return new AvnMTLSharedEvent(evId);
    } 
    else
    {
        return nil;
    }
}
