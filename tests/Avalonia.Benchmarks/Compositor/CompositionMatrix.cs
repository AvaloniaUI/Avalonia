using System;
using System.Runtime.CompilerServices;
using Avalonia.Rendering.Composition.Server;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks;

public class CompositionMatrixOperations
{

    private Matrix _matrix0 = Matrix.CreateScale(2, 2); // Simulate DPI scaling of the root 
    private Matrix _matrix1 = Matrix.Identity;
    private Matrix _matrix2 = Matrix.CreateTranslation(10, 10) * Matrix.CreateScale(1.5, 1.5);
    private Matrix _matrix3 = Matrix.CreateTranslation(10, 10) * Matrix.CreateScale(1.5, 1.5);
    private Matrix _matrix4 = Matrix.Identity;


    [MethodImpl(MethodImplOptions.NoInlining)]
    private void Consume<T>(ref T t)
    {
        
    }
    
    [Benchmark]
    public void MultiplyMatrix()
    {
        
        var m = Matrix.Identity;
        for (var c = 0; c < 1000; c++)
        {
            m = m * _matrix0 * _matrix1 * _matrix2 * _matrix3 * _matrix4;
        }
        Consume(ref m);
    }

    private CompositionMatrix _cmatrix0 = CompositionMatrix.CreateScale(2, 2); // Simulate DPI scaling of the root 
    private CompositionMatrix _cmatrix1 = CompositionMatrix.Identity;
    private CompositionMatrix _cmatrix2 = CompositionMatrix.CreateTranslation(10, 10) * CompositionMatrix.CreateScale(1.5, 1.5);
    private CompositionMatrix _cmatrix3 = CompositionMatrix.CreateTranslation(10, 10) * CompositionMatrix.CreateScale(1.5, 1.5);
    private CompositionMatrix _cmatrix4 = CompositionMatrix.Identity;
    
    [Benchmark]
    public void MultiplyCompositionMatrix()
    {
        var m = CompositionMatrix.Identity;
        for (var c = 0; c < 1000; c++)
        {
            m = m * _cmatrix0 * _cmatrix1 * _cmatrix2 * _cmatrix3 * _cmatrix4;
        }
        Consume(ref m);
    }
    
    
}