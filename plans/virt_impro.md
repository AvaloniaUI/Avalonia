# Container-Level Virtualization Implementation Summary

## Problem Statement

IVirtualizingDataTemplate virtualization was causing controls to:
- Vanish during scrolling
- Have 0x0 dimensions (invisible but in visual tree)
- Disappear on collection Reset events (collapse/expand)

## Root Cause

**Complexity from dual-level pooling:**
1. **Container pooling** - VirtualizingStackPanel pools ListBoxItems
2. **Content pooling** - ItemsControl separately pools Child controls

This caused complex interactions:
- When to clear Content property?
- When to detach Child from visual tree?
- Should `_recycledContentToUse` or `oldChild` be used?
- Which pool takes priority?

**Specific Issue:** When `PrepareRecycledContent` returned null (no content in pool because Child stayed attached), `CreateChild` would fall through to `IRecyclingDataTemplate` path which didn't properly reuse the attached `oldChild`, causing new controls to be created with 0x0 dimensions.

## Solution: Simplified Container-Level Virtualization

**Core Principle:** Container + Child = single reusable unit

### Key Changes

#### 1. ItemsControl.cs - Type-Aware Container Recycling (NeedsContainerOverride)

**Lines ~627-690**

Removed `ITypedDataTemplate` auto-recycling, keeping only:
- `IVirtualizingDataTemplate.GetKey()` for explicit recycling keys
- Fallback to `item.GetType()` for type-safe pooling

**Result:** Containers pooled by data type, ensuring template compatibility.

#### 2. ItemsControl.cs - Skip Content Clearing (ClearContainerForItemOverride)

**Lines ~555-630**

Simplified to only support `IVirtualizingDataTemplate`:
- Removed `ITypedDataTemplate && IRecyclingDataTemplate` support
- When `IVirtualizingDataTemplate` with valid key detected:
  - Skip clearing Content/ContentTemplate properties
  - Skip calling `ReturnContentToVirtualizationPool` (no separate content pooling)
  - Child stays attached to container

**Result:** Container + Child remain as a unit, no visual tree churn.

#### 3. ItemsControl.cs - Force Content Update (PrepareContainerForItemOverride)

**Lines ~397-500**

Simplified to only check `IVirtualizingDataTemplate`:
- Removed `ITypedDataTemplate && IRecyclingDataTemplate` checks
- Use `SetCurrentValue(ContentProperty, item)` instead of `SetIfUnset`
- Forces Content property update even when already set

**Result:** Content property always updated to new item when container reused.

#### 4. ContentPresenter.cs - Pass oldChild to Build() (CreateChild)

**Lines ~697-710**

Already implemented correctly:
```csharp
if (dataTemplate is IVirtualizingDataTemplate vdt)
{
    var recycled = _recycledContentToUse ?? oldChild;  // ← Key fix!
    _recycledContentToUse = null;

    newChild = vdt.Build(content, recycled);
    // ...
}
```

When `_recycledContentToUse` is null (because we skipped content pooling), use `oldChild` (the currently attached child).

**Result:** Template's `Build()` receives existing control, returns it unchanged, so `newChild == oldChild` → no visual tree changes.

## How It Works

### Scrolling Down (Item scrolls out)

1. **VSP.RecycleElement** called for ListBoxItem
2. **ItemsControl.ClearContainerForItemOverride** called
   - Detects `IVirtualizingDataTemplate` with valid key
   - **Skips** clearing Content property
   - **Skips** calling `ReturnContentToVirtualizationPool`
   - Child stays attached to ContentPresenter
3. **VSP** sets `ListBoxItem.IsVisible = false`
4. **VSP** pools ListBoxItem with `recycleKey = "YourApp.ViewModels.TextFieldViewModel"` (type-based)

### Scrolling Up (Need same item type again)

1. **VSP.GetRecycledElement** called with `recycleKey = "YourApp.ViewModels.TextFieldViewModel"`
2. **VSP** finds recycled ListBoxItem from pool (Child still attached!)
3. **VSP** sets `ListBoxItem.IsVisible = true`
4. **ItemsControl.PrepareContainerForItemOverride** called with new TextFieldViewModel instance
   - Detects container virtualization
   - **Forces** `Content = newTextFieldViewModel` (using SetCurrentValue)
   - `PrepareRecycledContent` returns null (no content in pool)
5. **ContentPresenter.UpdateChild** fires
   - **CreateChild** called: `_recycledContentToUse` is null, so uses `oldChild`
   - **Build(newTextFieldViewModel, oldChild)** → returns oldChild (TextFieldControl)
   - **newChild == oldChild** → skips visual tree changes (line 601-612 in ContentPresenter)
6. **ContentPresenter.DataContext** = newTextFieldViewModel
7. **Bindings update**, layout refreshes with new data

**Result:** Child (TextFieldControl) stays in visual tree, only DataContext changes!

## What We Removed

### ❌ Separate content pooling for container virtualization
- No calling `ReturnContentToVirtualizationPool` when skipping clear
- No interaction between VSP container pool and ItemsControl content pool
- Content pool only used for non-virtualized scenarios

### ❌ ITypedDataTemplate auto-recycling for container virtualization
- Only explicit `IVirtualizingDataTemplate` supported
- Simpler, more predictable behavior

### ❌ Complex PrepareRecycledContent interactions
- Still called for compatibility, but returns null for container virtualization
- No dual-pool coordination needed

## Benefits

✅ **Single pooling level** - Only container pooling active, no confusion
✅ **Clear ownership** - Container owns Child, never separated
✅ **Predictable behavior** - Child always stays attached during container reuse
✅ **Simpler code** - Fewer conditionals, clearer logic
✅ **No double-pooling bugs** - Can't happen with single pooling level
✅ **Easier to debug** - Single code path for container virtualization

## Files Modified

1. **src/Avalonia.Controls/ItemsControl.cs**
   - `NeedsContainerOverride` (~lines 627-690) - Type-aware recycling
   - `ClearContainerForItemOverride` (~lines 555-630) - Skip clearing for container virtualization
   - `PrepareContainerForItemOverride` (~lines 397-500) - Force Content update with SetCurrentValue

2. **src/Avalonia.Controls/Presenters/ContentPresenter.cs**
   - `CreateChild` (~lines 697-710) - Already had `_recycledContentToUse ?? oldChild` logic

## Testing Recommendations

1. **Basic scrolling:** Scroll up/down rapidly, verify no controls vanish or have 0x0 dimensions
2. **Reset events:** Collapse/expand sections, verify controls stay visible
3. **Memory profiling:** Verify containers are pooled (memory stabilizes after scroll)
4. **Type mixing:** Scroll through heterogeneous list with many different field types
5. **Rapid interactions:** Quickly scroll while expanding/collapsing to stress-test

## Critical Success Criteria

✅ Controls maintain correct dimensions during scrolling
✅ Controls don't vanish on collection Reset events
✅ DataContext updates correctly when container reused
✅ Visual tree stays stable (no detach/reattach)
✅ Memory pools containers by type

## Status

🚧 **Implementation complete, awaiting user testing**

This simplified approach eliminates all complexity around dual pooling and provides clean, predictable container-level virtualization for `IVirtualizingDataTemplate`.
