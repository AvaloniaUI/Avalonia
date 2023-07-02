using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Rendering.Composition.Animations;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport;

/// <summary>
/// The batch data is separated into 2 "streams":
/// - objects: CLR reference types that are references to either server-side or common objects
/// - structs: blittable types like int, Matrix, Color
/// Each "stream" consists of memory segments that are pooled 
/// </summary>
internal class BatchStreamData
{
    public Queue<BatchStreamSegment<object?[]>> Objects { get; } = new();
    public Queue<BatchStreamSegment<IntPtr>> Structs { get; } = new();
}

public record struct BatchStreamSegment<TData>
{
    public TData Data { get; set; }
    public int ElementCount { get; set; }
}


// Unsafe.ReadUnaligned/Unsafe.WriteUnaligned are broken on arm,
// see https://github.com/dotnet/runtime/issues/80068
static unsafe class UnalignedMemoryHelper
{
    public static T ReadUnaligned<T>(byte* src) where T : unmanaged
    {
#if NET6_0_OR_GREATER
        Unsafe.SkipInit<T>(out var rv);
#else
        T rv;
#endif
        UnalignedMemcpy((byte*)&rv, src, Unsafe.SizeOf<T>());
        return rv;
    }
    
    public static void WriteUnaligned<T>(byte* dst, T value) where T : unmanaged
    {
        UnalignedMemcpy(dst, (byte*)&value, Unsafe.SizeOf<T>());
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    static unsafe void UnalignedMemcpy(byte* dst, byte* src, int count)
    {
        for (var c = 0; c < count; c++)
        {
            dst[c] = src[c];
        }
    }
}

internal class BatchStreamWriter : IDisposable
{
    private readonly BatchStreamData _output;
    private readonly BatchStreamMemoryPool _memoryPool;
    private readonly BatchStreamObjectPool<object?> _objectPool;

    private BatchStreamSegment<object?[]?> _currentObjectSegment;
    private BatchStreamSegment<IntPtr> _currentDataSegment;
    
    public BatchStreamWriter(BatchStreamData output, BatchStreamMemoryPool memoryPool, BatchStreamObjectPool<object?> objectPool)
    {
        _output = output;
        _memoryPool = memoryPool;
        _objectPool = objectPool;
    }

    void CommitDataSegment()
    {
        if (_currentDataSegment.Data != IntPtr.Zero)
            _output.Structs.Enqueue(_currentDataSegment);
        _currentDataSegment = new ();
    }
    
    void NextDataSegment()
    {
        CommitDataSegment();
        _currentDataSegment.Data = _memoryPool.Get();
    }

    void CommitObjectSegment()
    {
        if (_currentObjectSegment.Data != null)
            _output.Objects.Enqueue(_currentObjectSegment!);
        _currentObjectSegment = new();
    }
    
    void NextObjectSegment()
    {
        CommitObjectSegment();
        _currentObjectSegment.Data = _objectPool.Get();
    }

    public unsafe void Write<T>(T item) where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (_currentDataSegment.Data == IntPtr.Zero || _currentDataSegment.ElementCount + size > _memoryPool.BufferSize)
            NextDataSegment();
        var ptr = (byte*)_currentDataSegment.Data + _currentDataSegment.ElementCount;
        
        // Unsafe.ReadUnaligned/Unsafe.WriteUnaligned are broken on arm32,
        // see https://github.com/dotnet/runtime/issues/80068
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            UnalignedMemoryHelper.WriteUnaligned(ptr, item);
        else
            Unsafe.WriteUnaligned(ptr, item);
        
        _currentDataSegment.ElementCount += size;
    }

    public void WriteObject(object? item)
    {
        if (_currentObjectSegment.Data == null ||
            _currentObjectSegment.ElementCount >= _currentObjectSegment.Data.Length)
            NextObjectSegment();
        _currentObjectSegment.Data![_currentObjectSegment.ElementCount] = item;
        _currentObjectSegment.ElementCount++;
    }

    public void Dispose()
    {
        CommitDataSegment();
        CommitObjectSegment();
    }
}

internal class BatchStreamReader : IDisposable
{
    private readonly BatchStreamData _input;
    private readonly BatchStreamMemoryPool _memoryPool;
    private readonly BatchStreamObjectPool<object?> _objectPool;

    private BatchStreamSegment<object?[]?> _currentObjectSegment;
    private BatchStreamSegment<IntPtr> _currentDataSegment;
    private int _memoryOffset, _objectOffset;
    
    public BatchStreamReader(BatchStreamData input, BatchStreamMemoryPool memoryPool, BatchStreamObjectPool<object?> objectPool)
    {
        _input = input;
        _memoryPool = memoryPool;
        _objectPool = objectPool;
    }

    public unsafe T Read<T>() where T : unmanaged
    {
        var size = Unsafe.SizeOf<T>();
        if (_currentDataSegment.Data == IntPtr.Zero)
        {
            if (_input.Structs.Count == 0)
                throw new EndOfStreamException();
            _currentDataSegment = _input.Structs.Dequeue();
            _memoryOffset = 0;
        }

        if (_memoryOffset + size > _currentDataSegment.ElementCount)
            throw new InvalidOperationException("Attempted to read more memory then left in the current segment");

        var ptr = (byte*)_currentDataSegment.Data + _memoryOffset;
        T rv;
        
        // Unsafe.ReadUnaligned/Unsafe.WriteUnaligned are broken on arm32,
        // see https://github.com/dotnet/runtime/issues/80068
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm)
            rv = UnalignedMemoryHelper.ReadUnaligned<T>(ptr);
        else
            rv = Unsafe.ReadUnaligned<T>(ptr);
        
        _memoryOffset += size;
        if (_memoryOffset == _currentDataSegment.ElementCount)
        {
            _memoryPool.Return(_currentDataSegment.Data);
            _currentDataSegment = new();
        }

        return rv;
    }

    public T ReadObject<T>() where T : class? => (T)ReadObject()!;
    
    public object? ReadObject()
    {
        if (_currentObjectSegment.Data == null)
        {
            if (_input.Objects.Count == 0)
                throw new EndOfStreamException();
            _currentObjectSegment = _input.Objects.Dequeue()!;
            _objectOffset = 0;
        }

        var rv = _currentObjectSegment.Data![_objectOffset];
        _objectOffset++;
        if (_objectOffset == _currentObjectSegment.ElementCount)
        {
            _objectPool.Return(_currentObjectSegment.Data);
            _currentObjectSegment = new();
        }

        return rv;
    }

    public bool IsObjectEof => _currentObjectSegment.Data == null && _input.Objects.Count == 0;
    
    public bool IsStructEof => _currentDataSegment.Data == IntPtr.Zero && _input.Structs.Count == 0;
    
    public void Dispose()
    {
        if (_currentDataSegment.Data != IntPtr.Zero)
        {
            _memoryPool.Return(_currentDataSegment.Data);
            _currentDataSegment = new();
        }

        while (_input.Structs.Count > 0)
            _memoryPool.Return(_input.Structs.Dequeue().Data);

        if (_currentObjectSegment.Data != null)
        {
            _objectPool.Return(_currentObjectSegment.Data);
            _currentObjectSegment = new();
        }

        while (_input.Objects.Count > 0)
            _objectPool.Return(_input.Objects.Dequeue().Data);
    }
}
