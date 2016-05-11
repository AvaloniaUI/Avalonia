using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

internal class CategoryAttribute : Attribute
{
    public CategoryAttribute(string s)
    {
        
    }
}
internal class DescriptionAttribute : Attribute
{
    public DescriptionAttribute(string s)
    {

    }
}

internal class BrowsableAttribute : Attribute
{
    public BrowsableAttribute(bool b)
    {

    }
}