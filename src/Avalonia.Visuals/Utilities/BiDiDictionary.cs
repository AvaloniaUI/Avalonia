// Copyright (c) Six Labors.
// Licensed under the Apache License, Version 2.0.
// Ported from: https://github.com/SixLabors/Fonts/

using System.Collections.Generic;

namespace Avalonia.Utilities
{
    /// <summary>
    /// A simple bi-directional dictionary.
    /// </summary>
    /// <typeparam name="T1">Key type</typeparam>
    /// <typeparam name="T2">Value type</typeparam>
    internal sealed class BiDiDictionary<T1, T2>
        where T1 : struct
        where T2 : struct
    {
        public Dictionary<T1, T2> Forward { get; } = new Dictionary<T1, T2>();

        public Dictionary<T2, T1> Reverse { get; } = new Dictionary<T2, T1>();

        public void Clear()
        {
            Forward.Clear();
            Reverse.Clear();
        }

        public void Add(T1 key, T2 value)
        {
            Forward.Add(key, value);
            Reverse.Add(value, key);
        }

        public bool TryGetValue(T1 key, out T2 value) => Forward.TryGetValue(key, out value);

        public bool TryGetKey(T2 value, out T1 key) => Reverse.TryGetValue(value, out key);

        public bool ContainsKey(T1 key) => Forward.ContainsKey(key);

        public bool ContainsValue(T2 value) => Reverse.ContainsKey(value);
    }
}
