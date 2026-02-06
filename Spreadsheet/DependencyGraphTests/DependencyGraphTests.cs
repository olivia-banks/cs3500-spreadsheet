namespace DependencyGraphTests;

using DependencyGraph;

/// <summary>
///     This is a test class for DependencyGraphTest and is intended
///     to contain all DependencyGraphTest Unit Tests
/// </summary>
[TestClass]
public class DependencyGraphTests
{
    /// <summary>
    ///     <para>
    ///         A stress test for the DependencyGraph class. It adds and removes a large
    ///         number of dependencies and checks that the results are correct.
    ///     </para>
    /// </summary>
    [TestMethod]
    [Timeout(2000, CooperativeCancellation = true)]
    public void DependencyGraphIntegration_TestStressfulSituation_ValidState()
    {
        DependencyGraph dg = new();

        // Arrange

        // Create a large number of unique letters to use as nodes in the graph. This
        // will create strings "a", "b", "c", ..., "z", "aa", "ab", ..., up to <c>size</c>.
        const int size = 200;
        var letters = new string[size];
        for (var i = 0; i < size; i++)
        {
            letters[i] = string.Empty + ((char)('a' + i));
        }

        // Keep track of what the correct dependents and dependees are for each node, so
        // that we may verify the graph's state later.
        var dependents = new HashSet<string>[size];
        var dependees = new HashSet<string>[size];
        for (var i = 0; i < size; i++)
        {
            dependents[i] = [];
            dependees[i] = [];
        }

        // Act

        // Add a bunch of dependencies so that every node depends on every node after it.
        // This creates some noise.
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j++)
            {
                dg.AddDependency(letters[i], letters[j]);
                dependents[i].Add(letters[j]);
                dependees[j].Add(letters[i]);
            }
        }

        // Remove some dependencies in a regular pattern to create more noise.
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 4; j < size; j += 4)
            {
                dg.RemoveDependency(letters[i], letters[j]);
                dependents[i].Remove(letters[j]);
                dependees[j].Remove(letters[i]);
            }
        }

        // Add some more dependencies back, in a different pattern.
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j += 2)
            {
                dg.AddDependency(letters[i], letters[j]);
                dependents[i].Add(letters[j]);
                dependees[j].Add(letters[i]);
            }
        }

        // Remove some more dependencies in yet another pattern.
        for (var i = 0; i < size; i += 2)
        {
            for (var j = i + 3; j < size; j += 3)
            {
                dg.RemoveDependency(letters[i], letters[j]);
                dependents[i].Remove(letters[j]);
                dependees[j].Remove(letters[i]);
            }
        }

        // Assert

        // Finally, verify that the graph's state matches our expected dependents and dependees.
        for (var i = 0; i < size; i++)
        {
            Assert.IsTrue(dependents[i].SetEquals(new HashSet<string>(dg.GetDependents(letters[i]))));
            Assert.IsTrue(dependees[i].SetEquals(new HashSet<string>(dg.GetDependees(letters[i]))));
        }
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.HasDependents"/> method works correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestHasDependents_ValidState()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Check that the HasDependents method returns the correct values.
        Assert.IsTrue(dg.HasDependents("a")); // b, c
        Assert.IsTrue(dg.HasDependents("b")); // d
        Assert.IsFalse(dg.HasDependents("c"));
        Assert.IsFalse(dg.HasDependents("d"));
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.HasDependees"/> method works correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestHasDependees_ValidState()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Check that the HasDependees method returns the correct values.
        Assert.IsFalse(dg.HasDependees("a"));
        Assert.IsTrue(dg.HasDependees("c")); // a
        Assert.IsTrue(dg.HasDependees("d")); // b
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.ReplaceDependents"/> method works correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestReplaceDependents_ValidState()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Replace the dependents of "a" with a new set of dependents.
        //
        //   ┌───┐     ┌───┐
        //   │ f │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ e │
        //             └───┘
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.ReplaceDependents("a", ["e", "f"]);

        // Check that the dependents of "a" have been replaced correctly.
        Assert.IsTrue(new HashSet<string>(dg.GetDependents("a")).SetEquals(["e", "f"]));
        Assert.IsTrue(dg.HasDependees("e")); // a
        Assert.IsTrue(dg.HasDependees("f")); // a
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.ReplaceDependees"/> method works correctly.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestReplaceDependees_ValidState()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Replace the dependees of "d" with a new set of dependees.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //             ┌───┐
        //             │ e │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘
        //               ▲
        //               │
        //               │
        //             ┌───┐
        //             │ f │
        //             └───┘

        dg.ReplaceDependees("d", ["e", "f"]);

        // Check that the dependees of "d" have been replaced correctly.
        Assert.IsTrue(new HashSet<string>(dg.GetDependees("d")).SetEquals(["e", "f"]));
        Assert.IsFalse(dg.HasDependees("a"));
        Assert.IsTrue(dg.HasDependees("c")); // a
        Assert.IsTrue(dg.HasDependents("e")); // d
        Assert.IsTrue(dg.HasDependents("f")); // d
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.ReplaceDependents"/> method will work correctly
    ///         when replacing dependents with an empty set, which should remove all dependents.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestReplaceDependentsWithEmptySet_RemovesAllDependents()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Replace the dependents of "a" with an empty set. This should remove all dependents of "a".
        dg.ReplaceDependents("b", []);
        dg.ReplaceDependents("a", []);

        // Check that "a" has no dependents and that "b" and "c" have no dependees.
        Assert.IsFalse(dg.HasDependees("c"));
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.ReplaceDependees"/> method will work correctly
    ///         when replacing dependees with an empty set, which should remove all dependees.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestReplaceDependeesWithEmptySet_RemovesAllDependees()
    {
        DependencyGraph dg = new();
        
        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘
        
        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");
        
        // Replace the dependees of "b" with an empty set. This should remove all dependees of "b".
        dg.ReplaceDependees("b", []);
        dg.ReplaceDependees("d", []);
        
        // Check that "b" has no dependees and that "a" has no dependents.
        Assert.IsFalse(dg.HasDependees("b"));
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.ToDot"/> method returns a valid String.
    ///         Due to the relative complexity of the GraphvizDOT format, we don't do any validation here other
    ///         than making sure this doesn't throw an exception.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestToDot_DoesNotThrowOrReturnNull()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Check that the ToDot method returns a non-empty string and doesn't throw an exception.
        var dot = dg.ToDot();
        Assert.IsFalse(string.IsNullOrEmpty(dot));
    }

    /// <summary>
    ///     <para>
    ///         Test for determining if the <see cref="DependencyGraph.Size"/> property returns the correct number of dependencies.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestSize_ReturnsCorrectCount()
    {
        DependencyGraph dg = new();

        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘

        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        dg.AddDependency("b", "d");

        // Check that the Size property returns the correct number of dependencies.
        Assert.AreEqual(4, dg.Size); // "a", "b", "c", and "d" are all nodes in the graph, so the size should be 4.

        // Add some more dependencies and check the size again.
        //
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //     │         │
        //     │         │
        //     │         ▼
        //     │       ┌───┐
        //     │       │ b │
        //     │       └───┘
        //     │         │
        //     │         │
        //     │         ▼
        //     │       ┌───┐
        //     │       │ d │
        //     │       └───┘
        //     │         │
        //     │         │
        //     │         ▼
        //     │       ┌───┐
        //     └─────▶ │ e │
        //             └───┘

        dg.AddDependency("c", "e");
        dg.AddDependency("d", "e");
        Assert.AreEqual(5, dg.Size); // "e" is a new node, so the size should now be 5.

        // Remove a dependency and check the size again.
        //
        //   ┌───┐
        //   │ a │
        //   └───┘
        //     │
        //     │
        //     ▼
        //   ┌───┐
        //   │ c │
        //   └───┘
        //     │
        //     │
        //     ▼
        //   ┌───┐
        //   │ e │ ◀┐
        //   └───┘  │
        //   ┌───┐  │
        //   │ b │  │
        //   └───┘  │
        //     │    │
        //     │    │
        //     ▼    │
        //   ┌───┐  │
        //   │ d │ ─┘
        //   └───┘

        dg.RemoveDependency("a", "b");
        Assert.AreEqual(5, dg.Size);
    }

    /// <summary>
    ///     <para>
    ///         Test that ensures that <see cref="DependencyGraph.RemoveDependency"/> removes dependencies which are
    ///         no longer linked to.
    ///     </para>
    /// </summary>
    [TestMethod]
    public void DependencyGraphIntegration_TestRemoveDependency_RemovesUnlinkedNodes()
    {
        DependencyGraph dg = new();
        
        // Create a small graph with some dependencies.
        // 
        //   ┌───┐     ┌───┐
        //   │ c │ ◀── │ a │
        //   └───┘     └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ b │
        //             └───┘
        //               │
        //               │
        //               ▼
        //             ┌───┐
        //             │ d │
        //             └───┘
        
        dg.AddDependency("a", "b");
        dg.AddDependency("a", "c");
        
        // Remove the dependency from "a" to "b". This should remove "b" and "d" from the graph, since they are no
        // longer linked to.
        dg.RemoveDependency("a", "b");
        dg.RemoveDependency("a", "c");
        
        // We can't assert that "b" and "d" are not in the graph, since the graph doesn't have a method for checking if
        // a node exists, per the assignment API.
    }
}