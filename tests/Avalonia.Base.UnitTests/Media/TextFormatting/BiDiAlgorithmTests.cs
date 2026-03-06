using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public class BiDiAlgorithmTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BiDiAlgorithmTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }

        [ClassData(typeof(BiDiTestDataGenerator))]
        [Theory(Skip = "Only run when the Unicode spec changes.")]
        public void Should_Process(int lineNumber, BidiClass[] classes, sbyte paragraphEmbeddingLevel, int[] levels)
        {
            var bidi = new BidiAlgorithm();

            // Run the algorithm...
            ArraySlice<sbyte> resultLevels;

            bidi.Process(
                classes,
                ArraySlice<BidiPairedBracketType>.Empty,
                ArraySlice<int>.Empty,
                paragraphEmbeddingLevel,
                false,
                null,
                null,
                null);

            resultLevels = bidi.ResolvedLevels;

            // Check the results match
            var pass = true;

            if (resultLevels.Length == levels.Length)
            {
                for (var i = 0; i < levels.Length; i++)
                {
                    if (levels[i] == -1)
                    {
                        continue;
                    }

                    if (resultLevels[i] != levels[i])
                    {
                        pass = false;
                        break;
                    }
                }
            }
            else
            {
                pass = false;
            }

            if (!pass)
            {
                _outputHelper.WriteLine($"Failed line {lineNumber}");
                _outputHelper.WriteLine($"        Data: {string.Join(" ", classes)}");
                _outputHelper.WriteLine($" Embed Level: {paragraphEmbeddingLevel}");
                _outputHelper.WriteLine($"    Expected: {string.Join(" ", levels)}");
                _outputHelper.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");
            }

            Assert.True(pass);
        }
    }
}
