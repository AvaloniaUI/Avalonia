using Perspex.Collections;

namespace TestApplication
{
    internal class Node
    {
        public Node()
        {
            Children = new PerspexList<Node>();
        }

        public string Name { get; set; }
        public PerspexList<Node> Children { get; set; }
    }

}
