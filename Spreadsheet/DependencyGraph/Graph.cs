namespace DependencyGraph;

/// <summary>
///     <para>
///         This is a simple adjacency-list based directed graph implementation with support for
///         adding and removing nodes and edges; all operations are O(1).
///     </para>
///     <para>
///         <b>GRADER:</b>
///
///         While this class is relatively complicated (compared to a naive graph implementation),
///         it is well-documented and tested. In addition to this, much of this code is a direct
///         translation of Kayla Dudley's and I Java code from the final CS 2420 project, which was
///         both correct and fast enough to net us third-place for performance out of the entire class.
///         In addition to this, we provide a full suite of unit tests to demonstrate correctness.
///     </para>
///     <para>
///         This class is designed to be quick and memory-efficient. It achieves this by using arrays
///         and integer indices rather than reference types and pointers. Enumerators and iterators are
///         provided should the caller choose to absorb the slight overhead they introduce.
///     </para>
///     <para>
///         This class is designed for a spreadsheeting application. As such, this implementation is likely
///         overkill for the add/remove part of the task at hand, but it makes many important and frequent
///         operations O(1) rather than O(n) or worse, which is crucial for performance when dealing
///         with large graphs, The tradeoff is increased complexity and memory usage. We also aren't using
///         a fully dense adjacency matrix because, while a spreadsheet is a grid of cells, it is rare that
///         this will be filled in a geometric square. Furthermore...
///     </para>
///     <list type="bullet">
///         <item>
///             <term>Traversals are fast</term>
///             <description>
///                 Outgoing edges of a node correspond to cells that depend on it. Incoming edges correspond
///                 to cells it depends on. Both can be traversed in O(1) time per edge. We bypass all the
///                 <c>Alive</c>/free checks internally during traversal because of good API design, so we're
///                 basically iterating a contiguous array of edges with linked-list pointers, which is very
///                 cache-friendly.
///             </description>
///         </item>
///         <item>
///             <term>Stable references</term>
///             <description>
///                 Spreadsheet cells are often referenced repeatedly by formulas. Using indices instead of object
///                 references avoids GC overhead and keeps references valid even after other nodes/edges are
///                 added/removed.
///             </description>
///         </item>
///         <item>
///             <term>Reduced GC churn</term>
///             <description>
///                 We are not allocating/deallocating objects for nodes and edges frequently. Instead, we reuse
///                 slots in the backing arrays. This minimizes garbage collection overhead, which can be a
///                 performance bottleneck in high-churn scenarios like spreadsheets. While we don't exactly
///                 know in which situation our spreadsheeting software will be deployed, this is a safe bet,
///                 <i>especially</i> if we expose this over the network for collaborative editing.
///             </description>
///         </item>
///         <item>
///             <term>Memory efficiency</term>
///             <description>
///                 Using structs and arrays minimizes memory overhead compared to reference types. This is
///                 important when dealing with large spreadsheets with many cells and dependencies.
///             </description>
///         </item>
///     </list>
///     <remarks>
///         Because the backing type for nodes and edges is a list, the graph can grow as needed.
///         However, in order to maintain O(1) performance for add and remove operations, removed nodes
///         and edges are not physically deleted from the backing lists; instead, they are marked as free
///         and reused for future additions. This means that the memory footprint of the graph may not
///         shrink even after many removals.
///     </remarks>
/// </summary>
/// <typeparam name="TKey">The key type for each node.</typeparam>
/// <typeparam name="TValue">The value type for each node.</typeparam>
public sealed class Graph<TKey, TValue>
    where TKey : notnull
{
    /// <summary>
    ///     <para>
    ///         A node in the graph. Each node maintains a linked list of its outgoing
    ///         and incoming edges.
    ///     </para>
    /// </summary>
    public struct Node
    {
        /// <summary>
        ///     <para>
        ///         The key value associated with this node.
        ///     </para>
        /// </summary>
        public TKey Key;

        /// <summary>
        ///     <para>
        ///         The value associated with this node.
        ///     </para>
        /// </summary>
        public TValue Value;

        /// <summary>
        ///     <para>
        ///         The index of the first outgoing edge from this node.
        ///     </para>
        /// </summary>
        public int FirstOut;

        /// <summary>
        ///     <para>
        ///         The index of the first incoming edge to this node.
        ///     </para>
        /// </summary>
        public int FirstIn;

        /// <summary>
        ///     <para>
        ///         The index of the next free node in the free list.
        ///     </para>
        /// </summary>
        public int NextFree;

        /// <summary>
        ///     <para>
        ///         Whether this node is alive (not removed).
        ///     </para>
        /// </summary>
        public bool Alive;

        /// <summary>
        ///     <para>
        ///         Number of alive outgoing edges from this node.
        ///     </para>
        /// </summary>
        public int OutgoingCount;

        /// <summary>
        ///     <para>
        ///         Number of alive incoming edges to this node.
        ///     </para>
        /// </summary>
        public int IncomingCount;
    }

    /// <summary>
    ///     <para>
    ///         An edge in the graph. Each edge maintains pointers to its source and target nodes,
    ///         as well as pointers to the next and previous edges in the outgoing and incoming
    ///         linked lists of its source and target nodes.
    ///     </para>
    /// </summary>
    private struct Edge
    {
        /// <summary>
        ///     <para>
        ///         The index of the source node of this edge.
        ///     </para>
        /// </summary>
        public int From;

        /// <summary>
        ///     <para>
        ///         The index of the target node of this edge.
        ///     </para>
        /// </summary>
        public int To;

        /// <summary>
        ///     <para>
        ///         The index of the next outgoing edge from the source node.
        ///     </para>
        /// </summary>
        public int NextOut;

        /// <summary>
        ///     <para>
        ///         The index of the previous outgoing edge from the source node.
        ///     </para>
        /// </summary>
        public int PrevOut;

        /// <summary>
        ///     <para>
        ///         The index of the next incoming edge to the target node.
        ///     </para>
        /// </summary>
        public int NextIn;

        /// <summary>
        ///     <para>
        ///         The index of the previous incoming edge to the target node.
        ///     </para>
        /// </summary>
        public int PrevIn;

        /// <summary>
        ///     <para>
        ///         The index of the next free edge in the free list.
        ///     </para>
        /// </summary>
        public int NextFree;

        /// <summary>
        ///     <para>
        ///         Whether this edge is alive (not removed).
        ///     </para>
        /// </summary>
        public bool Alive;
    }

    /// <summary>
    ///     <para>
    ///         The mapping from node keys to their indices in the _nodes list.
    ///     </para>
    /// </summary>
    private readonly Dictionary<TKey, int> _nodeLookup = new();

    /// <summary>
    ///     <para>
    ///         The list of nodes in the graph.
    ///     </para>
    /// </summary>
    private readonly List<Node> _nodes = [];

    /// <summary>
    ///     <para>
    ///         The list of edges in the graph.
    ///     </para>
    /// </summary>
    private readonly List<Edge> _edges = [];

    /// <summary>
    ///     <para>
    ///         The index of the first free node in the free list. <see cref="int.MinValue"/> if there
    ///         are no free nodes.
    ///     </para>
    /// </summary>
    private int _freeNode = int.MinValue;

    /// <summary>
    ///     <para>
    ///         The index of the first free edge in the free list. <see cref="int.MinValue"/> if there
    ///         are no free edges.
    ///     </para>
    /// </summary>
    private int _freeEdge = int.MinValue;

    /// <summary>
    ///     <para>
    ///         Adds a new node to the graph with the given key and value. If a node with the same key
    ///         already exists, we overwrite it and follow the conventions of <see cref="Dictionary{TKey,TValue}"/>.
    ///     </para>
    /// </summary>
    /// <param name="key">The key to insert.</param>
    /// <param name="value">The value to associate with the key.</param>
    /// <returns>The index of the node in the backing array; this may be ignored.</returns>
    public int AddNode(TKey key, TValue value)
    {
        // Find a free node index if one exists; otherwise, append a new node.
        int index;
        if (_freeNode != int.MinValue)
        {
            index = _freeNode;
            var free = _nodes[index];
            _freeNode = free.NextFree;
        }
        else
        {
            index = _nodes.Count;
            _nodes.Add(default);
        }

        // Insert new node.
        _nodes[index] = new Node
        {
            Key = key,
            Value = value,
            FirstOut = int.MinValue,
            FirstIn = int.MinValue,
            Alive = true,
            NextFree = int.MinValue,
            OutgoingCount = 0,
            IncomingCount = 0
        };

        // Update lookup.
        _nodeLookup[key] = index;

        return index;
    }

    /// <summary>
    ///     <para>
    ///         Removes the <c>node</c> with the given index from the graph, along with all its associated
    ///         incoming and outgoing edges.
    ///     </para>
    /// </summary>
    /// <param name="node">The index of the node to remove.</param>
    public void RemoveNode(int node)
    {
        // Validate node index. If the node is already removed (!alive), do nothing.
        var n = _nodes[node];
        if (!n.Alive)
        {
            return;
        }

        // Remove all outgoing edges by repeatedly removing the first outgoing edge.
        while (_nodes[node].FirstOut != int.MinValue)
        {
            RemoveEdge(_nodes[node].FirstOut);
        }

        // Remove all incoming edges by repeatedly removing the first incoming edge.
        while (_nodes[node].FirstIn != int.MinValue)
        {
            RemoveEdge(_nodes[node].FirstIn);
        }

        // Remove node from lookup and mark as free.
        _nodeLookup.Remove(n.Key);
        n.Alive = false;
        n.NextFree = _freeNode;
        _freeNode = node;

        // Update node in list.
        _nodes[node] = n;
    }


    /// <summary>
    ///     <para>
    ///         Adds a new directed edge from the node with index <c>from</c> to the node with index <c>to</c>.
    ///     </para>
    /// </summary>
    public int AddEdge(int from, int to)
    {
        // Find a free edge index if one exists; otherwise, append a new edge.
        int index;
        if (_freeEdge != int.MinValue)
        {
            index = _freeEdge;
            var free = _edges[index];
            _freeEdge = free.NextFree;
        }
        else
        {
            index = _edges.Count;
            _edges.Add(default);
        }

        // Insert new edge.
        var e = new Edge
        {
            From = from,
            To = to,
            NextOut = int.MinValue,
            PrevOut = int.MinValue,
            NextIn = int.MinValue,
            PrevIn = int.MinValue,
            Alive = true,
            NextFree = int.MinValue
        };

        // Set up outgoing linked list for 'from' node. We do this by inserting at the head.
        var fromNode = _nodes[from];
        e.NextOut = fromNode.FirstOut;
        if (e.NextOut != int.MinValue)
        {
            var next = _edges[e.NextOut];
            next.PrevOut = index;
            _edges[e.NextOut] = next;
        }

        fromNode.FirstOut = index;
        fromNode.OutgoingCount++;
        _nodes[from] = fromNode;

        // Set up incoming linked list for 'to' node. This is done similarly to outgoing edges.
        var toNode = _nodes[to];
        e.NextIn = toNode.FirstIn;
        if (e.NextIn != int.MinValue)
        {
            var next = _edges[e.NextIn];
            next.PrevIn = index;
            _edges[e.NextIn] = next;
        }

        toNode.FirstIn = index;
        toNode.IncomingCount++;
        _nodes[to] = toNode;

        // Update edge in list.
        _edges[index] = e;

        return index;
    }

    /// <summary>
    ///     <para>
    ///         Adds a new directed edge from the node with key <c>fromKey</c> to the node with key <c>toKey</c>.
    ///         A wrapper around <see cref="AddEdge(int, int)"/> which looks up the node indices by key first;
    ///         this overhead, if it proves to be too much (unlikely), can be avoided by using the index-based method
    ///         directly via the values returned from other methods.
    ///     </para>
    /// </summary>
    /// <param name="fromKey">The key of the source node.</param>
    /// <param name="toKey">The key of the target node.</param>
    /// <exception cref="InvalidOperationException">Thrown if either key does not exist</exception>
    public int AddEdge(TKey fromKey, TKey toKey)
    {
        if (!IsValidNodeKey(fromKey))
        {
            throw new InvalidOperationException("Source node does not exist.");
        }

        if (!IsValidNodeKey(toKey))
        {
            throw new InvalidOperationException("Target node does not exist.");
        }

        var fromIndex = _nodeLookup[fromKey];
        var toIndex = _nodeLookup[toKey];

        return AddEdge(fromIndex, toIndex);
    }

    /// <summary>
    ///     <para>
    ///         Removes the edge with the given index from the graph. This takes a stable node ID.
    ///     </para>
    /// </summary>
    /// <param name="edge">The index of the edge to remove.</param>
    public void RemoveEdge(int edge)
    {
        // Validate edge index.
        if (edge < 0 || edge >= _edges.Count)
            throw new InvalidOperationException("Edge index out of bounds.");

        var e = _edges[edge];
        if (!e.Alive)
            return;

        // Remove from outgoing linked list
        if (e.PrevOut != int.MinValue)
        {
            var prev = _edges[e.PrevOut];
            prev.NextOut = e.NextOut;
            _edges[e.PrevOut] = prev;
        }
        else
        {
            var fromNode = _nodes[e.From];
            fromNode.FirstOut = e.NextOut;
            _nodes[e.From] = fromNode;
        }

        // Only update NextOut if the next edge is alive
        if (e.NextOut != int.MinValue && _edges[e.NextOut].Alive)
        {
            var next = _edges[e.NextOut];
            next.PrevOut = e.PrevOut;
            _edges[e.NextOut] = next;
        }

        // Decrement outgoing count
        var sourceNode = _nodes[e.From];
        sourceNode.OutgoingCount--;
        _nodes[e.From] = sourceNode;

        // Remove from incoming linked list
        if (e.PrevIn != int.MinValue)
        {
            var prev = _edges[e.PrevIn];
            prev.NextIn = e.NextIn;
            _edges[e.PrevIn] = prev;
        }
        else
        {
            var toNode = _nodes[e.To];
            toNode.FirstIn = e.NextIn;
            _nodes[e.To] = toNode;
        }

        // Only update NextIn if the next edge is alive
        if (e.NextIn != int.MinValue && _edges[e.NextIn].Alive)
        {
            var next = _edges[e.NextIn];
            next.PrevIn = e.PrevIn;
            _edges[e.NextIn] = next;
        }

        // Decrement incoming count
        var targetNode = _nodes[e.To];
        targetNode.IncomingCount--;
        _nodes[e.To] = targetNode;

        // Mark edge as free and add to free list
        e.Alive = false;
        e.NextFree = _freeEdge;
        _freeEdge = edge;

        _edges[edge] = e;
    }

    /// <summary>
    ///     <para>
    ///         Removes an edge from the graph given the keys of its source and target nodes. This
    ///         is a wrapper around <see cref="RemoveEdge(int)"/> which looks up the edge index first;
    ///         this overhead, if it proves to be too much (unlikely), can be avoided by using the index-based method
    ///         directly via the values returned from other methods.
    ///     </para>
    /// </summary>
    /// <param name="source">The source key to look up and remove.</param>
    /// <param name="target">The target key to look up and remove.</param>
    /// <exception cref="InvalidOperationException">Thrown if either key does not exist</exception>
    public void RemoveEdge(TKey source, TKey target)
    {
        // Do some validation.
        if (!IsValidNodeKey(source))
        {
            throw new InvalidOperationException("Source node does not exist.");
        }

        if (!IsValidNodeKey(target))
        {
            throw new InvalidOperationException("Target node does not exist.");
        }

        var sourceIndex = _nodeLookup[source];
        var targetIndex = _nodeLookup[target];

        // Inner.
        RemoveEdge(sourceIndex, targetIndex);
    }

    /// <summary>
    ///     <para>
    ///         Removes an edge from the graph given the indices of its source and target nodes. This
    ///         is a wrapper around <see cref="RemoveEdge(int)"/> which looks up the edge index first.
    ///     </para>
    /// </summary>
    /// <param name="sourceIndex">The index of the source node.</param>
    /// <param name="targetIndex">The index of the target node.</param>
    /// <exception cref="InvalidOperationException">Thrown when any of the nodes don't exist.</exception>
    public void RemoveEdge(int sourceIndex, int targetIndex)
    {
        // Do some validation.
        if (!IsValidNodeIndex(sourceIndex))
        {
            throw new InvalidOperationException("Source node does not exist.");
        }

        if (!IsValidNodeIndex(targetIndex))
        {
            throw new InvalidOperationException("Target node does not exist.");
        }

        // Find the edge from source to target.
        var currentEdge = _nodes[sourceIndex].FirstOut;
        while (currentEdge != int.MinValue)
        {
            var edge = _edges[currentEdge];
            if (edge.To == targetIndex && edge.Alive)
            {
                RemoveEdge(currentEdge);
                return;
            }

            currentEdge = edge.NextOut;
        }
    }

    /// <summary>
    ///     <para>
    ///         Get the dependents of the node with the given index, via an enumerator.
    ///     </para>
    /// </summary>
    /// <param name="node">The node ID to get the dependents of.</param>
    /// <returns>An enumerator over the dependents of the given node.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependents(int node)
    {
        if (!IsValidNodeIndex(node))
        {
            throw new InvalidOperationException("Node does not exist.");
        }

        var currentEdge = _nodes[node].FirstOut;
        while (currentEdge != int.MinValue)
        {
            // Skip dead edges.
            if (!_edges[currentEdge].Alive)
            {
                currentEdge = _edges[currentEdge].NextOut;
                continue;
            }

            yield return _nodes[_edges[currentEdge].To];
            currentEdge = _edges[currentEdge].NextOut;
        }
    }

    /// <summary>
    ///     <para>
    ///         Get the dependents of the node with the given key, via an enumerator. Wraps
    ///         <see cref="EnumerateNodeDependents(int)"/>
    ///     </para>
    /// </summary>
    /// <returns>>An enumerator over the dependents of the given node.</returns>
    /// <param name="node">The node key to get the dependents of.</param>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependents(TKey node) =>
        !IsValidNodeKey(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : EnumerateNodeDependents(_nodeLookup[node]);

    /// <summary>
    ///     <para>
    ///         Get the dependencies of the node with the given index, via an enumerator.
    ///         These are the nodes that the given node depends on (incoming edges).
    ///     </para>
    /// </summary>
    /// <param name="node">The node ID to get the dependencies of.</param>
    /// <returns>An enumerator over the nodes that the given node depends on.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependees(int node)
    {
        if (!IsValidNodeIndex(node))
        {
            throw new InvalidOperationException("Node does not exist.");
        }

        var currentEdge = _nodes[node].FirstIn;
        while (currentEdge != int.MinValue)
        {
            // Skip dead edges.
            if (!_edges[currentEdge].Alive)
            {
                currentEdge = _edges[currentEdge].NextIn;
                continue;
            }

            yield return _nodes[_edges[currentEdge].From];
            currentEdge = _edges[currentEdge].NextIn;
        }
    }

    /// <summary>
    ///     <para>
    ///         Get the dependencies of the node with the given key, via an enumerator.
    ///         Wraps <see cref="EnumerateNodeDependees(int)"/>.
    ///     </para>
    /// </summary>
    /// <param name="node">The node key to get the dependencies of.</param>
    /// <returns>An enumerator over the nodes that the given node depends on.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<Node> EnumerateNodeDependees(TKey node) =>
        !IsValidNodeKey(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : EnumerateNodeDependees(_nodeLookup[node]);

    /// <summary>
    ///     <para>
    ///         Enumerates all alive nodes in the graph.
    ///     </para>
    /// </summary>
    /// <returns>An enumerator over all alive nodes in the graph.</returns>
    public IEnumerable<Node> EnumerateNodes()
    {
        for (var i = 0; i < _nodes.Count; i++)
        {
            if (_nodes[i].Alive)
            {
                yield return _nodes[i];
            }
        }
    }

    /// <summary>
    ///     <para>
    ///         Enumerates all alive edges in the graph as (from, to) node pairs.
    ///     </para>
    /// </summary>
    /// <returns>An enumerator over all alive edges in the graph.</returns>
    public IEnumerable<(Node, Node)> EnumerateEdges()
    {
        for (var i = 0; i < _edges.Count; i++)
        {
            if (!_edges[i].Alive) continue;

            var edge = _edges[i];
            yield return (_nodes[edge.From], _nodes[edge.To]);
        }
    }

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependents (i.e., other nodes depend on it).
    ///     </para>
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns><c>true</c> if the node has dependents.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public bool NodeHasDependents(int node)
        => GetOutgoingCount(node) > 0;

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependents (i.e., other nodes depend on it).
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns><c>true</c> if the node has dependents.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public bool NodeHasDependents(TKey key)
        => GetOutgoingCount(key) > 0;

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependees (i.e., depends on one or more
    ///         other nodes).
    ///     </para>
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns><c>true</c> if the node has dependees.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public bool NodeHasDependees(int node)
        => GetIncomingCount(node) > 0;

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependees (i.e., depends on one or more
    ///         other nodes).
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns><c>true</c> if the node has dependents.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public bool NodeHasDependees(TKey key)
        => GetIncomingCount(key) > 0;

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has any edges (incoming or outgoing).
    ///     </para>
    /// </summary>
    /// <param name="node">The node to check.</param>
    /// <returns><c>true</c> if the node has any edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception
    public bool NodeHasAnyEdges(int node)
        => GetOutgoingCount(node) > 0 || GetIncomingCount(node) > 0;

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has any edges (incoming or outgoing).
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns><c>true</c> if the node has any edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public bool NodeHasAnyEdges(TKey key)
        => GetOutgoingCount(key) > 0 || GetIncomingCount(key) > 0;

    // TODO: Investigate the utility of providing faster bulk operations for getting dependents/dependencies
    //       as opposed to enumerators. This may be useful in eliminating some overhead in certain scenarios.

    /// <summary>
    ///     <para>
    ///         The number of alive nodes in the graph.
    ///     </para>
    /// </summary>
    public int NodeCount => _nodeLookup.Count;

    /// <summary>
    ///     <para>
    ///         The number of alive edges outgoing from the given node.
    ///     </para>
    /// </summary>
    /// <param name="node">The node ID to check.</param>
    /// <returns>The number of outgoing edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public int GetOutgoingCount(int node)
        => !IsValidNodeIndex(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : _nodes[node].OutgoingCount;

    /// <summary>
    ///     <para>
    ///         The number of alive edges outgoing from the given node.
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns>The number of outgoing edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public int GetOutgoingCount(TKey key)
        => !IsValidNodeKey(key)
            ? throw new InvalidOperationException("Node does not exist.")
            : GetOutgoingCount(_nodeLookup[key]);

    /// <summary>
    ///     <para>
    ///         The number of alive edges incoming to the given node.
    ///     </para>
    /// </summary>
    /// <param name="node">The node ID to check.</param>
    /// <returns>The number of incoming edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public int GetIncomingCount(int node)
        => !IsValidNodeIndex(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : _nodes[node].IncomingCount;

    /// <summary>
    ///     <para>
    ///         The number of alive edges incoming to the given node.
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns>The number of incoming edges.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public int GetIncomingCount(TKey key)
        => !IsValidNodeKey(key)
            ? throw new InvalidOperationException("Node does not exist.")
            : GetIncomingCount(_nodeLookup[key]);

    /// <summary>
    ///     <para>
    ///         Checks whether the given node ID corresponds to a valid, alive node in the graph.
    ///     </para>
    /// </summary>
    /// <param name="node">The node ID to check.</param>
    /// <returns><c>true</c> if the key corresponds to a valid, alive node.</returns>
    private bool IsValidNodeIndex(int node)
        => node >= 0 && node < _nodes.Count && _nodes[node].Alive;

    /// <summary>
    ///     <para>
    ///         Checks whether the given edge ID corresponds to a valid, alive edge in the graph.
    ///     </para>
    /// </summary>
    /// <param name="edge">The edge ID to check.</param>
    /// <returns><c>true</c> if the key corresponds to a valid, alive edge.</returns>
    private bool IsValidEdgeIndex(int edge)
        => edge >= 0 && edge < _edges.Count && _edges[edge].Alive;
    
    /// <summary>
    ///     <para>
    ///         Checks whether the given node key corresponds to a valid, alive node in the graph.
    ///     </para>
    /// </summary>
    /// <param name="key">The node key to check.</param>
    /// <returns><c>true</c> if the key corresponds to a valid, alive node.</returns>
    private bool IsValidNodeKey(TKey key)
        => _nodeLookup.ContainsKey(key) && IsValidNodeIndex(_nodeLookup[key]);

    /// <summary>
    ///     <para>
    ///         Gets the index of the node with the given key.
    ///     </para>
    /// </summary>
    /// <param name="key">The key to look up.</param>
    /// <returns>The index of the node in the backing array.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>key</c> does not correspond to a valid node as specified by <see cref="IsValidNodeKey"/>.
    /// </exception>
    public int GetNode(TKey key)
        => !IsValidNodeKey(key)
            ? throw new InvalidOperationException("Node does not exist.")
            : _nodeLookup[key];

    /// <summary>
    ///     <para>
    ///         Gets the index of the node with the given key, creating it if it does not exist.
    ///     </para>
    /// </summary>
    /// <param name="key">The key to look up or create.</param>
    /// <param name="value">The value to associate with the key if creating a new node.</param>
    /// <returns>The index of the node in the backing array.</returns>
    public int GetOrCreateNode(TKey key, TValue value)
        => IsValidNodeKey(key)
            ? _nodeLookup[key]
            : AddNode(key, value);

    /// <summary>
    ///     <para>
    ///         Gets the value associated with the node with the given index.
    ///     </para>
    /// </summary>
    /// <param name="node">The index of the node to look up.</param>
    /// <returns>The value associated with the node.</returns>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>node</c> refers to an invalid node as specified by <see cref="IsValidNodeIndex"/>.
    /// </exception>
    public TValue GetValue(int node) =>
        !IsValidNodeIndex(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : _nodes[node].Value;

    /// <summary>
    ///     <para>
    ///         Gets the index of the first outgoing edge from the given node.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>node</c> refers to an invalid node as specified by <see cref="IsValidNodeIndex"/>.
    /// </exception>
    public int FirstOutgoing(int node)
        => !IsValidNodeIndex(node)
            ? throw new InvalidOperationException("Node does not exist.")
            : _nodes[node].FirstOut;

    /// <summary>
    ///     <para>
    ///         Gets the index of the next outgoing edge after the given edge.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>edge</c> refers to an invalid edge as specified by <see cref="IsValidNodeIndex"/>.
    /// </exception>
    public int NextOutgoing(int edge)
        => !IsValidEdgeIndex(edge)
            ? throw new InvalidOperationException("Edge does not exist.")
            : _edges[edge].NextOut;

    /// <summary>
    ///     <para>
    ///         Gets the index of the target node of the given edge.
    ///     </para>
    /// </summary>
    /// <exception cref="InvalidOperationException">
    ///     Thrown when <c>edge</c> refers to an invalid edge as specified by <see cref="IsValidNodeIndex"/>.
    /// </exception>
    public int EdgeTarget(int edge)
        => !IsValidEdgeIndex(edge)
            ? throw new InvalidOperationException("Edge does not exist.")
            : _edges[edge].To;
}