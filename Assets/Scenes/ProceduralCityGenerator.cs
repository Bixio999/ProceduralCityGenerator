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
    public bool saveRoads = false;
    public bool saveHDRoads = false;
    public GameObject player;



        // TODO SET RANGES FOR PARAMETERS  

    // PERLIN NOISE PARAMETERS
    [Foldout("Terrain Generator", foldEverything = true)]
    public float scalingFactor = 2;
    public int perlinNoiseOctaves = 5;
    public float perlinNoiseExponent = 4f;
    public float terrainWaterThreshold = 0.11f;

    // POPULATION DENSITY MAP PARAMETERS

    [Foldout("Population Density Map")]
    public float slopeThreshold = 0.1f;
    public int neighborhoodRadius = 2;
    public float popDensityWaterThreshold = 0.3f;
    public float mapBoundaryScale = 0.8f;
    public int cityRadius = 250;
    public float popDensityHeightTolerance = 1f;
    public int popDensityPNOctaves = 3;
    public float popDensityPNExponent = 2f;
    public float popDensityPNScaling = 2f;

    // ROAD MAP PARAMETERS

    [Foldout("Road Map")]
    public int iterationLimit;
    public RoadMapRules rule = RoadMapRules.basic;
    [Range(0,1)]    public float waterPruningFactor;

    [Range(1, 90)]  public int maximalAngleToFix;
    [Range(0,1)]    public float neighborhoodFactor;
    public int defaultDelay;
    [Range(0,1)]    public float probabilityToBranchHighway;
    public int roadLength;
    public int highwayThickness;
    public int bywayThickness;


    public enum RoadMapRules
    {
        basic = 0,
        checkers = 1,
        sanFrancisco = 2
    };

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

        td.SetHeights(0,0, map);

        RoadMapGenerator roadMapGenerator = RoadMapGenerator.CreateInstance(map, populationMap, cityCentre, cityRadius, waterPruningFactor, maximalAngleToFix, neighborhoodFactor, defaultDelay, probabilityToBranchHighway, highwayThickness, bywayThickness);
     
        RoadMapRule r;
        r = new BasicRule();

        // switch(this.rule)
        // {
        //     case RoadMapRules.basic:
        //         r = new BasicRule();
        //         break;
        //     case RoadMapRules.checkers:
        //         r = new CheckersRule();
        //         break;
        //     case RoadMapRules.sanFrancisco:
        //         r = new SanFranciscoRule();
        //         break;
        //     default:
        //         r = new BasicRule();
        //         break;
        // }
        
        Texture2D roadMap = roadMapGenerator.generateRoadMap(r, Random.insideUnitCircle, roadLength, iterationLimit);
        UnityEditor.AssetDatabase.CreateAsset(roadMap, "Assets/Resources/RoadMapTexture.asset");

        td.terrainLayers[3].diffuseTexture = roadMap;



        // Debug.LogFormat("corner color value: {0}", roadMap.GetPixel(0,0));

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
                else if (!roadMap.GetPixel(scaled_j, scaled_i).Equals(Color.clear))
                {
                    alphaMap[i,j,3] = 1;
                    // Debug.LogFormat("drawing street at i = {0}, j = {1}", scaled_i, scaled_j);
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

                // alphaMap[i,j,3] = 1;
            }
        }

        td.SetAlphamaps(0,0,alphaMap);

        spawnPlayer(map, x, y);

        if (savePopulationDensity)
            SaveImages.SaveMatrixAsPNG(populationMap, RandomSeed);

        if (saveRoads)
            SaveImages.SaveTextureAsPNG(roadMap, "RoadMap_" + RandomSeed);

        if (saveHDRoads)
            SaveImages.SaveTextureAsPNG(roadMapGenerator.generateHighResTexture(2f), "RoadMapHD_" + RandomSeed);

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

    
}
