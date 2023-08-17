#include "KeyTransform.h"

#import <Carbon/Carbon.h>
#include <array>
#include <unordered_map>

struct KeyInfo
{
    uint16_t scanCode;
    AvnPhysicalKey physicalKey;
    AvnKey qwertyKey;
    uint16_t menuChar;
};

// ScanCode - PhysicalKey - Key mapping (the virtual key is mapped as in a standard QWERTY keyboard)
// https://github.com/chromium/chromium/blob/main/ui/events/keycodes/dom/dom_code_data.inc
// This list has the same order as the PhysicalKey enum.
const KeyInfo keyInfos[] =
{
    // Writing System Keys
    { 0x32, AvnPhysicalKeyBackquote, AvnKeyOem3, '`' },
    { 0x2A, AvnPhysicalKeyBackslash, AvnKeyOem5, '\\' },
    { 0x21, AvnPhysicalKeyBracketLeft,AvnKeyOem4, '[' },
    { 0x1E, AvnPhysicalKeyBracketRight, AvnKeyOem6, ']' },
    { 0x2B, AvnPhysicalKeyComma, AvnKeyOemComma, ',' },
    { 0x1D, AvnPhysicalKeyDigit0, AvnKeyD0, '0' },
    { 0x12, AvnPhysicalKeyDigit1, AvnKeyD1, '1' },
    { 0x13, AvnPhysicalKeyDigit2, AvnKeyD2, '2' },
    { 0x14, AvnPhysicalKeyDigit3, AvnKeyD3, '3' },
    { 0x15, AvnPhysicalKeyDigit4, AvnKeyD4, '4' },
    { 0x17, AvnPhysicalKeyDigit5, AvnKeyD5, '5' },
    { 0x16, AvnPhysicalKeyDigit6, AvnKeyD6, '6' },
    { 0x1A, AvnPhysicalKeyDigit7, AvnKeyD7, '7' },
    { 0x1C, AvnPhysicalKeyDigit8, AvnKeyD8, '8' },
    { 0x19, AvnPhysicalKeyDigit9, AvnKeyD9, '9' },
    { 0x18, AvnPhysicalKeyEqual, AvnKeyOemMinus, '-' },
    { 0x0A, AvnPhysicalKeyIntlBackslash, AvnKeyOem102, 0 },
    { 0x5E, AvnPhysicalKeyIntlRo, AvnKeyOem102, 0 },
    { 0x5D, AvnPhysicalKeyIntlYen, AvnKeyOem5, 0 },
    { 0x00, AvnPhysicalKeyA, AvnKeyA, 'a' },
    { 0x0B, AvnPhysicalKeyB, AvnKeyB, 'b' },
    { 0x08, AvnPhysicalKeyC, AvnKeyC, 'c' },
    { 0x02, AvnPhysicalKeyD, AvnKeyD, 'd' },
    { 0x0E, AvnPhysicalKeyE, AvnKeyE, 'e' },
    { 0x03, AvnPhysicalKeyF, AvnKeyF, 'f' },
    { 0x05, AvnPhysicalKeyG, AvnKeyG, 'g' },
    { 0x04, AvnPhysicalKeyH, AvnKeyH, 'h' },
    { 0x22, AvnPhysicalKeyI, AvnKeyI, 'i' },
    { 0x26, AvnPhysicalKeyJ, AvnKeyJ, 'j' },
    { 0x28, AvnPhysicalKeyK, AvnKeyK, 'k' },
    { 0x25, AvnPhysicalKeyL, AvnKeyL, 'l' },
    { 0x2E, AvnPhysicalKeyM, AvnKeyM, 'm' },
    { 0x2D, AvnPhysicalKeyN, AvnKeyN, 'n' },
    { 0x1F, AvnPhysicalKeyO, AvnKeyO, 'o' },
    { 0x23, AvnPhysicalKeyP, AvnKeyP, 'p' },
    { 0x0C, AvnPhysicalKeyQ, AvnKeyQ, 'q' },
    { 0x0F, AvnPhysicalKeyR, AvnKeyR, 'r' },
    { 0x01, AvnPhysicalKeyS, AvnKeyS, 's' },
    { 0x11, AvnPhysicalKeyT, AvnKeyT, 't' },
    { 0x20, AvnPhysicalKeyU, AvnKeyU, 'u' },
    { 0x09, AvnPhysicalKeyV, AvnKeyV, 'v' },
    { 0x0D, AvnPhysicalKeyW, AvnKeyW, 'w' },
    { 0x07, AvnPhysicalKeyX, AvnKeyX, 'x' },
    { 0x10, AvnPhysicalKeyY, AvnKeyY, 'y' },
    { 0x06, AvnPhysicalKeyZ, AvnKeyZ, 'z' },
    { 0x1B, AvnPhysicalKeyMinus, AvnKeyOemMinus, '-' },
    { 0x2F, AvnPhysicalKeyPeriod, AvnKeyOemPeriod, '.' },
    { 0x27, AvnPhysicalKeyQuote, AvnKeyOem7, '\'' },
    { 0x29, AvnPhysicalKeySemicolon, AvnKeyOem1, ';' },
    { 0x2C, AvnPhysicalKeySlash, AvnKeyOem2, '/' },

    // Functional Keys
    { 0x3A, AvnPhysicalKeyAltLeft, AvnKeyLeftAlt, 0 },
    { 0x3D, AvnPhysicalKeyAltRight, AvnKeyRightAlt, 0 },
    { 0x33, AvnPhysicalKeyBackspace, AvnKeyBack, kBackspaceCharCode },
    { 0x39, AvnPhysicalKeyCapsLock, AvnKeyCapsLock, 0 },
    { 0x6E, AvnPhysicalKeyContextMenu, AvnKeyApps, 0 },
    { 0x3B, AvnPhysicalKeyControlLeft, AvnKeyLeftCtrl, 0 },
    { 0x3E, AvnPhysicalKeyControlRight, AvnKeyRightCtrl, 0 },
    { 0x24, AvnPhysicalKeyEnter, AvnKeyEnter, kReturnCharCode },
    { 0x37, AvnPhysicalKeyMetaLeft, AvnKeyLWin, 0 },
    { 0x36, AvnPhysicalKeyMetaRight, AvnKeyRWin, 0 },
    { 0x38, AvnPhysicalKeyShiftLeft, AvnKeyLeftShift, 0 },
    { 0x3C, AvnPhysicalKeyShiftRight, AvnKeyRightShift, 0 },
    { 0x31, AvnPhysicalKeySpace, AvnKeySpace, kSpaceCharCode },
    { 0x30, AvnPhysicalKeyTab, AvnKeyTab, kTabCharCode },
    //{   , AvnPhysicalKeyConvert, 0 },
    //{   , AvnPhysicalKeyKanaMode, 0 },
    { 0x68, AvnPhysicalKeyLang1, AvnKeyKanaMode, 0 },
    { 0x66, AvnPhysicalKeyLang2, AvnKeyHanjaMode, 0 },
    //{   , AvnPhysicalKeyLang3, 0 },
    //{   , AvnPhysicalKeyLang4, 0 },
    //{   , AvnPhysicalKeyLang5, 0 },
    //{   , AvnPhysicalKeyNonConvert, 0 },

    // Control Pad Section
    { 0x75, AvnPhysicalKeyDelete, AvnKeyDelete, NSDeleteFunctionKey },
    { 0x77, AvnPhysicalKeyEnd, AvnKeyEnd, NSEndFunctionKey },
    //{   , AvnPhysicalKeyHelp, 0 },
    { 0x73, AvnPhysicalKeyHome, AvnKeyHome, NSHomeFunctionKey },
    { 0x72, AvnPhysicalKeyInsert, AvnKeyInsert, NSInsertFunctionKey },
    { 0x79, AvnPhysicalKeyPageDown, AvnKeyPageDown, NSPageDownFunctionKey },
    { 0x74, AvnPhysicalKeyPageUp, AvnKeyPageUp, NSPageUpFunctionKey },

    // Arrow Pad Section
    { 0x7D, AvnPhysicalKeyArrowDown, AvnKeyDown, NSDownArrowFunctionKey },
    { 0x7B, AvnPhysicalKeyArrowLeft, AvnKeyLeft, NSLeftArrowFunctionKey },
    { 0x7C, AvnPhysicalKeyArrowRight, AvnKeyRight, NSRightArrowFunctionKey },
    { 0x7E, AvnPhysicalKeyArrowUp, AvnKeyUp, NSUpArrowFunctionKey },

    // Numpad Section
    { 0x47, AvnPhysicalKeyNumLock, AvnKeyClear, kClearCharCode },
    { 0x52, AvnPhysicalKeyNumPad0, AvnKeyNumPad0, '0' },
    { 0x53, AvnPhysicalKeyNumPad1, AvnKeyNumPad1, '1' },
    { 0x54, AvnPhysicalKeyNumPad2, AvnKeyNumPad2, '2' },
    { 0x55, AvnPhysicalKeyNumPad3, AvnKeyNumPad3, '3' },
    { 0x56, AvnPhysicalKeyNumPad4, AvnKeyNumPad4, '4' },
    { 0x57, AvnPhysicalKeyNumPad5, AvnKeyNumPad5, '5' },
    { 0x58, AvnPhysicalKeyNumPad6, AvnKeyNumPad6, '6' },
    { 0x59, AvnPhysicalKeyNumPad7, AvnKeyNumPad7, '7' },
    { 0x5B, AvnPhysicalKeyNumPad8, AvnKeyNumPad8, '8' },
    { 0x5C, AvnPhysicalKeyNumPad9, AvnKeyNumPad9, '9' },
    { 0x45, AvnPhysicalKeyNumPadAdd, AvnKeyAdd, '+' },
    //{   , AvnPhysicalKeyNumPadClear, 0 },
    { 0x5F, AvnPhysicalKeyNumPadComma, AvnKeyAbntC2, 0 },
    { 0x41, AvnPhysicalKeyNumPadDecimal, AvnKeyDecimal, '.' },
    { 0x4B, AvnPhysicalKeyNumPadDivide, AvnKeyDivide, '/' },
    { 0x4C, AvnPhysicalKeyNumPadEnter, AvnKeyEnter, kReturnCharCode },
    { 0x51, AvnPhysicalKeyNumPadEqual, AvnKeyOemPlus, '=' },
    { 0x43, AvnPhysicalKeyNumPadMultiply, AvnKeyMultiply, '*' },
    //{   , AvnPhysicalKeyNumPadParenLeft, 0 },
    //{   , AvnPhysicalKeyNumPadParenRight, 0 },
    { 0x4E, AvnPhysicalKeyNumPadSubtract, AvnKeySubtract, '-' },

    // Function Section
    { 0x35, AvnPhysicalKeyEscape, AvnKeyEscape, kEscapeCharCode },
    { 0x7A, AvnPhysicalKeyF1, AvnKeyF1, NSF1FunctionKey },
    { 0x78, AvnPhysicalKeyF2, AvnKeyF2, NSF2FunctionKey },
    { 0x63, AvnPhysicalKeyF3, AvnKeyF3, NSF3FunctionKey },
    { 0x76, AvnPhysicalKeyF4, AvnKeyF4, NSF4FunctionKey },
    { 0x60, AvnPhysicalKeyF5, AvnKeyF5, NSF5FunctionKey },
    { 0x61, AvnPhysicalKeyF6, AvnKeyF6, NSF6FunctionKey },
    { 0x62, AvnPhysicalKeyF7, AvnKeyF7, NSF7FunctionKey },
    { 0x64, AvnPhysicalKeyF8, AvnKeyF8, NSF8FunctionKey },
    { 0x65, AvnPhysicalKeyF9, AvnKeyF9, NSF9FunctionKey },
    { 0x6D, AvnPhysicalKeyF10, AvnKeyF10, NSF10FunctionKey },
    { 0x67, AvnPhysicalKeyF11, AvnKeyF11, NSF11FunctionKey },
    { 0x6F, AvnPhysicalKeyF12, AvnKeyF12, NSF12FunctionKey },
    { 0x69, AvnPhysicalKeyF13, AvnKeyF13, NSF13FunctionKey },
    { 0x6B, AvnPhysicalKeyF14, AvnKeyF14, NSF14FunctionKey },
    { 0x71, AvnPhysicalKeyF15, AvnKeyF15, NSF15FunctionKey },
    { 0x6A, AvnPhysicalKeyF16, AvnKeyF16, NSF16FunctionKey },
    { 0x40, AvnPhysicalKeyF17, AvnKeyF17, NSF17FunctionKey },
    { 0x4F, AvnPhysicalKeyF18, AvnKeyF18, NSF18FunctionKey },
    { 0x50, AvnPhysicalKeyF19, AvnKeyF19, NSF19FunctionKey },
    { 0x5A, AvnPhysicalKeyF20, AvnKeyF20, NSF20FunctionKey },
    //{   , AvnPhysicalKeyF21, 0 },
    //{   , AvnPhysicalKeyF22, 0 },
    //{   , AvnPhysicalKeyF23, 0 },
    //{   , AvnPhysicalKeyF24, 0 },
    //{   , AvnPhysicalKeyPrintScreen, 0 },
    //{   , AvnPhysicalKeyScrollLock, 0 },
    //{   , AvnPhysicalKeyPause, 0 },

    // Media Keys
    //{   , AvnPhysicalKeyBrowserBack, 0 },
    //{   , AvnPhysicalKeyBrowserFavorites, 0 },
    //{   , AvnPhysicalKeyBrowserForward, 0 },
    //{   , AvnPhysicalKeyBrowserHome, 0 },
    //{   , AvnPhysicalKeyBrowserRefresh, 0 },
    //{   , AvnPhysicalKeyBrowserSearch, 0 },
    //{   , AvnPhysicalKeyBrowserStop, 0 },
    //{   , AvnPhysicalKeyEject, 0 },
    //{   , AvnPhysicalKeyLaunchApp1, 0 },
    //{   , AvnPhysicalKeyLaunchApp2, 0 },
    //{   , AvnPhysicalKeyLaunchMail, 0 },
    //{   , AvnPhysicalKeyMediaPlayPause, 0 },
    //{   , AvnPhysicalKeyMediaSelect, 0 },
    //{   , AvnPhysicalKeyMediaStop, 0 },
    //{   , AvnPhysicalKeyMediaTrackNext, 0 },
    //{   , AvnPhysicalKeyMediaTrackPrevious, 0 },
    //{   , AvnPhysicalKeyPower, 0 },
    //{   , AvnPhysicalKeySleep, 0 },
    { 0x49, AvnPhysicalKeyAudioVolumeDown, AvnKeyVolumeDown, 0 },
    { 0x4A, AvnPhysicalKeyAudioVolumeMute, AvnKeyVolumeMute, 0 },
    { 0x48, AvnPhysicalKeyAudioVolumeUp, AvnKeyVolumeUp, 0 },
    //{   , AvnPhysicalKeyWakeUp, 0 },

    // Legacy Keys
    //{   , AvnPhysicalKeyAgain, 0 },
    //{   , AvnPhysicalKeyCopy, 0 },
    //{   , AvnPhysicalKeyCut, 0 },
    //{   , AvnPhysicalKeyFind, 0 },
    //{   , AvnPhysicalKeyOpen, 0 },
    //{   , AvnPhysicalKeyPaste, 0 },
    //{   , AvnPhysicalKeyProps, 0 },
    //{   , AvnPhysicalKeySelect, 0 },
    //{   , AvnPhysicalKeyUndo, 0 }
};

