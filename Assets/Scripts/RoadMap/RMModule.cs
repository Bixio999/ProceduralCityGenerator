using UnityEngine;

/*
 * Defines the current status of a road or branch module.
 */
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
    public int length; // Default road length
    public Vector2 direction; // Direction of the current road
    public bool highway; // Road type
}

/* ----- MODULES DEFINITION ----- */

interface IRMModule {}

class BranchModule : IRMModule
{
    public int del; // Current delay value
    public RoadAttributes roadAttr; // road parameters
    public IRoadMapRule ruleAttr; // Global goals rule
    public Crossroad startPoint; // start crossroad
}

class RoadModule : BranchModule
{
    public QueryStates state; // status of road generation
    public Crossroad endPoint; // end crossroad

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