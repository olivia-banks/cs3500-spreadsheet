// <summary>
//   <para>
//      Skeleton implementation written by Joe Zachary for CS 3500, September 2013
//      Version 1.1 - Joe Zachary
//          (Fixed error in comment for RemoveDependency)
//      Version 1.2 - Daniel Kopta Fall 2018
//          (Clarified meaning of dependent and dependee)
//          (Clarified names in solution/project structure)
//      Version 1.3 - H. James de St. Germain Fall 2024
//   </para>
// </summary>

using System.Diagnostics;

namespace DependencyGraph;

/// <summary>
///     <para>
///         (<c>s1</c>, <c>t1</c>) is an ordered pair of strings, meaning <c>t1</c> depends
///         on <c>s1</c>. In other words: <c>s1</c> must be evaluated before <c>t1</c>.
///     </para>
///     <para>
///         A <see cref="DependencyGraph"/> can be modeled as a set of ordered pairs of strings.
///         Two ordered pairs, (<c>s1</c>, <c>t1</c>) and (<c>s2</c>, <c>t2</c>) are considered
///         equal if and only if <c>s1</c> equals <c>s2</c> and <c>t1</c> equals <c>t2</c>.
///     </para>
///     <remarks>
///         Recall that sets never contain duplicates.  If an attempt is made to add an element
///         to a set, and the element is already in the set, the set remains unchanged.
///     </remarks>
///     <para>
///         Given a <see cref="DependencyGraph"/> DG:
///     </para>
///     <list type="number">
///         <item>
///             If <c>s</c> is a <see cref="string"/>, the set of all strings <c>t</c> such that
///             (<c>s</c>,<c>t</c>) is in DG is called dependents(s), or, the set of things that
///             depend on <c>s</c>.
///         </item>
///         <item>
///             If <c>s</c> is a string, the set of all strings <c>t</c> such that (<c>t</c>, <c>s</c>)
///             is in DG is called dependees(s), or, the set of things that <c>s</c> depends on.
///         </item>
///     </list>
///     <para>
///         For example, suppose <c>DG</c> = <c>{("a", "b"), ("a", "c"), ("b", "d"), ("d", "d")}</c>.
///     </para>
///     <code>
///         dependents("a") = {"b", "c"}
///         dependents("b") = {"d"}
///         dependents("c") = {}
///         dependents("d") = {"d"}
///         dependees("a")  = {}
///         dependees("b")  = {"a"}
///         dependees("c")  = {"a"}
///         dependees("d")  = {"b", "d"}
///     </code>
/// </summary>
public class DependencyGraph
{
    /// <summary>
    ///     <para>
    ///         The internal graph structure used to represent dependencies.
    ///     </para>
    /// </summary>
    private readonly Graph<string, object?> _graph;

    /// <summary>
    ///     <para>
    ///         A helper for traversing the graph.
    ///     </para>
    /// </summary>
    private readonly GraphHelper<string, object?> _graphHelper;

    /// <summary>
    ///     <para>
    ///         Initializes a new, empty instance of the <see cref="DependencyGraph"/> class.
    ///     </para>
    /// </summary>
    public DependencyGraph()
    {
        _graph = new Graph<string, object?>();
        _graphHelper = new GraphHelper<string, object?>(_graph);
    }

    /// <summary>
    ///     <para>
    ///         The number of ordered pairs recorded insofar.
    ///     </para>
    /// </summary>
    public int Size => _graph.NodeCount;