std::unordered_map<uint16_t, AvnKey> virtualKeyFromChar =
{
    // Alphabetic keys
    { 'A', AvnKeyA },
    { 'B', AvnKeyB },
    { 'C', AvnKeyC },
    { 'D', AvnKeyD },
    { 'E', AvnKeyE },
    { 'F', AvnKeyF },
    { 'G', AvnKeyG },
    { 'H', AvnKeyH },
    { 'I', AvnKeyI },
    { 'J', AvnKeyJ },
    { 'K', AvnKeyK },
    { 'L', AvnKeyL },
    { 'M', AvnKeyM },
    { 'N', AvnKeyN },
    { 'O', AvnKeyO },
    { 'P', AvnKeyP },
    { 'Q', AvnKeyQ },
    { 'R', AvnKeyR },
    { 'S', AvnKeyS },
    { 'T', AvnKeyT },
    { 'U', AvnKeyU },
    { 'V', AvnKeyV },
    { 'W', AvnKeyW },
    { 'X', AvnKeyX },
    { 'Y', AvnKeyY },
    { 'Z', AvnKeyZ },
    { 'a', AvnKeyA },
    { 'b', AvnKeyB },
    { 'c', AvnKeyC },
    { 'd', AvnKeyD },
    { 'e', AvnKeyE },
    { 'f', AvnKeyF },
    { 'g', AvnKeyG },
    { 'h', AvnKeyH },
    { 'i', AvnKeyI },
    { 'j', AvnKeyJ },
    { 'k', AvnKeyK },
    { 'l', AvnKeyL },
    { 'm', AvnKeyM },
    { 'n', AvnKeyN },
    { 'o', AvnKeyO },
    { 'p', AvnKeyP },
    { 'q', AvnKeyQ },
    { 'r', AvnKeyR },
    { 's', AvnKeyS },
    { 't', AvnKeyT },
    { 'u', AvnKeyU },
    { 'v', AvnKeyV },
    { 'w', AvnKeyW },
    { 'x', AvnKeyX },
    { 'y', AvnKeyY },
    { 'z', AvnKeyZ },

    // Punctuation: US specific mappings (same as Chromium)
    { ';', AvnKeyOem1 },
    { ':', AvnKeyOem1 },
    { '=', AvnKeyOemPlus },
    { '+', AvnKeyOemPlus },
    { ',', AvnKeyOemComma },
    { '<', AvnKeyOemComma },
    { '-', AvnKeyOemMinus },
    { '_', AvnKeyOemMinus },
    { '.', AvnKeyOemPeriod },
    { '>', AvnKeyOemPeriod },
    { '/', AvnKeyOem2 },
    { '?', AvnKeyOem2 },
    { '`', AvnKeyOem3 },
    { '~', AvnKeyOem3 },
    { '[', AvnKeyOem4 },
    { '{', AvnKeyOem4 },
    { '\\', AvnKeyOem5 },
    { '|', AvnKeyOem5 },
    { ']', AvnKeyOem6 },
    { '}', AvnKeyOem6 },
    { '\'', AvnKeyOem7 },
    { '"', AvnKeyOem7 },

    // Apple function keys
    // https://developer.apple.com/documentation/appkit/1535851-function-key_unicode_values
    { NSDeleteFunctionKey, AvnKeyDelete },
    { NSUpArrowFunctionKey, AvnKeyUp },
    { NSLeftArrowFunctionKey, AvnKeyLeft },
    { NSRightArrowFunctionKey, AvnKeyRight },
    { NSPageUpFunctionKey, AvnKeyPageUp },
    { NSPageDownFunctionKey, AvnKeyPageDown },
    { NSHomeFunctionKey, AvnKeyHome },
    { NSEndFunctionKey, AvnKeyEnd },
    { NSClearLineFunctionKey, AvnKeyClear },
    { NSExecuteFunctionKey, AvnKeyExecute },
    { NSHelpFunctionKey, AvnKeyHelp },
    { NSInsertFunctionKey, AvnKeyInsert },
    { NSMenuFunctionKey, AvnKeyApps },
    { NSPauseFunctionKey, AvnKeyPause },
    { NSPrintFunctionKey, AvnKeyPrint },
    { NSPrintScreenFunctionKey, AvnKeyPrintScreen },
    { NSScrollLockFunctionKey, AvnKeyScroll },
    { NSF1FunctionKey, AvnKeyF1 },
    { NSF2FunctionKey, AvnKeyF2 },
    { NSF3FunctionKey, AvnKeyF3 },
    { NSF4FunctionKey, AvnKeyF4 },
    { NSF5FunctionKey, AvnKeyF5 },
    { NSF6FunctionKey, AvnKeyF6 },
    { NSF7FunctionKey, AvnKeyF7 },
    { NSF8FunctionKey, AvnKeyF8 },
    { NSF9FunctionKey, AvnKeyF9 },
    { NSF10FunctionKey, AvnKeyF10 },
    { NSF11FunctionKey, AvnKeyF11 },
    { NSF12FunctionKey, AvnKeyF12 },
    { NSF13FunctionKey, AvnKeyF13 },
    { NSF14FunctionKey, AvnKeyF14 },
    { NSF15FunctionKey, AvnKeyF15 },
    { NSF16FunctionKey, AvnKeyF16 },
    { NSF17FunctionKey, AvnKeyF17 },
    { NSF18FunctionKey, AvnKeyF18 },
    { NSF19FunctionKey, AvnKeyF19 },
    { NSF20FunctionKey, AvnKeyF20 },
    { NSF21FunctionKey, AvnKeyF21 },
    { NSF22FunctionKey, AvnKeyF22 },
    { NSF23FunctionKey, AvnKeyF23 },
    { NSF24FunctionKey, AvnKeyF24 }
};

