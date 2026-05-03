using NUnit.Framework.Internal.Commands;
using NUnit.Framework.Legacy;

namespace TradingTests;

using NUnit.Framework;
using System;
using System.Collections.Generic;

[TestFixture]
public class GraphDfsBfsAlgoTests
{
    [Test]
    public void Dfs_SingleNode_ReturnsOnlyStart()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new()
        };

        var result = GraphDfsBfsAlgo.Traverse(graph, 1);

        CollectionAssert.AreEqual(new[] { 1 }, result);
    }

    [Test]
    public void Dfs_LinearGraph_ReturnsNodesInDepthOrder()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new() { 2 },
            [2] = new() { 3 },
            [3] = new() { 4 },
            [4] = new()
        };

        var result = GraphDfsBfsAlgo.Traverse(graph, 1);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3, 4 }, result);
    }

    [Test]
    public void Dfs_BranchingGraph_ReturnsDepthFirstOrder()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new() { 2, 3 },
            [2] = new() { 4, 5 },
            [3] = new() { 6 },
            [4] = new(),
            [5] = new(),
            [6] = new()
        };

        var result = GraphDfsBfsAlgo.Traverse(graph, 1);
        CollectionAssert.AreEquivalent(new[] { 1, 2, 4, 5, 3, 6 }, result);
    }

    [Test]
    public void Dfs_GraphWithCycle_DoesNotLoopForever()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new() { 2 },
            [2] = new() { 3 },
            [3] = new() { 1 }
        };

        var result = GraphDfsBfsAlgo.Traverse(graph, 1);

        CollectionAssert.AreEquivalent(new[] { 1, 2, 3 }, result);
    }

    [Test]
    public void Dfs_DisconnectedGraph_OnlyVisitsReachableNodes()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new() { 2 },
            [2] = new(),
            [3] = new() { 4 },
            [4] = new()
        };

        var result = GraphDfsBfsAlgo.Traverse(graph, 1);

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, result);
    }

    [Test]
    public void Dfs_StartNodeMissing_ThrowsArgumentException()
    {
        var graph = new Dictionary<int, List<int>>
        {
            [1] = new() { 2 },
            [2] = new()
        };

        Assert.Throws<ArgumentException>(() => GraphDfsBfsAlgo.Traverse(graph, 99));
    }

    [Test]
    public void Dfs_NullGraph_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => GraphDfsBfsAlgo.Traverse(null!, 1));
    }
}

public static class GraphDfsBfsAlgo
{
    public static List<int> Traverse2(Dictionary<int, List<int>> graph, int start)
    {
        if (graph == null) throw new ArgumentNullException("graph is empty");
        Stack<int> stack = new Stack<int>();
        HashSet<int> visited = new HashSet<int>();
        if (graph.ContainsKey(start))
            stack.Push(start);
        else throw new ArgumentException($"node {start} does not exist");
        while (stack.Count > 0)
        {
            var pop = stack.Pop();
            visited.Add(pop);
            if (graph.TryGetValue(pop, out var neighbors))
            {
                if (neighbors?.Count > 0)
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                            stack.Push(neighbor);
                    }
            }
            else throw new ArgumentException($"node {pop} does not exist");
        }

        var traverse = visited.ToList();
        Console.WriteLine(string.Join(", ", traverse));
        return traverse;
    }
// BFS
    public static List<int> Traverse(Dictionary<int, List<int>> graph, int start)
    {
        if (graph == null) throw new ArgumentNullException("graph is empty");
        Queue<int> q = new Queue<int>();
        HashSet<int> visited = new HashSet<int>();
        if (graph.ContainsKey(start))
            q.Enqueue(start);
        else throw new ArgumentException($"node {start} does not exist");
        while (q.TryDequeue(out int pop))
        {
            visited.Add(pop);
            if (graph.TryGetValue(pop, out var neighbors))
            {
                if (neighbors?.Count > 0)
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                            q.Enqueue(neighbor);
                    }
            }
            else throw new ArgumentException($"node {pop} does not exist");
        }

        var traverse = visited.ToList();
        Console.WriteLine(string.Join(", ", traverse));
        return traverse;
    }


    public static List<int> TraverseDeepFirst(Dictionary<int, List<int>> graph, int start)
    {
        if (graph == null) throw new ArgumentNullException("graph is empty");
        Stack<int> stack = new Stack<int>();
        Stack<int> deepFirst = new Stack<int>();
        HashSet<int> visited = new HashSet<int>();
        if (graph.ContainsKey(start))
            stack.Push(start);
        else throw new ArgumentException($"node {start} does not exist");
        while (stack.Count > 0)
        {
            var pop = stack.Pop();
            visited.Add(pop);
            deepFirst.Push(pop);
            if (graph.TryGetValue(pop, out var neighbors))
            {
                if (neighbors?.Count > 0)
                    foreach (var neighbor in neighbors)
                    {
                        if (!visited.Contains(neighbor))
                            stack.Push(neighbor);
                    }
            }
            else throw new ArgumentException($"node {pop} does not exist");
        }

        var traverse = new List<int>();
        while(deepFirst.TryPop(out int v))
            traverse.Add(v);
        Console.WriteLine(string.Join(", ", traverse));
        return traverse;
    }


    private static void TraverseStack(Stack<int> stack, Dictionary<int, List<int>> graph, int start, HashSet<int> visited)
    {
        if (visited.Contains(start)) return;
        if (graph.TryGetValue(start, out var nodes))
        {
            if (nodes != null && nodes.Count > 0)
            {
                foreach (var node in nodes)
                {
                    stack.Push(node);
                    TraverseStack(stack, graph, node, visited);
                }
            }
        }

        stack.Push(start);
        visited.Add(start);
    }

    public static List<int> TraverseRecursive(Dictionary<int, List<int>> graph, int start)
    {
        var path = new HashSet<int>();
        Traverse(path, graph, start);
        Console.WriteLine(string.Join(", ", path));

        return path.ToList();
    }

    private static void Traverse(HashSet<int> path, Dictionary<int, List<int>> graph, int start)
    {
        if (graph == null) throw new ArgumentNullException();

        if (path.Contains(start))
            return;
        if (graph.TryGetValue(start, out var nodes))
        {
            path.Add(start);
            if (nodes.Count > 0)
            {
                foreach (var n in nodes)
                {
                    Traverse(path, graph, n);
                }
            }
        }
        else throw new ArgumentException($"no node named {start}");
    }
}