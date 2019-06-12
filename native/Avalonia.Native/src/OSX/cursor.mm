// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#include "common.h"
#include "cursor.h"
#include <map>

class CursorFactory : public ComSingleObject<IAvnCursorFactory, &IID_IAvnCursorFactory>
{
    Cursor* arrowCursor = new Cursor([NSCursor arrowCursor]);
    Cursor* crossCursor = new Cursor([NSCursor crosshairCursor]);
    Cursor* resizeUpCursor = new Cursor([NSCursor resizeUpCursor]);
    Cursor* resizeDownCursor = new Cursor([NSCursor resizeDownCursor]);
    Cursor* resizeUpDownCursor = new Cursor([NSCursor resizeUpDownCursor]);
    Cursor* dragCopyCursor = new Cursor([NSCursor dragCopyCursor]);
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
        { CursorDragMove, dragCopyCursor },
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
        *retOut = s_cursorMap[cursorType];
        
        if(*retOut != nullptr)
        {
            (*retOut)->AddRef();
        }
            
        return S_OK;
    }
};

extern IAvnCursorFactory* CreateCursorFactory()
{
    @autoreleasepool
    {
        return new CursorFactory();
    }
}
