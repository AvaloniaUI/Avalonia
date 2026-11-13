using System;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Fonts.Tables;
using Avalonia.Media.Fonts.Tables.OpenTypeMath;
using Avalonia.Platform;
using Avalonia.UnitTests;
using BenchmarkDotNet.Attributes;

namespace Avalonia.Benchmarks.Media
{
    /// <summary>
    /// MATH table access as a math layout engine uses it: one table per typeface, then constants,
    /// per-glyph lookups and glyph variants for every formula element.
    /// </summary>
    /// <remarks>
    /// <list type="bullet">
    /// <item><c>Load</c>: parsing the STIX Two Math subset's table and resolving its constants, the
    /// cost of the first <see cref="GlyphTypeface.MathTable"/> access.</item>
    /// <item><c>Load_Varied</c>: the same for a variation instance: a table whose 51 value-record
    /// constants all carry VariationIndex records into InterVariable's GDEF store, at normalized
    /// wght 0.6.</item>
    /// <item>The lookups run on STIX at the default instance; the <c>_Varied</c> lookup resolves a
    /// variation delta per call on a wght=700 instance.</item>
    /// </list>
    /// </remarks>
    [MemoryDiagnoser]
    public class MathTableBenchmark : IDisposable
    {
        private const string StixAsset =
            "resm:Avalonia.Benchmarks.Assets.STIXTwoMath-Subset.otf?assembly=Avalonia.Benchmarks";

        private const string InterVariableAsset =
            "resm:Avalonia.Benchmarks.Assets.InterVariable.ttf?assembly=Avalonia.Benchmarks";

        private const ushort Integral = 116;
        private const ushort LetterA = 1;
        private const ushort ParenLeft = 9;
        private const ushort VariedGlyph = 10;

        private readonly IDisposable _app;

        private ReadOnlyMemory<byte> _stixMath;
        private ReadOnlyMemory<byte> _variedMath;
        private Avalonia.Media.Fonts.Tables.Variation.ItemVariationStore _store = null!;
        private float[] _variedCoords = null!;
        private OpenTypeMathTable _stix = null!;
        private OpenTypeMathTable _varied = null!;

        public MathTableBenchmark()
        {
            _app = UnitTestApplication.Start(TestServices.StyledWindow);
        }

        [GlobalSetup]
        public void GlobalSetup()
        {
            var stix = Load(StixAsset).CreateGlyphTypeface();
            _stix = stix.MathTable!;
            Assert(stix.PlatformTypeface.TryGetTable(MathTable.Tag, out _stixMath));

            _variedMath = BuildVariedMathTable();
            var inter = Load(InterVariableAsset).Replace("MATH", _variedMath.ToArray()).CreateGlyphTypeface();
            var bold = inter.WithVariations(FontVariationSettings.Parse("wght=700"));
            _varied = bold.MathTable!;

            Assert(GdefTable.TryLoad(inter, inter.VariationAxes.Count, out var gdef));
            _store = gdef!.VariationStore!;
            _variedCoords = new[] { 0f, 0.6f };
        }

        [Benchmark(Baseline = true)]
        public OpenTypeMathTable Load()
            => MathTable.TryLoad(_stixMath, out var data) ? new OpenTypeMathTable(data, null, null) : null;

        [Benchmark]
        public OpenTypeMathTable Load_Varied()
        {
            if (!MathTable.TryLoad(_variedMath, out var data))
            {
                return null;
            }

            var scalers = new float[_store.RegionCount];
            _store.ComputeRegionScalers(_variedCoords, scalers);

            return new OpenTypeMathTable(data, _store, scalers);
        }

        [Benchmark]
        public int GetConstant() => _stix.GetConstant(MathConstant.FractionRuleThickness);

        [Benchmark]
        public int GetItalicsCorrection() => _stix.GetItalicsCorrection(Integral);

        [Benchmark]
        public int GetItalicsCorrection_Varied() => _varied.GetItalicsCorrection(VariedGlyph);

        [Benchmark]
        public int GetKerning() => _stix.GetKerning(LetterA, MathKernCorner.TopRight, 300);

        [Benchmark]
        public int GetGlyphVariants() => _stix.GetGlyphVariants(ParenLeft, MathDirection.Vertical).Length;

        [Benchmark]
        public int TryGetGlyphAssembly()
            => _stix.TryGetGlyphAssembly(ParenLeft, MathDirection.Vertical, out var assembly) ? assembly.Parts.Length : 0;

        public void Dispose() => _app.Dispose();

        private static SyntheticFont Load(string asset)
        {
            using var stream = new StandardAssetLoader().Open(new Uri(asset));

            return SyntheticFont.FromStream(stream);
        }

        private static void Assert(bool condition)
        {
            if (!condition)
            {
                throw new InvalidOperationException("Benchmark setup failed.");
            }
        }

        /// <summary>
        /// A MATH table with all 51 value-record constants and one italics correction varied through
        /// GDEF delta set (0, 0), which InterVariable varies along wght.
        /// </summary>
        private static byte[] BuildVariedMathTable()
        {
            const int constantsOffset = 10;
            const int constantsSize = 8 + 51 * 4 + 2;
            const int deviceInConstants = constantsSize;
            const int glyphInfoOffset = constantsOffset + constantsSize + 6;

            var buffer = new BigEndianBuffer()
                .UInt16(1).UInt16(0).UInt16(constantsOffset).UInt16(glyphInfoOffset).UInt16(0);

            buffer.Int16(70).Int16(50).UInt16(1300).UInt16(1300);

            for (var i = 0; i < 51; i++)
            {
                buffer.Int16(100 + i).UInt16(deviceInConstants);
            }

            buffer.Int16(60);

            // VariationIndex (0, 0), delta format 0x8000.
            buffer.UInt16(0).UInt16(0).UInt16(0x8000);

            // MathGlyphInfo: an italics correction table at +8, nothing else.
            buffer.UInt16(8).UInt16(0).UInt16(0).UInt16(0);

            // MathItalicsCorrectionInfo: coverage at +8, one record varied by the device at +14.
            buffer.UInt16(8).UInt16(1).Int16(30).UInt16(14);
            buffer.UInt16(1).UInt16(1).UInt16(VariedGlyph);
            buffer.UInt16(0).UInt16(0).UInt16(0x8000);

            return buffer.ToArray();
        }
    }
}
