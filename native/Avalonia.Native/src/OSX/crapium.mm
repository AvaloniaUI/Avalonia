// The only reason this file exists is Appium which limits our highest Xcode version to 15.2. Please, purge Appium from our codebase
#import <Foundation/Foundation.h>
#import "crapium.h"
@class MTLSharedEventHandle;
@protocol MTLSharedEvent;
@protocol MTLEvent;

typedef void (^MTLSharedEventNotificationBlock)(id <MTLSharedEvent>, uint64_t value);

API_AVAILABLE(macos(10.14), ios(12.0))
@protocol MTLSharedEvent <MTLEvent>
// Synchronously wait for the signaledValue to be greater than or equal to 'value', with a timeout
// specified in milliseconds.   Returns YES if the value was signaled before the timeout, otherwise NO.
- (BOOL)waitUntilSignaledValue:(uint64_t)value timeoutMS:(uint64_t)milliseconds API_AVAILABLE(macos(12.0), ios(15.0));
@end

API_AVAILABLE(macos(12))
extern BOOL MtlSharedEventWaitUntilSignaledValueHack(id<MTLSharedEvent> ev, uint64_t value, uint64_t milliseconds)
{
    return [ev waitUntilSignaledValue:value timeoutMS:milliseconds];
}
