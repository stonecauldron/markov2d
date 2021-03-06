﻿using System;
using System.Linq;
using System.Collections.Generic;

class MapMatrix
{
    internal char[,] data { get; private set; }

    int N, M;

    internal MapMatrix(string mapString)
    {
        string[] lines = mapString.Split('\n');

        int nbRows = lines.Length, nbCols = lines[0].Length;
        data = new char[nbRows, nbCols];

        for (int i = 0; i < nbRows; i++)
        {
            for (int j = 0; j < nbCols; j++)
            {
                data[i, j] = lines[i].ElementAt(j);
            }
        }
        N = nbRows;
        M = nbCols;
    }

    internal MapMatrix(int width, int height)
    {
        data = new char[width,height];
        N = width;
        M = height;
    }

    internal void Learn(MarginalsTable mt, Kernel k)
    {
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < M; j++)
            {
                foreach (Kernel kernel in k.SubKernels())
                {
                    PredecessorMatrix preds = GetPredecessors(kernel.matrix, i, j);
                    char tile = data[i,j];
                    mt.IncrementCount(preds, tile);
                }
            }
        }
    }

    public override string ToString()
    {
        string result = "";
        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < M; j++)
            {
                result += data[i,j];
            }
            result += '\n';
        }
        return result;
    }

    internal PredecessorMatrix GetPredecessors(int[,] kernel, int x, int y)
    {
        int nbRows = kernel.GetLength(0), nbCols = kernel.GetLength(1);
        PredecessorMatrix preds = new PredecessorMatrix(nbRows);

        for (int i = 0; i < nbRows; i++)
        {
            for (int j = 0; j < nbCols; j++)
            {
                preds.data[i, j] = '0';
                if (kernel[i, j] == 1)
                {
                    int predXIndex = x - i;
                    int predYIndex = y - j;
                    if (predXIndex >= 0 && predXIndex < N &&
                        predYIndex >= 0 && predYIndex < M)
                    {
                        preds.data[i, j] = this.data[predXIndex, predYIndex];
                    }
                }
            }
        }
        return preds;
    }
}

public class MarkovChain
{
    MapMatrix map;
    MarginalsTable marginals;
    Kernel k;

    public static MarkovChain CreateChain(List<string> mapTexts, int[,] kernel)
    {
        if (mapTexts.Count() == 0)
        {
            throw new ArgumentException("Please provide at least one map");
        }

        MarginalsTable mt = new MarginalsTable();
        Kernel k = new Kernel(kernel);

        List<MapMatrix> maps = mapTexts.Select(x => new MapMatrix(x)).ToList();
        maps.ForEach(x => x.Learn(mt, k));

        return new MarkovChain(k, mt);
    }

    internal MarkovChain(Kernel k, MarginalsTable mt)
    {
        this.k = k;
        marginals = mt;
    }

    public string GenerateMap(int width, int height)
    {
        map = new MapMatrix(width, height);
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map.data[i,j] = NextTile(i, j);
            }
        }

        return map.ToString();
    }

    char NextTile(int x, int y)
    {
        foreach (Kernel kernel in k.SubKernels())
        {
            PredecessorMatrix preds = map.GetPredecessors(kernel.matrix, x, y);
            if (!marginals.table.ContainsKey(preds))
            {
                // if it does not contain this predecessor take a smaller one
                continue;
            }

            TileCounts tileCounts = marginals.table[preds];
            int totalCount = tileCounts.list.Select(tc => tc.count).Sum();

            Random r = new Random();
            int randomTileIndex = r.Next(1, totalCount + 1);

            int total = 0;
            for (int i = 0; i < tileCounts.Count(); i++)
            {
                TileCountPair tileCount = tileCounts.list[i];
                total += tileCount.count;
                if (randomTileIndex <= total)
                {
                    return tileCount.tile;
                }
            }
        }
        return 'X'; // something wrong happened
    }
}

public class Kernel
{
    internal int[,] matrix;
    int numberPredecessors;

