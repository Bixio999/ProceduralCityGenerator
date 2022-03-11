using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProceduralCityGenerator : MonoBehaviour
{
    public GameObject terrain;
    public int RandomSeed = 0;
    public bool instantQuit = true;

    // Cellular Automata parameters
    // public int noise_density = 50;
    // public int iterations = 5;
    // public int neighboorThreshold = 4;

    // PERLIN NOISE PARAMETERS
    public float scalingFactor = 2;
    public int perlinNoiseOctaves = 5;
    public float perlinNoiseExponent = 4f;
    public float terrainWaterThreshold = 0.11f;

    // POPULATION DENSITY MAP PARAMETERS

    public float slopeThreshold = 0.1f;
    public int neighborhoodRadius = 2;
    public float popDensityWaterThreshold = 0.3f;
    public float mapBoundaryScale = 0.8f;

    public GameObject cube;

    void Start()
    {
        if (RandomSeed == 0)
            RandomSeed = (int) System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);

        Terrain t = terrain.GetComponent<Terrain> ();
		TerrainData td = t.terrainData;

		int x = td.heightmapResolution;
		int y = td.heightmapResolution;

        // Debug.LogFormat("x = {0}, y = {1}, z = {2}", x,y,z);
        
        float [,,] alphaMap = new float [td.alphamapWidth, td.alphamapHeight, td.alphamapLayers];

        float [,] map = InputMapGenerator.generatePerlinNoiseMap(x,y, scalingFactor, perlinNoiseOctaves, perlinNoiseExponent, terrainWaterThreshold);

        float [,] populationMap = InputMapGenerator.createPopulationDensityMap(map, x, y, slopeThreshold, neighborhoodRadius, popDensityWaterThreshold, mapBoundaryScale);

        // UnityEditor.EditorApplication.isPlaying = false;

        float mapsFactor = (float) Mathf.Min(td.alphamapHeight, x) / Mathf.Max(td.alphamapHeight, x); 

        int scaled_i, scaled_j;
        for (int i = 0; i < td.alphamapHeight; i++)
        {
            for (int j = 0; j < td.alphamapWidth; j++)
            {
                scaled_i =  Mathf.RoundToInt((float)i / mapsFactor);
                scaled_j =  Mathf.RoundToInt((float)j / mapsFactor);

                if (map[scaled_i, scaled_j] == 0)
                {
                    alphaMap[i,j,0] = 0;
                    alphaMap[i,j,1] = 1;
                    alphaMap[i,j,2] = 0;
                }
                else if (populationMap[scaled_i, scaled_j] == 1)
                {
                    alphaMap[i,j,0] = 0.4f;
                    alphaMap[i,j,1] = 0;
                    alphaMap[i,j,2] = 0.6f;
                }
                else
                {
                    alphaMap[i,j,0] = 1;
                    alphaMap[i,j,1] = 0;
                    alphaMap[i,j,2] = 0;
                }

                // alphaMap[i,j,0] = (map[scaled_i,scaled_j] == 0 ? 0 : 1);

                // Debug.LogFormat("i = {0}, j = {1}, scaled_i = {2}, scaled_j = {3}, map value = {4}", i,j,scaled_i, scaled_j, map[scaled_i, scaled_j]);
            }
        }

        td.SetAlphamaps(0,0,alphaMap);
        td.SetHeights(0,0, map);

        

        spawnPlayer(map, x, y);

        if (instantQuit)
            UnityEditor.EditorApplication.isPlaying = false;
    }

    private void spawnPlayer(float[,] map, int w, int h)
    {
        Vector3 position = Random.insideUnitSphere * w;
        position.x = Mathf.Abs(position.x);
        position.z = Mathf.Abs(position.z);
        position.y = 100;

        while(map[(int)position.x, (int)position.z] == 0)
        {
            position = Random.insideUnitSphere * w;
            position.x = Mathf.Abs(position.x);
            position.z = Mathf.Abs(position.z);
            position.y = 100;
        }

        position.x = Mathf.Clamp(position.x + this.transform.position.x, this.transform.position.x, - this.transform.position.x);
        position.z = Mathf.Clamp(position.z + this.transform.position.z, this.transform.position.z, - this.transform.position.z);

        cube.transform.position = position;
    }

    
}
