#include "common.h"
#include "cursor.h"
#include <map>

class CursorFactory : public ComSingleObject<IAvnCursorFactory, &IID_IAvnCursorFactory>
{
    Cursor * arrowCursor = new Cursor([NSCursor arrowCursor]);
    Cursor * crossCursor = new Cursor([NSCursor crosshairCursor]);
    Cursor * resizeUpCursor = new Cursor([NSCursor resizeUpCursor]);
    Cursor * resizeDownCursor = new Cursor([NSCursor resizeDownCursor]);
    Cursor * dragCopyCursor = new Cursor([NSCursor dragCopyCursor]);
    Cursor * dragLinkCursor = new Cursor([NSCursor dragLinkCursor]);
    Cursor * pointingHandCursor = new Cursor([NSCursor pointingHandCursor]);
    Cursor * contextualMenuCursor = new Cursor([NSCursor contextualMenuCursor]);
    Cursor * IBeamCursor = new Cursor([NSCursor IBeamCursor]);
    Cursor * resizeLeftCursor = new Cursor([NSCursor resizeLeftCursor]);
    Cursor * resizeRightCursor = new Cursor([NSCursor resizeRightCursor]);
    Cursor * operationNotAllowedCursor = new Cursor([NSCursor operationNotAllowedCursor]);

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
        { CursorTopSide, resizeUpCursor },
        { CursorUpArrow, resizeUpCursor },
        { CursorBottomSize, resizeDownCursor },
        { CursorDragCopy, dragCopyCursor },
        { CursorDragMove, dragCopyCursor },
        { CursorDragLink, dragLinkCursor },
        { CursorHand, pointingHandCursor },
        { CursorHelp, contextualMenuCursor },
        { CursorIbeam, IBeamCursor },
        { CursorLeftSide, resizeLeftCursor },
        { CursorRightSide, resizeRightCursor },
        { CursorNo, operationNotAllowedCursor }
    };

public:
    virtual HRESULT GetCursor (AvnStandardCursorType cursorType, IAvnCursor** retOut)
    {
        *retOut = s_cursorMap[cursorType];
        (*retOut)->AddRef();
        return S_OK;
    }
};

extern IAvnCursorFactory* CreateCursorFactory()
{
    return new CursorFactory();
}
