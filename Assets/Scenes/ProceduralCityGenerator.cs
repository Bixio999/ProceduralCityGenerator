using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using Random = UnityEngine.Random;

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
    [Range(0, 1)] public float roadLengthVariability;
    public int highwayThickness;
    public int bywayThickness;
    public GameObject highway;
    public GameObject byway;
    public GameObject crossroad;
    public float modelsScalingFactor = .2f;
    public float modelsLength = 5;
    public float highwayModelWidth = 7;
    public float bywayModelWidth = 4;

    public enum RoadMapRules
    {
        basic = 0,
        newYork = 1
        //sanFrancisco = 2
    };

    // BUILDING GENERATOR PARAMETERS

    [Foldout("Building Generator")]
    public Vector2Int officeBuildingMaxLot;
    public Vector2Int simpleBuildingMaxLot;
    [Range(0, 1)] public float popDensityModelThreshold = .1f;
    public int buildingSpacing = 5;
    [Range(0, 1)] public float modelSelectionHighwayAdvantage = .5f;
    [Range(.5f, 2)] public float modelSelectionMaxIncreasal = 1.2f;
    public int buildingCacheSize = 10;
    public int obstacleCollisionAngleTolerance = 20;

    public GameObject buildingLotDebugModel;


    /* -------------------------------------- */

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

        (float[,] populationMap, Vector2 cityCentre, float maxPopDensityValue) = InputMapGenerator.createPopulationDensityMap(map, x, y, slopeThreshold, neighborhoodRadius, popDensityWaterThreshold, mapBoundaryScale, cityRadius, popDensityHeightTolerance, popDensityPNScaling, popDensityPNOctaves, popDensityPNExponent);

        td.SetHeights(0,0, map);

        RoadMapGenerator roadMapGenerator = RoadMapGenerator.CreateInstance(map, populationMap, cityCentre, cityRadius, waterPruningFactor, maximalAngleToFix, neighborhoodFactor, defaultDelay, probabilityToBranchHighway, highwayThickness, bywayThickness, roadLength, roadLengthVariability);
     
        RoadMapRule r;

        switch (this.rule)
        {
            case RoadMapRules.basic:
                r = new BasicRule();
                break;
            case RoadMapRules.newYork:
                r = new NewYorkRule();
                break;
            //case RoadMapRules.sanFrancisco:
            //    r = new SanFranciscoRule();
            //    break;
            default:
                r = new BasicRule();
                break;
        }

        roadMapGenerator.GenerateRoadMap(r, Random.insideUnitCircle, iterationLimit);

        Texture2D roadMap = new Texture2D(x, y, TextureFormat.RGBA32, true);

        GameObject RMrender = roadMapGenerator.Render(highway, byway, crossroad, modelsScalingFactor, modelsLength, td.size.x / x);
        RMrender.transform.position = Vector3.up * 0.5f;

        //roadMapGenerator.DrawConnectivity(roadMap);
        roadMapGenerator.DrawDebug(roadMap);
        //roadMapGenerator.DrawShortestCycles(roadMap);

        UnityEditor.AssetDatabase.CreateAsset(roadMap, "Assets/Resources/RoadMapTexture.asset");

        td.terrainLayers[3].diffuseTexture = roadMap;


        // BUILDING GENERATION

        SpawnBuildings(roadMapGenerator.GetRoads(), td.size.x / x, in populationMap, maxPopDensityValue - maxPopDensityValue * popDensityModelThreshold, in map);

        // TERRAIN TEXTURE PAINTING

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
                    alphaMap[i, j, 3] = 1;
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

        // MOVE CAMERA TO CITY CENTRE

        Vector3 v = new Vector3(cityCentre.x, 0, cityCentre.y) + Terrain.activeTerrain.GetPosition();
        v *= td.size.x / x;
        v.y = Terrain.activeTerrain.SampleHeight(v) + 1;

        Camera.main.transform.position = v;

        //GameObject building = ProceduralBuildingGenerator.Instance.GenerateFromRuleSet("SimpleOfficeBuilding", officeBuildingMaxLot);
        //building.transform.position = v;
        //building.transform.localScale *= modelsScalingFactor;


        //player.transform.position = v;

        //spawnPlayer(map, x, y);

        if (savePopulationDensity)
            SaveImages.SaveMatrixAsPNG(populationMap, RandomSeed);

        if (saveRoads)
            SaveImages.SaveTextureAsPNG(roadMap, "RoadMap_" + RandomSeed);

        if (saveHDRoads)
            SaveImages.SaveTextureAsPNG(roadMapGenerator.GenerateHighResTexture(2f), "RoadMapHD_" + RandomSeed);

        if (instantQuit)
            UnityEditor.EditorApplication.isPlaying = false;
    }

    private void SpawnBuildings(IEnumerable<Road> roads, float coordsScaling, in float[,] populationMap, float popDensityThreshold, in float[,] heightMap)
    {
        GameObject folder = new GameObject("Buildings");

        Dictionary<string, List<GameObject>> cache = new Dictionary<string, List<GameObject>>();

        foreach(Road road in roads)
        {
            // CALCULATE ROAD START AND END IN 3D WORLD

            Vector2 v = road.start.GetPosition();
            Vector3 start = new Vector3(v.x, 0, v.y) * coordsScaling;
            start += Terrain.activeTerrain.GetPosition();
            start.y = Terrain.activeTerrain.SampleHeight(start);

            v = road.end.GetPosition();
            Vector3 end = new Vector3(v.x, 0, v.y) * coordsScaling;
            end += Terrain.activeTerrain.GetPosition();
            end.y = Terrain.activeTerrain.SampleHeight(end);

            (Vector3, Vector3)[] sides = { (end, start), (start, end) };

            //Vector3 rotation = new Vector3(0, -90, 0);

            float offset = (road.highway ? highway.transform.localPosition.x : byway.transform.localPosition.x) * modelsScalingFactor;


            foreach ((Vector3 B, Vector3 A) in sides) // Vectors are considered as: A ---> B
            {
                // CALCULATE ROAD DIRECTION 

                Vector3 direction = B - A;

                Vector3 sideOffset = Quaternion.AngleAxis(-90, Vector3.up) * direction.normalized * offset;
                Quaternion rotation = Quaternion.LookRotation(direction) * Quaternion.AngleAxis(-90, Vector3.up);

                Vector3 upRoad = Vector3.Cross(sideOffset.normalized, direction.normalized).normalized;
            

                float dist = buildingSpacing;
                while (dist < direction.magnitude)
                {
                    // Position of the current building in 3D
                    Vector3 P = direction.normalized * dist + sideOffset + A;


                    // Compute the 2D coords to access population density matrix
                    Vector3 temp = P - Terrain.activeTerrain.GetPosition();
                    temp /= coordsScaling;
                    Vector2 P_2D = new(temp.x, temp.z);

                    float popValue = populationMap[Mathf.RoundToInt(P_2D.y), Mathf.RoundToInt(P_2D.x)];

                    // Get the right model type  -- TODO multiple building type with threshold and lot sizes arrays
                    Vector2 buildingLotSize, lotSizeForGeneration;
                    string model;
                    if (road.highway)
                        popValue *= Random.Range(modelSelectionHighwayAdvantage, modelSelectionMaxIncreasal);
                    else
                        popValue *= Random.Range(0, modelSelectionMaxIncreasal);
                    if (popValue > popDensityThreshold)
                    {
                        buildingLotSize = officeBuildingMaxLot;
                        model = ProceduralBuildingGenerator.Instance.ruleSets[0].name;
                        lotSizeForGeneration = officeBuildingMaxLot;
                    }
                    else
                    {
                        buildingLotSize = simpleBuildingMaxLot;
                        model = ProceduralBuildingGenerator.Instance.ruleSets[1].name;
                        lotSizeForGeneration = simpleBuildingMaxLot;
                    }
                    buildingLotSize *= modelsScalingFactor;

                    // check if the current building is out of boundaries
                    if (dist + buildingLotSize.x > direction.magnitude)
                        break;

                    // Update current distance for the next building
                    dist += buildingLotSize.x + buildingSpacing;

                    // check if lot is on water
                    Vector3[] moveP = { Vector3.zero, sideOffset.normalized * buildingLotSize.y, direction.normalized * buildingLotSize.x, sideOffset.normalized * -buildingLotSize.y };
                    Vector3 t = P;
                    bool invalidPosition = false;
                    foreach (Vector3 m in moveP)
                    {
                        t += m;
                        temp = t - Terrain.activeTerrain.GetPosition();
                        temp /= coordsScaling;
                        P_2D = new Vector2(temp.x, temp.z);

                        if (heightMap[Mathf.RoundToInt(P_2D.y), Mathf.RoundToInt(P_2D.x)] == 0)
                            invalidPosition = true;
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

                    DrawBox(center, rotation, Vector3.one * (buildingLotSize.x > buildingLotSize.y ? buildingLotSize.x : buildingLotSize.y), Color.red);

                    Physics.SyncTransforms();
                    invalidPosition = false;

                    Debug.DrawRay(P, upRoad, Color.green, Mathf.Infinity);
                    float maxStreetAngle = Vector3.SignedAngle(sideOffset.normalized, (center - P).normalized, upRoad) + obstacleCollisionAngleTolerance;
                    print(maxStreetAngle);

                    Vector3 cumulativeHit = Vector3.zero;
                    foreach(Collider hit in hits)
                    {
                        t = center - hit.ClosestPointOnBounds(center);
                        cumulativeHit += t;

                        if (hit.gameObject.tag == "Building" || Vector3.Angle(sideOffset.normalized, t.normalized) > maxStreetAngle)
                        {
                            Debug.DrawRay(center, -t, Color.red, Mathf.Infinity);
                            invalidPosition = true;
                        }
                        else
                        {
                            Debug.DrawRay(center, -t, Color.cyan, Mathf.Infinity);
                            Debug.Log("collision with " + hit.gameObject.name);
                        }
                    }

                    if (invalidPosition)
                        continue;

                    cumulativeHit /= hits.Length;
                    //Debug.DrawLine(center, center + cumulativeHit, Color.cyan, Mathf.Infinity);

                    float angle = -Vector3.SignedAngle(direction.normalized, cumulativeHit.normalized, rotation * Vector3.up);
                    Vector3 xDir = direction.normalized * cumulativeHit.magnitude * Mathf.Cos(angle);
                    Vector3 yDir = sideOffset.normalized * cumulativeHit.magnitude * Mathf.Sin(angle);
                    Vector3 yOffset = (buildingLotSize.y / 2f - yDir.magnitude) * sideOffset.normalized;

                    Debug.DrawRay(center, xDir, Color.blue, Mathf.Infinity);
                    Debug.DrawRay(center, yDir, Color.magenta, Mathf.Infinity);
                    Debug.DrawRay(center, yOffset, Color.green, Mathf.Infinity);




                    // Spawn the debug cube
                    //GameObject obj = Instantiate(buildingLotDebugModel);
                    //temp = new(buildingLotSize.x, 0, buildingLotSize.y);
                    //if (popValue > popDensityThreshold)
                    //    temp.y = 100 * Random.Range(.5f, 1) * modelsScalingFactor;
                    //else
                    //    temp.y = 10 * Random.Range(.7f, 1) * modelsScalingFactor ;
                    //obj.transform.localScale = temp;




                    // Generate building
                    List<GameObject> modelCache;
                    if (cache.ContainsKey(model))
                        modelCache = cache[model];
                    else
                    {
                        modelCache = new List<GameObject>();
                        cache[model] = modelCache;
                    }
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
                        //return;
                    }

                    // Set position and rotation of the building
                    obj.transform.position = P;
                    obj.transform.localRotation = rotation;
                    temp = obj.transform.localEulerAngles;
                    temp.z = 0;
                    obj.transform.localEulerAngles = temp;

                    obj.transform.parent = folder.transform;
                    obj.layer = LayerMask.NameToLayer("Obstacles");
                    obj.tag = "Building";

                }

            }

        }
    }

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

        // optional axis display
        //Debug.DrawRay(m.GetPosition(), m.GetForward(), Color.magenta);
        //Debug.DrawRay(m.GetPosition(), m.GetUp(), Color.yellow);
        //Debug.DrawRay(m.GetPosition(), m.GetRight(), Color.red);
    }
}
