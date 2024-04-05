using CodeMap.Core.Exceptions;
using System.IO;
using System.Text.RegularExpressions;

namespace CodeMap.Core;

public class Processor(string path)
{
    private string? _gitIgnorePath = Directory.GetFiles(path).FirstOrDefault(p => p.Contains(".gitignore"));
    public async Task<string> TextualDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new DirectoryNotFound();
        }

        var tree = await BuildTree(path);



        var directories = await GetDirectories(path);
        var files = GetFiles(path);
        
        var result = string.Join(Environment.NewLine, directories);
        return result;
    }

    private async Task<List<string>> ApplyGitIgnoreAsync(List<string> paths)
    {
        if (string.IsNullOrWhiteSpace(_gitIgnorePath)) return paths;
        var ignore = new Ignore.Ignore();
        var rules = await File.ReadAllLinesAsync(_gitIgnorePath);
        ignore.Add(rules);
        paths = paths.Select(p => p.Replace(@"\", "/")).ToList();
        return paths.Where(p => !ignore.IsIgnored(p)).ToList();
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

    private string NormalizeDirectoryPath(string path)
    {
        path = path.Replace(@"\", "/");
        if (!DoesEndInSlash(path))
            path = $"{path}/";
        return path;
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

        if (!string.IsNullOrEmpty(_gitIgnorePath))
        {
            directories = await ApplyGitIgnoreAsync(directories);
        }

        return directories;
    }

    private List<string> GetFiles(string path)
    {
        return Directory.GetFiles(path, "*", SearchOption.AllDirectories).ToList();
    }

    private async Task<Node> BuildTree(string rootPath)
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
        files = await ApplyGitIgnoreAsync(files);
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
        directories = await ApplyGitIgnoreAsync(directories);
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
}