    /// <summary>
    ///     Reports whether the given node has dependents (i.e., other nodes depend on it).
    /// </summary>
    /// <param name="nodeName">The name of the node.</param>
    /// <returns><c>true</c> if the node has dependents.</returns>
    public bool HasDependents(string nodeName)
        => _graph.NodeHasDependents(nodeName);

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependees (i.e., depends on one or more
    ///         other nodes).
    ///     </para>
    /// </summary>
    /// <returns><c>true</c> if the node has dependees.</returns>
    /// <param name="nodeName">The name of the node.</param>
    public bool HasDependees(string nodeName)
        => _graph.NodeHasDependees(nodeName);
    
    /// <summary>
    ///     <para>
    ///         Returns the dependents of the node with the given name, directly (i.e., only
    ///         the nodes that depend on it, not their dependents).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The direct dependents of nodeName.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<string> GetDependents(string nodeName)
        => _graph.EnumerateNodeDependents(nodeName).Select(n => n.Key);
    
    /// <summary>
    ///     <para>
    ///         Returns the dependees of the node with the given name, directly (i.e., only
    ///         the nodes that it depends on, not their dependees).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The direct dependees of nodeName.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<string> GetDependees(string nodeName)
        => _graph.EnumerateNodeDependees(nodeName).Select(n => n.Key);
    
    /// <summary>
    ///     <para>
    ///         Returns the dependents of the node with the given name, recursively (i.e., all nodes that
    ///         depend on it, either directly or indirectly).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The dependents of nodeName.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<string> GetAllDependents(string nodeName)
        => _graphHelper.Traverse(
            nodeName,
            nodeKey => _graph.EnumerateNodeDependents(nodeKey)
        ).Select(n => n.Key);

    /// <summary>
    ///     <para>
    ///         Returns the dependees of the node with the given name, recursively (i.e., all nodes that
    ///         it depends on, either directly or indirectly).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The dependees of nodeName.</returns>
    /// <exception cref="InvalidOperationException">Thrown if the node does not exist.</exception>
    public IEnumerable<string> GetAllDependees(string nodeName)
        => _graphHelper.Traverse(
            nodeName,
            nodeKey => _graph.EnumerateNodeDependees(nodeKey)
        ).Select(n => n.Key);

    /// <summary>
    ///     <para>
    ///         Adds the ordered pair (dependee, dependent), if it doesn't exist.
    ///     </para>
    ///     <para>
    ///         This can be thought of as: dependee must be evaluated before dependent
    ///     </para>
    /// </summary>
    /// <param name="dependee">
    ///     The name of the node that must be evaluated first.
    /// </param>
    /// <param name="dependent">
    ///     The name of the node that cannot be evaluated until after dependee.
    /// </param>
    public void AddDependency(string dependee, string dependent)
    {
        _graph.CreateNodeIfNotExists(dependee, null);
        _graph.CreateNodeIfNotExists(dependent, null);

        _graph.AddEdge(dependee, dependent);
    }

    /// <summary>
    ///     <para>
    ///         Removes the ordered pair (dependee, dependent), if it exists. This operation is
    ///         O(n) where n is the number of edges between dependee and dependent.
    ///     </para>
    ///     <para>
    ///         This function will always create nodes for the given names if they do not already
    ///         exist. If a node has no edges (after the removal of the edge), it is removed from
    ///         the graph.
    ///     </para>
    /// </summary>
    /// <param name="dependee">The name of the node that must be evaluated first.</param>
    /// <param name="dependent">The name of the node that cannot be evaluated until after dependee.</param>
    public void RemoveDependency(string dependee, string dependent)
    {
        _graph.RemoveEdge(dependee, dependent);

        // Do some cleanup to remove nodes with no edges.
        if (!_graph.NodeHasAnyEdges(dependee))
        {
            _graph.RemoveNode(dependee);
        }

        // ReSharper disable once InvertIf
        if (!_graph.NodeHasAnyEdges(dependent))
        {
            _graph.RemoveNode(dependent);
        }
    }

    /// <summary>
    ///     <para>
    ///         Removes all existing ordered pairs of the form (<c>nodeName</c>, <c>*</c>). Then,
    ///         for each <c>t</c> in <c>newDependents</c>, adds the ordered pair (<c>nodeName</c>, <c>t</c>).
    ///         This operation is O(D + nD) where D is the number of dependents of <c>nodeName</c> and nD is the
    ///         number of elements in <c>newDependents</c>.
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The name of the node whose dependents are being replaced.</param>
    /// <param name="newDependents">The new dependents for <c>nodeName</c>.</param>
    public void ReplaceDependents(string nodeName, IEnumerable<string> newDependents)
    {
        // Remove all existing dependents.
        _graph.EnumerateNodeDependents(nodeName).ToList().ForEach(n => _graph.RemoveEdge(nodeName, n.Key));

        // Add new dependents.
        foreach (var dependent in newDependents)
        {
            _graph.AddEdge(nodeName, dependent);
        }
    }

    /// <summary>
    ///     <para>
    ///         Removes all existing ordered pairs of the form (<c>*</c>, <c>nodeName</c>). Then,
    ///         for each <c>t</c> in newDependees, adds the ordered pair (<c>t</c>, <c>nodeName</c>).
    ///         This operation is O(D + nD) where D is the number of dependees of <c>nodeName</c> and nD is the
    ///         number of elements in <c>newDependees</c>.
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The name of the node who's dependees are being replaced.</param>
    /// <param name="newDependees">The new dependees for nodeName.</param>
    public void ReplaceDependees(string nodeName, IEnumerable<string> newDependees)
    {
        // Remove all existing dependees.
        _graph.EnumerateNodeDependees(nodeName).ToList().ForEach(n => _graph.RemoveEdge(n.Key, nodeName));

        // Add new dependees.
        foreach (var dependee in newDependees)
        {
            _graph.AddEdge(dependee, nodeName);
        }
    }

    /// <summary>
    ///     <para>
    ///         Outputs the dependency graph in DOT format for visualization with Graphviz.
    ///     </para>
    /// </summary>
    /// <returns>A string representing the dependency graph in DOT format.</returns>
    public string ToDot(string digraphName = "DependencyGraph")
    {
        var dot = $"digraph {digraphName} {{\n";
        foreach (var (from, to) in _graph.EnumerateEdges())
        {
            var sanitizedFromKey = from.Key.Replace("\"", "\\\"");
            var sanitizedToKey = to.Key.Replace("\"", "\\\"");
            dot += $"    \"{sanitizedFromKey}\" -> \"{sanitizedToKey}\";\n";
        }

        dot += "}\n";

        return dot;
    }
}