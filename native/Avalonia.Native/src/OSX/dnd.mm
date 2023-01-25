#include "common.h"

extern AvnDragDropEffects ConvertDragDropEffects(NSDragOperation nsop)
{
    int effects = 0;
    if((nsop & NSDragOperationCopy) != 0)
        effects |= (int)AvnDragDropEffects::Copy;
    if((nsop & NSDragOperationMove) != 0)
        effects |= (int)AvnDragDropEffects::Move;
    if((nsop & NSDragOperationLink) != 0)
        effects |= (int)AvnDragDropEffects::Link;
    return (AvnDragDropEffects)effects;
};

extern NSString* GetAvnCustomDataType()
{
    char buffer[256];
    sprintf(buffer, "net.avaloniaui.inproc.uti.n%in", getpid());
    return [NSString stringWithUTF8String:buffer];
}

@interface AvnDndSource : NSObject<NSDraggingSource>

@end

@implementation AvnDndSource
{
    NSDragOperation _operation;
    ComPtr<IAvnDndResultCallback> _cb;
    void* _sourceHandle;
};

- (NSDragOperation)draggingSession:(nonnull NSDraggingSession *)session sourceOperationMaskForDraggingContext:(NSDraggingContext)context
{
    return _operation;
}

- (AvnDndSource*) initWithOperation: (NSDragOperation)operation
                        andCallback: (IAvnDndResultCallback*) cb
                    andSourceHandle: (void*) handle
{
    self = [super init];
    _operation = operation;
    _cb = cb;
    _sourceHandle = handle;
    return self;
}

- (void)draggingSession:(NSDraggingSession *)session
           endedAtPoint:(NSPoint)screenPoint
              operation:(NSDragOperation)operation
{
    if(_cb != nil)
    {
        auto cb = _cb;
        _cb = nil;
        cb->OnDragAndDropComplete(ConvertDragDropEffects(operation));
    }
    if(_sourceHandle != nil)
    {
        FreeAvnGCHandle(_sourceHandle);
        _sourceHandle = nil;
    }
}

- (void*) gcHandle
{
    return _sourceHandle;
}

@end

extern NSObject<NSDraggingSource>* CreateDraggingSource(NSDragOperation op, IAvnDndResultCallback* cb, void* handle)
{
    return [[AvnDndSource alloc] initWithOperation:op andCallback:cb andSourceHandle:handle];
};

extern void* GetAvnDataObjectHandleFromDraggingInfo(NSObject<NSDraggingInfo>* info)
{
    id obj = [info draggingSource];
    if(obj == nil)
        return nil;
    if([obj isKindOfClass: [AvnDndSource class]])
    {
        auto src = (AvnDndSource*)obj;
        return [src gcHandle];
    }
    return nil;
}
