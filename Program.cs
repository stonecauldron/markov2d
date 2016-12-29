class Program
{
    static void Main(string[] args)
    {
        string inputFile = args[0];
        string outputFile = args[1];

        string mapText = System.IO.File.ReadAllText(inputFile);

        int[,] kernel = {{0, 1, 1, 1},
						 {1, 1, 1, 0},
						 {1, 1, 1, 0},
                         {1, 0, 0, 1}};

        MarkovChain chain = MarkovChain.CreateChain(mapText, kernel);

        string generatedMap = chain.GenerateMap(40, 40);

        System.IO.File.WriteAllText(outputFile, generatedMap);
    }
}
