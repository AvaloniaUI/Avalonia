using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Avalonia.Media.Fonts.Tables.Colr
{
    internal record class ResolvedPaint : Paint;

    internal record class ResolvedSolid(Color Color) : ResolvedPaint;
    
    internal record class ResolvedLinearGradient(
        Point P0, 
        Point P1, 
        GradientStop[] Stops, 
        GradientSpreadMethod Extend) : ResolvedPaint;
    
    internal record class ResolvedRadialGradient(
        Point C0, 
        double R0, 
        Point C1, 
        double R1, 
        GradientStop[] Stops, 
        GradientSpreadMethod Extend) : ResolvedPaint;
    
    internal record class ResolvedConicGradient(
        Point Center, 
        double StartAngle, 
        double EndAngle, 
        GradientStop[] Stops, 
        GradientSpreadMethod Extend) : ResolvedPaint;
    
    internal record class ResolvedTransform(Matrix Matrix, Paint Inner) : ResolvedPaint;

    internal record class ResolvedClipBox(Rect Box, Paint Inner) : ResolvedPaint;
}
