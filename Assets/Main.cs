using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {

	// Use this for initialization
	void Start () {
		string mapText = 
			System.IO.File.ReadAllText("Assets/data/processed/elements/pallet-town-city.txt");
		MapMatrix map = new MapMatrix(mapText);
		int[,] kernel = {{0, 1, 1}, {1, 1, 1}, {0, 0, 0}};

		Kernel k = new Kernel(kernel);
		MarkovChain chain = map.CreateChain(k);
		Debug.Log(chain.GenerateMap(20,20));

		foreach (var foo in k.SubKernels())
		{
			Debug.Log(foo);
		}
	}
}
