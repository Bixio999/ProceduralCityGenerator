using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class ProceduralCityGenerator : MonoBehaviour
{
    // GENERAL PARAMETERS

    public GameObject terrain;
    public int RandomSeed = 0; // Set generation seed
    public bool instantQuit = true; // Quit after execution - useful to only check terrain generation

    public bool savePopulationDensity = false; // Store population density generation as B/W image
    public bool saveRoads = false; // Store roadmap with terrain resolution as image
    public bool saveHDRoads = false; // Store zoomed roadmap as high res image

    public bool showPopDensityTexture = false; // Show population density as terrain yellow texture
    public bool showDebugRoadmapTexture = false; // Show debug roadmap texture

    // TERRAIN GENERATION PARAMETERS
    [Foldout("Terrain Generator", foldEverything = true)]

        // Perlin noise
    public float scalingFactor = 2;
    public int perlinNoiseOctaves = 5;
    public float perlinNoiseExponent = 4f;

    public float terrainWaterThreshold = 0.11f; // height threshold to define water

    // POPULATION DENSITY PARAMETERS
    [Foldout("Population Density Map")]

    [Range(0,1)] public float slopeThreshold = 0.1f; // decay factor for height difference from city centre
    public int neighborhoodRadius = 2; // matrix cell radius for city centre positioning test
    [Range(0, 1)] public float popDensityWaterThreshold = 0.3f; // max percentage of water cell near city centre
    [Range(0, 1)] public float mapBoundaryScale = 0.8f; // percentage of map to consider during city centre decision
    public int cityRadius = 250;
    public float popDensityHeightTolerance = 1f; // max tolerance for height difference to city centre

        // Perlin noise parameters
    [Header("--- Perlin noise parameters ---")]
    public int popDensityPNOctaves = 3;
    public float popDensityPNExponent = 2f;
    public float popDensityPNScaling = 2f;

    // ROAD MAP PARAMETERS
    [Foldout("Road Map")]

    public int iterationLimit; // max iteration to compute for roadmap generation
    public RoadMapRules rule = RoadMapRules.basic; // global goal rule to apply
    public int roadLength;

    [Header("Local constraints")]
    [Range(0,1)] public float waterPruningFactor; // max road lenght decrease when over water before abort positioning 
    [Range(1, 90)] public int maximalAngleToFix; // max road rotation when fixiing positioning
    [Range(0, 1)] public float highwayPopDensityLimit = 0.25f; // min population density required for highway positioning
    [Range(0, 1)] public float bywayPopDensityLimit = 0.1f; // min population density required for byway positioning
    [Range(0,1)] public float probabilityToBranchHighway; // probability factor to convert byway into highway
    [Range(0,1)] public float neighborhoodFactor; // percentage of road length used for neightborhood radius when evaluating road merging
    public int defaultDelay; // iteration to wait before extending new roads

    [Header("Debug texture parameters")]
    public int highwayThickness;
    public int bywayThickness;

    [Header("Models parameters")]
    public GameObject highway; // Model used for highways
    public GameObject byway; // Model used for byways
    public GameObject crossroad; // Model used for crossroads
    public float modelsScalingFactor = .2f; // Scaling factor for models
    public float modelsLength = 5; // road models length in local space

    public enum RoadMapRules // Available global goal rules
    {
        basic = 0,
        newYork = 1
    };

    // BUILDING GENERATOR PARAMETERS
    [Foldout("Building Generator")]

    public Vector2Int[] buildingLotSizes; // Array with max lot sizes for each building ruleset
    [Range(0, 1)] public float[] popDensityModelTresholds; // Array with population density tresholds used to decide the correct building
    public int buildingSpacing = 5; // offset between each buildings
    [Range(0, 1)] public float modelSelectionHighwayAdvantage = .5f; // min scaling factor for building decision if current road is highway
    [Range(.5f, 2)] public float modelSelectionMaxIncreasal = 1.2f; // max scaling factor for building decision for any road
    public int buildingCacheSize = 10; // Max number of unique building generated per ruleset
    [Range(0,90)] public int obstacleCollisionAngleTolerance = 20; // Max angle tolerance for near obstacle collision

    /* -------------------------------------- */

    void Start()
    {
        // SET THE GENERATION SEED
        if (RandomSeed == 0)
            RandomSeed = (int) System.DateTime.Now.Ticks;
        Random.InitState(RandomSeed);
        Debug.Log(RandomSeed);

        // CHECK FOR VALID INPUT PARAMETERS

        int buildingModels = ProceduralBuildingGenerator.Instance.ruleSets.Length;
        if (buildingModels != buildingLotSizes.Length || buildingModels != popDensityModelTresholds.Length + 1)
            return;

        /* -------------- */

        Terrain t = terrain.GetComponent<Terrain> ();
		TerrainData td = t.terrainData;

		int x = td.heightmapResolution;
		int y = td.heightmapResolution;

        // DEFINE THE TERRAIN TEXTURE ALPHAMAP
        float [,,] alphaMap = new float [td.alphamapWidth, td.alphamapHeight, td.alphamapLayers];

        // GENERATE THE TERRAIN HEIGHTMAP
        float [,] map = InputMapGenerator.generatePerlinNoiseMap(x,y, scalingFactor, perlinNoiseOctaves, perlinNoiseExponent, terrainWaterThreshold);
        td.SetHeights(0,0, map);

        // GENERATE THE POPULATION DENSITY MAP
        (float[,] populationMap, Vector2 cityCentre) = InputMapGenerator.createPopulationDensityMap(map, x, y, slopeThreshold, neighborhoodRadius, popDensityWaterThreshold, mapBoundaryScale, cityRadius, popDensityHeightTolerance, popDensityPNScaling, popDensityPNOctaves, popDensityPNExponent);

        // INSTANTIATE THE ROADMAP GENERATOR
        RoadMapGenerator roadMapGenerator = RoadMapGenerator.CreateInstance(in map, in populationMap, cityCentre, cityRadius, waterPruningFactor, maximalAngleToFix, neighborhoodFactor, defaultDelay, probabilityToBranchHighway, highwayThickness, bywayThickness, roadLength, highwayPopDensityLimit, bywayPopDensityLimit);

        // GET THE GLOBAL GOAL RULE TO APPLY     
        RoadMapRule r;

        switch (this.rule)
        {
            case RoadMapRules.basic:
                r = new BasicRule();
                break;
            case RoadMapRules.newYork:
                r = new NewYorkRule();
                break;
            default:
                r = new BasicRule();
                break;
        }

        // GENERATE THE ROADMAP WITH A RANDOM INITIAL ROAD DIRECTION
        roadMapGenerator.GenerateRoadMap(r, Random.insideUnitCircle, iterationLimit);

        // SPAWN THE ROADMAP RENDERING 
        GameObject RMrender = roadMapGenerator.Render(highway, byway, crossroad, modelsScalingFactor, modelsLength);
        RMrender.transform.position = Vector3.up * 0.5f;

        // DEFINE ADN STORE THE DEBUG ROADMAP TEXTURE 
        Texture2D roadMap = new Texture2D(x, y, TextureFormat.RGBA32, true);
        roadMapGenerator.DrawDebug(roadMap);
        UnityEditor.AssetDatabase.CreateAsset(roadMap, "Assets/Resources/RoadMapTexture.asset");

        td.terrainLayers[3].diffuseTexture = roadMap;


        // BUILDING GENERATION

        SpawnBuildings(roadMapGenerator.GetRoads(), td.size.x / x, in populationMap, in map);

        // TERRAIN TEXTURE PAINTING

        float mapsFactor = (float) Mathf.Min(td.alphamapHeight, x) / Mathf.Max(td.alphamapHeight, x); 

        int scaled_i, scaled_j;
        for (int i = 0; i < td.alphamapHeight; i++)
        {
            for (int j = 0; j < td.alphamapWidth; j++)
            {
                // Compute scaled coords due to different matrix resolution between alphamap and heightmap
                scaled_i =  Mathf.RoundToInt((float)i / mapsFactor);
                scaled_j =  Mathf.RoundToInt((float)j / mapsFactor);

                if (map[scaled_i, scaled_j] == 0)
                {
                    alphaMap[i,j,1] = 1; // Paint water texture
                }
                else if (!roadMap.GetPixel(scaled_j, scaled_i).Equals(Color.clear) && showDebugRoadmapTexture)
                {
                    alphaMap[i, j, 3] = 1; // Paint roadmap texture
                }
                else if (populationMap[scaled_i, scaled_j] > 0 && showPopDensityTexture)
                {
                    float value = populationMap[scaled_i, scaled_j];

                    // Paint population density texture
                    alphaMap[i,j,0] = 1 - value;
                    alphaMap[i,j,2] = value;
                }
                else
                {
                    alphaMap[i,j,0] = 1; // Paint terrain texture
                }
            }
        }

        td.SetAlphamaps(0,0,alphaMap);

        // MOVE CAMERA TO CITY CENTRE

        Vector3 v = ConvertPositionTo3D(cityCentre);
        v.y += 1;

        Camera.main.transform.position = v;

        // STORE THE POPULATION DENSITY GENERATION AS IMAGE

        if (savePopulationDensity)
            SaveImages.SaveMatrixAsPNG(populationMap, RandomSeed);

        // STORE THE ROADMAP GENERATION AS IMAGE

        if (saveRoads)
            SaveImages.SaveTextureAsPNG(roadMap, "RoadMap_" + RandomSeed);

        if (saveHDRoads)
            SaveImages.SaveTextureAsPNG(roadMapGenerator.GenerateHighResTexture(2f), "RoadMapHD_" + RandomSeed);

        // --------------------

        if (instantQuit)
            UnityEditor.EditorApplication.isPlaying = false;
    }


    /*
     * Utility function to convert position from bidimentional matrix coords 
     * used by heightmap and population density map to the corresponding 3D 
     * position in world coords.
     */
    public static Vector3 ConvertPositionTo3D(Vector2 input)
    {
        TerrainData td = Terrain.activeTerrain.terrainData;
        float coordsScaling = td.size.x / td.heightmapResolution;

        Vector3 output = new Vector3(input.x, 0, input.y) * coordsScaling;
        output += Terrain.activeTerrain.GetPosition();
        output.y = Terrain.activeTerrain.SampleHeight(output);
        return output;
    }

    /*
     * Utility function to convert position from 3D world coords to bidimentional 
     * matrix coords used to access heightmap and population density map.
     */
    public static Vector2 ConvertPositionTo2D(Vector3 input)
    {
        TerrainData td = Terrain.activeTerrain.terrainData;
        float coordsScaling = td.size.x / td.heightmapResolution;

        input -= Terrain.activeTerrain.GetPosition();
        input /= coordsScaling;
        return new Vector2(input.x, input.z);
    }

    /*
     * Generate and place the buildings for each road from the generated roadmap.
     * 
     * For each position, select the most coherent building by evaluating the 
     * population density. Once the model is chosen, check if the lot is in a 
     * valid location and eventual collision with other objects. 
     * 
     * In case of invalid positioning, retry with another model (if any) until 
     * valid or abort and move to the next position. 
     */
    private void SpawnBuildings(IEnumerable<Road> roads, float coordsScaling, in float[,] populationMap, in float[,] heightMap)
    {
        GameObject folder = new GameObject("Buildings");

        // DEFINE THE BUILDING CACHE 
        Dictionary<string, List<GameObject>> cache = new Dictionary<string, List<GameObject>>();

        foreach(Road road in roads)
        {
            // CALCULATE ROAD START AND END IN 3D WORLD

            Vector3 start = ConvertPositionTo3D(road.start.GetPosition());

            Vector3 end = ConvertPositionTo3D(road.end.GetPosition());

            (Vector3, Vector3)[] sides = { (end, start), (start, end) }; // Define the vectors of the sides of the road

                // Compute the distance to the side of the road
            float offset = (road.highway ? highway.transform.localPosition.x : byway.transform.localPosition.x) * modelsScalingFactor;

            foreach ((Vector3 B, Vector3 A) in sides) // Vectors are considered as: A ---> B
            {
                // CALCULATE ROAD DIRECTION 

                Vector3 direction = B - A;
                Vector3 sideOffset = Quaternion.AngleAxis(-90, Vector3.up) * direction.normalized * offset;
                Vector3 upRoad = Vector3.Cross(sideOffset.normalized, direction.normalized).normalized;

                Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.AngleAxis(-90, Vector3.up);

                // PLACE THE BUILDINGS ON THE CURRENT SIDE

                float dist = buildingSpacing;
                while (dist < direction.magnitude)
                {
                    // Position of the current building in 3D
                    Vector3 P = direction.normalized * dist + sideOffset + A;

                    // Compute the 2D coords to access population density matrix
                    Vector2 P_2D = ConvertPositionTo2D(P);

                    float popValue = populationMap[Mathf.RoundToInt(P_2D.y), Mathf.RoundToInt(P_2D.x)];

                    // GET THE CORRECT BUILDING MODEL TYPE
                    Vector2 buildingLotSize = Vector2.zero, lotSizeForGeneration = Vector2.zero;
                    string model = "";

                    // Introduce randomness to make taller buildings more sparse
                    if (road.highway)
                        popValue *= Random.Range(modelSelectionHighwayAdvantage, modelSelectionMaxIncreasal);
                    else
                        popValue *= Random.Range(0, modelSelectionMaxIncreasal);

                    float maxValue = 1f, minValue;
                    int i;
                    for (i = 0; i < popDensityModelTresholds.Length; i++)
                    {
                        minValue = 1 - popDensityModelTresholds[i];
                        if (maxValue >= popValue && popValue > minValue)
                            break;
                        maxValue = minValue;
                    }

                    // Sync physics for collision tests
                    Physics.SyncTransforms();

                    // Try building positioning and retry with another model until valid
                    for(; i < buildingLotSizes.Length; i++)
                    {
                        lotSizeForGeneration = buildingLotSizes[i];
                        buildingLotSize = lotSizeForGeneration * modelsScalingFactor;
                        model = ProceduralBuildingGenerator.Instance.ruleSets[i].name;

                        // check if the current building is out of boundaries
                        if (dist + buildingLotSize.x > direction.magnitude)
                            continue;

                        // check if lot is on water
                        Vector3[] moveP = { Vector3.zero, sideOffset.normalized * buildingLotSize.y, direction.normalized * buildingLotSize.x, sideOffset.normalized * -buildingLotSize.y };
                        Vector3 t = P;
                        bool invalidPosition = false;
                        foreach (Vector3 m in moveP)
                        {
                            t += m;
                            P_2D = ConvertPositionTo2D(t);

                            if (heightMap[Mathf.RoundToInt(P_2D.y), Mathf.RoundToInt(P_2D.x)] == 0)
                            {
                                invalidPosition = true;
                                break;
                            }
                        }
                        if (invalidPosition)
                            continue;

                        // Compute the center of the building
                        Vector3 center = P;
                        center += rotation * Vector3.forward * buildingLotSize.y / 2;
                        center += rotation * Vector3.right * buildingLotSize.x / 2;


                        // Check for road or building collision
                        Collider[] hits = Physics.OverlapBox(center,
                            Vector3.one * (buildingLotSize.x > buildingLotSize.y ? buildingLotSize.x : buildingLotSize.y) / 2f,
                            rotation,
                            LayerMask.GetMask("Obstacles"));

                        // Draw debug info
                        DrawBox(center, rotation, Vector3.one * (buildingLotSize.x > buildingLotSize.y ? buildingLotSize.x : buildingLotSize.y), Color.red);
                        Debug.DrawRay(P, upRoad, Color.green, Mathf.Infinity);

                        // Calculate the max angle a collision vector can form with sideOffset to find obstacles; this angle is used to
                        // distinguish actual collision from road parts in front of the building
                        float maxStreetAngle = Vector3.SignedAngle(sideOffset.normalized, (center - P).normalized, upRoad) + obstacleCollisionAngleTolerance;

                        invalidPosition = false;
                        foreach (Collider hit in hits)
                        {
                            t = center - hit.ClosestPointOnBounds(center); // Collision vector

                            // If another building or a road part collision with building is found, the current position is invalid
                            if (hit.gameObject.CompareTag("Building") || Vector3.Angle(sideOffset.normalized, t.normalized) > maxStreetAngle)
                            {
                                Debug.DrawRay(center, -t, Color.red, Mathf.Infinity);
                                invalidPosition = true;
                                break;
                            }
                            else
                                Debug.DrawRay(center, -t, Color.cyan, Mathf.Infinity);
                        }
                        if (invalidPosition)
                            continue;

                        break;
                    }

                    // Update current distance for the next building;
                    // if no model fits, skip lot
                    dist += buildingLotSize.x + buildingSpacing;

                    if (i >= buildingLotSizes.Length)
                        continue;

                    // GENERATE THE BUILDING

                    List<GameObject> modelCache;
                    if (cache.ContainsKey(model))
                        modelCache = cache[model];
                    else
                    {
                        modelCache = new List<GameObject>();
                        cache[model] = modelCache;
                    }

                    // If the cache limit has not yet been reached, it generates a unique building;
                    // otherwise it clones an existing one chosen at random
                    GameObject obj;
                    if (modelCache.Count < buildingCacheSize)
                    {
                        obj = ProceduralBuildingGenerator.Instance.GenerateFromRuleSet(model, lotSizeForGeneration);
                        modelCache.Add(obj);
                        obj.transform.localScale *= modelsScalingFactor;
                    }
                    else
                    {
                        obj = Instantiate(modelCache[Random.Range(0, modelCache.Count)]);
                    }

                    // Set position and rotation of the building
                    obj.transform.position = P;
                    obj.transform.localRotation = rotation;
                    Vector3 temp = obj.transform.localEulerAngles;
                    temp.z = 0;
                    obj.transform.localEulerAngles = temp;

                    obj.transform.parent = folder.transform;
                    obj.layer = LayerMask.NameToLayer("Obstacles");
                    obj.tag = "Building";

                }

            }

        }
    }

    /*
     * Utility function to draw a wireframe cube. 
     */
    public void DrawBox(Vector3 pos, Quaternion rot, Vector3 scale, Color c)
    {
        // create matrix
        Matrix4x4 m = new Matrix4x4();
        m.SetTRS(pos, rot, scale);

        var point1 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, 0.5f));
        var point2 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, 0.5f));
        var point3 = m.MultiplyPoint(new Vector3(0.5f, -0.5f, -0.5f));
        var point4 = m.MultiplyPoint(new Vector3(-0.5f, -0.5f, -0.5f));

        var point5 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, 0.5f));
        var point6 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, 0.5f));
        var point7 = m.MultiplyPoint(new Vector3(0.5f, 0.5f, -0.5f));
        var point8 = m.MultiplyPoint(new Vector3(-0.5f, 0.5f, -0.5f));

        Debug.DrawLine(point1, point2, c);
        Debug.DrawLine(point2, point3, c);
        Debug.DrawLine(point3, point4, c);
        Debug.DrawLine(point4, point1, c);

        Debug.DrawLine(point5, point6, c);
        Debug.DrawLine(point6, point7, c);
        Debug.DrawLine(point7, point8, c);
        Debug.DrawLine(point8, point5, c);

        Debug.DrawLine(point1, point5, c);
        Debug.DrawLine(point2, point6, c);
        Debug.DrawLine(point3, point7, c);
        Debug.DrawLine(point4, point8, c);
    }
}
