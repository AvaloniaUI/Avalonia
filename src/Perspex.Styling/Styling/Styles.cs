





namespace Perspex.Styling
{
    using Perspex.Collections;

    public class Styles : PerspexList<IStyle>, IStyle
    {
        public void Attach(IStyleable control)
        {
            foreach (IStyle style in this)
            {
                style.Attach(control);
            }
        }
    }
}
