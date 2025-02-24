using System.Xml.Linq;
using Sandcastle.Core.PresentationStyle.Transformation;
using Sandcastle.Core.PresentationStyle.Transformation.Elements;

namespace Avalonia.Sandcastle.PresentationStyles.AvaloniaMarkdown;

public class LinkElement(string name) : Element(name)
{
    public override void Render(TopicTransformationCore transformation, XElement element)
    {
        transformation.CurrentElement.Add((object)$"[{element.Value}]({element.Attribute("href")})");
    }
}
