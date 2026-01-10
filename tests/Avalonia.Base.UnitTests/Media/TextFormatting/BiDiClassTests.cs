using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Avalonia.Media.TextFormatting.Unicode;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public class BiDiClassTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BiDiClassTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [Theory(Skip = "Only run when the Unicode spec changes.")]
        [ClassData(typeof(BiDiClassTestDataGenerator))]
        [SuppressMessage("Usage", "xUnit1026:Theory methods should use all of their parameters", Justification = "Parameters match BiDi fields")]
        public void Should_Resolve(
            int lineNumber,
            int[] codePoints,
            sbyte paragraphLevel,
            sbyte resolvedParagraphLevel,
            sbyte[] resolvedLevels,
            int[] resolvedOrder)
        {

            var bidi = new BidiAlgorithm();
            var bidiData = new BidiData { ParagraphEmbeddingLevel = paragraphLevel };

            var text = Encoding.UTF32.GetString(MemoryMarshal.Cast<int, byte>(codePoints).ToArray());

            // Append
            bidiData.Append(text);

            // Act
            for (var i = 0; i < 10; i++)
            {
                bidi.Process(bidiData);
            }

            var resultLevels = bidi.ResolvedLevels;
            var resultParagraphLevel = bidi.ResolvedParagraphEmbeddingLevel;

            Assert.Equal(resolvedParagraphLevel, resultParagraphLevel);

            for (var i = 0; i < resolvedLevels.Length; i++)
            {
                if (resolvedLevels[i] == -1)
                {
                    continue;
                }

                var expectedLevel = resolvedLevels[i];
                var actualLevel = resultLevels[i];

                Assert.Equal(expectedLevel, actualLevel);
            }
        }
    }
}
