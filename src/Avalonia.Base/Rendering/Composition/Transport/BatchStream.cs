using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Server;

namespace Avalonia.Rendering.Composition.Transport;

internal class BatchStreamData
{
    public Queue<BatchStreamSegment<ServerObject[]>> Objects { get; } = new();
    public Queue<BatchStreamSegment<IntPtr>> Structs { get; } = new();
}

public struct BatchStreamSegment<TData>
{
    public TData Data { get; set; }
    public int ElementCount { get; set; }
}

internal class BatchStreamWriter : IDisposable
{
    private readonly BatchStreamData _output;
    private readonly BatchStreamMemoryPool _memoryPool;
    private readonly BatchStreamObjectPool<ServerObject> _objectPool;

    private BatchStreamSegment<ServerObject[]?> _currentObjectSegment;
    private BatchStreamSegment<IntPtr> _currentDataSegment;
    
    public BatchStreamWriter(BatchStreamData output, BatchStreamMemoryPool memoryPool, BatchStreamObjectPool<ServerObject> objectPool)
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
        *(T*)((byte*)_currentDataSegment.Data + _currentDataSegment.ElementCount) = item;
        _currentDataSegment.ElementCount += size;
    }

    public void Write(ServerObject item)
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
    private readonly BatchStreamObjectPool<ServerObject> _objectPool;

    private BatchStreamSegment<ServerObject[]?> _currentObjectSegment;
    private BatchStreamSegment<IntPtr> _currentDataSegment;
    private int _memoryOffset, _objectOffset;
    
    public BatchStreamReader(BatchStreamData _input, BatchStreamMemoryPool memoryPool, BatchStreamObjectPool<ServerObject> objectPool)
    {
        this._input = _input;
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

        var rv = *(T*)((byte*)_currentDataSegment.Data + size);
        _memoryOffset += size;
        if (_memoryOffset == _currentDataSegment.ElementCount)
        {
            _memoryPool.Return(_currentDataSegment.Data);
            _currentDataSegment = new();
        }

        return rv;
    }

    public ServerObject ReadObject()
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