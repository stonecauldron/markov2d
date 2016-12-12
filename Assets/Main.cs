using UnityEngine;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
		string mapText = 
			System.IO.File.ReadAllText("Assets/data/processed/subkernels-map.txt");
		MapMatrix map = new MapMatrix(mapText);
		int[,] kernel = {{0, 1, 1},
						 {1, 1, 0},
						 {1, 0, 1}};

		Kernel k = new Kernel(kernel);
		MarkovChain chain = map.CreateChain(k);
		System.IO.File.WriteAllText(@"Assets/test.txt", chain.GenerateMap(100,100).ToString());
	}
}
