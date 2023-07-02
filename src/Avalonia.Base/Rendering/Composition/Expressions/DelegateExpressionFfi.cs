using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Avalonia.Media;

namespace Avalonia.Rendering.Composition.Expressions
{
    /// <summary>
    /// Foreign function interface for composition animations based on calling delegates
    /// </summary>
    internal class DelegateExpressionFfi : IExpressionForeignFunctionInterface, IEnumerable
    {
        struct FfiRecord
        {
            public VariantType[] Types;
            public Func<IReadOnlyList<ExpressionVariant>, ExpressionVariant> Delegate;
        }

        private readonly Dictionary<string, Dictionary<int, List<FfiRecord>>>
            _registry = new Dictionary<string, Dictionary<int, List<FfiRecord>>>();

        public bool Call(string name, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result)
        {
            result = default;
            if (!_registry.TryGetValue(name, out var nameGroup))
                return false;
            if (!nameGroup.TryGetValue(arguments.Count, out var countGroup))
                return false;
            foreach (var record in countGroup)
            {
                var match = true;
                for (var c = 0; c < arguments.Count; c++)
                {
                    if (record.Types[c] != arguments[c].Type)
                    {
                        match = false;
                        break;
                    }
                }

                if (match)
                {
                    result = record.Delegate(arguments);
                    return true;
                }
            }

            return CallWithCast(countGroup, arguments, out result, false);
        }
        
        bool CallWithCast(List<FfiRecord> countGroup, IReadOnlyList<ExpressionVariant> arguments, out ExpressionVariant result, bool anyCast)
        {
            result = default;
            foreach (var record in countGroup)
            {
                var match = true;
                for (var c = 0; c < arguments.Count; c++)
                {
                    var parameter = record.Types[c];
                    var arg = arguments[c].Type;
                    if (parameter != arg)
                    {
                        var canCast = (parameter == VariantType.Double && arg == VariantType.Scalar)
                                      || (parameter == VariantType.Vector3D && arg == VariantType.Vector3)
                                      || (parameter == VariantType.Vector && arg == VariantType.Vector2)
                                      || (anyCast && (
                                          (arg == VariantType.Double && parameter == VariantType.Scalar)
                                          || (arg == VariantType.Vector3D && parameter == VariantType.Vector3)
                                          || (arg == VariantType.Vector && parameter == VariantType.Vector2)
                                      ));
                        if (!canCast)
                        {
                            match = false;
                            break;
                        }
                    }
                }

                if (match)
                {
                    result = record.Delegate(arguments);
                    return true;
                }
            }

            if (anyCast == false)
                return CallWithCast(countGroup, arguments, out result, true);
            return false;
        }

        // Stub for collection initializer 
        IEnumerator IEnumerable.GetEnumerator() => Array.Empty<object>().GetEnumerator();

        void Add(string name, Func<IReadOnlyList<ExpressionVariant>, ExpressionVariant> cb,
            params Type[] types)
        {
            if (!_registry.TryGetValue(name, out var nameGroup))
                _registry[name] = nameGroup =
                    new Dictionary<int, List<FfiRecord>>();
            if (!nameGroup.TryGetValue(types.Length, out var countGroup))
                nameGroup[types.Length] = countGroup = new List<FfiRecord>();

            countGroup.Add(new FfiRecord
            {
                Types = types.Select(t => TypeMap[t]).ToArray(),
                Delegate = cb
            });
        }

        static readonly Dictionary<Type, VariantType> TypeMap = new Dictionary<Type, VariantType>
        {
            [typeof(bool)] = VariantType.Boolean,
            [typeof(float)] = VariantType.Scalar,
            [typeof(double)] = VariantType.Double,
            [typeof(Vector2)] = VariantType.Vector2,
            [typeof(Vector)] = VariantType.Vector,
            [typeof(Vector3)] = VariantType.Vector3,
            [typeof(Vector3D)] = VariantType.Vector3D,
            [typeof(Vector4)] = VariantType.Vector4,
            [typeof(Matrix3x2)] = VariantType.Matrix3x2,
            [typeof(Matrix4x4)] = VariantType.Matrix4x4,
            [typeof(Quaternion)] = VariantType.Quaternion,
            [typeof(Color)] = VariantType.Color
        };

