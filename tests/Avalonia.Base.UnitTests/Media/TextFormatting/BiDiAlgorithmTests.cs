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
        
        [Fact(Skip = "Only run when the Unicode spec changes.")]
        public void Should_Process()
        {
            var generator = new BiDiTestDataGenerator();
            
            foreach(var testData in generator)
            {
                Assert.True(Run(testData));
            }
        }
        
        private bool Run(BiDiTestData testData)
        {
            var bidi = new BidiAlgorithm();
            
            // Run the algorithm...
            ArraySlice<sbyte> resultLevels;

            bidi.Process(
                testData.Classes,
                ArraySlice<BidiPairedBracketType>.Empty,
                ArraySlice<int>.Empty,
                testData.ParagraphEmbeddingLevel,
                false,
                null,
                null,
                null);

            resultLevels = bidi.ResolvedLevels;

            // Check the results match
            var pass = true;
            
            if (resultLevels.Length == testData.Levels.Length)
            {
                for (var i = 0; i < testData.Levels.Length; i++)
                {
                    if (testData.Levels[i] == -1)
                    {
                        continue;
                    }

                    if (resultLevels[i] != testData.Levels[i])
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
                _outputHelper.WriteLine($"Failed line {testData.LineNumber}");
                _outputHelper.WriteLine($"        Data: {string.Join(" ", testData.Classes)}");
                _outputHelper.WriteLine($" Embed Level: {testData.ParagraphEmbeddingLevel}");
                _outputHelper.WriteLine($"    Expected: {string.Join(" ", testData.Levels)}");
                _outputHelper.WriteLine($"      Actual: {string.Join(" ", resultLevels)}");

                return false;
            }

            return true;
        }
    }
}
