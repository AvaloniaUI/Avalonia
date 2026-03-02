namespace ControlCatalog.Pages
{
    public class FluidNavItem
    {
        public string SvgPath { get; }
        public string Label { get; }

        public FluidNavItem(string svgPath, string label)
        {
            SvgPath = svgPath;
            Label = label;
        }
    }
}
