# Rendering Benchmarks

This directory contains benchmarks for the Avalonia rendering pipeline and compositor.

## Benchmark Categories

### CompositorBenchmarks.cs

Tests the high-level compositor operations:
- `RenderVisualTree` - Baseline rendering of the visual tree
- `InvalidateSingleControl` - Cost of invalidating and re-rendering one control
- `InvalidateAllControls` - Cost of invalidating and re-rendering all controls
- `ChangeOpacity` - Cost of opacity changes triggering compositor updates
- `ChangeTransforms` - Cost of transform changes on controls

### DirtyRectBenchmarks.cs

Tests the dirty rect tracking system:
- `AddDirtyRects` - Cost of adding dirty rects to the tracker
- `IntersectionChecks` - Cost of checking if rects intersect dirty regions
- `AddAndCheck` - Combined workflow of adding and checking

### BatchStreamBenchmarks.cs

Tests the compositor transport layer:
- `WritePrimitives` - Cost of serializing primitive values
- `WriteMatrices` - Cost of serializing Matrix values
- `WriteObjects` - Cost of serializing object references
- `WriteMixed` - Mixed writes (typical usage pattern)
- `WriteAndRead` - Round-trip serialization performance

### DrawingContextBenchmarks.cs

Tests drawing operations:
- `DrawRectangle` - Cost of rectangle drawing
- `DrawEllipse` - Cost of ellipse drawing
- `DrawLine` - Cost of line drawing
- `DrawGeometry` - Cost of geometry drawing
- `PushPopClip` - Cost of clip operations
- `PushPopOpacity` - Cost of opacity operations
- `PushPopTransform` - Cost of transform operations
- `NestedOperations` - Cost of nested drawing context operations

### VisualTreeRenderBenchmarks.cs

Tests different visual tree structures:
- `RenderFlatTree` - Flat visual tree (all controls in one panel)
- `RenderDeepTree` - Deep visual tree (nested panels)
- `RenderWideTree` - Wide visual tree (multiple sibling panels)
- `InvalidatePartialFlatTree` - Partial invalidation patterns

### MatrixBenchmarks.cs

Tests matrix operations used in rendering:
- `MatrixMultiplication` - Cost of matrix multiplication
- `MatrixInversion` - Cost of matrix inversion
- `TransformPoint` - Cost of point transformation
- `TransformRect` - Cost of rect transformation
- `CreateMatrices` - Cost of common matrix creation patterns
- `MatrixDecompose` - Cost of matrix decomposition
- `BuildTransformChain` - Cost of building transform chains

## Running the Benchmarks

Run all rendering benchmarks:
```bash
dotnet run -c Release --filter '*Rendering*'
```

Run specific benchmark class:
```bash
dotnet run -c Release --filter '*CompositorBenchmarks*'
dotnet run -c Release --filter '*DirtyRectBenchmarks*'
dotnet run -c Release --filter '*BatchStreamBenchmarks*'
dotnet run -c Release --filter '*DrawingContextBenchmarks*'
dotnet run -c Release --filter '*VisualTreeRenderBenchmarks*'
dotnet run -c Release --filter '*MatrixBenchmarks*'
```

## Key Metrics to Monitor

1. **Allocations** - Memory allocations per operation (should be minimal during rendering)
2. **Mean Time** - Average execution time
3. **Gen0/Gen1 Collections** - GC pressure indicators

## Benchmark Results (Apple M3 Pro, .NET 10.0)

### CompositorBenchmarks (100 controls)

| Method | Mean | Allocated |
|--------|------|-----------|
| RenderVisualTree | 15 ns | 0 B |
| InvalidateSingleControl | 17 ns | 0 B |
| InvalidateAllControls | 113 ns | 0 B |
| ChangeOpacity | 4,023 ns | 8,000 B |
| ChangeTransforms (new) | 38,383 ns | 108,560 B |
| UpdateExistingTransforms | 5,622 ns | 8,000 B |

**Key Finding**: Creating new transform objects allocates 13.5x more memory than updating existing ones.
The base 80 bytes/control allocation for opacity/transform changes comes from compositor serialization.

### DirtyRectBenchmarks (100 rects)

| Method | Mean | Allocated |
|--------|------|-----------|
| AddDirtyRects | 211 ns | 0 B |
| IntersectionChecks | 56 ns | 0 B |
| AddAndCheck | 140 ns | 0 B |

**Key Finding**: Dirty rect operations are allocation-free.

### BatchStreamBenchmarks (1000 items)

| Method | Mean | Allocated |
|--------|------|-----------|
| WritePrimitives | 14,219 ns | 704 B |
| WriteMatrices | 15,364 ns | 4,288 B |
| WriteObjects | 3,943 ns | 8,736 B |
| WriteMixed | 24,370 ns | 12,984 B |

**Key Finding**: Object serialization is fastest but allocates more due to object pool growth.

### DrawingContextBenchmarks

| Method | Mean | Allocated |
|--------|------|-----------|
| DrawRectangle | 196 ns | 0 B |
| DrawEllipse | 85 ns | 0 B |
| DrawLine | 444 ns | 0 B |
| DrawGeometry | 113 ns | 0 B |
| PushPopClip | 598 ns | 0 B |
| PushPopOpacity | 651 ns | 0 B |
| PushPopTransform | 1,215 ns | 0 B |
| NestedOperations | 2,282 ns | 0 B |

**Key Finding**: All drawing operations are allocation-free.

## Optimization Areas

Based on these benchmarks, potential optimization areas include:

1. **Dirty Rect Tracking** - ✅ Already efficient and allocation-free
2. **Batch Serialization** - Some allocations for pool growth, consider pre-warming pools
3. **Transform Changes** - Reuse transform objects instead of creating new ones
4. **Drawing Context** - ✅ Already allocation-free
5. **Compositor Batching** - CompositionBatch creates TaskCompletionSource per commit
