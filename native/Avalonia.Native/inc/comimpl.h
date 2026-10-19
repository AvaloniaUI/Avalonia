// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.
#include "com.h"
#pragma clang diagnostic push
#pragma ide diagnostic ignored "OCUnusedGlobalDeclarationInspection"
#ifndef COMIMPL_H_INCLUDED
#define COMIMPL_H_INCLUDED

#include <cstring>
#include <memory>
#include <utility>
#include <type_traits>
#include <cstddef>

__IID_DEF(IUnknown, 0, 0, 0, C0, 00, 00, 00, 00, 00, 00, 46);

class ComObject;
template<class T> struct MicroComImpl;

// Private IID: ComObject answers QueryInterface for it with its own ComObject pointer.
// Objects implemented in C# do not know it and return E_NOINTERFACE.
__IID_DEF(AvnComObjectSelf, 6c1e2a4b, 93d5, 4f0e, b7, 21, 5a, 8e, c3, 0d, 4f, 96);

template<class TInterface>
class ComPtr
{
private:
    TInterface* _obj;
public:
    ComPtr()
    {
        _obj = nullptr;
    }

    ComPtr(std::nullptr_t)
    {
        _obj = nullptr;
    }

    ComPtr(const ComPtr& ptr) : ComPtr()
    {
        _microcom_attach(ptr._obj, false);
    }

    ComPtr(ComPtr&& ptr) noexcept
    {
        _obj = ptr._obj;
        ptr._obj = nullptr;
    }

    template<class U, class = typename std::enable_if<std::is_convertible<U*, TInterface*>::value>::type>
    ComPtr(const ComPtr<U>& other) : ComPtr()
    {
        _microcom_attach(other._microcom_get(), false);
    }

    ComPtr& operator=(const ComPtr& other)
    {
        _microcom_attach(other._obj, false);
        return *this;
    }

    ComPtr& operator=(ComPtr&& other) noexcept
    {
        if (this != &other)
        {
            auto old = _obj;
            _obj = other._obj;
            other._obj = nullptr;
            if (old)
                old->Release();
        }
        return *this;
    }

    ~ComPtr()
    {
        if (_obj)
        {
            _obj->Release();
            _obj = nullptr;
        }
    }

    TInterface* getRetainedReference() const
    {
        if (_obj == nullptr)
            return nullptr;
        _obj->AddRef();
        return _obj;
    }

    // Safe on objects implemented in C#: those yield null
    template<class TCast> ComPtr<TCast> dynamicCast() const;

    // MicroCom smart pointer contract
    void _microcom_attach(TInterface* obj, bool owns)
    {
        if (obj && !owns)
            obj->AddRef();
        if (_obj)
            _obj->Release();
        _obj = obj;
    }

    TInterface* _microcom_get() const { return _obj; }
    TInterface** _microcom_out()
    {
        if (_obj)
        {
            _obj->Release();
            _obj = nullptr;
        }
        return &_obj;
    }

    TInterface* _microcom_detach()
    {
        auto rv = _obj;
        _obj = nullptr;
        return rv;
    }

    explicit operator bool() const { return _obj != nullptr; }
    bool operator==(std::nullptr_t) const { return _obj == nullptr; }
    bool operator!=(std::nullptr_t) const { return _obj != nullptr; }
    template<class U> bool operator==(const ComPtr<U>& other) const { return _obj == other._microcom_get(); }
    template<class U> bool operator!=(const ComPtr<U>& other) const { return _obj != other._microcom_get(); }

    TInterface* operator->() const
    {
        return _obj;
    }
};

template<class T, class... Args>
ComPtr<T> comnew(Args&&... args)
{
    ComPtr<T> rv;
    rv._microcom_attach(new T(std::forward<Args>(args)...), true);
    return rv;
}

/**
 * ComStaticPtr is a COM smart pointer for process-lifetime static singletons.
 * It AddRef/Release on set/reset, but intentionally does NOT Release in its destructor
 * to avoid crashes during app teardown when destruction order is undefined.
 */
template<class TInterface>
class ComStaticPtr
{
private:
    TInterface* _obj = nullptr;
public:
    ComStaticPtr() = default;
    ~ComStaticPtr() = default;

    ComStaticPtr(const ComPtr<TInterface>& ptr)
    {
        _obj = ptr.getRetainedReference();
    }

