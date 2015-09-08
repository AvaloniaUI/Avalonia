





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
            this.chunks = path.Split('.');
        }

        public string[] Chunks
        {
            get { return this.chunks; }
            set { this.chunks = value; }
        }

        public PropertyPath Clone()
        {
            return new PropertyPath(this);
        }

        public override string ToString()
        {
            return string.Join(".", this.chunks);
        }
    }
}