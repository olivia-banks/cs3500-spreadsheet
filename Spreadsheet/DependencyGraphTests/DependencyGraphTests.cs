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
}