    static Dictionary<Kernel,List<Kernel>> memoizeDict = new Dictionary<Kernel,List<Kernel>>();

    public Kernel(int[,] m)
    {
        matrix = (int[,]) m.Clone();

        numberPredecessors = matrix.Cast<int>()
                                .Where(x => x == 1)
                                .Count();
    } 

    internal List<Kernel> SubKernels()
    {
        if (memoizeDict.ContainsKey(this))
        {
            return memoizeDict[this];
        }

        if (matrix.Cast<int>().All(x => x == 0))
        {
            List<Kernel> zeros = new List<Kernel>();
            zeros.Add(this);
            return zeros;
        }


        // find largest distance index
        int d = GetHighestIndexDistance();
        // get all the indices at this distance that are equal to one
        List<Index> indices = GetIndicesAtDistance(d)
                              .Where(index => matrix[index.i, index.j] == 1)
                              .ToList();
        List<Kernel> subList = new List<Kernel>();

        foreach (var index in indices)
        {
            int[,] newMatrix = (int[,]) matrix.Clone();
            newMatrix[index.i, index.j] = 0;
            Kernel k = new Kernel(newMatrix);
            subList = subList.Union(k.SubKernels()).ToList();
        }

        List<Kernel> result = new List<Kernel>();
        result.Add(this);
        result = result.Union(subList).ToList();

        // sort result by number of predecessors
        result = result.OrderByDescending(k => k.numberPredecessors).ToList();

        // memoization
        memoizeDict.Add(this, result);

        return result;
    }

    List<Index> GetIndicesAtDistance(int d)
    {
        List<Index> result = new List<Index>();
        result.Add(new Index(d, d));

        for (int i = 1; i <= d; i++)
        {
            result.Add(new Index(d - i, d));
            result.Add(new Index(d, d - i));
        }

        return result;
    }

    int GetHighestIndexDistance()
    {
        int dimension = matrix.GetLength(0);

        for (int d = dimension - 1; d > 0; d--)
        {
            List<Index> indices = GetIndicesAtDistance(d);
            int nbElements = indices.Select(index => matrix[index.i, index.j])
                                    .Where(v => v == 1)
                                    .Count();
            if (nbElements > 0)
            {
                return d;
            }
        }

        return 0;
    }

    Kernel Clone()
    {
        int[,] newMatrix = (int[,]) matrix.Clone();
        return new Kernel(newMatrix);
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as Kernel);
    }

    public bool Equals(Kernel m)
    {
        if (Object.ReferenceEquals(m, null))
        {
            return false;
        }
        if (Object.ReferenceEquals(this, m))
        {
            return true;
        }
        return this.matrix.GetLength(0) == m.matrix.GetLength(0) &&
            this.matrix.GetLength(1) == m.matrix.GetLength(1) &&
            this.matrix.Cast<int>().SequenceEqual(m.matrix.Cast<int>());
    }

    public static bool operator == (Kernel lhs, Kernel rhs)
    {
        if (Object.ReferenceEquals(lhs, null))
        {
            if (Object.ReferenceEquals(rhs, null))
            {
                return true;
            }
            return false;
        }
        return lhs.Equals(rhs);
    }

    public static bool operator != (Kernel lhs, Kernel rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode()
    {
        return matrix.Cast<int>().Where(x => x == 1).Count();
    }

    public override string ToString()
    {
        string result = "";
        int dimension = matrix.GetLength(0);
        for (int i = 0; i < dimension; i++)
        {
            result += "{ ";
            for (int j = 0; j < dimension; j++)
            {
                result += matrix[i,j] + " "; 
            }
            result += "}\n";
        }
        return result;
    }
}

class KernelComparer : IEqualityComparer<Kernel>
{
    public bool Equals(Kernel x, Kernel y)
    {
        return x.matrix.Rank == x.matrix.Rank &&
        Enumerable.Range(0, x.matrix.Rank)
        .All(dimension => x.matrix.GetLength(dimension) == y.matrix.GetLength(dimension)) &&
        x.matrix.Cast<double>().SequenceEqual(y.matrix.Cast<double>());
    }

