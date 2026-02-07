using DependencyGraph;

namespace DependencyGraphTests;

using DependencyGraph;

/// <summary>
///     <para>
///         This is a test class for <see cref="Graph{TKey,TValue}"/> and is intended to contain all
///         Graph Unit Tests.
///     </para>
/// </summary>
[TestClass]
public class GraphTests
{
    /// <summary>
    ///     <para>Tests adding a new node to the graph.</para>
    ///     <para>Verifies that the node exists and the value is correct.</para>
    /// </summary>
    [TestMethod]
    public void GraphAddNode_TestAddNode_NodeExistsWithCorrectValue()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        graph.AddNode(1, "A");

        // Assert
        Assert.AreEqual(1, graph.NodeCount);
        Assert.AreEqual("A", graph.GetValue(1));
    }

    /// <summary>
    ///     <para>Tests overwriting an existing node's value.</para>
    ///     <para>Verifies that the node's value is updated correctly.</para>
    /// </summary>
    [TestMethod]
    public void GraphAddNode_TestOverwriteNodeValue_NodeValueUpdated()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");

        // Act
        graph.AddNode(1, "B");

        // Assert
        Assert.AreEqual(1, graph.NodeCount);
        Assert.AreEqual("B", graph.GetValue(1));
    }

    /// <summary>
    ///     <para>Tests removing a node from the graph.</para>
    ///     <para>Verifies that the node is removed and edges are cleared.</para>
    /// </summary>
    [TestMethod]
    public void GraphRemoveNode_TestRemoveNode_NodeRemovedAndEdgesCleared()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddEdge(1, 2);

        // Act
        graph.RemoveNode(1);

        // Assert
        Assert.AreEqual(1, graph.NodeCount);
        Assert.IsFalse(graph.NodeHasDependees(2));
    }

    /// <summary>
    ///     <para>Tests adding an edge between two nodes.</para>
    ///     <para>Verifies that the edge is created correctly.</para>
    /// </summary>
    [TestMethod]
    public void GraphAddEdge_TestAddEdge_EdgeCreated()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");

        // Act
        graph.AddEdge(1, 2);

        // Assert
        Assert.IsTrue(graph.NodeHasDependents(1));
        Assert.IsTrue(graph.NodeHasDependees(2));
    }

    /// <summary>
    ///     <para>Tests adding an edge when the source node does not exist.</para>
    ///     <para>Verifies that InvalidOperationException is thrown.</para>
    /// </summary>
    [TestMethod]
    public void GraphAddEdge_TestSourceNodeMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(2, "B");

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.AddEdge(1, 2));
    }

    /// <summary>
    ///     <para>Tests adding an edge when the target node does not exist.</para>
    ///     <para>Verifies that InvalidOperationException is thrown.</para>
    /// </summary>
    [TestMethod]
    public void GraphAddEdge_TestTargetNodeMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.AddEdge(1, 2));
    }

    /// <summary>
    ///     <para>Tests removing an edge from the graph.</para>
    ///     <para>Verifies that the edge is removed correctly.</para>
    /// </summary>
    [TestMethod]
    public void GraphRemoveEdge_TestRemoveEdge_EdgeRemoved()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddEdge(1, 2);

        // Act
        graph.RemoveEdge(1, 2);

        // Assert
        Assert.IsFalse(graph.NodeHasDependents(1));
        Assert.IsFalse(graph.NodeHasDependees(2));
    }

    /// <summary>
    ///     <para>Tests enumerating dependents of a node.</para>
    ///     <para>Verifies that the correct nodes are returned.</para>
    /// </summary>
    [TestMethod]
    public void GraphEnumerateNodeDependents_TestEnumerateDependents_ReturnsCorrectNodes()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddNode(3, "C");
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);

        // Act
        var dependents = graph.EnumerateNodeDependents(1).Select(n => n.Key).ToList();

        // Assert
        CollectionAssert.AreEquivalent(new[] { 2, 3 }, dependents);
    }

    /// <summary>
    ///     <para>Tests enumerating dependees of a node.</para>
    ///     <para>Verifies that the correct nodes are returned.</para>
    /// </summary>
    [TestMethod]
    public void GraphEnumerateNodeDependees_TestEnumerateDependees_ReturnsCorrectNodes()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddNode(3, "C");
        graph.AddEdge(1, 2);
        graph.AddEdge(3, 2);

        // Act
        var dependees = graph.EnumerateNodeDependees(2).Select(n => n.Key).ToList();

        // Assert
        CollectionAssert.AreEquivalent(new[] { 1, 3 }, dependees);
    }

    /// <summary>
    ///     <para>Tests GetOutgoingCount and GetIncomingCount methods.</para>
    ///     <para>Verifies the correct counts are returned.</para>
    /// </summary>
    [TestMethod]
    public void GraphGetCounts_TestGetOutgoingAndIncomingCounts_ReturnsCorrectValues()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddNode(3, "C");
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);
        graph.AddEdge(3, 2);

        // Act
        var outgoing = graph.GetOutgoingCount(1);
        var incoming = graph.GetIncomingCount(2);

        // Assert
        Assert.AreEqual(2, outgoing);
        Assert.AreEqual(2, incoming);
    }

    /// <summary>
    ///     <para>Tests CreateNodeIfNotExists method.</para>
    ///     <para>Verifies that a new node is created when it does not exist.</para>
    /// </summary>
    [TestMethod]
    public void GraphCreateNodeIfNotExists_TestNodeDoesNotExist_CreatesNode()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        var created = graph.CreateNodeIfNotExists(1, "A");

        // Assert
        Assert.IsTrue(created);
        Assert.AreEqual("A", graph.GetValue(1));
    }

    /// <summary>
    ///     <para>Tests CreateNodeIfNotExists method when the node already exists.</para>
    ///     <para>Verifies that the node is not overwritten.</para>
    /// </summary>
    [TestMethod]
    public void GraphCreateNodeIfNotExists_TestNodeExists_DoesNotOverwrite()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");

        // Act
        var created = graph.CreateNodeIfNotExists(1, "B");

        // Assert
        Assert.IsFalse(created);
        Assert.AreEqual("A", graph.GetValue(1));
    }

    /// <summary>
    ///     <para>Tests EnumerateEdges method.</para>
    ///     <para>Verifies that all edges are returned correctly.</para>
    /// </summary>
    [TestMethod]
    public void GraphEnumerateEdges_TestEdgesReturned_CorrectEdges()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddNode(3, "C");
        graph.AddEdge(1, 2);
        graph.AddEdge(1, 3);
        graph.AddEdge(3, 2);

        // Act
        var edges = graph.EnumerateEdges().Select(e => (e.Item1.Key, e.Item2.Key)).ToList();

        // Assert
        CollectionAssert.AreEquivalent(new[] { (1, 2), (1, 3), (3, 2) }, edges);
    }

    /// <summary>
    ///     <para>Tests NodeHasAnyEdges method for a node with no edges.</para>
    ///     <para>Verifies that the method returns false.</para>
    /// </summary>
    [TestMethod]
    public void GraphNodeHasAnyEdges_TestNodeWithoutEdges_ReturnsFalse()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");

        // Act
        var hasEdges = graph.NodeHasAnyEdges(1);

        // Assert
        Assert.IsTrue(graph.ContainsNode(1));
        Assert.IsNotNull(graph.GetNode(1));
        Assert.IsNotNull(graph.EnumerateNodes());
        Assert.IsFalse(hasEdges);
    }

    /// <summary>
    ///     <para>Tests NodeHasAnyEdges method for a node with edges.</para>
    ///     <para>Verifies that the method returns true.</para>
    /// </summary>
    [TestMethod]
    public void GraphNodeHasAnyEdges_TestNodeWithEdges_ReturnsTrue()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddEdge(1, 2);

        // Act
        var hasEdges = graph.NodeHasAnyEdges(1);

        // Assert
        Assert.IsTrue(hasEdges);
    }

    /// <summary>
    ///     <para>
    ///         Tests that everything is okay if we remove the entire graph and then add new nodes and edges to it.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void GraphRemoveNode_TestRemoveEntireGraph_AddNewNodesAndEdges()
    {
        // Arrange
        var graph = new Graph<int, string>();
        graph.AddNode(1, "A");
        graph.AddNode(2, "B");
        graph.AddNode(3, "C");
        graph.AddEdge(1, 2);
        graph.AddEdge(2, 3);
        graph.AddEdge(1, 3);
        graph.AddEdge(3, 1);

        // Act
        graph.RemoveNode(1);
        graph.RemoveNode(2);
        graph.RemoveNode(3);

        // Assert
        Assert.AreEqual(0, graph.NodeCount);
    }

    /// <summary>
    ///     <para>
    ///         Tests that we fail to enumerate dependents of a node that does not exist.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void GraphEnumerateNodeDependents_TestNodeDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.EnumerateNodeDependents(1).ToList());
    }
    
    /// <summary>
    ///     <para>
    ///         Tests that we fail to enumerate dependees of a node that does not exist.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void GraphEnumerateNodeDependees_TestNodeDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.EnumerateNodeDependees(1).ToList());
    }

    /// <summary>
    ///     <para>
    ///         Tests that we fail to get the value of a node that does not exist.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void GraphGetValue_TestNodeDoesNotExist_ThrowsInvalidOperationException()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        Assert.ThrowsExactly<InvalidOperationException>(() => graph.GetValue(1));
    }

    /// <summary>
    ///     <para>
    ///         Tests that we don't fail to remove a node that does not exist.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void GraphRemoveNode_TestNodeDoesNotExist_DoesNotThrow()
    {
        // Arrange
        var graph = new Graph<int, string>();

        // Act
        graph.RemoveNode(1);

        // Assert
        Assert.AreEqual(0, graph.NodeCount);
    }
}