// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#ifndef cursor_h
#define cursor_h

#include "common.h"
#include <map>

class Cursor : public ComSingleObject<IAvnCursor, &IID_IAvnCursor>
{
private:
    NSCursor * _native;
    bool _isHidden;
public:
    FORWARD_IUNKNOWN()
    Cursor(NSCursor * cursor, bool isHidden = false)
    {
        _native = cursor;
        _isHidden = isHidden;
    }

    NSCursor* GetNative()
    {
        return _native;
    }
    
    bool IsHidden ()
    {
        return _isHidden;
    }
};

extern std::map<AvnStandardCursorType, Cursor*> s_cursorMap;
#endif /* cursor_h */
