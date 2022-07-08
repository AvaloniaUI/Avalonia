#pragma once
#include "com.h"
#ifndef COMIMPL_H_INCLUDED
#define COMIMPL_H_INCLUDED

#include <cstring>

#ifndef WIN32
__IID_DEF(IUnknown, 0, 0, 0, C0, 00, 00, 00, 00, 00, 00, 46);
#endif

class ComObject : public virtual IUnknown
{
  private:
    unsigned int _refCount;

  public:
    virtual ULONG STDMETHODCALLTYPE AddRef()
    {
        _refCount++;
        return _refCount;
    };

    virtual ULONG STDMETHODCALLTYPE Release()
    {
        _refCount--;
        ULONG rv = _refCount;
        if (_refCount == 0)
            delete (this);
        return rv;
    };

    ComObject()
    {
        _refCount = 1;
    };

    virtual ~ComObject(){};

    virtual ::HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void** ppvObject) = 0;

    virtual ::HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject)
    {
        if (0 == memcmp(riid, &IID_IUnknown, sizeof(GUID)))
            *ppvObject = (IUnknown*)this;
        else
        {
            auto rv = QueryInterfaceImpl(riid, ppvObject);
            if (rv != S_OK)
                return rv;
        }
        _refCount++;
        return S_OK;
    };
};

#define FORWARD_IUNKNOWN()                                                                                             \
    virtual ULONG STDMETHODCALLTYPE Release() override                                                                 \
    {                                                                                                                  \
        return ComObject::Release();                                                                                   \
    };                                                                                                                 \
    virtual ULONG STDMETHODCALLTYPE AddRef() override                                                                  \
    {                                                                                                                  \
        return ComObject::AddRef();                                                                                    \
    };                                                                                                                 \
    virtual HRESULT STDMETHODCALLTYPE QueryInterface(REFIID riid, void** ppvObject) override                           \
    {                                                                                                                  \
        return ComObject::QueryInterface(riid, ppvObject);                                                             \
    };

#define BEGIN_INTERFACE_MAP()                                                                                          \
  public:                                                                                                              \
    virtual HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void** ppvObject) override                       \
    {
#define INTERFACE_MAP_ENTRY(TInterface, IID)                                                                           \
    if (0 == memcmp(riid, &IID, sizeof(GUID)))                                                                        \
    {                                                                                                                  \
        TInterface* casted = this;                                                                                     \
        *ppvObject = casted;                                                                                           \
        return S_OK;                                                                                                   \
    }
#define END_INTERFACE_MAP()                                                                                            \
    return E_NOINTERFACE;                                                                                              \
    }
#define INHERIT_INTERFACE_MAP(TBase)                                                                                   \
    if (TBase::QueryInterfaceImpl(riid, ppvObject) == S_OK)                                                            \
        return S_OK;

class ComUnknownObject : public ComObject
{
  public:
    virtual ULONG STDMETHODCALLTYPE Release() override
    {
        return ComObject::Release();
    }

    virtual ::HRESULT STDMETHODCALLTYPE QueryInterfaceImpl(REFIID riid, void** ppvObject) override
    {
        return E_NOINTERFACE;
    };
    virtual ~ComUnknownObject()
    {
    }
};

template <class TInterface, GUID const* TIID> class ComSingleObject : public ComObject, public virtual TInterface
{
    BEGIN_INTERFACE_MAP()
    INTERFACE_MAP_ENTRY(TInterface, *TIID)
    END_INTERFACE_MAP()

  public:
    virtual ~ComSingleObject()
    {
    }
};

template <class TInterface> class ComPtr
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

    ComPtr(TInterface* pObj, bool preOwned)
    {
        _obj = 0;

        if (pObj)
        {
            _obj = pObj;
            if (!preOwned)
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
        if (_obj != NULL)
            _obj->Release();
        _obj = other._obj;
        if (_obj != NULL)
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
        if (_obj == NULL)
            return NULL;
        _obj->AddRef();
        return _obj;
    }

    TInterface** getPPV()
    {
        return &_obj;
    }

    void** getVoidPPV()
    {
        return (void**)&_obj;
    }

    template <typename TCastToInterface> bool QueryInterface(const IID& ID, ComPtr<TInterface>& OutPtr)
    {
        if (_obj == nullptr)
            return false;
        ComPtr<TCastToInterface> Temp = nullptr;
        if (_obj->QueryInterface(ID, Temp.getVoidPPV()) == 0)
        {
            OutPtr = Temp;
            return true;
        }
        return false;
    }

    template <typename TCastTo> ComPtr<TCastTo> TryCast(const IID& Iid)
    {
        if (_obj == nullptr)
            return nullptr;
        ComPtr<TCastTo> Temp;
        if (_obj->QueryInterface(Iid, Temp.getVoidPPV()) == 0)
        {
            return Temp;
        }
        return nullptr;
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
