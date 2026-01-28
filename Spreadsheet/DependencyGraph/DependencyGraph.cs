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
public class DependencyGraph()
{
    /// <summary>
    ///     <para>
    ///         The number of ordered pairs recorded insofar.
    ///     </para>
    /// </summary>
    public int Size { get; private set; }

    /// <summary>
    ///     Reports whether the given node has dependents (i.e., other nodes depend on it).
    /// </summary>
    /// <param name="nodeName">The name of the node.</param>
    /// <returns><c>true</c> if the node has dependents.</returns>
    public bool HasDependents(string nodeName)
    {
        return false;
    }

    /// <summary>
    ///     <para>
    ///         Reports whether the given node has dependees (i.e., depends on one or more other nodes).
    ///     </para>
    /// </summary>
    /// <returns><c>true</c> if the node has dependees.</returns>
    /// <param name="nodeName">The name of the node.</param>
    public bool HasDependees(string nodeName)
    {
        return false;
    }

    /// <summary>
    ///     <para>
    ///         Returns the dependents of the node with the given name.
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The dependents of nodeName.</returns>
    public IEnumerable<string> GetDependents(string nodeName)
    {
        return new List<string>(); // Choose your own data structure
    }

    /// <summary>
    ///     <para>
    ///         Returns the dependees of the node with the given name.
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The node we are looking at.</param>
    /// <returns>The dependees of nodeName.</returns>
    public IEnumerable<string> GetDependees(string nodeName)
    {
        return new List<string>(); // Choose your own data structure
    }

    /// <summary>
    ///     <para>
    ///         Adds the ordered pair (dependee, dependent), if it doesn't exist.
    ///     </para>
    ///     <para>
    ///         This can be thought of as: dependee must be evaluated before dependent
    ///     </para>
    /// </summary>
    /// <param name="dependee">The name of the node that must be evaluated first.</param>
    /// <param name="dependent">The name of the node that cannot be evaluated until after dependee.</param>
    public void AddDependency(string dependee, string dependent)
    {
    }

    /// <summary>
    ///     <para>
    ///         Removes the ordered pair (dependee, dependent), if it exists.
    ///     </para>
    /// </summary>
    /// <param name="dependee">The name of the node that must be evaluated first.</param>
    /// <param name="dependent">The name of the node that cannot be evaluated until after dependee.</param>
    public void RemoveDependency(string dependee, string dependent)
    {
    }

    /// <summary>
    ///     <para>
    ///         Removes all existing ordered pairs of the form (<c>nodeName</c>, <c>*</c>). Then,
    ///         for each <c>t</c> in <c>newDependents</c>, adds the ordered pair (<c>nodeName</c>, <c>t</c>).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The name of the node whose dependents are being replaced.</param>
    /// <param name="newDependents">The new dependents for <c>nodeName</c>.</param>
    public void ReplaceDependents(string nodeName, IEnumerable<string> newDependents)
    {
    }

    /// <summary>
    ///     <para>
    ///         Removes all existing ordered pairs of the form (<c>*</c>, <c>nodeName</c>). Then,
    ///         for each <c>t</c> in newDependees, adds the ordered pair (<c>t</c>, <c>nodeName</c>).
    ///     </para>
    /// </summary>
    /// <param name="nodeName">The name of the node who's dependees are being replaced.</param>
    /// <param name="newDependees">The new dependees for nodeName.</param>
    public void ReplaceDependees(string nodeName, IEnumerable<string> newDependees)
    {
    }
}