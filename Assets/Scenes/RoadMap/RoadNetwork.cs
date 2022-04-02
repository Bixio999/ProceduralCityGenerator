using UnityEngine;
using System.Collections.Generic;

class Crossroad : IQuadTreeObject{
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

   public Crossroad(Vector2 position)
   {
       this.roadList = new List<Road>();
       this.position = position;
   }
}

class Road : IQuadTreeObject {
   public Crossroad start;
   public Crossroad end;
   public bool isOneWay;
   Vector2 position;

   public Vector2 GetPosition()
   {
       return position;
       
   }

   public Road(Crossroad start, Crossroad end, bool isOneWay)
   {
       this.start = start;
       this.end = end;
       this.isOneWay = isOneWay;
       this.position = (end.GetPosition() - start.GetPosition()) * 0.5f + start.GetPosition();
   }
}

public class RoadNetwork {
   List<Crossroad> crossroadList = new List<Crossroad>();
   List<Road> roadList = new List<Road>();
   Crossroad currentCrossroad;
}