    ComStaticPtr(const ComStaticPtr&) = delete;
    ComStaticPtr(ComStaticPtr&&) = delete;
    ComStaticPtr& operator=(const ComStaticPtr&) = delete;
    ComStaticPtr& operator=(ComStaticPtr&&) = delete;

    void set(const ComPtr<TInterface>& obj)
    {
        if (_obj == obj._microcom_get())
            return;
        if (_obj)
            _obj->Release();
        _obj = obj.getRetainedReference();
    }

    void reset()
    {
        if (_obj)
        {
            _obj->Release();
            _obj = nullptr;
        }
    }

    operator ComPtr<TInterface>() const { return get(); }
    ComPtr<TInterface> get() const
    {
        ComPtr<TInterface> rv;
        rv._microcom_attach(_obj, false);
        return rv;
    }
    explicit operator bool() const { return _obj != nullptr; }
    TInterface* operator->() const { return _obj; }
};

class ComObjectWeakRefToken
{
public:
    bool Alive = true;
};


class ComObject : public virtual IUnknown
{
private:
    unsigned int _refCount;
    std::shared_ptr<ComObjectWeakRefToken> _weakRefs;
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
        if(_weakRefs)
            _weakRefs->Alive = false;
    }
    
    std::shared_ptr<ComObjectWeakRefToken> __GetWeakRefToken()
    {
        if(_weakRefs == nullptr)
        {
            _weakRefs = std::make_shared<ComObjectWeakRefToken>();
        }
        return _weakRefs;
    }

    
    virtual ::HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void **ppvObject) = 0;
    
    virtual ::HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid,
                                                     void **ppvObject)
    {
        if(0 == memcmp(riid, &IID_AvnComObjectSelf, sizeof(GUID)))
            *ppvObject = this;
        else if(0 == memcmp(riid, &IID_IUnknown, sizeof(GUID)))
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

template<class TInterface>
template<class TCast>
ComPtr<TCast> ComPtr<TInterface>::dynamicCast() const
{
    if (_obj == nullptr)
        return nullptr;
    ComPtr<ComObject> self;
    if (_obj->QueryInterface(&IID_AvnComObjectSelf, (void**)self._microcom_out()) != S_OK || !self)
        return nullptr;
    ComPtr<TCast> rv;
    rv._microcom_attach(dynamic_cast<TCast*>(self._microcom_get()), false);
    return rv;
}

// Strong reference to the calling object, for passing `this` where a ComPtr is expected
template<class T, class = typename std::enable_if<std::is_base_of<ComObject, T>::value>::type>
ComPtr<T> comself(T* self)
{
    ComPtr<T> rv;
    rv._microcom_attach(self, false);
    return rv;
}

template<class TClass>
class ComObjectWeakPtr
{
private:
    std::shared_ptr<ComObjectWeakRefToken> _token;
    TClass* _rawPtr;
public:
    ComPtr<TClass> tryGet()
    {
        if(_rawPtr == nullptr)
            return nullptr;
        if(!_token->Alive)
            return nullptr;
        ComPtr<TClass> rv;
        rv._microcom_attach(_rawPtr, false);
        return rv;
    }
    
    template<class TCast> ComPtr<TCast> tryGetWithCast()
    {
        return tryGet().template dynamicCast<TCast>();
    }
    
    template<class U, class = typename std::enable_if<std::is_convertible<U*, TClass*>::value>::type>
    ComObjectWeakPtr(const ComPtr<U>& obj)
    {
        _rawPtr = static_cast<TClass*>(obj._microcom_get());
        if(_rawPtr)
            _token = _rawPtr->__GetWeakRefToken();
    }
    
    ComObjectWeakPtr()
    {
        _rawPtr = nullptr;
        _token = nullptr;
    }

    ComObjectWeakPtr(std::nullptr_t) : ComObjectWeakPtr()
    {
    }
    
    bool operator==(const ComObjectWeakPtr& other) const { return _rawPtr == other._rawPtr; }
    bool operator!=(const ComObjectWeakPtr& other) const { return _rawPtr != other._rawPtr; }
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

template<class TInterface, GUID const* TIID> class ComSingleObject : public ComObject, public virtual MicroComImpl<TInterface>
{
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(TInterface, *TIID)
    END_INTERFACE_MAP()
    
public:
    virtual ~ComSingleObject(){}
};

#endif // COMIMPL_H_INCLUDED
#pragma clang diagnostic pop
