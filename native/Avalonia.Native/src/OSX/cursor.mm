#include "common.h"
#include "cursor.h"

class CursorFactory : public ComSingleObject<IAvnCursorFactory, &IID_IAvnCursorFactory>
{
    Cursor* arrowCursor = new Cursor([NSCursor arrowCursor]);
    Cursor* crossCursor = new Cursor([NSCursor crosshairCursor]);
    Cursor* resizeUpCursor = new Cursor([NSCursor resizeUpCursor]);
    Cursor* resizeDownCursor = new Cursor([NSCursor resizeDownCursor]);
    Cursor* resizeUpDownCursor = new Cursor([NSCursor resizeUpDownCursor]);
    Cursor* dragCopyCursor = new Cursor([NSCursor dragCopyCursor]);
    Cursor* openHandCursor = new Cursor([NSCursor openHandCursor]);
    Cursor* dragLinkCursor = new Cursor([NSCursor dragLinkCursor]);
    Cursor* pointingHandCursor = new Cursor([NSCursor pointingHandCursor]);
    Cursor* contextualMenuCursor = new Cursor([NSCursor contextualMenuCursor]);
    Cursor* IBeamCursor = new Cursor([NSCursor IBeamCursor]);
    Cursor* resizeLeftCursor = new Cursor([NSCursor resizeLeftCursor]);
    Cursor* resizeRightCursor = new Cursor([NSCursor resizeRightCursor]);
    Cursor* resizeWestEastCursor = new Cursor([NSCursor resizeLeftRightCursor]);
    Cursor* operationNotAllowedCursor = new Cursor([NSCursor operationNotAllowedCursor]);
    Cursor* noCursor = new Cursor([NSCursor arrowCursor], true);

    std::map<AvnStandardCursorType, Cursor*> s_cursorMap =
    {
        { CursorArrow, arrowCursor },
        { CursorAppStarting, arrowCursor },
        { CursorWait, arrowCursor },
        { CursorTopLeftCorner, crossCursor },
        { CursorTopRightCorner, crossCursor },
        { CursorBottomLeftCorner, crossCursor },
        { CursorBottomRightCorner, crossCursor },
        { CursorCross, crossCursor },
        { CursorSizeAll, crossCursor },
        { CursorSizeNorthSouth, resizeUpDownCursor},
        { CursorSizeWestEast, resizeWestEastCursor},
        { CursorTopSide, resizeUpCursor },
        { CursorUpArrow, resizeUpCursor },
        { CursorBottomSize, resizeDownCursor },
        { CursorDragCopy, dragCopyCursor },
        { CursorDragMove, openHandCursor },
        { CursorDragLink, dragLinkCursor },
        { CursorHand, pointingHandCursor },
        { CursorHelp, contextualMenuCursor },
        { CursorIbeam, IBeamCursor },
        { CursorLeftSide, resizeLeftCursor },
        { CursorRightSide, resizeRightCursor },
        { CursorNo, operationNotAllowedCursor },
        { CursorNone, noCursor }
    };

public:
    FORWARD_IUNKNOWN()
    
    virtual HRESULT GetCursor (AvnStandardCursorType cursorType, IAvnCursor** retOut) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            *retOut = s_cursorMap[cursorType];
            
            if(*retOut != nullptr)
            {
                (*retOut)->AddRef();
            }
                
            return S_OK;
        }
    }
    
    virtual HRESULT CreateCustomCursor (void* bitmapData, size_t length, AvnPixelSize hotPixel, IAvnCursor** retOut) override
    {
        START_COM_CALL;
        
        @autoreleasepool
        {
            if(bitmapData == nullptr || retOut == nullptr)
            {
                return E_POINTER;
            }
            
            NSData *imageData = [NSData dataWithBytes:bitmapData length:length];
            NSImage *image = [[NSImage alloc] initWithData:imageData];
            
            
            NSPoint hotSpot;
            hotSpot.x = hotPixel.Width;
            hotSpot.y = hotPixel.Height;
            
            *retOut = new Cursor([[NSCursor new] initWithImage: image hotSpot: hotSpot]);
            
            (*retOut)->AddRef();
            
            return S_OK;
        }
    }
};

extern IAvnCursorFactory* CreateCursorFactory()
{
    @autoreleasepool
    {
        return new CursorFactory();
    }
}
