using UnityEngine;
using System.Collections.Generic;

public class Crossroad : IQuadTreeObject{
    List<Road> roadList;
    Vector2 position;

    public Vector2 GetPosition()
    {
        return position;
    }

    public void AddRoad(Road r)
    {
        this.roadList.Add(r);
    }

    public void RemoveRoad(Road r)
    {
        this.roadList.Remove(r);
    }

    public Crossroad(Vector2 position)
    {
        this.roadList = new List<Road>();
        this.position = position;
    }

    public List<Road> GetRoadList()
    {
        return new List<Road>(this.roadList);
    }

    public override string ToString()
    {
        return "Crossroad: " + this.position;
    }
}

public class Road : IQuadTreeObject {
    public Crossroad start;
    public Crossroad end;
    public bool highway;
    Vector2 position;

    public Vector2 GetPosition()
    {
        return position;
    }

    public override string ToString()
    {
        return "Road: " + this.start + " to " + this.end;
    }

   public Road(Crossroad start, Crossroad end, bool highway)
   {
        this.start = start;
        this.end = end;
        this.highway = highway;
        this.position = (end.GetPosition() - start.GetPosition()) * 0.5f + start.GetPosition();
   }
}

public class RoadNetwork {
    List<Crossroad> crossroadList = new List<Crossroad>();
    List<Road> roadList = new List<Road>();
    Crossroad currentCrossroad;

    public void AddRoad(Road r)
    {
        this.roadList.Add(r);
    }
}
