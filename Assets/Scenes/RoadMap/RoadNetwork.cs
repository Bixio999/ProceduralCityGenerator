using UnityEngine;
using System.Collections.Generic;
using QuikGraph;

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

public class Road : IQuadTreeObject, IEdge<Crossroad> {
    public Crossroad start;
    public Crossroad end;
    public bool highway;
    Vector2 position;

    public Crossroad Source {
        get {
            return start;
        }
    }

    public Crossroad Target {
        get {
            return end;
        }
    }

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

    public IEnumerable<Road> Roads
    {
        get
        {
            return this.roadList;
        }
    }

    public IEnumerable<Crossroad> Crossroads
    {
        get
        {
            return this.crossroadList;
        }
    }

    public void AddRoad(Road r)
    {
        this.roadList.Add(r);
    }

    public void AddCrossroad(Crossroad c)
    {
        this.crossroadList.Add(c);
    }

    public bool RemoveRoad(Road r)
    {
        return this.roadList.Remove(r);
    }

    public bool RemoveCrossroad(Crossroad c)
    {
        return this.crossroadList.Remove(c);
    }

    public bool ContainsCrossroad(Crossroad c)
    {
        return this.crossroadList.Contains(c);
    }
}
