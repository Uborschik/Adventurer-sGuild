using Godot;
using System;
using System.Collections.Generic;

namespace AdventurersGuild.Core;

public static class NodeExtensions
{
    public static IEnumerable<Node> Descendants(this Node node, bool includeSelf = false, bool ownedOnly = false)
    {
        if (node == null) yield break;

        var stack = new Stack<Node>();
        stack.Push(node);

        while (stack.Count > 0)
        {
            var current = stack.Pop();

            if ((includeSelf || !ReferenceEquals(current, node))
                && (!ownedOnly || current.Owner != null))
            {
                yield return current;
            }

            var children = current.GetChildren();

            for (int i = children.Count - 1; i >= 0; i--)
                stack.Push(children[i]);
        }
    }

    public static T GetInstance<T>(this Node node, bool includeSelf = false) where T : Node
    {
        ArgumentNullException.ThrowIfNull(node);

        if (node.TryGetInstance<T>(out var result, includeSelf))
            return result;

        throw new InvalidOperationException(
            $"Узел типа {typeof(T).Name} не найден среди потомков {node.Name}");
    }

    public static bool TryGetInstance<T>(this Node node, out T result, bool includeSelf = false) where T : Node
    {
        result = null;
        if (node == null) return false;

        foreach (var descendant in node.Descendants(includeSelf))
        {
            if (descendant is T typed)
            {
                result = typed;
                return true;
            }
        }
        return false;
    }

    public static List<T> GetAllInstances<T>(this Node node, bool includeSelf = false, Func<T, bool> predicate = null) where T : Node
    {
        var results = new List<T>();
        if (node == null) return results;

        foreach (var descendant in node.Descendants(includeSelf))
        {
            if (descendant is T typed && (predicate == null || predicate(typed)))
                results.Add(typed);
        }
        return results;
    }
}