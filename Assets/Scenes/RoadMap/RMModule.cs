using UnityEngine;

public enum RoadMapRules
{
    basic = 0,
    checkers = 1,
    sanFrancisco = 2
};

public enum QueryStates
{
    UNASSIGNED = 0,
    SUCCEED = 1,
    FAILED = 2
};

// Road map rule parameters
struct RuleAttributes
{
    public bool highway;
    public RoadMapRules rule;
};

// Interaction query parameters
struct RoadAttributes
{
    public int length;
    public Vector2 direction;
}

interface RMModule{}

class BranchModule : RMModule
{
    public int del;
    public RoadAttributes roadAttr;
    public RuleAttributes ruleAttr;
    public Crossroad startPoint;
}

class RoadModule : BranchModule
{
    public QueryStates state;

    public RoadModule()
    {
        this.state = QueryStates.UNASSIGNED;
    }

    public RoadModule(BranchModule b)
    {
        this.del = b.del;
        this.roadAttr = b.roadAttr;
        this.ruleAttr = b.ruleAttr;
        this.startPoint = b.startPoint;
        this.state = QueryStates.UNASSIGNED;
    }
}