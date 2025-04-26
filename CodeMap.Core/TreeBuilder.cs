using System.Text.RegularExpressions;

namespace CodeMap.Core;

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