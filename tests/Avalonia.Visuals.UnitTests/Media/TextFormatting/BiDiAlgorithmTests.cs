using Avalonia.Media.TextFormatting.Unicode;
using Avalonia.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace Avalonia.Visuals.UnitTests.Media.TextFormatting
{
    public class BiDiAlgorithmTests
    {
        private readonly ITestOutputHelper _outputHelper;

        public BiDiAlgorithmTests(ITestOutputHelper outputHelper)
        {
            _outputHelper = outputHelper;
        }
        
        [Fact]
        public void Should_Process()
        {
            var generator = new BiDiTestDataGenerator();
            
            foreach(var (lineNumber, classes, paragraphEmbeddingLevel, expectedLevels) in generator)
            {
                Assert.True(Run(lineNumber, classes, paragraphEmbeddingLevel, expectedLevels));
            }
        }
        
        public bool Run(int lineNumber, BiDiClass[] classes, sbyte paragraphEmbeddingLevel, int[] expectedLevels)
        {
            var bidi = BiDiAlgorithm.Instance.Value;
            
            // Run the algorithm...
            ArraySlice<sbyte> resultLevels;

            bidi.Process(
                classes,
                ArraySlice<BiDiPairedBracketType>.Empty,
                ArraySlice<int>.Empty,
                paragraphEmbeddingLevel,
                false,
                null,
                null,
                null);

            resultLevels = bidi.ResolvedLevels;

            // Check the results match
            var pass = true;
            
            if (resultLevels.Length == expectedLevels.Length)
            {
                for (var i = 0; i < expectedLevels.Length; i++)
                {
                    if (expectedLevels[i] == -1)
                    {
                        continue;
                    }

                    if (resultLevels[i] != expectedLevels[i])
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
                _outputHelper.WriteLine($"    Expected: {string.Join(" ", expectedLevels)}");
                _outputHelper.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");

                return false;
            }

            return true;
        }
    }
}
