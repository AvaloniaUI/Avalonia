namespace Tmds.DBus.Protocol;

sealed class PathNode
{
    // _childNames is null when there are no child names
    // a string if there is a single child name
    // a List<string> List<string>.Count child names
    private object? _childNames;
    public IMethodHandler? MethodHandler;
    public PathNode? Parent { get; set; }

    public int ChildNameCount =>
        _childNames switch
        {
            null => 0,
            string => 1,
            var list => ((List<string>)list).Count
        };

    public void ClearChildNames()
    {
        Debug.Assert(ChildNameCount == 1, "Method isn't expected to be called unless there is 1 child name.");
        if (_childNames is List<string> list)
        {
            list.Clear();
        }
        else
        {
            _childNames = null;
        }
    }

    public void RemoveChildName(string name)
    {
        Debug.Assert(ChildNameCount > 1, "Caller is expected to call ClearChildNames instead.");
        var list = (List<string>)_childNames!;
        list.Remove(name);
    }

    public void AddChildName(string value)
    {
        if (_childNames is null)
        {
            _childNames = value;
        }
        else if (_childNames is string first)
        {
            _childNames = new List<string>() { first, value };
        }
        else
        {
            ((List<string>)_childNames).Add(value);
        }
    }

    public void CopyChildNamesTo(MethodContext methodContext)
    {
        Debug.Assert(methodContext.IntrospectChildNameList is null || methodContext.IntrospectChildNameList.Count == 0);

        if (_childNames is null)
        {
            return;
        }

        methodContext.IntrospectChildNameList ??= new();
        if (_childNames is string s)
        {
            methodContext.IntrospectChildNameList.Add(s);
        }
        else
        {
            methodContext.IntrospectChildNameList.AddRange((List<string>)_childNames);
        }
    }
}

sealed class PathNodeDictionary : Dictionary<string, PathNode>
{
    public void AddMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers)
    {
        if (methodHandlers is null)
        {
            throw new ArgumentNullException(nameof(methodHandlers));
        }

        int registeredCount = 0;
        try
        {
            for (int i = 0; i < methodHandlers.Count; i++)
            {
                IMethodHandler methodHandler = methodHandlers[i] ?? throw new ArgumentNullException("methodHandler");
                string path = methodHandler.Path ?? throw new ArgumentNullException(nameof(methodHandler.Path));

                // Validate the path starts with '/' and has no empty sections.
                // GetParentPath relies on this.
                if (path[0] != '/' || path.IndexOf("//", StringComparison.Ordinal) != -1)
                {
                    throw new FormatException($"The path '{path}' is not valid.");
                }

                PathNode node = GetOrCreateNode(path);

                if (node.MethodHandler is not null)
                {
                    throw new InvalidOperationException($"A method handler is already registered for the path '{path}'.");
                }
                node.MethodHandler = methodHandler;

                registeredCount++;
            }
        }
        catch
        {
            RemoveMethodHandlers(methodHandlers, registeredCount);

            throw;
        }
    }


    private PathNode GetOrCreateNode(string path)
    {
#if NET6_0_OR_GREATER
        ref PathNode? node = ref CollectionsMarshal.GetValueRefOrAddDefault(this, path, out bool exists);
        if (exists)
        {
            return node!;
        }
        PathNode newNode = new PathNode();
        node = newNode;
#else
        if (this.TryGetValue(path, out PathNode? node))
        {
            return node;
        }
        PathNode newNode = new PathNode();
        Add(path, newNode);
#endif
        string? parentPath = GetParentPath(path);
        if (parentPath is not null)
        {
            PathNode parent = GetOrCreateNode(parentPath);
            newNode.Parent = parent;
            parent.AddChildName(GetChildName(path));
        }

        return newNode;
    }

    private static string? GetParentPath(string path)
    {
        if (path.Length == 1)
        {
            return null;
        }

        int index = path.LastIndexOf('/');
        Debug.Assert(index != -1);

        // When index == 0, return '/'.
        index = Math.Max(index, 1);

        return path.Substring(0, index);
    }

    private static string GetChildName(string path)
    {
        int index = path.LastIndexOf('/');
        return path.Substring(index + 1);
    }

    private void RemoveMethodHandlers(IReadOnlyList<IMethodHandler> methodHandlers, int count)
    {
        // We start by (optimistically) removing all nodes (assuming they form a tree that is pruned).
        // If there are nodes that are still needed to serve as parent nodes, we'll add them back at the end.
        (string Path, PathNode Node)[] nodes = new (string, PathNode)[count];
        int j = 0;
        for (int i = 0; i < count; i++)
        {
            string path = methodHandlers[i].Path;
            if (this.Remove(path, out PathNode? node))
            {
                nodes[j++] = (path, node);
                node.MethodHandler = null;
            }
        }
        count = j; j = 0;
        
        // Reverse sort by path length to remove leaves before parents.
        Array.Sort(nodes, 0, count, RemoveKeyComparerInstance);
        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            if (node.Node.ChildNameCount == 0)
            {
                RemoveFromParent(node.Path, node.Node);
            }
            else
            {
                nodes[j++] = node;
            }
        }
        count = j; j = 0;

        // Add back the nodes that serve as parent nodes.
        for (int i = 0; i < count; i++)
        {
            var node = nodes[i];
            this[node.Path] = node.Node;
        }

        void RemoveFromParent(string path, PathNode node)
        {
            PathNode? parent = node.Parent;
            if (parent is null)
            {
                return;
            }
            Debug.Assert(parent.ChildNameCount >= 1, "node is expected to be a known child");
            if (parent.ChildNameCount == 1) // We're the only child.
            {
                if (parent.MethodHandler is not null)
                {
                    // Parent is still needed for the MethodHandler.
                    parent.ClearChildNames();
                }
                else
                {
// Suppress netstandard2.0 nullability warnings around NetstandardExtensions.Remove.
#if NETSTANDARD2_0
#pragma warning disable CS8620
#pragma warning disable CS8604
#endif

                    // Parent is no longer needed.
                    string parentPath = GetParentPath(path)!;
                    Debug.Assert(parentPath is not null);
                    this.Remove(parentPath, out PathNode? parentNode);
                    Debug.Assert(parentNode is not null);
                    RemoveFromParent(parentPath, parentNode);
#if NETSTANDARD2_0
#pragma warning restore CS8620
#pragma warning restore CS8604
#endif
                }
            }
            else
            {
                string childName = GetChildName(path);
                parent.RemoveChildName(childName);
            }
        }
    }

    private static readonly RemoveKeyComparer RemoveKeyComparerInstance = new();

    sealed class RemoveKeyComparer : IComparer<(string Path, PathNode Node)>
    {
        public int Compare((string Path, PathNode Node) x, (string Path, PathNode Node) y)
            => x.Path.Length - y.Path.Length;
    }
}