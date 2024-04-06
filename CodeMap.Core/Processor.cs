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

    private void NormalizeDirectoryPaths(List<string> paths)
    {
        for (var i = 0; i < paths.Count; i++)
        {
            paths[i] = paths[i].Replace(@"\", "/");
            if (!DoesEndInSlash(paths[i]))
            {
                paths[i] = $"{paths[i]}/";
            }
        }
    }
    bool DoesEndInSlash(string path)
    {
        var endsInSlashPattern = @"^.*/$";
        return new Regex(endsInSlashPattern).IsMatch(path);
    }

    private async Task<List<string>> GetDirectories(string path)
    {
        var directories = new List<string>() { path };
        var currentIndex = 0;
        do
        {
            directories.AddRange(Directory.GetDirectories(directories[currentIndex]));
            currentIndex++;
        } while (currentIndex < directories.Count - 1);

        NormalizeDirectoryPaths(directories);


        directories = await gitIgnoreService.ApplyGitIgnoreAsync(directories);


        return directories;
    }

    private List<string> GetFiles(string path)
    {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
    }


}

public class TreeBuilder(GitIgnoreService gitIgnoreService)
{

    public async Task<Node> BuildTree(string rootPath)
    {
        var isFile = File.Exists(rootPath);
        rootPath = NormalizeDirectoryPath(rootPath);
        var root = new Node()
        {
            Type = isFile ? NodeType.File : NodeType.Directory,
            FullPath = rootPath,
            Name = Path.GetDirectoryName(rootPath)
        };
        root.Children.AddRange(await BuildTreeChildren(rootPath));
        return root;
    }

    private async Task<List<Node>> BuildTreeChildren(string rootPath)
    {
        var files = Directory.GetFiles(rootPath).ToList();
        files = await gitIgnoreService.ApplyGitIgnoreAsync(files);
        var fileNodes = files.Select(f =>
        {
            var node = new Node()
            {
                Type = NodeType.File,
                FullPath = f,
                Name = Path.GetFileName(f),
            };
            return node;
        });
        var directories = Directory.GetDirectories(rootPath).Select(NormalizeDirectoryPath).ToList();
        directories = await gitIgnoreService.ApplyGitIgnoreAsync(directories);
        var directoryNodes = directories.Select(d =>
        {
            var node = new Node()
            {
                Type = NodeType.Directory,
                Name = Path.GetDirectoryName(d) ?? d,
                FullPath = d,
            };
            return node;
        }).ToList();

        foreach (var directoryNode in directoryNodes)
        {
            directoryNode.Children.AddRange(await BuildTreeChildren(directoryNode.FullPath));
        }
        return fileNodes.Concat(directoryNodes).ToList();
    }

    private string NormalizeDirectoryPath(string path)
    {
        path = path.Replace(@"\", "/");
        if (!DoesEndInSlash(path))
            path = $"{path}/";
        return path;
    }

    bool DoesEndInSlash(string path)
    {
        var endsInSlashPattern = @"^.*/$";
        return new Regex(endsInSlashPattern).IsMatch(path);
    }

}

public class GitIgnoreService
{
    private string? GitIgnorePath { get; set; }
    private Ignore.Ignore Ignore { get; set; }
    private bool IsInitialized { get; set; }
    public GitIgnoreService(string ignoreFilePath)
    {
        GitIgnorePath =
            File.Exists(ignoreFilePath) ?
                ignoreFilePath
                : Directory.Exists(ignoreFilePath) ?
                    Directory.GetFiles(ignoreFilePath).FirstOrDefault(p => p.Contains(".gitignore"))
                    : null;
    }

    private async Task Initialize()
    {
        Ignore = new Ignore.Ignore();
        if (!string.IsNullOrWhiteSpace(GitIgnorePath))
        {
            var rules = await File.ReadAllLinesAsync(GitIgnorePath!);
            Ignore.Add(rules);
        }

        IsInitialized = true;
    }

    public async Task<List<string>> ApplyGitIgnoreAsync(List<string> paths)
    {
        if (!IsInitialized) await Initialize();
        if (string.IsNullOrWhiteSpace(GitIgnorePath)) return paths;

        paths = paths.Select(p => p.Replace(@"\", "/")).ToList();
        return paths.Where(p => !Ignore.IsIgnored(p)).ToList();
    }
}