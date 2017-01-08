using System.Collections.Generic;

class Program
{
    static void Main(string[] args)
    {
        string outputPrefix = "generated/";

        int[,] kernel = {{0, 1, 1},
						 {1, 1, 1},
                         {1, 1, 1}};
        int nbMaps = 100;

        List<string> maps = new List<string>();

        foreach (string file in args)
        {
            maps.Add(System.IO.File.ReadAllText(file));
        }

        MarkovChain chain = MarkovChain.CreateChain(maps, kernel);

        for (int i = 0; i < nbMaps; i++)
        {
            string generatedMap = chain.GenerateMap(50, 40);
            string outputFile = outputPrefix + i + ".txt";
            System.IO.File.WriteAllText(outputFile, generatedMap);
        }
    }
}
