namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    public class PropertyPath
    {
        private string[] chunks;

        private PropertyPath(PropertyPath propertyPath)
        {
            this.chunks = propertyPath.Chunks;
        }

        public PropertyPath(string path)
        {        
            chunks = path.Split('.');
        }

        public string[] Chunks
        {
            get { return chunks; }
            set { chunks = value; }
        }

        public PropertyPath Clone()
        {
            return new PropertyPath(this);
        }
    }
}