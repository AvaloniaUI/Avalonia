//
//  AvnString.h
//  Avalonia.Native.OSX
//
//  Created by Dan Walmsley on 07/11/2018.
//  Copyright © 2018 Avalonia. All rights reserved.
//

#ifndef AvnString_h
#define AvnString_h

extern ComPtr<IAvnString> CreateAvnString(NSString* string);
extern ComPtr<IAvnStringArray> CreateAvnStringArray(NSArray<NSString*>* array);
extern ComPtr<IAvnStringArray> CreateAvnStringArray(NSArray<NSURL*>* array);
extern ComPtr<IAvnStringArray> CreateAvnStringArray(NSString* string);
extern ComPtr<IAvnString> CreateByteArray(void* data, int len);
extern NSString* GetNSString(const ComPtr<IAvnString>& s);
extern NSArray<NSString*>* GetNSArrayOfStrings(const ComPtr<IAvnStringArray>& array);
#endif /* AvnString_h */
