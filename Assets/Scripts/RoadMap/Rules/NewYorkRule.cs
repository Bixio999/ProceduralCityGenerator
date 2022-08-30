
/*
 * Example of alternative rule, similar to the road distribution of Manhattan
 * in New York: place roads with angles of 90 degrees.
 * 
 * This rule is obtained by just setting scanAngle to zero, with just one ray 
 * as test.
 */
public class NewYorkRule : BasicRule
{
    public NewYorkRule()
    {
        scanAngle = 0;
        scanRays = 1;
    }
}
