#pragma once

#include "com.h"
#include "comimpl.h"
#include "avalonia-native.h"

@protocol IRenderTarget
-(void) setNewLayer: (CALayer*) layer;
-(HRESULT) setSwFrame: (AvnFramebuffer*) fb;
-(void) resize: (AvnPixelSize) size withScale: (float) scale;
-(AvnPixelSize) pixelSize;
-(IAvnGlSurfaceRenderTarget*) createSurfaceRenderTarget;
@end

@interface IOSurfaceRenderTarget : NSObject<IRenderTarget>
-(IOSurfaceRenderTarget*) initWithOpenGlContext: (IAvnGlContext*) context;
@end
