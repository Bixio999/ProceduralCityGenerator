using System.Collections.Generic;
using UnityEngine;

public class RoadMapGenerator : ScriptableObject
{
    // INPUT PARAMETERS
    private float[,] heightmap;
    private float[,] populationDensity;

    // AUXILIARY STRUCTURES
    private QuadTree<IQuadTreeObject> quadTree;
    private RoadNetwork graph; 
    private Queue<RMModule> moduleQueue;

    // GENERATOR'S PARAMETERS
    private float waterPruningFactor;
    private int maximalAngleToFix;
    private float neighborhoodFactor;
    private int defaultDelay;
    private float probabilityToBranchHighway;
    private int highwayThickness;
    private int bywayThickness;
    private int cityRadius;
    private int roadLength;
    private float highwayPopDensityLimit;
    private float bywayPopDensityLimit;

    // OTHER PARAMETERS
    private Crossroad cityCentre;

    /* -------------------------------- */

    /*
     * Create a new instance of RoadMapGenerator and setup the parameters.
     */
    public static RoadMapGenerator CreateInstance(in float[,] heightmap, in float[,] populationDensity, Vector2 cityCentre, int cityRadius, float waterPruningFactor, int maximalAngleToFix, float neighborhoodFactor, int defaultDelay, float probabilityToBranchHighway, int highwayThickness, int bywayThickness, int roadLength, float highwayPopDensityLimit, float bywayPopDensityLimit)
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
        instance.highwayPopDensityLimit = highwayPopDensityLimit;
        instance.bywayPopDensityLimit = bywayPopDensityLimit;

        instance.graph = new RoadNetwork();        

        instance.cityCentre = new Crossroad(cityCentre); // Define the city centre as the first crossroad of the road map
        instance.cityRadius = cityRadius;

        instance.quadTree = new QuadTree<IQuadTreeObject>(4, new Rect(cityCentre.x - cityRadius, cityCentre.y - cityRadius, 2 * cityRadius, 2 * cityRadius));
        instance.moduleQueue = new Queue<RMModule>();

