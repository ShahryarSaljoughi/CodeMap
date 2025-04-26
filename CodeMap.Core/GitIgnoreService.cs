namespace CodeMap.Core;

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
        return paths.Where(p => !Ignore.IsIgnored(p) && !BinaryDetector.IsBinary(p)).ToList();
    }
}

public class BinaryDetector
{
    public static bool IsBinary(string path)
    {
        if (string.IsNullOrWhiteSpace(path)) return false;
        if (!File.Exists(path) && Directory.Exists(path)) return false; // it's a directory

        using (StreamReader stream = new StreamReader(path))
        {
            int ch;
            while ((ch = stream.Read()) != -1)
            {
                if (IsControlChar(ch))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsControlChar(int ch)
    {
        return (ch > Chars.NUL && ch < Chars.BS)
               || (ch > Chars.CR && ch < Chars.SUB);
    }

    public static class Chars
    {
        public static char NUL = (char)0; // Null char
        public static char BS = (char)8; // Back Space
        public static char CR = (char)13; // Carriage Return
        public static char SUB = (char)26; // Substitute
    }
}