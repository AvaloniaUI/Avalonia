using System;
using System.Collections.Generic;
using Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.Transformers;
using XamlX.Ast;
using XamlX.Emit;
using XamlX.IL;
using XamlX.TypeSystem;

namespace Avalonia.Markup.Xaml.XamlIl.CompilerExtensions.AstNodes;

/// <summary>
/// Emits a <c>StreamGeometry</c> built from path markup which was already parsed at compile time.
/// </summary>
internal class AvaloniaXamlIlStreamGeometryAstNode(
    IXamlLineInfo lineInfo,
    AvaloniaXamlIlWellKnownTypes types,
    IReadOnlyList<GeometryCommand> commands)
    : XamlAstNode(lineInfo), IXamlAstValueNode, IXamlAstILEmitableNode
{
    private readonly IXamlConstructor _streamGeometryCtor = types.StreamGeometry.GetConstructor();
    private readonly IXamlMethod _streamGeometryOpen = types.StreamGeometry.GetMethod("Open", types.StreamGeometryContext, false);
    private readonly IXamlMethod _streamGeometryContextSetFillRule = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("SetFillRule", types.XamlIlTypes.Void, types.FillRule));
    private readonly IXamlMethod _streamGeometryContextBeginFigure = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("BeginFigure", types.XamlIlTypes.Void, types.Point, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextLineTo = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("LineTo", types.XamlIlTypes.Void, types.Point, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextQuadraticBezierTo = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("QuadraticBezierTo", types.XamlIlTypes.Void, types.Point, types.Point, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextCubicBezierTo = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("CubicBezierTo", types.XamlIlTypes.Void, types.Point, types.Point, types.Point, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextArcTo = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("ArcTo", types.XamlIlTypes.Void, types.Point, types.Size, types.XamlIlTypes.Double, types.XamlIlTypes.Boolean, types.SweepDirection, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextEndFigure = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("EndFigure", types.XamlIlTypes.Void, types.XamlIlTypes.Boolean));
    private readonly IXamlMethod _streamGeometryContextDispose = types.StreamGeometryContext.GetMethod(new FindMethodMethodSignature("Dispose", types.XamlIlTypes.Void));

    public IXamlAstTypeReference Type { get; } = new XamlAstClrTypeReference(lineInfo, types.StreamGeometry, false);

    public XamlILNodeEmitResult Emit(XamlEmitContext<IXamlILEmitter, XamlILNodeEmitResult> context, IXamlILEmitter codeGen)
    {
        codeGen
            .Newobj(_streamGeometryCtor)
            .Dup()
            .EmitCall(_streamGeometryOpen);

        using (var pooledContext = codeGen.LocalsPool.GetLocal(types.StreamGeometryContext))
        {
            var contextLocal = pooledContext.Local;

            codeGen.Stloc(contextLocal);

            foreach (var command in commands)
            {
                codeGen.Ldloc(contextLocal);

                switch (command.Kind)
                {
                    case GeometryCommandKind.SetFillRule:
                        codeGen
                            .Ldc_I4((int)command.FillRule)
                            .EmitCall(_streamGeometryContextSetFillRule);
                        break;

                    case GeometryCommandKind.BeginFigure:
                        EmitPoint(codeGen, command.Point1);
                        codeGen
                            .Ldc_I4(command.IsFilled ? 1 : 0)
                            .EmitCall(_streamGeometryContextBeginFigure);
                        break;

                    case GeometryCommandKind.LineTo:
                        EmitPoint(codeGen, command.Point1);
                        codeGen
                            .Ldc_I4(command.IsStroked ? 1 : 0)
                            .EmitCall(_streamGeometryContextLineTo);
                        break;

                    case GeometryCommandKind.QuadraticBezierTo:
                        EmitPoint(codeGen, command.Point1);
                        EmitPoint(codeGen, command.Point2);
                        codeGen
                            .Ldc_I4(command.IsStroked ? 1 : 0)
                            .EmitCall(_streamGeometryContextQuadraticBezierTo);
                        break;

                    case GeometryCommandKind.CubicBezierTo:
                        EmitPoint(codeGen, command.Point1);
                        EmitPoint(codeGen, command.Point2);
                        EmitPoint(codeGen, command.Point3);
                        codeGen
                            .Ldc_I4(command.IsStroked ? 1 : 0)
                            .EmitCall(_streamGeometryContextCubicBezierTo);
                        break;

                    case GeometryCommandKind.ArcTo:
                        EmitPoint(codeGen, command.Point1);
                        codeGen
                            .Ldc_R8(command.Size.Width)
                            .Ldc_R8(command.Size.Height)
                            .Newobj(types.SizeFullConstructor)
                            .Ldc_R8(command.RotationAngle)
                            .Ldc_I4(command.IsLargeArc ? 1 : 0)
                            .Ldc_I4((int)command.SweepDirection)
                            .Ldc_I4(command.IsStroked ? 1 : 0)
                            .EmitCall(_streamGeometryContextArcTo);
                        break;

                    case GeometryCommandKind.EndFigure:
                        codeGen
                            .Ldc_I4(command.IsClosed ? 1 : 0)
                            .EmitCall(_streamGeometryContextEndFigure);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(command), $"Unsupported geometry command {command.Kind}");
                }
            }

            codeGen
                .Ldloc(contextLocal)
                .EmitCall(_streamGeometryContextDispose);
        }

        return XamlILNodeEmitResult.Type(0, types.StreamGeometry);
    }

    private void EmitPoint(IXamlILEmitter codeGen, Point point)
        => codeGen
            .Ldc_R8(point.X)
            .Ldc_R8(point.Y)
            .Newobj(types.PointFullConstructor);
}