typedef std::array<AvnPhysicalKey, 0x7F> PhysicalKeyArray;

static PhysicalKeyArray BuildPhysicalKeyFromScanCode()
{
    PhysicalKeyArray result {};

    for (auto& keyInfo : keyInfos)
    {
        result[keyInfo.scanCode] = keyInfo.physicalKey;
    }

    return result;
}

PhysicalKeyArray physicalKeyFromScanCode = BuildPhysicalKeyFromScanCode();

static std::unordered_map<AvnPhysicalKey, AvnKey, std::hash<int>> BuildQwertyVirtualKeyFromPhysicalKey()
{
    std::unordered_map<AvnPhysicalKey, AvnKey, std::hash<int>> result;
    result.reserve(sizeof(keyInfos) / sizeof(keyInfos[0]));

    for (auto& keyInfo : keyInfos)
    {
        result[keyInfo.physicalKey] = keyInfo.qwertyKey;
    }

    return result;
}

std::unordered_map<AvnPhysicalKey, AvnKey, std::hash<int>> qwertyVirtualKeyFromPhysicalKey = BuildQwertyVirtualKeyFromPhysicalKey();

static std::unordered_map<AvnKey, uint16_t, std::hash<int>> BuildMenuCharFromVirtualKey()
{
    std::unordered_map<AvnKey, uint16_t, std::hash<int>> result;
    result.reserve(100);
    
    for (auto& keyInfo : keyInfos)
    {
        if (keyInfo.menuChar != 0)
            result[keyInfo.qwertyKey] = keyInfo.menuChar;
    }

    return result;
}