    public int GetHashCode(Kernel obj)
    {
        return obj.matrix.Cast<int>().Where(x => x == 1).Count();
    }
}

class PredecessorMatrix
{
    internal char[,] data { get; private set; }
    int size { get; }

    internal PredecessorMatrix(int size)
    {
        this.data = new char[size, size];
        this.size = size;
    }

    public override bool Equals(object obj)
    {
        return this.Equals(obj as PredecessorMatrix);
    }

    public bool Equals(PredecessorMatrix m)
    {
        if (Object.ReferenceEquals(m, null))
        {
            return false;
        }
        if (Object.ReferenceEquals(this, m))
        {
            return true;
        }
        return this.data.GetLength(0) == m.data.GetLength(0) &&
            this.data.GetLength(1) == m.data.GetLength(1) &&
            this.data.Cast<char>().SequenceEqual(m.data.Cast<char>());
    }

    public static bool operator ==(PredecessorMatrix lhs, PredecessorMatrix rhs)
    {
        if (Object.ReferenceEquals(lhs, null))
        {
            if (Object.ReferenceEquals(rhs, null))
            {
                return true;
            }
            return false;
        }
        return lhs.Equals(rhs);
    }

    public static bool operator !=(PredecessorMatrix lhs, PredecessorMatrix rhs)
    {
        return !(lhs == rhs);
    }

    public override int GetHashCode()
    {
        int result = 0;
        foreach (char c in data)
        {
            result += c.GetHashCode();
        }
        return result;
    }

    public override string ToString()
    {
        string str = "";
        for (int i = 0; i < size; i++)
        {
            str += "{";
            for (int j = 0; j < size; j++)
            {
                str += this.data[i, j] + " ";
            }
            str += "}\n";
        }
        return str;
    }
}

class MarginalsTable
{
    internal Dictionary<PredecessorMatrix, TileCounts> table { get; }

    internal MarginalsTable()
    {
        table = new Dictionary<PredecessorMatrix, TileCounts>();
    }

    internal void IncrementCount(PredecessorMatrix pred, char tile)
    {
        if (table.ContainsKey(pred))
        {
            var listPairs = table[pred];
            listPairs.IncrementCount(tile);
        }
        else
        {
            table[pred] = new TileCounts();
            table[pred].Add(new TileCountPair(tile, 1));
        }
    }

    public override string ToString()
    {
        string res = "";
        foreach (KeyValuePair<PredecessorMatrix, TileCounts> keyValuePair in table)
        {
            res += keyValuePair.Key.ToString();
            res += keyValuePair.Value.ToString();
            res += "\n===========================\n";
        }
        return res;
    }
}

class TileCounts
{
    internal List<TileCountPair> list { get; private set; }

    internal TileCounts()
    {
        list = new List<TileCountPair>();
    }

    internal int Count()
    {
        return list.Count;
    }

    internal void IncrementCount(char tile)
    {
        int index = list.FindIndex(x => x.tile == tile);
        if (index >= 0)
        {
            TileCountPair tc = list[index];
            list[index] = tc.IncrementCount();
        }
        else
        {
            list.Add(new TileCountPair(tile, 1));
        }
    }

    internal void Add(TileCountPair tc)
    {
        list.Add(tc);
    }

    public override string ToString()
    {
        string result = "{ ";
        foreach (var tc in list)
        {
            result += tc.ToString();
            result += " ";
        }
        result += "}";
        return result;
    }
}

struct TileCountPair
{
    internal char tile { get; }
    internal int count { get; }

    internal TileCountPair(char t, int c)
    {
        tile = t;
        count = c;
    }

    internal TileCountPair IncrementCount()
    {
        return new TileCountPair(tile, count + 1);
    }

    public override string ToString()
    {
        return "(" + tile + ", " + count + ")";
    }
}

struct Index
{
    internal int i, j;

    internal Index(int x, int y)
    {
        i = x;
        j = y;
    }
    public override string ToString()
    {
        return i + ", " + j;
    }
}