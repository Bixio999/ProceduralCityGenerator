using UnityEngine;

public enum QueryStates
{
    UNASSIGNED = 0,
    SUCCEED = 1,
    FAILED = 2,
    MERGED = 3
};

// Interaction query parameters
public struct RoadAttributes
{
    public int length;
    public Vector2 direction;
    public bool highway;
}

interface RMModule{}

class BranchModule : RMModule
{
    public int del;
    public RoadAttributes roadAttr;
    public RoadMapRule ruleAttr;
    public Crossroad startPoint;
}

class RoadModule : BranchModule
{
    public QueryStates state;
    public Crossroad endPoint;

    public RoadModule()
    {
        this.endPoint = null;
        this.state = QueryStates.UNASSIGNED;
    }

    public RoadModule(BranchModule b)
    {
        this.del = b.del;
        this.roadAttr = b.roadAttr;
        this.ruleAttr = b.ruleAttr;
        this.startPoint = b.startPoint;
        this.endPoint = null;
        this.state = QueryStates.UNASSIGNED;
    }
}