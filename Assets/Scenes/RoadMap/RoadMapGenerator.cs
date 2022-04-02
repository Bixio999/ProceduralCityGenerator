using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadMapGenerator : MonoBehaviour
{
    private float[,] heightmap;
    private float[,] populationDensity;
    private QuadTree<IQuadTreeObject> quadTree;
    private RoadNetwork roadNetwork;
    private Queue<RMModule> moduleQueue;
    // private Vector2 cityCentre;
    private float waterPruningFactor;
    private int maximalAngleToFix;
    private float neighborhoodFactor;

    public RoadMapGenerator(float[,] heightmap, float[,] populationDensity, Vector2 cityCentre, float waterPruningFactor, int maximalAngleToFix, float neighborhoodFactor)
    {
        this.heightmap = heightmap;
        this.populationDensity = populationDensity;

        this.waterPruningFactor = waterPruningFactor;
        this.maximalAngleToFix = maximalAngleToFix;
        this.neighborhoodFactor = neighborhoodFactor;

        this.roadNetwork = new RoadNetwork();
        this.quadTree = new QuadTree<IQuadTreeObject>(new Rect(0, 0, heightmap.GetLength(0), heightmap.GetLength(1)));
        this.moduleQueue = new Queue<RMModule>();
    }

    public TerrainLayer generateRoadMap(RoadMapRules rule, Vector2 initialDirection, int roadLength, Vector2 cityCentre)
    {
        Crossroad p = new Crossroad(cityCentre);

        RoadModule r = new RoadModule();
        r.ruleAttr.rule = rule;
        r.ruleAttr.highway = true;
        r.roadAttr.length = roadLength;
        r.roadAttr.direction = initialDirection;
        r.startPoint = p;
        
        this.quadTree.Insert(p);

        this.moduleQueue.Enqueue(r);

        RMModule m;
        while(this.moduleQueue.Count > 0)
        {
            m = this.moduleQueue.Dequeue();
            if (m is RoadModule)
                roadModuleHandler((RoadModule) m);
            else
                branchModuleHandler((BranchModule) m);
        }

        return null;
    }

    private void roadModuleHandler(RoadModule m)
    {
        if (m.del < 0 || m.state == QueryStates.FAILED)
            return;

        switch(m.state)
        {
            case QueryStates.UNASSIGNED:
                m.state = localConstraints(m.roadAttr, m.startPoint);
                break;
            
            case QueryStates.SUCCEED:
                
                break;

            default:
                return;
        }
        this.moduleQueue.Enqueue(m);
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

    private QueryStates localConstraints(RoadAttributes roadAttr, Crossroad start)
    {
        Vector2 endingPoint = start.GetPosition() + roadAttr.direction * roadAttr.length;

        if (!isPositionValid(endingPoint))
        {
            endingPoint = fixPosition(endingPoint, roadAttr, start.GetPosition());
            if (endingPoint.Equals(Vector2.zero))
                return QueryStates.FAILED;
        }

        int neighborhoodRadius = Mathf.RoundToInt(roadAttr.length * this.neighborhoodFactor);

        List<IQuadTreeObject> neighborhood = this.quadTree.RetrieveObjectsInArea(new Rect(endingPoint.x - neighborhoodRadius, 
                                                     endingPoint.y - neighborhoodRadius, 
                                                     endingPoint.x + neighborhoodRadius, 
                                                     endingPoint.y + neighborhoodRadius)
                                            );

        Road nearestRoad = null;
        Crossroad nearestCrossRoad = null;
        foreach(IQuadTreeObject item in neighborhood)
        {
            float distanceToItem = Vector2.Distance(endingPoint, item.GetPosition());
            if (distanceToItem > neighborhoodRadius)
                continue;

            if (item is Road)
            {
                if (nearestRoad == null)
                    nearestRoad = (Road) item;
                else if (distanceToItem < Vector2.Distance(endingPoint, nearestRoad.GetPosition()))
                    nearestRoad = (Road) item;
            }
            else
            {
                if (nearestCrossRoad == null)
                    nearestCrossRoad = (Crossroad) item;
                else if (distanceToItem < Vector2.Distance(endingPoint, nearestCrossRoad.GetPosition()))
                    nearestCrossRoad = (Crossroad) item;
            }
        }

        Crossroad end;
        if (nearestRoad == null && nearestCrossRoad == null)
            end = new Crossroad(endingPoint);
        else if (nearestCrossRoad == null)
        {
            Vector2 intersection, linesIntersection;
            if (LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out intersection, out linesIntersection))
                end = new Crossroad(intersection);
            else if (!linesIntersection.Equals(Vector2.zero))
                end = new Crossroad(linesIntersection);
            else
                end = new Crossroad(endingPoint);
        }
        else if (nearestRoad == null)
            end = nearestCrossRoad;
        else if (Vector2.Distance(endingPoint, nearestRoad.GetPosition()) < Vector2.Distance(endingPoint, nearestCrossRoad.GetPosition()))
        {
            Vector2 intersection, linesIntersection;
            if (LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out intersection, out linesIntersection))
                end = new Crossroad(intersection);
            else if (!linesIntersection.Equals(Vector2.zero))
                end = new Crossroad(linesIntersection);
            else
                end = new Crossroad(endingPoint);
        }
        else
            end = nearestCrossRoad;

        Road r = new Road(start, end, false);
        end.AddRoad(r);

        this.quadTree.Insert(end);
        this.quadTree.Insert(r);

        return QueryStates.SUCCEED;
    }

    private bool isPositionValid(Vector2 position)
    {
        if (position.x < 0 || position.x >= this.heightmap.GetLength(0))
            return false;
        if (position.y < 0 || position.y >= this.heightmap.GetLength(1))
            return false;
        if (this.heightmap[Mathf.RoundToInt(position.y), Mathf.RoundToInt(position.x)] == 0)
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
            if (isPositionValid(end))
            {
                roadAttr.length = tempLength;
                return end;
            }
        }

        // angle must be within range (1,90)
        int angle;
        Vector2 direction;
        for (int i = 1; i <= this.maximalAngleToFix * 2; i++)
        {
            if ((i % 2) == 0)
                angle = 1 - i;
            else
                angle = i;
            
            direction = Quaternion.AngleAxis(angle, Vector2.up) * roadAttr.direction;
            end = start + direction * tempLength;

            if (isPositionValid(end))
            {
                roadAttr.direction = direction;
                roadAttr.length = tempLength;
                return end;
            }
        }
        return Vector2.zero;
    }

}
