using System;

namespace Avalonia.Rendering.Composition
{
    public enum CompositionBlendMode
    {
        /// <summary>No regions are enabled. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_clr.svg)</summary>
        Clear,

        /// <summary>Only the source will be present. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_src.svg)</summary>
        Src,

        /// <summary>Only the destination will be present. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_dst.svg)</summary>
        Dst,

        /// <summary>Source is placed over the destination. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_src-over.svg)</summary>
        SrcOver,

        /// <summary>Destination is placed over the source. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_dst-over.svg)</summary>
        DstOver,

        /// <summary>The source that overlaps the destination, replaces the destination. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_src-in.svg)</summary>
        SrcIn,

        /// <summary>Destination which overlaps the source, replaces the source. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_dst-in.svg)</summary>
        DstIn,

        /// <summary>Source is placed, where it falls outside of the destination. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_src-out.svg)</summary>
        SrcOut,

        /// <summary>Destination is placed, where it falls outside of the source. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_dst-out.svg)</summary>
        DstOut,

        /// <summary>Source which overlaps the destination, replaces the destination. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_src-atop.svg)</summary>
        SrcATop,

        /// <summary>Destination which overlaps the source replaces the source. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_dst-atop.svg)</summary>
        DstATop,

        /// <summary>The non-overlapping regions of source and destination are combined. [Porter Duff Compositing Operators] (https://drafts.fxtf.org/compositing-1/examples/PD_xor.svg)</summary>
        Xor,

        /// <summary>Display the sum of the source image and destination image. [Porter Duff Compositing Operators]</summary>
        Plus,

        /// <summary>Multiplies all components (= alpha and color). [Separable Blend Modes]</summary>
        Modulate,

        /// <summary>Multiplies the complements of the backdrop and source CompositionColorvalues, then complements the result. [Separable Blend Modes]</summary>
        Screen,

        /// <summary>Multiplies or screens the colors, depending on the backdrop CompositionColorvalue. [Separable Blend Modes]</summary>
        Overlay,

        /// <summary>Selects the darker of the backdrop and source colors. [Separable Blend Modes]</summary>
        Darken,

        /// <summary>Selects the lighter of the backdrop and source colors. [Separable Blend Modes]</summary>
        Lighten,

        /// <summary>Brightens the backdrop CompositionColorto reflect the source color. [Separable Blend Modes]</summary>
        ColorDodge,

        /// <summary>Darkens the backdrop CompositionColorto reflect the source color. [Separable Blend Modes]</summary>
        ColorBurn,

        /// <summary>Multiplies or screens the colors, depending on the source CompositionColorvalue. [Separable Blend Modes]</summary>
        HardLight,

        /// <summary>Darkens or lightens the colors, depending on the source CompositionColorvalue. [Separable Blend Modes]</summary>
        SoftLight,

        /// <summary>Subtracts the darker of the two constituent colors from the lighter color. [Separable Blend Modes]</summary>
        Difference,

        /// <summary>Produces an effect similar to that of the Difference mode but lower in contrast. [Separable Blend Modes]</summary>
        Exclusion,

        /// <summary>The source CompositionColoris multiplied by the destination CompositionColorand replaces the destination [Separable Blend Modes]</summary>
        Multiply,

        /// <summary>Creates a CompositionColorwith the hue of the source CompositionColorand the saturation and luminosity of the backdrop color. [Non-Separable Blend Modes]</summary>
        Hue,

        /// <summary>Creates a CompositionColorwith the saturation of the source CompositionColorand the hue and luminosity of the backdrop color. [Non-Separable Blend Modes]</summary>
        Saturation,

        /// <summary>Creates a CompositionColorwith the hue and saturation of the source CompositionColorand the luminosity of the backdrop color. [Non-Separable Blend Modes]</summary>
        Color,

        /// <summary>Creates a CompositionColorwith the luminosity of the source CompositionColorand the hue and saturation of the backdrop color. [Non-Separable Blend Modes]</summary>
        Luminosity,
    }

    public enum CompositionGradientExtendMode
    {
        Clamp,
        Wrap,
        Mirror
    }
    
    [Flags]
    public enum CompositionTileMode
    {
        None = 0,
        TileX = 1,
        TileY = 2,
        FlipX = 4,
        FlipY = 8,
        Tile = TileX | TileY,
        Flip = FlipX | FlipY
    }

    public enum CompositionStretch
    {
        None = 0,
        Fill = 1,
        //TODO: Uniform, UniformToFill
    }
}
