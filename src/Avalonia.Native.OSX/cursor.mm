#include "common.h"

class Cursor : public ComSingleObject<IAvnCursor, &IID_IAvnCursor>
{
public:
    virtual HRESULT GetCursor (AvnStandardCursorType cursorType, void** ptr)
    {
        NSCursor * cursor;
        switch (cursorType) {
            case CursorArrow:
            case CursorAppStarting:
            case CursorWait:
                cursor = [NSCursor arrowCursor];
                break;
            case CursorTopLeftCorner:
            case CursorTopRightCorner:
            case CursorBottomLeftCorner :
            case CursorBottomRightCorner:
            case CursorCross:
            case CursorSizeAll:
                cursor = [NSCursor crosshairCursor];
                break;
            case CursorTopSide:
            case CursorUpArrow:
                cursor = [NSCursor resizeUpCursor];
                break;
            case CursorBottomSize:
                cursor = [NSCursor resizeDownCursor];
                break;
            case CursorDragCopy:
            case CursorDragMove:
                cursor = [NSCursor dragCopyCursor];
                break;
            case CursorDragLink:
                cursor = [NSCursor dragLinkCursor];
                break;
            case CursorHand:
                cursor = [NSCursor pointingHandCursor];
                break;
            case CursorHelp:
                cursor = [NSCursor contextualMenuCursor];
                break;
            case CursorIbeam:
                cursor = [NSCursor IBeamCursor];
                break;
            case CursorLeftSide:
                cursor = [NSCursor resizeLeftCursor];
                break;
            case CursorRightSide:
                cursor = [NSCursor resizeRightCursor];
                break;
            case CursorNo:
                cursor = [NSCursor operationNotAllowedCursor];
                break;
            default:
                cursor = [NSCursor operationNotAllowedCursor];
                break;
        }
        *ptr = (__bridge void*)cursor;
        return S_OK;
    }
};

extern IAvnCursor* CreateCursor()
{
    return new Cursor();
}