        public void Add<T1>(string name, Func<T1, ExpressionVariant> cb) where T1 : struct
        {
            Add(name, args => cb(args[0].CastOrDefault<T1>()), typeof(T1));
        }

        public void Add<T1, T2>(string name, Func<T1, T2, ExpressionVariant> cb) where T1 : struct where T2 : struct
        {
            Add(name, args => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>()), typeof(T1), typeof(T2));
        }


        public void Add<T1, T2, T3>(string name, Func<T1, T2, T3, ExpressionVariant> cb)
            where T1 : struct where T2 : struct where T3 : struct
        {
            Add(name, args => cb(args[0].CastOrDefault<T1>(), args[1].CastOrDefault<T2>(), args[2].CastOrDefault<T3>()), typeof(T1), typeof(T2),
                typeof(T3));
        }
        
        public void Add<T1, T2, T3, T4>(string name, Func<T1, T2, T3, T4, ExpressionVariant> cb)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct
        {
            Add(name, args => cb(
                    args[0].CastOrDefault<T1>(),
                    args[1].CastOrDefault<T2>(), 
                    args[2].CastOrDefault<T3>(),
                    args[3].CastOrDefault<T4>()),
                typeof(T1), typeof(T2), typeof(T3), typeof(T4));
        }
        
        public void Add<T1, T2, T3, T4, T5>(string name, Func<T1, T2, T3, T4, T5, ExpressionVariant> cb)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct
        {
            Add(name, args => cb(
                    args[0].CastOrDefault<T1>(),
                    args[1].CastOrDefault<T2>(), 
                    args[2].CastOrDefault<T3>(),
                    args[3].CastOrDefault<T4>(),
                    args[4].CastOrDefault<T5>()),
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5));
        }
        
        public void Add<T1, T2, T3, T4, T5, T6>(string name, Func<T1, T2, T3, T4, T5, T6, ExpressionVariant> cb)
            where T1 : struct where T2 : struct where T3 : struct where T4 : struct where T5 : struct where T6 : struct
        {
            Add(name, args => cb(
                    args[0].CastOrDefault<T1>(),
                    args[1].CastOrDefault<T2>(), 
                    args[2].CastOrDefault<T3>(),
                    args[3].CastOrDefault<T4>(),
                    args[4].CastOrDefault<T5>(),
                    args[4].CastOrDefault<T6>()),
                typeof(T1), typeof(T2), typeof(T3), typeof(T4), typeof(T5), typeof(T6));
        }


        public void Add<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16>(string name,
            Func<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11, T12, T13, T14, T15, T16, ExpressionVariant> cb)
            where T1 : struct
            where T2 : struct
            where T3 : struct
            where T4 : struct
            where T5 : struct
            where T6 : struct
            where T7 : struct
            where T8 : struct
            where T9 : struct
            where T10 : struct
            where T11 : struct
            where T12 : struct
            where T13 : struct
            where T14 : struct
            where T15 : struct
            where T16 : struct
        {
            Add(name, args => cb(
                    args[0].CastOrDefault<T1>(),
                    args[1].CastOrDefault<T2>(),
                    args[2].CastOrDefault<T3>(),
                    args[3].CastOrDefault<T4>(),
                    args[4].CastOrDefault<T5>(),
                    args[4].CastOrDefault<T6>(),
                    args[4].CastOrDefault<T7>(),
                    args[4].CastOrDefault<T8>(),
                    args[4].CastOrDefault<T9>(),
                    args[4].CastOrDefault<T10>(),
                    args[4].CastOrDefault<T11>(),
                    args[4].CastOrDefault<T12>(),
                    args[4].CastOrDefault<T13>(),
                    args[4].CastOrDefault<T14>(),
                    args[4].CastOrDefault<T15>(),
                    args[4].CastOrDefault<T16>()
                ),
                typeof(T1), typeof(T2), typeof(T3), typeof(T4),
                typeof(T5), typeof(T6), typeof(T7), typeof(T8),
                typeof(T9), typeof(T10), typeof(T11), typeof(T12),
                typeof(T13), typeof(T14), typeof(T15), typeof(T16)
            );
        }
    }
}
