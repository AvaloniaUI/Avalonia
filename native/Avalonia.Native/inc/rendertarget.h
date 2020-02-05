
@protocol IRenderTarget
-(void) setNewLayer: (CALayer*) layer;
-(HRESULT) setSwFrame: (AvnFramebuffer*) fb;
-(void) resize: (AvnPixelSize) size withScale: (float) scale;
-(AvnPixelSize) pixelSize;
@end

@interface IOSurfaceRenderTarget : NSObject<IRenderTarget>
-(IOSurfaceRenderTarget*) init;
@end

