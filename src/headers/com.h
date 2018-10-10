// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

#pragma clang diagnostic push
#pragma ide diagnostic ignored "OCUnusedGlobalDeclarationInspection"
#ifndef COM_H_INCLUDED
#define COM_H_INCLUDED

#include <cstring>

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
__IID_DEF(IUnknown, 0, 0, 0, C0, 00, 00, 00, 00, 00, 00, 46);

class ComObject : public virtual IUnknown
{
private:
    unsigned int _refCount;
public:
    
    virtual ULONG AddRef()
    {
        _refCount++;
        return _refCount;
    }
    
    
    virtual ULONG Release()
    {
        _refCount--;
        ULONG rv = _refCount;
        if(_refCount == 0)
            delete(this);
        return rv;
    }
    
    ComObject()
    {
        _refCount = 1;
        
    }
    virtual ~ComObject()
    {
    }
    
    
    virtual ::HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void **ppvObject) = 0;
    
    virtual ::HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                     void **ppvObject)
    {
        if(0 == memcmp(riid, &IID_IUnknown, sizeof(GUID)))
            *ppvObject = (IUnknown*)this;
        else
        {
            auto rv = QueryInterfaceImpl(riid, ppvObject);
            if(rv != S_OK)
                return rv;
        }
        _refCount++;
        return S_OK;
    }

};


#define FORWARD_IUNKNOWN() \
virtual ULONG Release(){ \
return ComObject::Release(); \
} \
virtual ULONG AddRef() \
{ \
    return ComObject::AddRef(); \
} \
virtual HRESULT QueryInterface(REFIID riid, void **ppvObject) \
{ \
    return ComObject::QueryInterface(riid, ppvObject); \
}

#define BEGIN_INTERFACE_MAP() public: virtual HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void **ppvObject){
#define INTERFACE_MAP_ENTRY(TInterface, IID) if(0 == memcmp(riid, &IID, sizeof(GUID))) { TInterface* casted = this; *ppvObject = casted; return S_OK; }
#define END_INTERFACE_MAP() return E_NOINTERFACE; }
#define INHERIT_INTERFACE_MAP(TBase) if(TBase::QueryInterfaceImpl(riid, ppvObject) == S_OK) return S_OK;



class ComUnknownObject : public ComObject
{
public:
    FORWARD_IUNKNOWN()
    virtual ::HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void **ppvObject) override
    {
        return E_NOINTERFACE;
    };
};

template<class TInterface, GUID const* TIID> class ComSingleObject : public ComObject, public virtual TInterface
{
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(TInterface, *TIID)
    END_INTERFACE_MAP()
};

template<class TInterface>
class ComPtr
{
private:
    TInterface* _obj;
public:
    ComPtr()
    {
        _obj = 0;
    }
    
    ComPtr(TInterface* pObj)
    {
        _obj = 0;

        if (pObj)
        {
            _obj = pObj;
            _obj->AddRef();
        }
    }
    
    ComPtr(const ComPtr& ptr)
    {
        _obj = 0;
        
        if (ptr._obj)
        {
            _obj = ptr._obj;
            _obj->AddRef();
        }

    }
    
    ComPtr& operator=(ComPtr other)
    {
        if(_obj != NULL)
            _obj->Release();
        _obj = other._obj;
        if(_obj != NULL)
            _obj->AddRef();
        return *this;
    }

    ~ComPtr()
    {
        if (_obj)
        {
            _obj->Release();
            _obj = 0;
        }
    }

public:
    operator TInterface*() const
    {
        return _obj;
    }
    TInterface& operator*() const
    {
        return *_obj;
    }
    TInterface** operator&()
    {
        return &_obj;
    }
    TInterface* operator->() const
    {
        return _obj;
    }
};

#endif // COM_H_INCLUDED
#pragma clang diagnostic pop
