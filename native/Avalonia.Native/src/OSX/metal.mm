#import <AppKit/AppKit.h>
#import <Metal/Metal.h>
#import <QuartzCore/QuartzCore.h>
#include "common.h"
#include "rendertarget.h"

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
public:
    FORWARD_IUNKNOWN()

    AvnMetalRenderSession(AvnMetalDevice* device, CAMetalLayer* layer, id <CAMetalDrawable> drawable, const AvnPixelSize &size, double scaling)
            : _drawable(drawable), _size(size), _scaling(scaling), _queue(device->queue),
            _texture([drawable texture]) {
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
        auto buffer = [_queue commandBuffer];
        [buffer presentDrawable: _drawable];
        [buffer commit];
    }
};

class AvnMetalRenderTarget : public ComSingleObject<IAvnMetalRenderTarget, &IID_IAvnMetalRenderTarget>
{
    CAMetalLayer* _layer;
    double _scaling = 1;
    AvnPixelSize _size = {1,1};
    ComPtr<AvnMetalDevice> _device;
public:
    double PendingScaling = 1;
    AvnPixelSize PendingSize = {1,1};
    FORWARD_IUNKNOWN()
    AvnMetalRenderTarget(CAMetalLayer* layer, ComPtr<AvnMetalDevice> device)
    {
        _layer = layer;
        _device = device;
    }

    HRESULT BeginDrawing(IAvnMetalRenderingSession **ret) override {
        if([NSThread isMainThread])
        {
            // Flush all existing rendering
            auto buffer = [_device->queue commandBuffer];
            [buffer commit];
            [buffer waitUntilCompleted];
            _size = PendingSize;
            _scaling= PendingScaling;
            CGSize layerSize = {(CGFloat)_size.Width, (CGFloat)_size.Height};

            [_layer setDrawableSize: layerSize];
        }
        auto drawable = [_layer nextDrawable];
        if(drawable == nil)
        {
            ret = nil;
            return E_FAIL;
        }
        *ret = new AvnMetalRenderSession(_device, _layer, drawable, _size, _scaling);
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
    _layer.device = _device->device;
    _target.setNoAddRef(new AvnMetalRenderTarget(_layer, _device));
    return self;
}


-(void) getRenderTarget: (IAvnMetalRenderTarget**) ppv
{
    *ppv = static_cast<IAvnMetalRenderTarget*>(_target.getRetainedReference());
}

- (void)resize:(AvnPixelSize)size withScale:(float)scale {
    CGSize layerSize = {(CGFloat)size.Width, (CGFloat)size.Height};
    _target->PendingScaling = scale;
    _target->PendingSize = size;
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

static AvnMetalDisplay* _display = new AvnMetalDisplay();

extern IAvnMetalDisplay* GetMetalDisplay()
{
    return _display;
}