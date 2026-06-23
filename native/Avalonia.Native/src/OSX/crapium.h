// The only reason this file exists is Appium which limits our highest Xcode version to 15.2. Please, purge Appium from our codebase
#ifndef crapium_h
#define crapium_h
#import <Foundation/Foundation.h>
@protocol MTLSharedEvent;

API_AVAILABLE(macos(12))
extern BOOL MtlSharedEventWaitUntilSignaledValueHack(id<MTLSharedEvent> ev, uint64_t value, uint64_t milliseconds);
#endif /* crapium_h */
