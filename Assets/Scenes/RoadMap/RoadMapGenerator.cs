using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;
using QuikGraph.Algorithms.ConnectedComponents;
using QuikGraph.Algorithms.ShortestPath;

public class RoadMapGenerator : ScriptableObject
{
    private float[,] heightmap;
    private float[,] populationDensity;
    private QuadTree<IQuadTreeObject> quadTree;

    private UndirectedGraph<Crossroad, Road> graph;

    private Queue<RMModule> moduleQueue;
    private float waterPruningFactor;
    private int maximalAngleToFix;
    private float neighborhoodFactor;
    private int defaultDelay;
    private float probabilityToBranchHighway;
    private int highwayThickness;
    private int bywayThickness;
    private Crossroad cityCentre;
    private int cityRadius;
    private int roadLength;
    private float highwayPopDensityLimit;
    private float bywayPopDensityLimit;
    private float roadLengthVariability;

    public static RoadMapGenerator CreateInstance(float[,] heightmap, float[,] populationDensity, Vector2 cityCentre, int cityRadius, float waterPruningFactor, int maximalAngleToFix, float neighborhoodFactor, int defaultDelay, float probabilityToBranchHighway, int highwayThickness, int bywayThickness, int roadLength, float roadLengthVariability, float highwayPopDensityLimit, float bywayPopDensityLimit)
    {
        RoadMapGenerator instance = CreateInstance<RoadMapGenerator>();

        instance.heightmap = heightmap;
        instance.populationDensity = populationDensity;

        instance.waterPruningFactor = waterPruningFactor;
        instance.maximalAngleToFix = maximalAngleToFix;
        instance.neighborhoodFactor = neighborhoodFactor;
        instance.defaultDelay = defaultDelay;
        instance.probabilityToBranchHighway = probabilityToBranchHighway;
        instance.highwayThickness = highwayThickness;
        instance.bywayThickness = bywayThickness;
        instance.roadLength = roadLength;
        instance.roadLengthVariability = roadLengthVariability;
        instance.highwayPopDensityLimit = highwayPopDensityLimit;
        instance.bywayPopDensityLimit = bywayPopDensityLimit;

        instance.graph = new UndirectedGraph<Crossroad, Road>();

        // QuikGraph.Algorithms.ConnectedComponents.StronglyConnectedComponentsAlgorithm<Crossroad, Road> scc = new QuikGraph.Algorithms.ConnectedComponents.StronglyConnectedComponentsAlgorithm<Crossroad, Road>(graph);
        


        instance.cityCentre = new Crossroad(cityCentre);
        instance.cityRadius = cityRadius;

        instance.quadTree = new QuadTree<IQuadTreeObject>(4, new Rect(cityCentre.x - cityRadius, cityCentre.y - cityRadius, 2 * cityRadius, 2 * cityRadius));
        instance.moduleQueue = new Queue<RMModule>();

        return instance;
    }

    public void GenerateRoadMap(RoadMapRule rule, Vector2 initialDirection, int iterationLimit)
    {
        RoadModule r = new RoadModule();
        r.ruleAttr = rule;
        r.roadAttr.highway = true;
        r.roadAttr.length = roadLength;
        r.roadAttr.direction = initialDirection;
        r.startPoint = this.cityCentre;
        
        this.quadTree.Insert(this.cityCentre);
        this.graph.AddVertex(this.cityCentre);

        this.moduleQueue.Enqueue(r);

        RMModule m;

        int iteration;
        for (iteration = 0; this.moduleQueue.Count > 0; iteration++)
        {
            int moduleToEvaulate = this.moduleQueue.Count;
            for(int i = 0; i < moduleToEvaulate; i++)
            {
                m = this.moduleQueue.Dequeue();
                if (m is RoadModule)
                    roadModuleHandler((RoadModule) m);
                else
                    branchModuleHandler((BranchModule) m);
            }

            if (iterationLimit > 0 && iteration >= iterationLimit)
                break;
        }

        Debug.LogFormat("iterations: {0}", iteration);

        //FindConnectivity();

        //this.quadTree.DrawDebug();

        //this.FixRoadMapConnectivity();
    }

    public IEnumerable<Road> GetRoads()
    {
        return this.graph.Edges;
    }

