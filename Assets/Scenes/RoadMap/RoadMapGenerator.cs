using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using QuikGraph;

public class RoadMapGenerator : ScriptableObject
{
    private float[,] heightmap;
    private float[,] populationDensity;
    private QuadTree<IQuadTreeObject> quadTree;
    private List<Road> roadList;
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

    public static RoadMapGenerator CreateInstance(float[,] heightmap, float[,] populationDensity, Vector2 cityCentre, int cityRadius, float waterPruningFactor, int maximalAngleToFix, float neighborhoodFactor, int defaultDelay, float probabilityToBranchHighway, int highwayThickness, int bywayThickness)
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

        instance.cityCentre = new Crossroad(cityCentre);
        instance.cityRadius = cityRadius;

        instance.roadList = new List<Road>();
        instance.quadTree = new QuadTree<IQuadTreeObject>(4, new Rect(cityCentre.x - cityRadius, cityCentre.y - cityRadius, cityCentre.x + cityRadius, cityCentre.y + cityRadius));
        instance.moduleQueue = new Queue<RMModule>();

        return instance;
    }

    public Texture2D generateRoadMap(RoadMapRule rule, Vector2 initialDirection, int roadLength, int iterationLimit)
    {
        RoadModule r = new RoadModule();
        r.ruleAttr = rule;
        r.roadAttr.highway = true;
        r.roadAttr.length = roadLength;
        r.roadAttr.direction = initialDirection;
        r.startPoint = this.cityCentre;
        
        this.quadTree.Insert(this.cityCentre);

        this.moduleQueue.Enqueue(r);


        RMModule m;

        int iteration;
        for (iteration = 0; this.moduleQueue.Count > 0; iteration++)
        {
            // if (iteration == 21)
            // {
            //     Debug.Log("iteration limit reached");
            // }

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

            // m = this.moduleQueue.Dequeue();
            // if (m is RoadModule)
            //     roadModuleHandler((RoadModule) m);
            // else
            //     branchModuleHandler((BranchModule) m);
        }

        // this.quadTree.DrawDebug();

        Debug.LogFormat("iterations: {0}", iteration);

        Texture2D texture = new Texture2D(this.heightmap.GetLength(0), this.heightmap.GetLength(1), TextureFormat.RGBA32, true);
        FillTextureWithTransparency(texture);

        foreach(Road road in this.roadList)
            drawLine(texture, road.start.GetPosition(), road.end.GetPosition(), road.highway? Color.black : Color.grey, road.highway? this.highwayThickness : this.bywayThickness, road.highway);

        // Queue<Crossroad> crossroads = new Queue<Crossroad>();
        // crossroads.Enqueue(this.cityCentre);

        // while (crossroads.Count > 0)
        // {
        //     Crossroad c = crossroads.Dequeue();
        //     foreach(Road road in c.GetRoadList())
        //     {
        //         drawLine(texture, road.start.GetPosition(), road.end.GetPosition(), road.highway? Color.black : Color.grey, road.highway? this.highwayThickness : this.bywayThickness, road.highway);
        //         crossroads.Enqueue(road.start == c ? road.end : road.start);
        //         road.end.RemoveRoad(road);
        //     }
        // }
        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
        
        return texture;
    }

    public Texture2D generateHighResTexture(float textureScalingFactor)
    {
        int width = (int) (this.heightmap.GetLength(0) * textureScalingFactor);
        int height = (int) (this.heightmap.GetLength(1) * textureScalingFactor);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        FillTextureWithTransparency(texture);

        Vector2 origin = new Vector2((float)width / 2, (float)height / 2);

        float scalingFactor = (float) Mathf.Max(width, height) / (cityRadius * 2);

        foreach(Road road in this.roadList)
        {
            Vector2 roadStart = ((road.start.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;
            Vector2 roadEnd = ((road.end.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;

            drawLine(texture, roadStart, roadEnd, road.highway? Color.black : Color.grey, Mathf.RoundToInt((road.highway? this.highwayThickness : this.bywayThickness) * scalingFactor), road.highway);   
        }

        // Queue<Crossroad> crossroads = new Queue<Crossroad>();
        // crossroads.Enqueue(this.cityCentre);

        // while(crossroads.Count > 0)
        // {
        //     Crossroad c = crossroads.Dequeue();
        //     foreach(Road road in c.GetRoadList())
        //     {
        //         Vector2 roadStart = ((road.start.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;
        //         Vector2 roadEnd = ((road.end.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;

        //         drawLine(texture, roadStart, roadEnd, road.highway? Color.black : Color.grey, Mathf.RoundToInt((road.highway? this.highwayThickness : this.bywayThickness) * scalingFactor), road.highway);


        //         // drawLine(texture, road.start.GetPosition(), road.end.GetPosition(), road.highway? Color.black : Color.grey, road.highway? this.highwayThickness : this.bywayThickness);
        //         crossroads.Enqueue(road.start == c ? road.end : road.start);
        //         // road.end.RemoveRoad(road);
        //     }
        // }
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

        if (!isPositionValid(endingPoint, roadAttr.highway))
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

        List<IQuadTreeObject> neighborhood = this.quadTree.RetrieveObjectsInArea(new Rect(endingPoint.x - neighborhoodRadius, 
                                                     endingPoint.y - neighborhoodRadius, 
                                                     neighborhoodRadius * 2, 
                                                     neighborhoodRadius * 2)
                                            );


        Road nearestRoad = null;
        Crossroad nearestCrossRoad = null;

        float distanceToNearestRoad = 0, distanceToNearestCrossRoad = 0;

        if (neighborhood != null)
        {
            foreach(IQuadTreeObject item in neighborhood)
            {
                if (item == start)
                    continue;

                float distanceToItem = Vector2.Distance(endingPoint, item.GetPosition());
                if (distanceToItem > neighborhoodRadius)
                    continue;

                if (item is Road)
                {
                    if (nearestRoad == null || distanceToItem < distanceToNearestRoad)
                    {
                        nearestRoad = (Road) item;
                        distanceToNearestRoad = distanceToItem;
                    }
                }
                else
                {
                    if (nearestCrossRoad == null || distanceToItem < distanceToNearestCrossRoad)
                    {
                        nearestCrossRoad = (Crossroad) item;
                        distanceToNearestCrossRoad = distanceToItem;
                    }
                }
            }
        }

        bool merged = false;

        if (nearestRoad == null && nearestCrossRoad == null)
            end = new Crossroad(endingPoint);
        else if (nearestCrossRoad == null)
        {
            Vector2 intersection, linesIntersection;
            if (LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out intersection, out linesIntersection))
            {
                end = new Crossroad(intersection);
                merged = true;
            }
            else if (!linesIntersection.Equals(Vector2.zero))
            {
                end = new Crossroad(linesIntersection);
                merged = true;
            }
            else
                end = new Crossroad(endingPoint);

            if (merged)
                intersectRoads(start, end, nearestRoad);
        }
        else if (nearestRoad == null)
        {
            end = nearestCrossRoad;
            merged = true;
            foreach(Road road in nearestCrossRoad.GetRoadList())
            {
                if (road.start == start && distanceToNearestCrossRoad == 0)
                    return QueryStates.FAILED;
                if (road.start == start || road.end == start)
                {
                    end = new Crossroad(endingPoint);
                    merged = false;
                    break;
                }
            }
        }
        else if (Vector2.Distance(endingPoint, nearestRoad.GetPosition()) < Vector2.Distance(endingPoint, nearestCrossRoad.GetPosition()))
        {
            Vector2 intersection, linesIntersection;
            if (LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out intersection, out linesIntersection))
            {
                end = new Crossroad(intersection);
                merged = true;
            }
            else if (!linesIntersection.Equals(Vector2.zero))
            {
                end = new Crossroad(linesIntersection);
                merged = true;
            }
            else
                end = new Crossroad(endingPoint);

            if (merged)
                intersectRoads(start, end, nearestRoad);
        }
        else
        {
            end = nearestCrossRoad;
            merged = true;
        }

        Road r = new Road(start, end, roadAttr.highway);
        start.AddRoad(r);
        end.AddRoad(r);

        this.roadList.Add(r);

        this.quadTree.Insert(end);
        this.quadTree.Insert(r);

        // Debug.LogFormat("Road {0} - {1}", start.GetPosition(), end.GetPosition());

        return merged ? QueryStates.MERGED : QueryStates.SUCCEED;
    }

    private void intersectRoads(Crossroad start, Crossroad intersection, Road r)
    {            
        Road newSection1 = new Road(r.start, intersection, r.highway);
        Road newSection2 = new Road(intersection, r.end, r.highway);

        r.start.RemoveRoad(r);
        r.end.RemoveRoad(r);

        r.start.AddRoad(newSection1);
        r.end.AddRoad(newSection2);
    }
    

    private bool isPositionValid(Vector2 position, bool highway)
    {
        if (position.x < 0 || position.x >= this.heightmap.GetLength(0))
            return false;
        if (position.y < 0 || position.y >= this.heightmap.GetLength(1))
            return false;
        if (this.heightmap[Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.x)] == 0)
            return false;
        if (this.populationDensity[Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.x)] <= (highway? 0.1f : 0.01f))
            return false;
        return true;
    }

    private Vector2 fixPosition(Vector2 end, RoadAttributes roadAttr, Vector2 start)
    {
        int pruningLimit = Mathf.RoundToInt(roadAttr.length * this.waterPruningFactor);
        int tempLength = roadAttr.length;
        for (; tempLength > pruningLimit; tempLength--)
        {
            end = start + roadAttr.direction * tempLength;
            if (isPositionValid(end, roadAttr.highway))
            {
                roadAttr.length = tempLength;
                return end;
            }
        }

        // angle must be within range (1,90)
        int angle;
        Vector2 direction;
        Vector3 t;
        for (int i = 1; i <= this.maximalAngleToFix * 2; i++)
        {
            if ((i % 2) == 0)
                angle = 1 - i;
            else
                angle = i;
            
            t = Quaternion.AngleAxis(angle, Vector2.up) * new Vector3(roadAttr.direction.x, 0, roadAttr.direction.y);
            direction = new Vector2(t.x, t.z);
            end = start + direction * tempLength;

            if (isPositionValid(end, roadAttr.highway))
            {
                roadAttr.direction = direction;
                roadAttr.length = tempLength;
                return end;
            }
        }
        return Vector2.zero;
    }

}
