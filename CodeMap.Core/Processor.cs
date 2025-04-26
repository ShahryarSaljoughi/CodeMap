using CodeMap.Core.Exceptions;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CodeMap.Core;

public class Processor(string path, GitIgnoreService gitIgnoreService, TreeBuilder treeBuilder)
{

    public async Task<string> TextualDirectory()
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new DirectoryNotFound();
        }

        var tree = await treeBuilder.BuildTree(path);
        StringBuilder result = new StringBuilder();
        result.AppendLine(GetOverallStructure(tree));
        result.AppendLine(await GetFileContents(tree));
        return result.ToString();
    }

    private async Task<string> GetFileContents(Node tree)
    {
        StringBuilder result = new StringBuilder();
        foreach (var node in tree.Where(t => t.Type == NodeType.File))
        {
            var content = await File.ReadAllTextAsync(node.FullPath);
            result.AppendLine(@$"

{node.FullPath}:
{content}
");
        }

        return result.ToString();
    }

    private string GetOverallStructure(Node tree)
    {
        StringBuilder result = new StringBuilder();
        PrintTree(tree);
        return result.ToString();
        void PrintTree(Node node, string indent = "-")
        {
            result.AppendLine($"{indent}{node.Name}");
            foreach (var nodeChild in node.Children)
            {
                PrintTree(nodeChild, indent + "-");
            }
        }
    }
}