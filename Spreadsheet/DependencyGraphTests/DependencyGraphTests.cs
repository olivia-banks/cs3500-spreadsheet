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
}