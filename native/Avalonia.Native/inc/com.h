#pragma clang diagnostic push
#pragma ide diagnostic ignored "OCUnusedGlobalDeclarationInspection"
#ifndef COM_H_INCLUDED
#define COM_H_INCLUDED


typedef struct _GUID {
    unsigned int  Data1;
    unsigned short Data2;
    unsigned short Data3;
    unsigned char  Data4[ 8 ];
} GUID;
typedef GUID IID;
typedef const IID* REFIID;
typedef unsigned int HRESULT;
typedef unsigned int DWORD;
typedef DWORD ULONG;

#define STDMETHODCALLTYPE

#define S_OK                             0x0L

#define E_NOTIMPL                        0x80004001L
#define E_NOINTERFACE                    0x80004002L
#define E_POINTER                        0x80004003L
#define E_ABORT                          0x80004004L
#define E_FAIL                           0x80004005L
#define E_UNEXPECTED                     0x8000FFFFL
#define E_HANDLE                         0x80070006L
#define E_INVALIDARG                     0x80070057L
#define COR_E_INVALIDOPERATION 0x80131509L

struct IUnknown
{
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(
            REFIID riid,
            void **ppvObject) = 0;

    virtual ULONG STDMETHODCALLTYPE AddRef( void) = 0;

    virtual ULONG STDMETHODCALLTYPE Release( void) = 0;

};

#ifdef COM_GUIDS_MATERIALIZE
#define __IID_DEF(name,d1,d2,d3, d41, d42, d43, d44, d45, d46, d47, d48) extern "C" const GUID IID_ ## name = {0x ## d1, 0x ## d2, 0x ## d3, \
{0x ## d41, 0x ## d42, 0x ## d42, 0x ## d42, 0x ## d42, 0x ## d42, 0x ## d42, 0x ## d42 } };
#else
#define __IID_DEF(name,d1,d2,d3, d41, d42, d43, d44, d45, d46, d47, d48) extern "C" const GUID IID_ ## name;
#endif
#define COMINTERFACE(name,d1,d2,d3, d41, d42, d43, d44, d45, d46, d47, d48) __IID_DEF(name,d1,d2,d3, d41, d42, d43, d44, d45, d46, d47, d48) \
struct __attribute__((annotate("uuid(" #d1 "-" #d2 "-" #d3 "-" #d41 #d42 "-" #d43 #d44 #d45 #d46 #d47 #d48 ")" ))) name

#endif // COM_H_INCLUDED
#pragma clang diagnostic pop