std::unordered_map<AvnKey, uint16_t, std::hash<int>> menuCharFromVirtualKey = BuildMenuCharFromVirtualKey();

static bool IsNumpadOrNumericKey(AvnPhysicalKey physicalKey)
{
    return (physicalKey >= AvnPhysicalKeyDigit0 && physicalKey <= AvnPhysicalKeyDigit9)
        || (physicalKey >= AvnPhysicalKeyNumLock && physicalKey <= AvnPhysicalKeyNumPadSubtract);
}

AvnPhysicalKey PhysicalKeyFromScanCode(uint16_t scanCode)
{
    return scanCode < physicalKeyFromScanCode.size() ? physicalKeyFromScanCode[scanCode] : AvnPhysicalKeyNone;
}

static bool IsAllowedAsciiChar(UniChar c)
{
    if (c < 0x20)
    {
        switch (c)
        {
            case kBackspaceCharCode:
            case kReturnCharCode:
            case kTabCharCode:
            case kEscapeCharCode:
                return true;
            default:
                return false;
        }
    }

    if (c == kDeleteCharCode)
        return false;

    return true;
}

static UniCharCount CharsFromScanCode(UInt16 scanCode, NSEventModifierFlags modifierFlags, UInt16 keyAction, UniChar* buffer, UniCharCount bufferSize)
{
    auto currentKeyboard = TISCopyCurrentKeyboardInputSource();
    if (!currentKeyboard)
        return 0;

    auto layoutData = static_cast<CFDataRef>(TISGetInputSourceProperty(currentKeyboard, kTISPropertyUnicodeKeyLayoutData));
    if (!layoutData)
        return 0;

    auto* keyboardLayout = reinterpret_cast<const UCKeyboardLayout*>(CFDataGetBytePtr(layoutData));

    UInt32 deadKeyState = 0;
    UniCharCount length = 0;
    
    int glyphModifiers = 0;
    if (modifierFlags & NSEventModifierFlagShift)
        glyphModifiers |= shiftKey;
    if (modifierFlags & NSEventModifierFlagCapsLock)
        glyphModifiers |= alphaLock;
    if (modifierFlags & NSEventModifierFlagOption)
        glyphModifiers |= optionKey;

    auto result = UCKeyTranslate(
        keyboardLayout,
        scanCode,
        keyAction,
        (glyphModifiers >> 8) & 0xFF,
        LMGetKbdType(),
        kUCKeyTranslateNoDeadKeysBit,
        &deadKeyState,
        bufferSize,
        &length,
        buffer);

    if (result != noErr)
        return 0;

    if (deadKeyState)
    {
        // translate a space with dead key state to get the dead key itself
        result = UCKeyTranslate(
            keyboardLayout,
            kVK_Space,
            keyAction,
            0,
            LMGetKbdType(),
            kUCKeyTranslateNoDeadKeysBit,
            &deadKeyState,
            bufferSize,
            &length,
            buffer);

        if (result != noErr)
            return 0;
    }

    if (length == 1 && buffer[0] <= 0x7F && !IsAllowedAsciiChar(buffer[0]))
        return 0;

    return length;
}

