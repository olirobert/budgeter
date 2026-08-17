using System.Text;

namespace BudgetCalculator
{
    internal class HtmlSpaceRemover
    {
        public static string RemoveSpaces(string html)
        {
            List<TextNode> nodes = new List<TextNode>();

            // start at html tag
            int start = html.IndexOf("<html");
            nodes.Add(new TextNode { NodeType = NodeType.Tag, Text = html.Substring(0, start) });

            NodeType currentNode = NodeType.None;
            int startIndex = 0;
            bool keepingSpaces = false;

            for (int i = start; i < html.Length; i++)
            {
                char c = html[i];

                if (currentNode == NodeType.None)
                {
                    startIndex = i;

                    if (c == '<')
                    {
                        currentNode = NodeType.Tag;
                        keepingSpaces = false;
                    }
                    else if ((c == ' ' || c == '\r' || c == '\n') && !keepingSpaces)
                    {
                        currentNode = NodeType.Space;
                    }
                    else
                    {
                        currentNode = NodeType.Text;
                    }

                    continue;
                }

                if (currentNode == NodeType.Tag)
                {
                    if (c == '>')
                    {
                        string tagContent = html.Substring(startIndex, i - startIndex + 1);
                        nodes.Add(new TextNode { NodeType = NodeType.Tag, Text = tagContent });
                        currentNode = NodeType.None;

                        if (tagContent.Contains("<style type=\"text/css\">"))
                        {
                            keepingSpaces = true;
                        }
                    }
                }
                else if (currentNode == NodeType.Space)
                {
                    if (!(c == ' ' || c == '\t' || c == '\n'))
                    {
                        nodes.Add(new TextNode { NodeType = NodeType.Space, Text = html.Substring(startIndex, i - startIndex) });

                        currentNode = NodeType.None;
                        i--; // re-evaluate this character in the next iteration
                    }
                }
                else
                {
                    if (c == '<' || ((c == ' ' || c == '\r' || c == '\n') && !keepingSpaces) )
                    {
                        nodes.Add(new TextNode { NodeType = NodeType.Text, Text = html.Substring(startIndex, i - startIndex) });

                        currentNode = NodeType.None;
                        i--; // re-evaluate this character in the next iteration
                    }
                }
            }

            for (int i = 1; i < nodes.Count - 1; i++)
            {
                if (nodes[i - 1].NodeType == NodeType.Text && nodes[i + 1].NodeType == NodeType.Text)
                {
                    nodes[i - 1].Text = nodes[i - 1].Text + nodes[i].Text + nodes[i + 1].Text;

                    nodes.RemoveRange(i, 2);
                    i--;
                }
            }

            StringBuilder sb = new StringBuilder();
            foreach (var node in nodes)
            {
                if (node.NodeType == NodeType.Text || node.NodeType == NodeType.Tag)
                {
                    sb.Append(node.Text);
                }
            }

            return sb.ToString();
        }

        private enum NodeType
        {
            None,
            Text,
            Tag,
            Space,
        }

        private class TextNode
        {
            public NodeType NodeType { get; set; }
            public string Text { get; set; } = "";

#if DEBUG
            public override string ToString()
            {
                return Text;
            }
#endif
        }
    }
}
