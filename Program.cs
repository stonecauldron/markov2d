using System.Collections.Generic;
using CommandLine;

class Program
{
    static void Main(string[] args)
    {
        var result = CommandLine.Parser.Default.ParseArguments<Options>(args)
            .WithParsed(options =>
            {
                string outputPrefix = options.outputDirectory;

                int[,] kernel = {{0, 1, 1},
                                 {1, 1, 1},
                                 {1, 1, 1}};

                List<string> maps = new List<string>();

                foreach (string file in options.trainingData)
                {
                    maps.Add(System.IO.File.ReadAllText(file));
                }

                MarkovChain chain = MarkovChain.CreateChain(maps, kernel);

                for (int i = 0; i < options.nbMaps; i++)
                {
                    string generatedMap = chain.GenerateMap(options.mapHeight, options.mapWidth);
                    string outputFile = outputPrefix + i + ".txt";
                    System.IO.File.WriteAllText(outputFile, generatedMap);
                }
            });
    }
}

class Options
{
    [Option('n', "number", Default = 100, HelpText = "The number of maps to generate.")]
    public int nbMaps {get; set;}

    [Option('w', "width", Default = 40, HelpText = "The width of the generated maps.")]
    public int mapWidth {get; set;}

    [Option('h', "height", Default = 50, HelpText = "The height of the generated maps.")]
    public int mapHeight {get; set;}

    [Option('o', "output", Default = "examples/generated/text/", HelpText = "The directory where to put the generated maps")]
    public string outputDirectory {get; set;}

    [Value(0, MetaName = "training-maps", Required = true, HelpText = "The set of maps that will serve as training data")]
    public IEnumerable<string> trainingData {get; set;}
}
