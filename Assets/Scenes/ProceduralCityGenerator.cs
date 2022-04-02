using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;

public class ProceduralCityGenerator : MonoBehaviour
{
    public GameObject terrain;
    public int RandomSeed = 0;
    public bool instantQuit = true;
    public bool savePopulationDensity = false;


        // TODO SET RANGES FOR PARAMETERS  

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
    public int cityRadius = 250;
    public float popDensityHeightTolerance = 1f;
    public int popDensityPNOctaves = 3;
    public float popDensityPNExponent = 2f;
    public float popDensityPNScaling = 2f;

    public GameObject player;

    void Start()
    {
        if (RandomSeed == 0)
            RandomSeed = (int) System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);
        Debug.Log(RandomSeed);

        Terrain t = terrain.GetComponent<Terrain> ();
		TerrainData td = t.terrainData;

		int x = td.heightmapResolution;
		int y = td.heightmapResolution;

        // Debug.LogFormat("x = {0}, y = {1}, z = {2}", x,y,z);
        
        float [,,] alphaMap = new float [td.alphamapWidth, td.alphamapHeight, td.alphamapLayers];

        float [,] map = InputMapGenerator.generatePerlinNoiseMap(x,y, scalingFactor, perlinNoiseOctaves, perlinNoiseExponent, terrainWaterThreshold);

        (float[,] populationMap, Vector2 cityCentre) = InputMapGenerator.createPopulationDensityMap(map, x, y, slopeThreshold, neighborhoodRadius, popDensityWaterThreshold, mapBoundaryScale, cityRadius, popDensityHeightTolerance, popDensityPNScaling, popDensityPNOctaves, popDensityPNExponent);

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
                    alphaMap[i,j,1] = 1;
                }
                else if (populationMap[scaled_i, scaled_j] > 0)
                {
                    float value = populationMap[scaled_i, scaled_j];

                    alphaMap[i,j,0] = 1 - value;
                    alphaMap[i,j,2] = value;
                }
                else
                {
                    alphaMap[i,j,0] = 1;
                }

                // Debug.LogFormat("i = {0}, j = {1}, scaled_i = {2}, scaled_j = {3}, map value = {4}", i,j,scaled_i, scaled_j, map[scaled_i, scaled_j]);
            }
        }

        td.SetAlphamaps(0,0,alphaMap);
        td.SetHeights(0,0, map);

        spawnPlayer(map, x, y);

        if (savePopulationDensity)
            SaveMatrixAsPNG(populationMap, RandomSeed);

        if (instantQuit)
            UnityEditor.EditorApplication.isPlaying = false;
    }

    private void spawnPlayer(float[,] map, int w, int h)
    {
        Vector3 position = Random.insideUnitSphere * (w - 1);
        position.x = Mathf.Abs(position.x);
        position.z = Mathf.Abs(position.z);
        position.y = 0;

        while(map[Mathf.RoundToInt(position.x), Mathf.RoundToInt(position.z)] == 0)
        {
            position = Random.insideUnitSphere * (w - 1);
            position.x = Mathf.Abs(position.x);
            position.z = Mathf.Abs(position.z);
        }

        position.x = Mathf.Clamp(position.x + this.transform.position.x, this.transform.position.x, - this.transform.position.x);
        position.z = Mathf.Clamp(position.z + this.transform.position.z, this.transform.position.z, - this.transform.position.z);
        position.y = Terrain.activeTerrain.SampleHeight(position) + 2;

        player.transform.position = position;
    }

    public static void TextureFromColourMap(Color[] colourMap, int width, int height, int seed) {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGB24, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.SetPixels (colourMap);
        texture.Apply ();

        byte[] bytes = texture.EncodeToPNG();
        var dirPath = Application.dataPath + "/../SaveImages/";
        if(!Directory.Exists(dirPath)) {
            Directory.CreateDirectory(dirPath);
        }
        File.WriteAllBytes(dirPath + "PopulationDensity-" + seed + ".png", bytes);
    }
    public static void SaveMatrixAsPNG(float[,] matrix, int seed) {
        int width = matrix.GetLength (0);
        int height = matrix.GetLength (1);
        Color[] colourMap = new Color[width * height];
        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                colourMap [y * width + x] = Color.Lerp (Color.black, Color.white, matrix [x, y]);
            }
        }
        TextureFromColourMap (colourMap, width, height, seed);
    }
}
