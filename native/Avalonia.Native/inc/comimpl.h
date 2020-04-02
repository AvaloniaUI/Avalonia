// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
#include "com.h"
#pragma clang diagnostic push
#pragma ide diagnostic ignored "OCUnusedGlobalDeclarationInspection"
#ifndef COMIMPL_H_INCLUDED
#define COMIMPL_H_INCLUDED

#include <cstring>

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
virtual ULONG Release() override \
{ \
return ComObject::Release(); \
} \
virtual ULONG AddRef() override \
{ \
    return ComObject::AddRef(); \
} \
virtual HRESULT QueryInterface(REFIID riid, void **ppvObject) override \
{ \
    return ComObject::QueryInterface(riid, ppvObject); \
}

#define BEGIN_INTERFACE_MAP() public: virtual HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void **ppvObject) override {
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
    virtual ~ComUnknownObject(){}
};

template<class TInterface, GUID const* TIID> class ComSingleObject : public ComObject, public virtual TInterface
{
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(TInterface, *TIID)
    END_INTERFACE_MAP()
    
public:
    virtual ~ComSingleObject(){}
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
    
    TInterface* getRaw()
    {
        return _obj;
    }
    
    TInterface* getRetainedReference()
    {
        if(_obj == NULL)
            return NULL;
        _obj->AddRef();
        return _obj;
    }
    
    TInterface** getPPV()
    {
        return &_obj;
    }
    
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

#endif // COMIMPL_H_INCLUDED
#pragma clang diagnostic pop
