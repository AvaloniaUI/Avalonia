#include "common.h"


IAvnNativeControlHostTopLevelAttachment* CreateAttachment();

class AvnNativeControlHost :
    public ComSingleObject<IAvnNativeControlHost, &IID_IAvnNativeControlHost>
{
public:
    FORWARD_IUNKNOWN();
    NSView* View;
    AvnNativeControlHost(NSView* view)
    {
        View = view;
    }
    
    virtual HRESULT CreateDefaultChild(void* parent, void** retOut) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            NSView* view = [NSView new];
            [view setWantsLayer: true];
            
            *retOut = (__bridge_retained void*)view;
            return S_OK;
        }
    };
    
    virtual IAvnNativeControlHostTopLevelAttachment* CreateAttachment() override
    {
        return ::CreateAttachment();
    };
    
    virtual void DestroyDefaultChild(void* child) override
    {
        // ARC will release the object for us
        #pragma clang diagnostic push
        #pragma clang diagnostic ignored "-Wunused-value"
        (__bridge_transfer NSView*) child;
        #pragma clang diagnostic pop
    }
};

class AvnNativeControlHostTopLevelAttachment :
public ComSingleObject<IAvnNativeControlHostTopLevelAttachment, &IID_IAvnNativeControlHostTopLevelAttachment>
{
    NSView* _holder;
    NSView* _child;
public:
    FORWARD_IUNKNOWN();
    
    AvnNativeControlHostTopLevelAttachment()
    {
        _holder = [NSView new];
        [_holder setWantsLayer:true];
    }
    
    virtual ~AvnNativeControlHostTopLevelAttachment()
    {
        if(_child != nil && [_child superview] == _holder)
        {
            [_child removeFromSuperview];
        }
        
        if([_holder superview] != nil)
        {
            [_holder removeFromSuperview];
        }
    }
    
    virtual void* GetParentHandle() override
    {
        return (__bridge void*)_holder;
    };
    
    virtual HRESULT InitializeWithChildHandle(void* child) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(_child != nil)
                return E_FAIL;
            _child = (__bridge NSView*)child;
            if(_child == nil)
                return E_FAIL;
            [_holder addSubview:_child];
            [_child setHidden: false];
            return S_OK;
        }
    };
    
    virtual HRESULT AttachTo(IAvnNativeControlHost* host) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(host == nil)
            {
                [_holder removeFromSuperview];
                [_holder setHidden: true];
            }
            else
            {
                AvnNativeControlHost* chost = dynamic_cast<AvnNativeControlHost*>(host);
                if(chost == nil || chost->View == nil)
                    return E_FAIL;
                [_holder setHidden:true];
                [chost->View addSubview:_holder];
            }
            return S_OK;
        }
    };
    
    virtual void ShowInBounds(float x, float y, float width, float height) override
    {
        if(_child == nil)
            return;
        if(AvnInsidePotentialDeadlock::IsInside())
        {
            IAvnNativeControlHostTopLevelAttachment* slf = this;
            slf->AddRef();
            dispatch_async(dispatch_get_main_queue(), ^{
                slf->ShowInBounds(x, y, width, height);
                slf->Release();
            });
            return;
        }
        
        NSRect childFrame = {0, 0, width, height};
        NSRect holderFrame = {x, y, width, height};
        
        [_child setFrame: childFrame];
        [_holder setFrame: holderFrame];
        [_holder setHidden: false];
        if([_holder superview] != nil)
            [[_holder superview] setNeedsDisplay:true];
    }
    
    virtual void HideWithSize(float width, float height) override
    {
        if(_child == nil)
            return;
        if(AvnInsidePotentialDeadlock::IsInside())
        {
            IAvnNativeControlHostTopLevelAttachment* slf = this;
            slf->AddRef();
            dispatch_async(dispatch_get_main_queue(), ^{
                slf->HideWithSize(width, height);
                slf->Release();
            });
            return;
        }
        
        NSRect frame = {0, 0, width, height};
        [_holder setHidden: true];
        [_child setFrame: frame];
    }
    
    virtual void ReleaseChild() override
    {
        [_child removeFromSuperview];
        _child = nil;
    }
};

IAvnNativeControlHostTopLevelAttachment* CreateAttachment()
{
    return new AvnNativeControlHostTopLevelAttachment();
}

extern IAvnNativeControlHost* CreateNativeControlHost(NSView* parent)
{
    return new AvnNativeControlHost(parent);
}
