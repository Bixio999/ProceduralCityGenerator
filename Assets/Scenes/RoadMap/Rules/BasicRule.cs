using UnityEngine;

public class BasicRule : RoadMapRule
{
    protected int scanRays = 5; // 5
    [SerializeField]
    protected int scanRadius = 20; // 20
    [SerializeField]

    private Vector2 scanForMaxPopulationDensity(Vector2 direction, Vector2 position, int length, in float[,] populationDensity)
    {
        Vector2 maxDirection = direction;
        float maxDensity = 0;

        for (float i = - (float) this.scanRadius / 2; i < this.scanRadius / 2; i += (float) this.scanRadius / this.scanRays)
        {
            Vector3 scanDir = Quaternion.AngleAxis(i, Vector3.up) * new Vector3(direction.x, 0, direction.y);
            Vector2 scanDirection = new Vector2(scanDir.x, scanDir.z);

            Vector2 scanPosition;
            float weight = 0;

            for (int j = 0; j < length; j++)
            {
                scanPosition = position + scanDirection * j;

                if (!isValidPosition(Mathf.RoundToInt(scanPosition.y), Mathf.RoundToInt(scanPosition.x), populationDensity.GetLength(0), populationDensity.GetLength(1)))
                    break;
                weight += populationDensity[Mathf.RoundToInt(scanPosition.y), Mathf.RoundToInt(scanPosition.x)] * (1 - ((float)j / length));
            }
            
            if (weight > maxDensity)
            {
                maxDensity = weight;
                maxDirection = scanDirection;
            }
        }
        return maxDirection;
    }

    public static bool isValidPosition(int x, int y, int w, int h)
    {
        if (x < 0 || x >= w)
            return false;
        if (y < 0 || y >= h)
            return false;
        return true;
    }

    public RoadAttributes generateHighway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity)
    {
        roadAttr.direction = this.scanForMaxPopulationDensity(roadAttr.direction, start.GetPosition(), roadAttr.length, populationDensity);
        return roadAttr;
    }

    public RoadAttributes genereateByway(RoadAttributes roadAttr, Crossroad start, in float[,] populationDensity)
    {
        return generateHighway(roadAttr, start, populationDensity);
    }
}