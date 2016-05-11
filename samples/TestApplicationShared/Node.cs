using Avalonia.Collections;

namespace TestApplication
{
    internal class Node
    {
        public Node()
        {
            Children = new AvaloniaList<Node>();
        }

        public string Name { get; set; }
        public AvaloniaList<Node> Children { get; set; }
    }

}
