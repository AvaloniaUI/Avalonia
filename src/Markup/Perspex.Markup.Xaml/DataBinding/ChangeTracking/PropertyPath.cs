// Copyright (c) The Perspex Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

namespace Perspex.Markup.Xaml.DataBinding.ChangeTracking
{
    public class PropertyPath
    {
        private string[] _chunks;

        private PropertyPath(PropertyPath propertyPath)
        {
            _chunks = propertyPath.Chunks;
        }

        public PropertyPath(string path)
        {
            _chunks = path.Split('.');
        }

        public string[] Chunks
        {
            get { return _chunks; }
            set { _chunks = value; }
        }

        public PropertyPath Clone()
        {
            return new PropertyPath(this);
        }

        public override string ToString()
        {
            return string.Join(".", _chunks);
        }
    }
}