using System.Collections;
using System.Runtime.CompilerServices;

namespace CodeMap.Core;

public class Node: IEnumerable<Node>
{
    public required NodeType Type { get; init; }
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public List<Node> Children { get; set; } = new();

    public IEnumerator<Node> GetEnumerator()
    {
        yield return this;
        foreach (var child in Children)
        {
            var enumerator = child.GetEnumerator();
            while (enumerator.MoveNext())
            {
                yield return enumerator.Current;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}

public enum NodeType
{
    Directory, 
    File
}