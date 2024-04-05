namespace CodeMap.Core;

public class Node
{
    public required NodeType Type { get; init; }
    public required string FullPath { get; init; }
    public required string Name { get; init; }
    public List<Node> Children { get; set; } = new();
}

public enum NodeType
{
    Directory, 
    File
}