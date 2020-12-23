#ifndef keytransform_h
#define keytransform_h
#include "common.h"
#include <map>

extern std::map<int, AvnKey> s_KeyMap;

extern std::map<AvnKey, int> s_AvnKeyMap;

extern std::map<int, const char*> s_QwertyKeyMap;

extern std::map<AvnKey, int> s_UnicodeKeyMap;

#endif
