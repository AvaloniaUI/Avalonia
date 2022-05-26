// ReSharper disable InconsistentNaming
// ReSharper disable CheckNamespace

using System;
using System.Collections.Generic;
using System.Numerics;
using Avalonia.Rendering.Composition.Expressions;
using Avalonia.Rendering.Composition.Server;
using Avalonia.Rendering.Composition.Transport;

namespace Avalonia.Rendering.Composition.Animations
{
      public abstract class CompositionAnimation : CompositionObject,  ICompositionAnimationBase
      {
          private readonly CompositionPropertySet _propertySet;
          internal CompositionAnimation(Compositor compositor) : base(compositor, null!)
          {
              _propertySet = new CompositionPropertySet(compositor);
          }

          public void ClearAllParameters() => _propertySet.ClearAll();

          public void ClearParameter(string key) => _propertySet.Clear(key);

          void SetVariant(string key, ExpressionVariant value) => _propertySet.Set(key, value);
          
          public void SetColorParameter(string key, Avalonia.Media.Color value) => SetVariant(key, value);

          public void SetMatrix3x2Parameter(string key, Matrix3x2 value) => SetVariant(key, value);

          public void SetMatrix4x4Parameter(string key, Matrix4x4 value) => SetVariant(key, value);

          public void SetQuaternionParameter(string key, Quaternion value) => SetVariant(key, value);

          public void SetReferenceParameter(string key, CompositionObject compositionObject) =>
              _propertySet.Set(key, compositionObject);

          public void SetScalarParameter(string key, float value) => SetVariant(key, value);

          public void SetVector2Parameter(string key, Vector2 value) => SetVariant(key, value);

          public void SetVector3Parameter(string key, Vector3 value) => SetVariant(key, value);

          public void SetVector4Parameter(string key, Vector4 value) => SetVariant(key, value);
          
          // TODO: void SetExpressionReferenceParameter(string parameterName, IAnimationObject source)

          public string? Target { get; set; }

          internal abstract IAnimationInstance CreateInstance(ServerObject targetObject,
              ExpressionVariant? finalValue);

          internal PropertySetSnapshot CreateSnapshot() => _propertySet.Snapshot();

          void ICompositionAnimationBase.InternalOnly()
          {
              
          }
      }
}