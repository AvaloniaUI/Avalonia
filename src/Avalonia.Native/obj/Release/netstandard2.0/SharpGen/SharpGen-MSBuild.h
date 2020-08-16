// SharpGen include config [SharpGen-MSBuild] - Version 1.1

    // Use unicode
    #define UNICODE

    // for SAL annotations
    #define _PREFAST_

    // To force GUID to be declared
    #define INITGUID

    #define _ALLOW_KEYWORD_MACROS

    // Wrap all declspec for code-gen
    #define __declspec(x) __attribute__((annotate(#x)))
  

#include "AvaloniaNative.h"
#include "SharpGen.Runtime.COM.h"
