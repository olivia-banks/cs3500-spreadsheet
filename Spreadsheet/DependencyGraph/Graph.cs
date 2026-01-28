namespace DependencyGraph;

/// <summary>
///     <para>
///         A simple directed graph implementation using dictionaries and hash sets.
///         Nodes are identified by unique keys, and edges are stored as sets of dependents
///         and dependees. This structure is optimized for small-sized graphs, which is
///         likely what our spreadsheeting application will use.
///     </para>
///     <remarks>
///         In the future, we may want to migrate to something like adjacency lists or
///         matrices if we find that our graphs are growing significantly in size and want
///         memory locality. Plus, this implementation is extremely prone to GC pressure
///         due to the large number of small objects created (Node records, HashSets, etc).
///     </remarks>
/// </summary>
/// <typeparam name="TKey">The type of the key used to identify nodes. Must be non-null.</typeparam>
/// <typeparam name="TValue">The type of value stored in each node.</typeparam>
public sealed class Graph<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    ///     <para>
    ///         Represents a node in the graph, storing a key, a value, and its relationships.
    ///     </para>
    /// </summary>
    /// <param name="Key">The key identifying this node.</param>
    /// <param name="Value">The value stored in this node.</param>
    public record Node(TKey Key, TValue Value)
    {
        /// <summary>
        ///     <para>Nodes that depend on this node (outgoing edges).</para>
        /// </summary>
        internal readonly HashSet<TKey> Dependents = [];

        /// <summary>
        ///     <para>Nodes that this node depends on (incoming edges).</para>
        /// </summary>
        internal readonly HashSet<TKey> Dependees = [];
    }

    /// <summary>
    ///     <para>Mapping from node keys to Node objects in the graph.</para>
    /// </summary>
    private readonly Dictionary<TKey, Node> _nodes = new();

    /// <summary>
    ///     <para>Adds a new node with the given key and value, or overwrites the value if the key exists.</para>
    /// </summary>
    /// <param name="key">The key of the node to add.</param>
    /// <param name="value">The value to associate with the node.</param>
    public void AddNode(TKey key, TValue value)
    {
        var node = new Node(key, value);
        _nodes[key] = node;
    }

    /// <summary>
    ///     <para>Removes the node with the given key, along with all its associated edges.</para>
    /// </summary>
    /// <param name="key">The key of the node to remove.</param>
    public void RemoveNode(TKey key)
    {
        if (!_nodes.TryGetValue(key, out var node))
            return;

        foreach (var dependentKey in node.Dependents)
        {
            if (_nodes.TryGetValue(dependentKey, out var depNode))
                depNode.Dependees.Remove(key);
        }

        foreach (var dependeeKey in node.Dependees)
        {
            if (_nodes.TryGetValue(dependeeKey, out var depNode))
                depNode.Dependents.Remove(key);
        }

        _nodes.Remove(key);
    }

    /// <summary>
    ///     <para>Adds a directed edge from 'fromKey' to 'toKey'.</para>
    /// </summary>
    /// <param name="fromKey">The key of the source node.</param>
    /// <param name="toKey">The key of the target node.</param>
    /// <exception cref="InvalidOperationException">Thrown if either node does not exist.</exception>
    public void AddEdge(TKey fromKey, TKey toKey)
    {
        if (!_nodes.TryGetValue(fromKey, out Node? value))
            throw new InvalidOperationException("Source node does not exist.");

        if (!_nodes.TryGetValue(toKey, out Node? value1))
            throw new InvalidOperationException("Target node does not exist.");

        value.Dependents.Add(toKey);
        value1.Dependees.Add(fromKey);
    }

    /// <summary>
    ///     <para>Removes a directed edge from 'fromKey' to 'toKey'. Does nothing if the edge does not exist.</para>
    /// </summary>
    /// <param name="fromKey">The key of the source node.</param>
    /// <param name="toKey">The key of the target node.</param>
    public void RemoveEdge(TKey fromKey, TKey toKey)
    {
        if (_nodes.TryGetValue(fromKey, out var fromNode))
            fromNode.Dependents.Remove(toKey);

        if (_nodes.TryGetValue(toKey, out var toNode))
            toNode.Dependees.Remove(fromKey);
    }

    /// <summary>
    ///     <para>Enumerates all dependents of the node with the given key.</para>
    /// </summary>
    /// <param name="key">The key of the node.</param>
    /// <returns>An enumeration of Node objects that depend on the specified node.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependents(TKey key)
    {
        if (!_nodes.TryGetValue(key, out var node))
            throw new InvalidOperationException("Node does not exist.");

        foreach (var dependentKey in node.Dependents)
            yield return _nodes[dependentKey];
    }

    /// <summary>
    ///     <para>Enumerates all dependees of the node with the given key.</para>
    /// </summary>
    /// <param name="key">The key of the node.</param>
    /// <returns>An enumeration of Node objects that the specified node depends on.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependees(TKey key)
    {
        if (!_nodes.TryGetValue(key, out var node))
            throw new InvalidOperationException("Node does not exist.");

        foreach (var dependeeKey in node.Dependees)
            yield return _nodes[dependeeKey];
    }

    /// <summary>
    ///     <para>Enumerates all nodes in the graph.</para>
    /// </summary>
    /// <returns>An enumeration of all Node objects in the graph.</returns>
    public IEnumerable<Node> EnumerateNodes() => _nodes.Values;

    /// <summary>
    ///     <para>Enumerates all edges as (fromNode, toNode) pairs.</para>
    /// </summary>
    /// <returns>An enumeration of edges represented by source and target Node objects.</returns>
    public IEnumerable<(Node, Node)> EnumerateEdges()
        => from node in _nodes.Values
            from dependentKey in node.Dependents
            select (node, _nodes[dependentKey]);

    /// <summary>
    ///     <para>Returns whether the node has dependents (outgoing edges).</para>
    /// </summary>
    public bool NodeHasDependents(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Dependents.Count > 0;

    /// <summary>
    ///     <para>Returns whether the node has dependees (incoming edges).</para>
    /// </summary>
    public bool NodeHasDependees(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Dependees.Count > 0;

    /// <summary>
    ///     <para>Returns whether the node has any edges (incoming or outgoing).</para>
    /// </summary>
    public bool NodeHasAnyEdges(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Dependents.Count > 0 || node.Dependees.Count > 0;

    /// <summary>
    ///     <para>Gets the value stored in the node with the given key.</para>
    /// </summary>
    public TValue GetValue(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Value;

    /// <summary>
    ///     <para>Gets the number of dependents (outgoing edges) for the node with the given key.</para>
    /// </summary>
    public int GetOutgoingCount(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Dependents.Count;

    /// <summary>
    ///     <para>Gets the number of dependees (incoming edges) for the node with the given key.</para>
    /// </summary>
    public int GetIncomingCount(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node.Dependees.Count;

    /// <summary>
    ///     <para>Returns the total number of nodes in the graph.</para>
    /// </summary>
    public int NodeCount => _nodes.Count;

    /// <summary>
    ///     <para>Returns the Node object for the given key.</para>
    /// </summary>
    public Node GetNode(TKey key)
        => !_nodes.TryGetValue(key, out var node)
            ? throw new InvalidOperationException("Node does not exist.")
            : node;

    /// <summary>
    ///     <para>Creates a new node with the given key and value if it does not already exist.</para>
    ///     <para>Returns true if the node was created, false if it already existed.</para>
    /// </summary>
    /// <param name="key">The key to insert into the maybe-new node.</param>
    /// <param name="value">The value to insert into the maybe-new node.</param>
    /// <returns><c>true</c> if a new node was created, <c>false</c> otherwise.</returns>
    public bool CreateNodeIfNotExists(TKey key, TValue value)
    {
        if (_nodes.ContainsKey(key))
        {
            return false;
        }

        var node = new Node(key, value);
        _nodes[key] = node;
        return true;
    }
}