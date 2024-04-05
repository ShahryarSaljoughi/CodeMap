using CodeMap.Core.Exceptions;

namespace CodeMap.Core;

public class Processor
{
    public Task<string> TextualDirectory(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new DirectoryNotFound();
        }

        var directories = new List<string>() { path };
        var currentIndex = 0;
        do
        {
            directories.AddRange(Directory.GetDirectories(directories[currentIndex]));
            currentIndex++;
        } while (currentIndex < directories.Count - 1);
        var result = string.Join(Environment.NewLine, directories);
        return Task.FromResult(result);
    }
}