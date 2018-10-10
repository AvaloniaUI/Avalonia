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

public:
    FORWARD_IUNKNOWN()
    Cursor(NSCursor * cursor)
    {
        _native = cursor;
    }

    NSCursor* GetNative()
    {
        return _native;
    }
};

extern std::map<AvnStandardCursorType, Cursor*> s_cursorMap;
#endif /* cursor_h */
