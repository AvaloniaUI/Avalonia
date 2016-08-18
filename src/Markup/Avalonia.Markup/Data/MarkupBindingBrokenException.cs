using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Data;

namespace Avalonia.Markup.Data
{
    public class MarkupBindingBrokenException : BindingBrokenException
    {
        private string _message;

        public MarkupBindingBrokenException()
        {
        }

        public MarkupBindingBrokenException(string message)
        {
            _message = message;
        }

        internal MarkupBindingBrokenException(ExpressionNode node)
        {
            Nodes.Add(node.Description);
        }

        public override string Message
        {
            get
            {
                if (_message != null)
                {
                    return _message;
                }
                else
                {
                    return _message = BuildMessage();
                }
            }
        }

        internal string Expression { get; set; }
        internal IList<string> Nodes { get; } = new List<string>();

        private string BuildMessage()
        {
            if (Nodes.Count == 0)
            {
                return "The binding chain was broken.";
            }
            else if (Nodes.Count == 1)
            {
                return $"'{Nodes[0]}' is null in expression '{Expression}'.";
            }
            else
            {
                var brokenPath = string.Join(".", Nodes.Skip(1).Reverse())
                    .Replace(".!", "!")
                    .Replace(".[", "[");
                return $"'{brokenPath}' is null in expression '{Expression}'.";
            }
        }
    }
}
