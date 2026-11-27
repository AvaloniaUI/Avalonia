#pragma once

#include "com.h"
#include "comimpl.h"
#include "avalonia-native.h"

@protocol IRenderTarget

-(void) resize: (AvnPixelSize) size withScale: (float) scale;
-(CALayer*) layer;

@end

@interface IOSurfaceRenderTarget : NSObject<IRenderTarget>
-(IOSurfaceRenderTarget*) initWithOpenGlContext: (const ComPtr<IAvnGlContext>&) context;
-(ComPtr<IAvnGlSurfaceRenderTarget>) createSurfaceRenderTarget;
-(ComPtr<IAvnSoftwareRenderTarget>) createSoftwareRenderTarget;
-(HRESULT) setSwFrame: (AvnFramebuffer*) fb;
-(void)consumeSurfaces;
@end

@interface MetalRenderTarget : NSObject<IRenderTarget>
-(MetalRenderTarget*) initWithDevice: (const ComPtr<IAvnMetalDevice>&) device;
-(ComPtr<IAvnMetalRenderTarget>) createRenderTarget;
@end