        return instance;
    }

    /* -------------------------------- */

    /*
     * Start the roadmap generation. Assign as global goal rule, the direction of the first road, 
     * and execute the iterations until the limit is reached. 
     */
    public void GenerateRoadMap(RoadMapRule rule, Vector2 initialDirection, int iterationLimit)
    {
        // Define the first module as a new road from the city centre and the given
        // initial direction
        RoadModule r = new RoadModule();
        r.ruleAttr = rule;
        r.roadAttr.highway = true;
        r.roadAttr.length = roadLength;
        r.roadAttr.direction = initialDirection;
        r.startPoint = this.cityCentre;

        // Add the city centre crossroad to quadtree and graph
        this.quadTree.Insert(this.cityCentre);
        this.graph.AddCrossroad(this.cityCentre);

        // Enqueue the module and start the generation
        this.moduleQueue.Enqueue(r);

        RMModule m;
        int iteration;
        for (iteration = 0; this.moduleQueue.Count > 0; iteration++)
        {
            // For each iteration, repeat the module evaluation as many times as
            // the current number of modules in the queue.
            int moduleToEvaulate = this.moduleQueue.Count;

            for(int i = 0; i < moduleToEvaulate; i++)
            {
                m = this.moduleQueue.Dequeue();

                // Check the module type and pass it to the corresponding handler
                if (m is RoadModule module)
                    RoadModuleHandler(module);
                else
                    BranchModuleHandler((BranchModule) m);
            }

            // Once the iteration limit is reacehd, stop the evaluation
            if (iterationLimit > 0 && iteration >= iterationLimit)
                break;
        }

        Debug.LogFormat("iterations: {0}", iteration);

        this.quadTree.Clear();
        this.moduleQueue.Clear();
            

    }

    /*
     * Return the list of generated roads.
     */
    public IEnumerable<Road> GetRoads()
    {
        return this.graph.Roads;
    }

    /*
     * Handler for RoadModule.
     * 
     * Based on the state of the module, do:
     * - if FAILED, destroy the module;
     * - if UNASSIGNED, call LocalConstraints;
     * - if SUCCEED, call GlobalGoals, and create road and side branches.
     */
    private void RoadModuleHandler(RoadModule m)
    {
        // Destroy the module if FAILED or weird delay value
        if (m.del < 0 || m.state == QueryStates.FAILED)
            return;

        switch(m.state)
        {
            case QueryStates.UNASSIGNED:
                // Compute the local constraints for the current road and get
                // the end crossroad
                Crossroad end;
                m.state = LocalConstraints(m.roadAttr, m.startPoint, out end);
                m.endPoint = end;
                this.moduleQueue.Enqueue(m);
                break;
            
            case QueryStates.SUCCEED:
                // Compute the global goals for a new road, and get its data with
                // the side branches
                (int[] pDel, RoadMapRule[] pRuleAttr, RoadAttributes[] pRoadAttr) = this.GlobalGoals(m.ruleAttr, m.roadAttr, m.startPoint);

                // Create the new road module 
                RoadModule r = new RoadModule();
                r.del = pDel[0];
                r.ruleAttr = pRuleAttr[0];
                r.roadAttr = pRoadAttr[0];
                r.startPoint = m.endPoint;
                r.state = QueryStates.UNASSIGNED;

                this.moduleQueue.Enqueue(r);

                // Create the side branch modules
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

    /*
     * Handler for BranchModule.
     * 
     * Check the delay value, and decrease it until zero; then 
     * convert it to a new road module.
     */
    private void BranchModuleHandler(BranchModule m)
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

    /*
     * Define the generic data for a new road from start crossroad plus the two
     * side branches with perpendicular initial direction. The obtained data 
     * follows the global goal rule of this generation.
     * 
     * Each side branch is by default a byway, but has a probability of promotion
     * to highway based on the population density value in its starting 
     * position, and weighted by probabilityToBranchHighway 
     * parameter. 
     */
    private (int[], RoadMapRule[], RoadAttributes[]) GlobalGoals(RoadMapRule ruleAttr, RoadAttributes roadAttr, Crossroad start)
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

    /*
     * The function actually in charge about the creation of a new road by 
     * calculating the ending crossroad.
     * 
     * From the generic data assigned by global goals, complete the creation by 
     * evaluatiing the environment around the initial position, in order to 
     * adjust the road in case of intersection with another roads or crossroads, 
     * or illegal position. 
     * 
     * If the creation succeeds, return the corresponding state. Otherwise warn
     * about failure. 
     * 
     * Whenever a road is merged, no other new roads or branches from it are 
     * generated.
     */
    private QueryStates LocalConstraints(RoadAttributes roadAttr, Crossroad start, out Crossroad end)
    {
        // Compute the potential ending position only by the direction of the road
        Vector2 endingPoint = start.GetPosition() + roadAttr.direction * roadAttr.length;

        // Check for illegal position, and adjust the ending point until valid
        if (!IsPositionValid(start.GetPosition(), endingPoint, roadAttr.highway))
        {
            endingPoint = FixPosition(roadAttr, start.GetPosition());
            if (endingPoint.Equals(Vector2.zero))
            {
                // If no valid positions have been found, generation is stopped
                end = null;
                return QueryStates.FAILED;
            }
        }

        // Get the neighborhood items around the selected ending point
        int neighborhoodRadius = Mathf.RoundToInt(roadAttr.length * this.neighborhoodFactor);

        List<IQuadTreeObject> neighborhood = this.quadTree.RetrieveObjectsInArea(new Rect(endingPoint.x - roadAttr.length, 
                                                     endingPoint.y - roadAttr.length, 
                                                     roadAttr.length * 2, 
                                                     roadAttr.length * 2)
                                            );

        // Search in the neighborhood for the nearest road and crossroad

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
                if (item == start) 
                    continue;

                // Compute the distance to the item, and exclude the ones outside
                // the radius
                float distanceToItem = Vector2.Distance(endingPoint, item.GetPosition());
                if (distanceToItem > neighborhoodRadius && item is Crossroad)
                    continue;

                if (item is Road road)
                {             
                    // IF THE ROAD SEGMENT INTERSECTS ANOTHER ONE, FIND THE NEAREST FROM START POINT - for intersection case
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
                    // IF THEY DO NOT INTERSECT, CHECK IF IS THE NEAREST ROAD - for merging road case
                    else if (distanceToItem <= neighborhoodRadius && (nearestRoad == null || distanceToItem < distanceToNearestRoad))
                    {
                        nearestRoad = road;
                        distanceToNearestRoad = distanceToItem;
                    }
                }
                else
                {
                    // CHECK IF IT IS THE NEAREST CROSSROAD - for merging crossroad case
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
            if (!IntersectRoads(end, intersectedRoad, neighborhoodRadius / 2f))
                return QueryStates.FAILED; // If during the intersection an error occured, stop the generation
            merged = true;
        }
        // CHECK IF THERE WERE NO NEAREST ROAD AND CROSSROAD, JUST CONFIRM THE ENDING POINT
        else if (nearestRoad == null && nearestCrossRoad == null)
            end = new Crossroad(endingPoint);

        // ONLY A NEAREST ROAD WAS FOUND
        else if (nearestCrossRoad == null)
        {
            // CHECK IF THE ROADS LINES INTERSECT, AND EVENTUALLY IF THE INTERSECTION IS IN THE NEIGHBORHOOD
            if (!LineUtil.IntersectLineSegments2D(start.GetPosition(), endingPoint, nearestRoad.start.GetPosition(), nearestRoad.end.GetPosition(), out _, out linesIntersection)
                && !linesIntersection.Equals(Vector2.zero)
                && Vector2.Distance(linesIntersection, endingPoint) <= neighborhoodRadius)
            {
                end = new Crossroad(linesIntersection);
                merged = true;
                if (!IntersectRoads(end, nearestRoad, neighborhoodRadius / 2f))
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
                if (!IntersectRoads(end, nearestRoad, neighborhoodRadius / 2f))
                    return QueryStates.FAILED;
            }
            else // OTHERWISE CONSIDER THE NEAREST CROSSROAD, AND CHECK FOR EXISTING ROADS CONNECTING TO START
            {
                end = nearestCrossRoad;
                foreach (Road road in nearestCrossRoad.GetRoadList())
                {
                    if (CheckRoadOverlap(start.GetPosition(), end.GetPosition(), road.start.GetPosition(), road.end.GetPosition()))
                        return QueryStates.FAILED;
                }
            }
        }

        // Once the end crossroad is found, complete the road generation

        Road r = new Road(start, end, roadAttr.highway);
        start.AddRoad(r);
        end.AddRoad(r);

        // Update the status of the data structures

        if (!this.graph.ContainsCrossroad(end))
        {
            this.quadTree.Insert(end);
            this.graph.AddCrossroad(end);
        }
        this.graph.AddRoad(r);
   
        this.quadTree.Insert(r);

        // Return the correct query state
        return merged ? QueryStates.MERGED : QueryStates.SUCCEED;
    }

    /*
     * Check if the given two roads overlap by evaluating the dot product 
     * between their direction vector and crossroads equivalences.
     */
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

    /*
     * Compute the intersection between the two given roads, if the two sections
     * obtained are at least long as minimumLength. 
     * 
     * The last condition avoids the creation of too small roads. 
     * 
     * If the intersection succeeds, add to the data structures the new sections
     * as roads, and remove the old road.
     */
    private bool IntersectRoads(Crossroad intersection, Road r, float minimumLength)
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

        this.graph.RemoveRoad(r);
        this.graph.AddCrossroad(intersection);
        this.graph.AddRoad(newSection1);
        this.graph.AddRoad(newSection2);
        return true;
    }
    
    /*
     * Check for high-level terrain and population density constraints, for the
     * given road.
     */
    private bool IsPositionValid(Vector2 start, Vector2 end, bool highway)
    {
        end.x = Mathf.Round(end.x);
        end.y = Mathf.Round(end.y);

        // Map boundaries
        if (end.x < 0 || end.x >= this.heightmap.GetLength(0))
            return false;
        if (end.y < 0 || end.y >= this.heightmap.GetLength(1))
            return false;

        // End point on water
        if (this.heightmap[Mathf.RoundToInt(end.y), Mathf.RoundToInt(end.x)] == 0)
            return false;

        // Absent population on end point
        if (this.populationDensity[Mathf.RoundToInt(end.y), Mathf.RoundToInt(end.x)] <= (highway? highwayPopDensityLimit : bywayPopDensityLimit))
            return false;

        // Illegal road
        if (start == end)
            return false;

        // Road over water
        Vector2 dir = (end - start).normalized;
        for (int i = 0; i < Mathf.RoundToInt(Vector2.Distance(start, end)); i++)
        {
            Vector2 t = start + i * dir;
            if (this.heightmap[Mathf.RoundToInt(t.y), Mathf.RoundToInt(t.x)] == 0)
                return false;
        }

        return true;
    }

    /*
     * Try fix an illegal road position by finding a valid ending point.
     * 
     * 1. Reduce road length until the limit value computed by waterPruningFactor
     *    parameter. 
     * 2. Rotate road along y-axis by a max angle (maximalAngleToFix parameter).
     * 
     * If no valid position can be found, (0,0) vector is returned.
     */
    private Vector2 FixPosition(RoadAttributes roadAttr, Vector2 start)
    {
        Vector2 end;

        // Calculate minimum length accepted
        int pruningLimit = Mathf.RoundToInt(roadAttr.length * this.waterPruningFactor);

        // Reduce road length by one unit until valid position is found or length
        // limit is reached
        int tempLength = roadAttr.length;
        for (; tempLength > pruningLimit; tempLength--)
        {
            end = start + roadAttr.direction * tempLength;
            if (IsPositionValid(start, end, roadAttr.highway))
            {
                roadAttr.length = tempLength;
                return end;
            }
        }

        // Progressively rotate road until a valid position is found or angle
        // limit is reached. For each angle, try rotate by each side (+/- angle)
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

            if (IsPositionValid(start, end, roadAttr.highway))
            {
                roadAttr.direction = direction;
                roadAttr.length = tempLength;
                return end;
            }
        }
        return Vector2.zero;
    }


    /* -------------  RENDER AND DRAW FUNCTIONS -------------- */


    /*
     * Render the roadmap by spawning the roads and the crossroads with the given corresponding models.
     * 
     * The highway and byway models represets a single subsection of a road. The entire road is rendered
     * as the combination of multiple models of them.
     */
    public GameObject Render(GameObject highway, GameObject byway, GameObject crossroad, float scalingFactor, float roadModelsLength)
    {
        // Define a parent object as hierarchy folder
        GameObject render = new GameObject("RoadMap Render");

        // Render the roads
        foreach (Road road in this.graph.Roads)
        {
            // Skip any possible bad generated road
            if (road.start.GetPosition() == road.end.GetPosition())
                continue;

            // Get the position of the start and end of the road in 3D world coords
            Vector3 start = ProceduralCityGenerator.ConvertPositionTo3D(road.start.GetPosition());
            Vector3 end = ProceduralCityGenerator.ConvertPositionTo3D(road.end.GetPosition());

            // Compute the direction of the road
            Vector3 vector = (end - start);

            // Calculate the number of subsections needed to render the entire road
            int nSections = Mathf.RoundToInt(vector.magnitude / (roadModelsLength * scalingFactor));

            // Define the gameobject of the entire road
            GameObject model = new GameObject();

            // Spawn the subsections models
            Vector3 prev = Vector3.zero;
            for (int i = 1; i < nSections; i++)
            {
                // Clone and setup the model 
                GameObject roadSection = road.highway ? GameObject.Instantiate(highway) : GameObject.Instantiate(byway);
                roadSection.layer = LayerMask.NameToLayer("Obstacles");
                roadSection.tag = "Road";

                // Calculate the position of the current subsection, and its height 
                Vector3 t = vector * i / nSections + start;
                t.y = Terrain.activeTerrain.SampleHeight(t);

                // Calculate the direction of the subsection by using the previous subsection
                // position and the current one
                Vector3 dir = ((t - start) / scalingFactor - prev).normalized;
                Vector3 curr = prev + dir * roadModelsLength;

                // Define the gameobject rotation for its direction
                Quaternion roadDir = Quaternion.LookRotation(dir);

                // Calculate the roll rotation for road inclination
                Vector3 left = t + roadDir * Vector3.left;
                left.y = Terrain.activeTerrain.SampleHeight(left);

                Vector3 right = t + roadDir * Vector3.right;
                right.y = Terrain.activeTerrain.SampleHeight(right);

                float angle = Vector3.SignedAngle(right - left, roadDir * Vector3.right, roadDir * Vector3.forward);
                Quaternion zRotation = Quaternion.AngleAxis(-angle, Vector3.forward);

                // Setup the subsection model and proceed
                roadSection.transform.localPosition = curr;
                roadSection.transform.localRotation = roadDir * zRotation;
                roadSection.transform.parent = model.transform;

                prev = curr;
            }
            // Scale the entire road object
            model.transform.localScale = Vector3.one * scalingFactor;

            // Move the road to the correct position
            model.transform.position = start;
            model.transform.position += Quaternion.LookRotation(vector) * ((road.highway ? 1.25f : 1) * roadModelsLength * scalingFactor * Vector3.right);

            model.transform.parent = render.transform;
            model.name = road.ToString();
        }

        // Render the crossroads
        foreach (Crossroad c in this.graph.Crossroads)
        {
            // Clone and setup the crossroad model
            GameObject model = GameObject.Instantiate(crossroad);
            model.transform.localScale = model.transform.localScale * scalingFactor;

            // Move it to its position
            Vector3 position = ProceduralCityGenerator.ConvertPositionTo3D(c.GetPosition());
            model.transform.position = position;

            // Find the object rotation by calculating its sloping plane from three points around the
            // crossroad
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

            model.transform.parent = render.transform;
            model.name = c.ToString();
        }
        return render;
    }

    /*
     * Draw into the given texture the roads as lines, maintaing the real aspect from the terrain.
     */
    public void DrawDebug(Texture2D texture)
    {
        FillTextureWithTransparency(texture);

        foreach (Road road in this.graph.Roads)
            DrawLine(texture, road.start.GetPosition(), road.end.GetPosition(), road.highway ? Color.black : Color.grey, road.highway ? this.highwayThickness : this.bywayThickness, road.highway);

        texture.Apply();
        texture.wrapMode = TextureWrapMode.Clamp;
    }

    /*
     * Create an high resolution texture with only the roadmap. Use the scaling factor to 
     * calculate the dimension of the texture, based on the heightmap resolution.
     */
    public Texture2D GenerateHighResTexture(float textureScalingFactor)
    {
        int width = (int)(this.heightmap.GetLength(0) * textureScalingFactor);
        int height = (int)(this.heightmap.GetLength(1) * textureScalingFactor);

        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, true);
        FillTextureWithTransparency(texture);

        Vector2 origin = new Vector2((float)width / 2, (float)height / 2);

        float scalingFactor = (float)Mathf.Max(width, height) / (cityRadius * 2);

        foreach (Road road in this.graph.Roads)
        {
            Vector2 roadStart = ((road.start.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;
            Vector2 roadEnd = ((road.end.GetPosition() - this.cityCentre.GetPosition()) * scalingFactor) + origin;

            DrawLine(texture, roadStart, roadEnd, road.highway ? Color.black : Color.grey, Mathf.RoundToInt((road.highway ? this.highwayThickness : this.bywayThickness) * scalingFactor), road.highway);
        }

        texture.Apply();
        return texture;
    }

    /*
     * Overwrite the entire texture with clean color.
     */
    private void FillTextureWithTransparency(Texture2D texture)
    {
        Color[] colors = new Color[texture.width * texture.height];
        texture.SetPixels(0, 0, texture.width, texture.height, colors);
        texture.Apply();
    }

    /*
     * Draw a line in the given texture, from start to end. Set the color and the thickness of the line.
     * 
     * If overwrite is set to false, avoid writing over an already painted pixel. Highway are the only allowed
     * to overwrite roads.
     */
    private void DrawLine(Texture2D tex, Vector2 start, Vector2 end, Color color, int thickness, bool overwrite)
    {
        Vector2 dir = end - start;
        Vector2 n1 = new Vector2(dir.y, -dir.x);
        Vector2 n2 = new Vector2(-dir.y, dir.x);

        float length = dir.magnitude;
        dir.Normalize();
        n1.Normalize();
        n2.Normalize();

        thickness = ((int)Mathf.Ceil((float)thickness / 2));

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

}
