using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string inputFile = args[0];
        string outputFile = args[1];

        string mapText = System.IO.File.ReadAllText(inputFile);

        int[,] kernel = {{0, 1, 1},
						 {1, 1, 1},
                         {1, 1, 1}};

        List<string> maps = new List<string>();
        maps.Add(mapText);
        maps.Add(mapText);
        maps.Add(mapText);
        maps.Add(mapText);
        maps.Add(mapText);

        MarkovChain chain = MarkovChain.CreateChain(maps, kernel);

        string generatedMap = chain.GenerateMap(40, 40);

        System.IO.File.WriteAllText(outputFile, generatedMap);
    }
}