AvnKey VirtualKeyFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags)
{
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);
    if (!IsNumpadOrNumericKey(physicalKey))
    {
        const UniCharCount charCount = 4;
        UniChar chars[charCount];
        auto length = CharsFromScanCode(scanCode, modifierFlags, kUCKeyActionDown, chars, charCount);
        if (length > 0)
        {
            auto it = virtualKeyFromChar.find(chars[0]);
            if (it != virtualKeyFromChar.end())
                return it->second;
        }
    }

    auto it = qwertyVirtualKeyFromPhysicalKey.find(physicalKey);
    return it == qwertyVirtualKeyFromPhysicalKey.end() ? AvnKeyNone : it->second;
}

NSString* KeySymbolFromScanCode(uint16_t scanCode, NSEventModifierFlags modifierFlags)
{
    auto physicalKey = PhysicalKeyFromScanCode(scanCode);

    const UniCharCount charCount = 4;
    UniChar chars[charCount];
    auto length = CharsFromScanCode(scanCode, modifierFlags, kUCKeyActionDisplay, chars, charCount);
    if (length > 0)
        return [NSString stringWithCharacters:chars length:length];

    auto it = qwertyVirtualKeyFromPhysicalKey.find(physicalKey);
    if (it == qwertyVirtualKeyFromPhysicalKey.end())
        return nullptr;

    auto menuChar = MenuCharFromVirtualKey(it->second);
    return menuChar == 0 || menuChar > 0x7E ? nullptr : [NSString stringWithCharacters:&menuChar length:1];
}

uint16_t MenuCharFromVirtualKey(AvnKey key)
{
    auto it = menuCharFromVirtualKey.find(key);
    return it == menuCharFromVirtualKey.end() ? 0 : it->second;
}
