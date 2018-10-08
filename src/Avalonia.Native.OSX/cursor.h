//
//  cursor.h
//  Avalonia.Native.OSX
//
//  Created by ElBuda on 10/5/18.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#ifndef cursor_h
#define cursor_h

#include "common.h"
#include <map>

class Cursor : public ComSingleObject<IAvnCursor, &IID_IAvnCursor>
{
private:
    NSCursor * _native;

public:
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
