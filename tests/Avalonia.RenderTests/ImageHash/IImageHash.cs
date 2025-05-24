// <copyright file="IImageHash.cs" company="Drastic Actions">
// Copyright (c) Drastic Actions. All rights reserved.
// </copyright>

using System;
using SkiaSharp;

namespace Avalonia.UnitTests;

/// <summary>
/// Interface for perceptual image hashing algorithm.
/// </summary>
public interface IImageHash
{
    /// <summary>Hash the image using the algorithm.</summary>
    /// <param name="bitmap">image to calculate hash from.</param>
    /// <returns>hash value of the image.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="bitmap"/> is <c>null</c>.</exception>
    ulong Hash(SKBitmap bitmap);
}