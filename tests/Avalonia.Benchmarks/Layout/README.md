# Avalonia Layout System Benchmarks

This directory contains comprehensive benchmarks for analyzing the Avalonia layout system performance. These benchmarks are designed to measure and validate the performance characteristics identified in the [Layout Performance Analysis](/docs/layout-performance-analysis.md).

## Benchmark Categories

### Core Layout Benchmarks

| Benchmark Class | Description | Key Metrics |
|-----------------|-------------|-------------|
| `LayoutManagerBenchmarks` | Core layout orchestration | Layout pass time, allocations |
| `TreeTraversalBenchmarks` | Different tree structures | Deep vs wide vs balanced trees |
| `LayoutQueueBenchmarks` | Queue operations and cycle detection | Enqueue overhead, batching |
| `InvalidationBenchmarks` | Invalidation patterns | Single vs multiple invalidations |

### Panel-Specific Benchmarks

| Benchmark Class | Description | Key Metrics |
|-----------------|-------------|-------------|
| `GridBenchmarks` | Grid layout with stars and spans | Star resolution, span storage |
| `StackPanelBenchmarks` | StackPanel orientations | Horizontal vs vertical, nesting |
| `CanvasBenchmarks` | Canvas with positioned children | Attached property lookups |
| `DockPanelBenchmarks` | DockPanel layout | Dock property lookups |

### Virtualization Benchmarks

| Benchmark Class | Description | Key Metrics |
|-----------------|-------------|-------------|
| `VirtualizationBenchmarks` | VirtualizingStackPanel scroll | Scroll direction, element recycling |
| `ListVsDequeBenchmarks` | Data structure comparison | O(1) vs O(n) insertions |

### Supporting Benchmarks

| Benchmark Class | Description | Key Metrics |
|-----------------|-------------|-------------|
| `EffectiveViewportBenchmarks` | Viewport calculations | Transform chain, listener count |
| `TransformBenchmarks` | TransformToVisual operations | Matrix calculations |
| `DictionaryVsHashtableBenchmarks` | Collection performance | Boxing overhead |
| `RealWorldScenarioBenchmarks` | Common UI patterns | Forms, dashboards, data grids |

## Running Benchmarks

### Run All Benchmarks

```bash
cd tests/Avalonia.Benchmarks
dotnet run -c Release -- --filter "*"
```

### Run Specific Category

```bash
# Layout-related benchmarks
dotnet run -c Release -- --filter "*Layout*"

# Grid benchmarks only
dotnet run -c Release -- --filter "*GridBenchmarks*"

# Virtualization benchmarks
dotnet run -c Release -- --filter "*Virtualization*"
```

### Run with Memory Diagnostics

All benchmarks already include `[MemoryDiagnoser]`, but you can get additional details:

```bash
dotnet run -c Release -- --filter "*" --memory
```

### Run Quick (Shorter iterations)

```bash
dotnet run -c Release -- --filter "*" --job short
```

### Export Results

```bash
# JSON export
dotnet run -c Release -- --filter "*" --exporters json

# HTML report
dotnet run -c Release -- --filter "*" --exporters html

# All exporters
dotnet run -c Release -- --filter "*" --exporters all
```

## Benchmark Parameters

Many benchmarks accept parameters to test different scales:

| Benchmark | Parameter | Values |
|-----------|-----------|--------|
| `LayoutManagerBenchmarks` | `ControlCount` | 100, 500, 1000 |
| `GridBenchmarks` | `GridSize` | 5, 10, 20 |
| `VirtualizationBenchmarks` | `ItemCount` | 1000, 10000 |
| `StackPanelBenchmarks` | `ChildCount` | 10, 50, 200 |
| `CanvasBenchmarks` | `ChildCount` | 50, 200, 500 |
| `EffectiveViewportBenchmarks` | `ListenerCount` | 5, 20, 50 |
| `DictionaryVsHashtableBenchmarks` | `KeyCount` | 100, 500, 1000 |
| `ListVsDequeBenchmarks` | `ElementCount` | 20, 100, 500 |
| `LayoutQueueBenchmarks` | `ControlCount` | 100, 500, 1000 |

## Understanding Results

### Key Columns

- **Mean**: Average execution time
- **Error**: Half of 99.9% confidence interval
- **StdDev**: Standard deviation
- **Gen0/Gen1/Gen2**: GC collections per 1000 operations
- **Allocated**: Memory allocated per operation

### Target Thresholds

Based on 60fps target (16.67ms frame budget):

| Scenario | Target | Notes |
|----------|--------|-------|
| Full layout pass (1000 controls) | <16ms | Must not exceed frame budget |
| Single control invalidation | <0.1ms | Should be near-instant |
| Scroll operation | <8ms | Leave room for rendering |
| Grid measure (20×20) | <5ms | Complex but bounded |

### Memory Targets

| Scenario | Target | Notes |
|----------|--------|-------|
| Layout pass | <100KB | Minimize GC pressure |
| Grid measure | <10KB | Watch for boxing |
| Virtualization scroll | <50KB | Element recycling should minimize |

## Comparing Before/After Optimization

1. Run benchmarks and save baseline:
```bash
dotnet run -c Release -- --filter "*" --exporters json --artifacts ./baseline
```

2. Apply optimizations

3. Run benchmarks again:
```bash
dotnet run -c Release -- --filter "*" --exporters json --artifacts ./optimized
```

4. Compare using BenchmarkDotNet comparison tools or manual analysis.

## Adding New Benchmarks

When adding benchmarks:

1. Use `[MemoryDiagnoser]` attribute
2. Use `[MethodImpl(MethodImplOptions.NoInlining)]` to prevent inlining
3. Include setup in `[GlobalSetup]` method
4. Test both best-case and worst-case scenarios
5. Document what the benchmark measures

Example:

```csharp
[MemoryDiagnoser]
public class MyNewBenchmarks
{
    private TestRoot _root = null!;

    [GlobalSetup]
    public void Setup()
    {
        _root = new TestRoot { Renderer = new NullRenderer() };
        // Setup code...
        _root.LayoutManager.ExecuteInitialLayoutPass();
    }

    [Benchmark(Baseline = true)]
    [MethodImpl(MethodImplOptions.NoInlining)]
    public void BaselineScenario()
    {
        // Benchmark code...
    }
}
```

## Related Documentation

- [Layout Performance Analysis](/docs/layout-performance-analysis.md) - Detailed analysis and optimization proposals
- [BenchmarkDotNet Documentation](https://benchmarkdotnet.org/articles/overview.html)
