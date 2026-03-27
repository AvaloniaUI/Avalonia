# Avalonia.Native

This project implements the macOS native platform backend for Avalonia using Objective-C++ and COM (Component Object Model) interfaces.


## COM Reference Management

This codebase uses COM for cross-boundary object lifetime management. COM types are typically prefixed with `IAvn` (interfaces) or `Avn` (implementations). All COM objects ultimately derive from `IUnknown` and use reference counting (`AddRef`/`Release`).

**No raw COM pointer should ever be stored in class fields, instance variables, closures, or containers.** Raw COM pointers are only acceptable as function parameters and local variables with the lifetime of the current function call.

### COM Return Value Convention

By convention, any COM interface reference returned from a COM call has its reference counter incremented — it is the caller's responsibility to release it. With the `IFoo* GetFoo()` pattern it is hard to track this correctly, so all methods in `avn.idl` should use the out-parameter pattern instead:

```cpp
// WRONG — easy to leak or forget Release:
IFoo* GetFoo();

// CORRECT — use HRESULT + out-parameter:
HRESULT GetFoo(IFoo** ppv);
```
# Smart pointer types

### Objective-C Objects

For Objective-C objects, reference counting is provided by the compiler. Objective-C pointers _look_ like raw pointers, however every `NSObject*` reference is properly ref-counted by ARC (Automatic Reference Counting). No manual wrapping is needed for `NSObject*` fields.

### COM Smart Pointer Types

All retained COM references must use one of the following wrappers defined in `inc/comimpl.h`:

#### `ComPtr<T>` — Owning Reference

Use for any COM pointer that the holder needs to keep alive.

```cpp
ComPtr<IAvnWindow> _window; // correct
IAvnWindow* _window;        // WRONG — raw COM pointer in a field
```

#### `ComObjectWeakPtr<T>` — Non-Owning Weak Reference

Use for intentional non-owning references to `ComObject`-derived objects (internal implementations). Allows safely referencing COM objects without extending their lifetime.

```cpp
ComObjectWeakPtr<WindowBaseImpl> _parent; // correct
WindowBaseImpl* _parent;                  // WRONG
```

Access weak references with `tryGet()`, which returns a `ComPtr<T>` (null if the object was destroyed):

```cpp
auto parent = _parent.tryGet();
if (parent) {
    parent->DoSomething();
}
```

#### `ComStaticPtr<T>` — Process-Lifetime Static Reference

Use for static/global COM singletons that must live for the entire process lifetime. Intentionally does **not** Release in its destructor to avoid crashes during app teardown.

```cpp
static ComStaticPtr<IAvnGlDisplay> GlDisplay; // correct
static IAvnGlDisplay* GlDisplay;              // WRONG
```

Assign via `set()`:

```cpp
GlDisplay.set(comnew<AvnGlDisplay>());
```

### `comnew<T>(args...)` — COM Object Factory

A convenience template similar to `std::make_shared`. Creates a new COM object and returns it wrapped in a `ComPtr<T>` with correct ownership (no double-AddRef):

```cpp
// Instead of:
ComPtr<Cursor> cursor(new Cursor(nsCursor), true);

// Write:
ComPtr<Cursor> cursor = comnew<Cursor>(nsCursor);
```

This works because `new T()` on a `ComObject`-derived type starts with refcount=1, and `comnew` wraps it in a `ComPtr` that takes ownership without an additional `AddRef`.
