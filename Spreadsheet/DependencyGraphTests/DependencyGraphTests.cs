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
    public void StressTest()
    {
        DependencyGraph dg = new();

        // A bunch of strings to use
        const int size = 200;
        var letters = new string[size];
        for (var i = 0; i < size; i++)
        {
            letters[i] = string.Empty + ((char)('a' + i));
        }

        // The correct answers
        HashSet<string>[] dependents = new HashSet<string>[size];
        HashSet<string>[] dependees = new HashSet<string>[size];
        for (var i = 0; i < size; i++)
        {
            dependents[i] = [];
            dependees[i] = [];
        }

        // Add a bunch of dependencies
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j++)
            {
                dg.AddDependency(letters[i], letters[j]);
                dependents[i].Add(letters[j]);
                dependees[j].Add(letters[i]);
            }
        }

        // Remove a bunch of dependencies
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 4; j < size; j += 4)
            {
                dg.RemoveDependency(letters[i], letters[j]);
                dependents[i].Remove(letters[j]);
                dependees[j].Remove(letters[i]);
            }
        }

        // Add some back
        for (var i = 0; i < size; i++)
        {
            for (var j = i + 1; j < size; j += 2)
            {
                dg.AddDependency(letters[i], letters[j]);
                dependents[i].Add(letters[j]);
                dependees[j].Add(letters[i]);
            }
        }

        // Remove some more
        for (var i = 0; i < size; i += 2)
        {
            for (var j = i + 3; j < size; j += 3)
            {
                dg.RemoveDependency(letters[i], letters[j]);
                dependents[i].Remove(letters[j]);
                dependees[j].Remove(letters[i]);
            }
        }

        // Make sure everything is right
        for (var i = 0; i < size; i++)
        {
            Assert.IsTrue(dependents[i].SetEquals(new HashSet<string>(dg.GetDependents(letters[i]))));
            Assert.IsTrue(dependees[i].SetEquals(new HashSet<string>(dg.GetDependees(letters[i]))));
        }
    }
}