    public GameObject Render(GameObject highway, GameObject byway, GameObject crossroad, float scalingFactor, float roadModelsLength, float coordScaling)
    {
        GameObject render = new GameObject("RoadMap Render");

        foreach(Road road in this.graph.Edges)
        {
            if (road.start.GetPosition() == road.end.GetPosition())
                continue;
            Vector2 v = road.start.GetPosition();
            Vector3 start = new Vector3(v.x, 0, v.y) * coordScaling;
            start += Terrain.activeTerrain.GetPosition();
            start.y = Terrain.activeTerrain.SampleHeight(start);

            v = road.end.GetPosition();
            Vector3 end = new Vector3(v.x, 0, v.y) * coordScaling;
            end += Terrain.activeTerrain.GetPosition();
            end.y = Terrain.activeTerrain.SampleHeight(end);

            Vector3 vector = (end - start);

            //Quaternion rotation = Quaternion.LookRotation(new Vector3(vector.x, 0, vector.z));
            Quaternion rotation = Quaternion.LookRotation(vector);

            //Debug.LogFormat("length of road {0}: {1}", road, vector.magnitude);

            int nSections = Mathf.RoundToInt(vector.magnitude / (roadModelsLength * scalingFactor));
            //int nInterpolation = Mathf.CeilToInt((float) nSections / terrainInterpolationAccuracy);

            GameObject model = new GameObject();

            Vector3 prev = Vector3.zero;
            for(int i = 1; i < nSections; i++)
            {
                GameObject roadSection = road.highway ? GameObject.Instantiate(highway) : GameObject.Instantiate(byway);
                //Debug.Log(roadSection.transform.localPosition);
                roadSection.layer = LayerMask.NameToLayer("Obstacles");
                roadSection.tag = "Road";
                
                Vector3 t = vector * i / nSections + start;
                t.y = Terrain.activeTerrain.SampleHeight(t);
                //t -= start;

                Vector3 dir = ((t - start) / scalingFactor - prev).normalized;
                Vector3 curr = prev + dir * roadModelsLength;

                Quaternion roadDir = Quaternion.LookRotation(dir);

                //t += roadDir * (Vector3.forward * (roadModelsLength / 2));

                Vector3 left = t + roadDir * Vector3.left;
                left.y = Terrain.activeTerrain.SampleHeight(left);

                Vector3 right = t + roadDir * Vector3.right;
                right.y = Terrain.activeTerrain.SampleHeight(right);

                //Debug.DrawRay(left, right - left, Color.red, Mathf.Infinity);
                //Debug.DrawRay(t, roadDir * Vector3.right, Color.blue, Mathf.Infinity);
                //Debug.DrawRay(t, roadDir * Vector3.forward * 0.5f, Color.green, Mathf.Infinity);

                float angle = Vector3.SignedAngle(right - left, roadDir * Vector3.right, roadDir * Vector3.forward);
                Quaternion zRotation = Quaternion.AngleAxis(-angle, Vector3.forward);

                roadSection.transform.localPosition = curr;
                roadSection.transform.localRotation = roadDir * zRotation;
                roadSection.transform.parent = model.transform;

                prev = curr;
            }
            model.transform.localScale = Vector3.one * scalingFactor;

            model.transform.position = start;

            //Debug.LogFormat("current road: {0}", road);
            //model.transform.rotation = rotation;

            model.transform.position += Quaternion.LookRotation(vector) * (Vector3.right * roadModelsLength * scalingFactor * (road.highway ? 1.25f : 1));

            model.transform.parent = render.transform;
            model.name = road.ToString();
        }

        foreach(Crossroad c in this.graph.Vertices)
        {
            GameObject model = GameObject.Instantiate(crossroad);
            model.transform.localScale = model.transform.localScale * scalingFactor;

            Vector2 v = c.GetPosition();
            Vector3 position = new Vector3(v.x, 0, v.y) * coordScaling;
            position += Terrain.activeTerrain.GetPosition();
            position.y = Terrain.activeTerrain.SampleHeight(position);
            model.transform.position = position;

            Vector3 p1 = position + model.transform.forward;
            p1.y = Terrain.activeTerrain.SampleHeight(p1);
            Vector3 p2 = position + model.transform.right - model.transform.forward;
            p2.y = Terrain.activeTerrain.SampleHeight(p2);
            Vector3 p3 = position - model.transform.right - model.transform.forward;
            p3.y = Terrain.activeTerrain.SampleHeight(p3);

            Vector3 v1 = p2 - p1;
            Vector3 v2 = p3 - p1;
            Vector3 v3 = Vector3.Cross(v1, v2);

            model.transform.up = v3.normalized;

            //Debug.DrawRay(p1, v1, Color.red, Mathf.Infinity);
            //Debug.DrawRay(p1, v2, Color.blue, Mathf.Infinity);
            //Debug.DrawRay(p1, v3, Color.green, Mathf.Infinity);

            model.transform.parent = render.transform;
            model.name = c.ToString();
        }
        //render.transform.position = Vector3.up * 0.5f;
        return render;
    }

