namespace DependencyGraph;

using System.Collections.Generic;

/// <summary>
///     <para>
///         A helper class for traversing a graph.
///     </para>
/// </summary>
public class GraphHelper<TKey, TValue>(Graph<TKey, TValue> graph)
    where TKey : notnull
{
    /// <summary>
    ///     <para>
    ///         Performs a depth-first traversal of the graph starting from the specified node.
    ///     </para>
    /// </summary>
    public IEnumerable<Graph<TKey, TValue>.Node> Traverse(
        TKey startNode,
        Func<TKey, IEnumerable<Graph<TKey, TValue>.Node>> neighborSelector)
    {
        var visited = new HashSet<TKey>();
        var stack = new Stack<TKey>();
        stack.Push(startNode);

        while (stack.Count > 0)
        {
            var currentKey = stack.Pop();

            if (!visited.Add(currentKey))
            {
                continue;
            }

            var neighbors = neighborSelector(currentKey);

            foreach (var neighbor in neighbors)
            {
                if (visited.Contains(neighbor.Key)) continue;

                stack.Push(neighbor.Key);
                yield return neighbor;
            }
        }
    }
}