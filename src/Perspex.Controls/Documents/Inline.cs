namespace Perspex.Controls.Documents
{
    /// <summary>
    /// An abstract class that provides a base for all inline content.
    /// </summary>
    public abstract class Inline : PerspexObject
    {
        internal new PerspexObject InheritanceParent
        {
            get
            {
                return base.InheritanceParent;
            }
            set
            {
                base.InheritanceParent = value;
            }
        }
    }
}