    public void DrawDebug(Texture2D texture)
    {
        FillTextureWithTransparency(texture);

        foreach (Road road in this.graph.Edges)
            drawLine(texture, road.start.GetPosition(), road.end.GetPosition(), road.highway ? Color.black : Color.grey, road.highway ? this.highwayThickness : this.bywayThickness, road.highway);

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    public Texture2D GenerateHighResTexture(float textureScalingFactor)
    {
        int width = (int) (this.heightmap.GetLength(0) * textureScalingFactor);
        int height = (int) (this.heightmap.GetLength(1) * textureScalingFactor);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        FillTextureWithTransparency(texture);

        Vector2 origin = new Vector2((float)width / 2, (float)height / 2);

        float scalingFactor = (float) Mathf.Max(width, height) / (cityRadius * 2);

        foreach(Road road in this.graph.Edges)
        {
            Vector2 roadStart = ((road.start.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;
            Vector2 roadEnd = ((road.end.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;

            drawLine(texture, roadStart, roadEnd, road.highway? Color.black : Color.grey, Mathf.RoundToInt((road.highway? this.highwayThickness : this.bywayThickness) * scalingFactor), road.highway);   
        }

        texture.Apply();
        return texture; 
    }

    private void FillTextureWithTransparency(Texture2D texture)
    {
        Color[] colors = new Color[texture.width * texture.height];
        texture.SetPixels(0, 0, texture.width, texture.height, colors);
        texture.Apply();
    }

    private void drawLine(Texture2D tex, Vector2 start, Vector2 end, Color color, int thickness, bool overwrite)
    {
        int width = tex.width;
        int height = tex.height;

        Vector2 dir = end - start;
        Vector2 n1 = new Vector2(dir.y, -dir.x);
        Vector2 n2 = new Vector2(-dir.y, dir.x);

        float length = dir.magnitude;
        dir.Normalize();
        n1.Normalize();
        n2.Normalize();

        thickness = ((int)Mathf.Ceil((float) thickness / 2));

        int steps = (int)length;
        for (int i = 0; i < steps; i++)
        {
            Vector2 pos = start + dir * i;
            if (tex.GetPixel((int)pos.x, (int)pos.y).Equals(Color.clear) || overwrite)
            {
                tex.SetPixel((int)pos.x, (int)pos.y, color);

                for (int j = 0; j < thickness; j++)
                {
                    Vector2 t1 = pos + n1 * j;
                    Vector2 t2 = pos + n2 * j;

                    tex.SetPixel((int)t1.x, (int)t1.y, color);
                    tex.SetPixel((int)t2.x, (int)t2.y, color);
                }
            }
        }

        tex.SetPixel((int)start.x, (int)start.y, Color.red);
        tex.SetPixel((int)end.x, (int)end.y, Color.red);
    }

    private void roadModuleHandler(RoadModule m)
    {
        if (m.del < 0 || m.state == QueryStates.FAILED)
            return;

        switch(m.state)
        {
            case QueryStates.UNASSIGNED:
                Crossroad end;
                m.state = localConstraints(m.roadAttr, m.startPoint, out end);
                m.endPoint = end;
                this.moduleQueue.Enqueue(m);
                break;
            
            case QueryStates.SUCCEED:
                (int[] pDel, RoadMapRule[] pRuleAttr, RoadAttributes[] pRoadAttr) = this.globalGoals(m.ruleAttr, m.roadAttr, m.startPoint);

                RoadModule r = new RoadModule();
                r.del = pDel[0];
                r.ruleAttr = pRuleAttr[0];
                r.roadAttr = pRoadAttr[0];
                r.startPoint = m.endPoint;
                r.state = QueryStates.UNASSIGNED;

                this.moduleQueue.Enqueue(r);

                for (int i = 1; i < 3; i++)
                {
                    BranchModule b = new BranchModule();
                    b.del = pDel[i];
                    b.ruleAttr = pRuleAttr[i];
                    b.roadAttr = pRoadAttr[i];
                    b.startPoint = m.startPoint;

                    this.moduleQueue.Enqueue(b);
                }
                break;

            default:
                return;
        }
    }

    private void branchModuleHandler(BranchModule m)
    {
        if (m.del < 0)
            return;
        else if (m.del > 0)
        {
            m.del--;
            this.moduleQueue.Enqueue(m);
        }
        else
            this.moduleQueue.Enqueue(new RoadModule(m));
    }

    private (int[], RoadMapRule[], RoadAttributes[]) globalGoals(RoadMapRule ruleAttr, RoadAttributes roadAttr, Crossroad start)
    {
        int[] pDel = new int[3];
        RoadMapRule[] pRuleAttr = new RoadMapRule[3];
        RoadAttributes[] pRoadAttr = new RoadAttributes[3];

        Vector3 t;

        // road 0 - forward

        pDel[0] = 0;
        pRuleAttr[0] = ruleAttr;
        pRoadAttr[0] = roadAttr.highway? ruleAttr.generateHighway(roadAttr, start, in populationDensity) : ruleAttr.genereateByway(roadAttr, start, in populationDensity);

        // branch 1 - left

        pRuleAttr[1] = ruleAttr;
        pRoadAttr[1] = roadAttr;
        pRoadAttr[1].highway = (Random.value < this.probabilityToBranchHighway * populationDensity[Mathf.RoundToInt(start.GetPosition().y), Mathf.RoundToInt(start.GetPosition().x)]);
        pDel[1] = pRoadAttr[1].highway? 0 : this.defaultDelay;
        t = Quaternion.AngleAxis(90, Vector2.up) * new Vector3(roadAttr.direction.x, 0, roadAttr.direction.y);
        pRoadAttr[1].direction = new Vector2(t.x, t.z);

        // branch 2 - right

        pRuleAttr[2] = ruleAttr;
        pRoadAttr[2] = roadAttr;
        pRoadAttr[2].highway = (Random.value < this.probabilityToBranchHighway * populationDensity[Mathf.RoundToInt(start.GetPosition().y), Mathf.RoundToInt(start.GetPosition().x)]);
        pDel[2] = pRoadAttr[2].highway? 0 : this.defaultDelay;
        t = Quaternion.AngleAxis(-90, Vector2.up) * new Vector3(roadAttr.direction.x, 0, roadAttr.direction.y);
        pRoadAttr[2].direction = new Vector2(t.x, t.z);

        return (pDel, pRuleAttr, pRoadAttr);
    }

    private QueryStates localConstraints(RoadAttributes roadAttr, Crossroad start, out Crossroad end)
    {
        Vector2 endingPoint = start.GetPosition() + roadAttr.direction * roadAttr.length;

        if (!isPositionValid(start.GetPosition(), endingPoint, roadAttr.highway))
        {
            endingPoint = fixPosition(endingPoint, roadAttr, start.GetPosition());
            if (endingPoint.Equals(Vector2.zero))
            {
                end = null;
                return QueryStates.FAILED;
            }
        }

        // Debug.LogFormat("heightmap at ({0},{1}) = {2}", endingPoint.x, endingPoint.y, this.heightmap[Mathf.RoundToInt(endingPoint.y), Mathf.RoundToInt(endingPoint.x)]);

        int neighborhoodRadius = Mathf.RoundToInt(roadAttr.length * this.neighborhoodFactor);

        List<IQuadTreeObject> neighborhood = this.quadTree.RetrieveObjectsInArea(new Rect(endingPoint.x - roadAttr.length, 
                                                     endingPoint.y - roadAttr.length, 
                                                     roadAttr.length * 2, 
                                                     roadAttr.length * 2)
                                            );


        Road nearestRoad = null, intersectedRoad = null;
        Vector2 nearestIntersection = Vector2.zero;
        Crossroad nearestCrossRoad = null;

        float distanceToNearestRoad = 0, distanceToNearestCrossRoad = 0, distanceToNearestIntersection = 0;
        Vector2 intersection, linesIntersection;

        if (neighborhood != null)
        {
            foreach(IQuadTreeObject item in neighborhood)
            {
                // IF ITEM IS THE START CROSSROAD
                if (item == start) //|| item.GetPosition() == endingPoint)
                    continue;

                float distanceToItem = Vector2.Distance(endingPoint, item.GetPosition());
                if (distanceToItem > neighborhoodRadius && item is Crossroad)
                    continue;

                if (item is Road road)
                {             
                    // IF THE ROAD INTERSECTS ANOTHER ONE, FIND THE NEAREST FROM START POINT
                    if (LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, road.start.GetPosition(), road.end.GetPosition(), out intersection, out linesIntersection)
                        && !(intersection == start.GetPosition()) && intersection != endingPoint)
                    {
                        float distance = Vector2.Distance(intersection, start.GetPosition());
                        if (nearestIntersection == Vector2.zero || distance < distanceToNearestIntersection)
                        {
                            distanceToNearestIntersection = distance;
                            nearestIntersection = intersection;
                            intersectedRoad = road;
                        }
                    }
                    // IF THEY DO NOT INTERSECT, CHECK IF IS THE NEAREST ROAD
                    else if (distanceToItem <= neighborhoodRadius && (nearestRoad == null || distanceToItem < distanceToNearestRoad))
                    {
                        nearestRoad = road;
                        distanceToNearestRoad = distanceToItem;
                    }
                }
                else
                {
                    // CHECK IF IT IS THE NEAREST CROSSROAD
                    if (nearestCrossRoad == null || distanceToItem < distanceToNearestCrossRoad)
                    {
                        nearestCrossRoad = (Crossroad) item;
                        distanceToNearestCrossRoad = distanceToItem;
                    }
                }
            }
        }

        bool merged = false;

        // CHECK IF A ROAD INTERSECTION WAS FOUND, AND APPLY THE INTERSECTION
        if (nearestIntersection != Vector2.zero)
        {
            end = new Crossroad(nearestIntersection);
            if (!intersectRoads(end, intersectedRoad, neighborhoodRadius / 2f))
                return QueryStates.FAILED;
            merged = true;
        }
        // CHECK IF THERE WERE NO NEAREST ROAD AND CROSSROAD. JUST CONFIRM THE ENDING POINT
        else if (nearestRoad == null && nearestCrossRoad == null)
            end = new Crossroad(endingPoint);

        // ONLY A NEAREST ROAD WAS FOUND
        else if (nearestCrossRoad == null)
        {
            // CHECK IF THE ROADS LINES INTERSECT, AND EVENTUALLY IF THE INTERSECTION IS IN THE NEIGHBORHOOD
            if (!LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out intersection, out linesIntersection)
                && !linesIntersection.Equals(Vector2.zero)
                && Vector2.Distance(linesIntersection, endingPoint) <= neighborhoodRadius)
            {
                end = new Crossroad(linesIntersection);
                merged = true;
                if (!intersectRoads(end, nearestRoad, neighborhoodRadius / 2f))
                    return QueryStates.FAILED;
            }
            else
                end = new Crossroad(endingPoint);
        }

        // ONLY A NEAREST CROSSROAD WAS FOUND
        else if (nearestRoad == null)
        {
            end = nearestCrossRoad;
            merged = true;

            // CHECK IF AN EXISTING ROAD IN THIS CROSSROAD ALREADY CONNECTS TO THE START CROSSROAD
            foreach (Road road in nearestCrossRoad.GetRoadList())
            {
                if (CheckRoadOverlap(start.GetPosition(), end.GetPosition(), road.start.GetPosition(), road.end.GetPosition()))
                    return QueryStates.FAILED;
            }
        }

        // BOTH NEAREST CROSSROAD AND ROAD WERE FOUND
        else
        {
            merged = true;

            LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out _, out linesIntersection);

            // CHECK IF THE ROADS LINES INTERSECTION IS NEARER THAN THE CROSSROAD
            if (!linesIntersection.Equals(Vector2.zero)
                && (linesIntersection != start.GetPosition() && linesIntersection != endingPoint)
                && distanceToNearestRoad < distanceToNearestCrossRoad)
            {
                end = new Crossroad(linesIntersection);
                if (!intersectRoads(end, nearestRoad, neighborhoodRadius / 2f))
                    return QueryStates.FAILED;
            }
            else
            {
                end = nearestCrossRoad;
                foreach (Road road in nearestCrossRoad.GetRoadList())
                {
                    if (CheckRoadOverlap(start.GetPosition(), end.GetPosition(), road.start.GetPosition(), road.end.GetPosition()))
                        return QueryStates.FAILED;
                }
            }
        }

        Road r = new Road(start, end, roadAttr.highway);
        start.AddRoad(r);
        end.AddRoad(r);

        if (!this.graph.ContainsVertex(end))
        {
            this.quadTree.Insert(end);
            this.graph.AddVertex(end);
        }
        this.graph.AddEdge(r);
   
        this.quadTree.Insert(r);

        return merged ? QueryStates.MERGED : QueryStates.SUCCEED;
    }

    private bool CheckRoadOverlap(Vector2 start1, Vector2 end1, Vector2 start2, Vector2 end2)
    {
        Vector2 v1 = (end1 - start1).normalized;
        Vector2 v2 = (end2 - start2).normalized;

        float dot = Vector2.Dot(v1, v2);

        if ((start1 == start2 && LineUtil.Approximately(dot, 1)) ||
            (start1 == end2 && LineUtil.Approximately(dot, -1)))

            return true;

        if ((end1 == start2 && LineUtil.Approximately(dot, -1)) ||
            (end1 == end2 && LineUtil.Approximately(dot, 1)))

            return true;

        return false;
    }

    private bool intersectRoads(Crossroad intersection, Road r, float minimumLength)
    {
        if (intersection.GetPosition() == r.start.GetPosition() ||
            intersection.GetPosition() == r.end.GetPosition())
            return false;

        Road newSection1 = new Road(r.start, intersection, r.highway);
        Road newSection2 = new Road(intersection, r.end, r.highway);

        if ((newSection1.start.GetPosition() - newSection1.end.GetPosition()).magnitude <= minimumLength ||
            (newSection2.start.GetPosition() - newSection2.end.GetPosition()).magnitude <= minimumLength)
            return false;

        r.start.RemoveRoad(r);
        r.end.RemoveRoad(r);

        r.start.AddRoad(newSection1);
        r.end.AddRoad(newSection2);

        intersection.AddRoad(newSection1);
        intersection.AddRoad(newSection2);

        this.quadTree.Remove(r);
        this.quadTree.Insert(newSection1);
        this.quadTree.Insert(newSection2);

        this.graph.RemoveEdge(r);
        this.graph.AddVertex(intersection);
        this.graph.AddEdge(newSection1);
        this.graph.AddEdge(newSection2);
        return true;
    }
    

    private bool isPositionValid(Vector2 start, Vector2 end, bool highway)
    {
        if (end.x < 0 || end.x >= this.heightmap.GetLength(0))
            return false;
        if (end.y < 0 || end.y >= this.heightmap.GetLength(1))
            return false;
        if (this.heightmap[Mathf.RoundToInt(end.y), Mathf.RoundToInt(end.x)] == 0)
            return false;
        if (this.populationDensity[Mathf.RoundToInt(end.y), Mathf.RoundToInt(end.x)] <= (highway? highwayPopDensityLimit : bywayPopDensityLimit))
            return false;
        if (start == end)
            return false;

        Vector2 dir = (end - start).normalized;
        for (int i = 0; i < Mathf.RoundToInt(Vector2.Distance(start, end)); i++)
        {
            Vector2 t = start + i * dir;
            if (this.heightmap[Mathf.RoundToInt(t.y), Mathf.RoundToInt(t.x)] == 0)
                return false;
        }
        return true;
    }

    private Vector2 fixPosition(Vector2 end, RoadAttributes roadAttr, Vector2 start)
    {
        int pruningLimit = Mathf.RoundToInt(roadAttr.length * this.waterPruningFactor);
        int tempLength = roadAttr.length;
        for (; tempLength > pruningLimit; tempLength--)
        {
            end = start + roadAttr.direction * tempLength;
            if (isPositionValid(start, end, roadAttr.highway))
            {
                roadAttr.length = tempLength;
                return end;
            }
        }

        // angle must be within range (1,90)
        int angle = 1;
        Vector2 direction;
        Vector3 t;
        for (int i = 1; i <= this.maximalAngleToFix * 2; i++)
        {
            if ((i % 2) == 0)
                angle = 1 - i;
            else if (i > 1)
                angle = i - 1;
            
            t = Quaternion.AngleAxis(angle, Vector2.up) * new Vector3(roadAttr.direction.x, 0, roadAttr.direction.y);
            direction = new Vector2(t.x, t.z);
            end = start + direction * tempLength;

            if (isPositionValid(start, end, roadAttr.highway))
            {
                roadAttr.direction = direction;
                roadAttr.length = tempLength;
                return end;
            }
        }
        return Vector2.zero;
    }

    public void DrawConnectivity(Texture2D texture)
    {
        FillTextureWithTransparency(texture);

        StronglyConnectedComponentsAlgorithm<Crossroad, Road> algorithm = new(this.graph.ToBidirectionalGraph());
        algorithm.Compute();

        foreach(BidirectionalGraph<Crossroad, Road> g in algorithm.Graphs)
        {
            Color c = Random.ColorHSV();
            foreach(Road r in g.Edges)
                drawLine(texture, r.start.GetPosition(), r.end.GetPosition(), c, r.highway ? this.highwayThickness : this.bywayThickness, false);
        }
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    public void DrawShortestCycles(Texture2D texture)
    {
        FillTextureWithTransparency(texture);

        ShortestGraphCycles shortestGraphCycles = new ShortestGraphCycles(this.graph.Vertices);

        foreach (List<Road> roads in shortestGraphCycles.Compute())
        {
            Color c = Random.ColorHSV();
            foreach (Road r in roads)
                drawLine(texture, r.start.GetPosition(), r.end.GetPosition(), c, r.highway ? this.highwayThickness : this.bywayThickness, false);
        }
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    private void FindConnectivity()
    {
        //DijkstraShortestPathAlgorithm<Crossroad, Road> dijkstra = new DijkstraShortestPathAlgorithm<Crossroad, Road>(this.graph.ToBidirectionalGraph(), x => 1f);
        FloydWarshallAllShortestPathAlgorithm<Crossroad, Road> pathAlgorithm = new FloydWarshallAllShortestPathAlgorithm<Crossroad, Road>(this.graph.ToBidirectionalGraph(), x => 1f);
        pathAlgorithm.Compute();
        //dijkstra.Compute();

        foreach (Crossroad c in this.graph.Vertices)
        {
            string output = "";
            IEnumerable<Road> path;
            if (pathAlgorithm.TryGetPath(c, c, out path))
            //if (dijkstra.)
            {
                foreach (Road r in path)
                {
                    output += r.ToString() + " | ";
                }
                Debug.Log(output);
            }
            else
                Debug.Log("No path found for" + c);
        }

        
    }

    //private void FixRoadMapConnectivity()
    //{
    //    StronglyConnectedComponentsAlgorithm<Crossroad, Road> algorithm;

    //    bool completed = false;

    //    while(!completed)
    //    {
    //        algorithm = new(this.graph.ToBidirectionalGraph());
    //        algorithm.Compute();
    //        Debug.LogFormat("strongly connected components: {0}", algorithm.ComponentCount);

    //        foreach (BidirectionalGraph<Crossroad, Road> component in algorithm.Graphs)
    //        {
    //            float area = 0;
    //            Queue<Crossroad> triangle = new Queue<Crossroad>(3);
    //            foreach (Crossroad v in component.Vertices)
    //            {
    //                if (triangle.Count < 3)
    //                    triangle.Enqueue(v);
    //                else
    //                {
    //                    Crossroad[] t = triangle.ToArray();

    //                    Vector2 a = t[0].GetPosition() - t[1].GetPosition();
    //                    Vector2 b = t[0].GetPosition() - t[2].GetPosition();

    //                    float alpha = Vector2.Angle(a, b);

    //                    area += a.magnitude * b.magnitude * Mathf.Sin(alpha) * 0.5f;

    //                    triangle.Dequeue();
    //                }
    //            }

    //            if (area < this.minLotArea)
    //            {
    //                // MERGE GRAPH
    //                continue;
    //            }
    //        }
    //        completed = true;
    //    }

        
    //}
}
