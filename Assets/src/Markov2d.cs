using System;
using System.Linq;
using System.Collections.Generic;

public class MapMatrix
{
    public char[,] data { get; private set; }

    int N, M;

    public MapMatrix(string mapString)
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

    public MapMatrix(int width, int height)
    {
        data = new char[width,height];
        N = width;
        M = height;
    }

    public MarkovChain CreateChain(Kernel k)
    {
        MarginalsTable mt = new MarginalsTable();

        for (int i = 0; i < N; i++)
        {
            for (int j = 0; j < M; j++)
            {
                foreach (int[,] kernel in k)
                {
                    PredecessorMatrix preds = GetPredecessors(kernel, i, j);
                    char tile = data[i,j];
                    mt.IncrementCount(preds, tile);
                }
            }
        }
        UnityEngine.Debug.Log(mt);
        return new MarkovChain(k, mt);
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

    internal MarkovChain(Kernel k, MarginalsTable mt)
    {
        this.k = k;
        marginals = mt;
    }

    public MapMatrix GenerateMap(int width, int height)
    {
        map = new MapMatrix(width, height);
        
        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                map.data[i,j] = NextTile(i, j);
            }
        }

        return map;
    }

    char NextTile(int x, int y)
    {
        foreach (int[,] kernel in k)
        {
            PredecessorMatrix preds = map.GetPredecessors(kernel, x, y);
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
        return 'X'; // something wrong happenned
    }
}

public class Kernel : System.Collections.IEnumerable
{
    internal int[,] matrix;
    internal int numberPredecessors;

    static Dictionary<Kernel,List<Kernel>> memoizeDict = new Dictionary<Kernel,List<Kernel>>();

    public Kernel(int[,] m)
    {
        matrix = (int[,]) m.Clone();

        numberPredecessors = matrix.Cast<int>()
                                .Where(x => x == 1)
                                .Count();
    } 

    public List<Kernel> SubKernels()
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

        List<Kernel> subList = new List<Kernel>();

        int dimension = matrix.GetLength(0);

        List<Kernel> result = new List<Kernel>();
        result.Add(this);
        result = result.Union(subList).ToList();

        // memoization
        memoizeDict.Add(this, result);

        return result;
    }

    public System.Collections.IEnumerator GetEnumerator()
    {
        yield return matrix;

        /**
        int dimension = matrix.GetLength(0);
        for (int i = dimension - 1;  i >= 0; i--)
        {
            for (int j = dimension - 1; j >= 0; j--)
            {
                int[,] newMatrix = (int[,]) matrix.Clone();
                if (newMatrix[i,j] == 1)
                {
                    newMatrix[i,j] = 0;
                    yield return newMatrix;
                }
            }
        }
        //*/
    }

    public int GetHighestIndexDistance()
    {
        int dimension = matrix.GetLength(0);

        for (int d = dimension - 1; d >= 0; d--)
        {
            // take care of diagonal case
            if (matrix[d,d] == 1)
            {
                return d;
            }
            for (int i = 0; i < d; i++)
            {
                if ((d - i != i) && matrix[d - i, i] == 1)
                {
                    return d;
                }
            }
        }
        return -1;
    }

    public Kernel Clone()
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

public class KernelComparer : IEqualityComparer<Kernel>
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
    public char[,] data { get; private set; }
    public int size { get; }

    public PredecessorMatrix(int size)
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
        return data[0, 0].GetHashCode();
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
    public Dictionary<PredecessorMatrix, TileCounts> table { get; }

    public MarginalsTable()
    {
        table = new Dictionary<PredecessorMatrix, TileCounts>();
    }

    public void IncrementCount(PredecessorMatrix pred, char tile)
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
    public List<TileCountPair> list { get; private set; }

    public TileCounts()
    {
        list = new List<TileCountPair>();
    }

    public int Count()
    {
        return list.Count;
    }

    public void IncrementCount(char tile)
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

    public void Add(TileCountPair tc)
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
    public char tile { get; }
    public int count { get; }

    public TileCountPair(char t, int c)
    {
        tile = t;
        count = c;
    }

    public TileCountPair IncrementCount()
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
    public int i, j;

    public Index(int x, int y)
    {
        i = x;
        j = y;
    }
    public override string ToString()
    {
        return i + ", " + j;
    }
}