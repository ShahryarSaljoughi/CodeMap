// See https://aka.ms/new-console-template for more information

using CodeMap.Core;
using System.CommandLine;

var projectArgument = new Argument<DirectoryInfo>(
    name: "project path", 
    getDefaultValue: () => new DirectoryInfo("."),
    description: "root of your project, or folder you want to get a flat textual map of");

var gitIgnorePathOptions = new Option<FileInfo?>(
    name: "--ignore", 
    description: "gitignore-style file to skip folders and files.");

var outputOption= new Option<FileInfo?>(
    name: "--output",
    description: "specify a file path to store output", getDefaultValue:() => new FileInfo($"./codemap-{DateTimeOffset.Now.Ticks}.txt"));

var rootCommand = new RootCommand("CodeMap gives a map of your multi-file structured project within a single text file.");

rootCommand.AddArgument(projectArgument);
rootCommand.AddOption(gitIgnorePathOptions);
rootCommand.AddOption(outputOption);

rootCommand.SetHandler(async (projectPath, gitIgnore, outputPath) =>
{
    var gitIgnoreService = new GitIgnoreService(gitIgnore?.FullName ?? projectPath.FullName);
    var processor = new Processor(projectPath.FullName, gitIgnoreService, new TreeBuilder(gitIgnoreService));
    var result = await processor.TextualDirectory();
    await using var writer = new StreamWriter(outputPath.FullName);
    await writer.WriteLineAsync(result);
}, projectArgument, gitIgnorePathOptions, outputOption);

return await rootCommand.InvokeAsync(args);
