using Avalonia.Styling;

namespace Avalonia.Benchmarks
{
    public class TestStyles : Styles
    {
        public TestStyles(int childStylesCount, int childInnerStyleCount, int childResourceCount)
        {
            for (int i = 0; i < childStylesCount; i++)
            {
                var childStyles = new Styles();

                for (int j = 0; j < childInnerStyleCount; j++)
                {
                    var childStyle = new Style();

                    for (int k = 0; k < childResourceCount; k++)
                    {
                        childStyle.Resources.Add($"resource.{i}.{j}.{k}", null);
                    }
                    
                    childStyles.Add(childStyle);
                }
                
                Add(childStyles);
            }
        }
    }
}
