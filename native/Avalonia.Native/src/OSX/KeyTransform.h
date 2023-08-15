#ifndef keytransform_h
#define keytransform_h

#import <cstdint>
#include "common.h"

AvnPhysicalKey PhysicalKeyFromScanCode(uint16_t scanCode);

AvnKey VirtualKeyFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags);

NSString* KeySymbolFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags);

uint16_t MenuCharFromVirtualKey(AvnKey key);

#endif
