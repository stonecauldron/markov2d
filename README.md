# markov2d
A command line tool for generating tile-maps with 2D Markov chains

## Installation 
You first need to install the [dotnet platform](https://www.microsoft.com/net/core/platform).

You can then clone the repo and install the dependencies
```
$ git clone https://github.com/stonecauldron/markov2d
$ cd markov2d
$ dotnet restore
```

## Usage
Here is an example usage:
```
$ dotnet run -n 250 -w -h 40 examples/training-data/text/*.txt
```
The only required argument is a least one text file containing the training data.
The general usage syntax can be described by the following:
```
dotnet run [flags] training-data
```

The tool accepts various command line options:
```
-n, --number              (Default: 100) The number of maps to generate.

-w, --width               (Default: 40) The width of the generated maps.

-h, --height              (Default: 50) The height of the generated maps.

-o, --output              (Default: examples/generated/text/) The directory
                            where to put the generated maps.
```

## Library use
Being a pure C# project the code in Markov2D can be used directly within Unity.
```csharp
// defines how many and which predecessors to take into account
int[,] kernel = {{0, 1, 1},
                 {1, 1, 1},
                 {1, 1, 1}};
                 
List<string> maps = ... // import your maps in string format

// Train a chain with the given maps and kernel
MarkovChain chain = MarkovChain.CreateChain(maps, kernel);

// generates a map in string format
string generatedMap = chain.GenerateMap(height, width);
```

## API
The only methods you will need to call are the following:
```csharp
MarkovChain MarkovChain.CreateChain(List<string> maps, int[,] kernel)
```
Trains a 2D chain on the maps dataset with the predecessor matrix defined by kernel.
Returns a Markov chain object.

```csharp
string chain.GenerateMap(int height, int width)
```
Generates a map with height `height` and width `width` based on the trained Markov chain `chain`.
