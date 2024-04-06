// See https://aka.ms/new-console-template for more information

using CodeMap.Core;

Console.WriteLine("Hello, World!");
var rootPath = "D:/Apps/Server";
var outputPath = $"./code-map-{DateTime.Now.Ticks}.txt";
var gitIgnoreService = new GitIgnoreService(rootPath);
var processor = new Processor(rootPath, gitIgnoreService, new TreeBuilder(gitIgnoreService));
var result = await processor.TextualDirectory("D:/Apps/Server");
await using var writer =  new StreamWriter(outputPath);
await writer.WriteLineAsync(result);
