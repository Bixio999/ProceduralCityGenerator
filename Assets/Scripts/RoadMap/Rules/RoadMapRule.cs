
/*
 * Interface for global goals rule.
 * 
 * Each rule must implements these two functions, to allow the generation of 
 * highway and byway. 
 */
public interface IRoadMapRule 
{
    public abstract RoadAttributes GenerateHighway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity);
    public abstract RoadAttributes GenereateByway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity);
}

