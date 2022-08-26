using UnityEngine;

/*
 * Use this class to generate procedural heightmap and population density map.
 * Both generate values by using Perlin noise function with several frequencies
 * and octaves.
 * 
 * The noise function can be modified from editor, changing the values for octaves,
 * scaling factor and exponent factor. 
 * 
 * Population density also uses two decay functions to improve complexity make 
 * the generation more realistic: 
 * - Cosine-down-to-zero: used to decrease the value by distance from city centre;
 *   if more distant than the radius of the city, set value to 0
 * - Gaussian: the value decreases the farther the height is from that of the city 
 *   center
 */
public class InputMapGenerator
{
    /*
     * Utility function used to check if a position is inside map boundaries.
     */
    public static bool IsValidPosition(int x, int y, int w, int h)
    {
        if (x < 0 || x >= w)
            return false;
        if (y < 0 || y >= h)
            return false;
        return true;
    }

    /* ---------------------------- */

    // PERLIN NOISE PROCEDURAL GENERATION

    /*
     * Generate the heightmap of a terrain with Perlin Noise function.
     * 
     * Control the noise function with parameters:
     * - scaleFactor: set the noise function coords scaling
     * - nOctaves: the number of frequencies to calculate
     * - exponent: the factor to consider with final power; used to increase
     *    terrain valleys by lowering small numbers
     *    
     * Use the waterThreshold parameter to set the height from which the ground 
     * is water.
     */
    public static float[,] GeneratePerlinNoiseMap(int w, int h, float scaleFactor, int nOctaves, float exponent, float waterThreshold)
    {
        // Calculate the origin of values acquisition
        float amplifier = Random.value * 1000;
        Vector2 origin = Random.insideUnitCircle * amplifier;

        Debug.LogFormat("origin x = {0} | origin y = {1} | amplifier = {2}", origin.x, origin.y, amplifier);

        // Define the heightmap
        float[,] map = new float[w,h];

        // Calculate the values

        float value, xCoord, yCoord, normalizer, amplitude;
        int alpha;

        // Memorize the min and max values 
        float maxValue = 0f;
        float minValue = Mathf.Infinity;

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                // Calculate the current coords
                xCoord = origin.x + (float) j / w * scaleFactor;
                yCoord = origin.y + (float) i / h * scaleFactor;

                value = 0f;
                normalizer = 0f;

                // Calculate the octaves 
                for (int k = 0; k < nOctaves; k++)
                {
                    alpha = (int) Mathf.Pow(2, k);
                    amplitude = 1 / ((float) alpha);

                    normalizer += amplitude;

                    value += amplitude * Mathf.Clamp01(Mathf.PerlinNoise(alpha * xCoord, alpha * yCoord));
                }
                value /= normalizer;

                // Power the obtained value
                value = Mathf.Pow(value, exponent);

                // Store the computed value
                map[i,j] = value;

                // Update the min/max
                if (maxValue < value) maxValue = value;
                if (minValue > value) minValue = value;
            }
        }

        // Calculate the waterThreshold height
        maxValue -= minValue;
        waterThreshold *= maxValue;

        // Set the water values and decrease all by waterThreshold in order
        // to have them at height value 0
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                value = map[i,j];

                value -= minValue;

                value = value > waterThreshold ? value : waterThreshold; 
                value -= waterThreshold;
                map[i,j] = value;
            }
        }

        Debug.LogFormat("minValue = {0}, maxValue = {1}, waterThreshold = {2}", minValue, maxValue, waterThreshold * maxValue);

        return map;
    }

    /* ---------------------------- */

    // POPULATION DENSITY MAP GENERATION

    /*
     * Generate the population density map from the terrain height map. The raw 
     * values are obtained from the Perlin noise function and weighted by a pair 
     * of distance functions used to smooth out the values farthest from the 
     * city center, on xz plane and y axis (unity axes reference).
     * 
     * First, decide on the city center by randomly choosing a location on the 
     * map until find one with sufficient properties. 
     * 
     * The decision on the city center can be controlled by reducing map 
     * boundaries, terrain flatness, and proximity to water.
     */
    public static (float [,] populationMap, Vector2 cityCentre) CreatePopulationDensityMap(float [,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold, float mapBoundaryScale, float cityRadius, float heightTolerance, float PNScaling, int PNOctaves, float perlinNoiseExponent)
    {
        // City centre decision

        int cityCentreX, cityCentreY;
        do
        {
            cityCentreX = Mathf.RoundToInt((w - 1) * mapBoundaryScale * Random.value + (w - 1) * (1 - mapBoundaryScale) / 2);
            cityCentreY = Mathf.RoundToInt((h - 1) * mapBoundaryScale * Random.value + (h - 1) * (1 - mapBoundaryScale) / 2);
        } while(!IsCityCentreValid(cityCentreX, cityCentreY, heightmap, w, h, slopeThreshold, originRadius, waterThreshold));


        // Population density generation

        float[,] populationMap = new float[h,w];

        float value, PNvalue, xCoord, yCoord, normalizer, amplitude;
        int alpha;

        // Obtain values from Perlin noise

        float amplifier = Random.value * 1000;
        Vector2 perlinNoiseOrigin = Random.insideUnitCircle * amplifier;

        // Store min/max values to normalize map later
        float maxValue = 0f;
        float minValue = Mathf.Infinity;

        for (int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                xCoord = perlinNoiseOrigin.x + (float) j / w * PNScaling;
                yCoord = perlinNoiseOrigin.y + (float) i / h * PNScaling;

                PNvalue = 0f;
                normalizer = 0f;

                if (heightmap[i,j] != 0)
                {
                    // Compute the weight by distance functions

                    value = DistanceToCityOriginProbability(cityCentreX, cityCentreY, j, i, cityRadius);
                    value *= heightToCityCentreProbability(heightmap[cityCentreY, cityCentreX], heightmap[i,j], slopeThreshold, heightTolerance);

                    for (int k = 0; k < PNOctaves; k++)
                    {
                        alpha = (int) Mathf.Pow(2, k);
                        amplitude = 1 / ((float) alpha);

                        normalizer += amplitude;

                        PNvalue += amplitude * Mathf.Clamp01(Mathf.PerlinNoise(alpha * xCoord, alpha * yCoord));
                    }

                    // Compute the final value by weighting the perlin noise value
                    if (PNvalue > 0)
                    {
                        PNvalue /= normalizer;
                        PNvalue = Mathf.Pow(PNvalue, perlinNoiseExponent);
                        value *= (PNvalue);
                    }

                    populationMap[i,j] = value;

                    // Update min/max value
                    if (maxValue < value) maxValue = value;
                    if (minValue > value) minValue = value;
                }                    
            }
        }

        // Apply value normalization

        for(int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                value = populationMap[i, j];
                populationMap[i, j] = (value - minValue) / (maxValue - minValue);
            }
        }

        return (populationMap, new Vector2(cityCentreX, cityCentreY));
    }

    /*
     * Check if the given position can be a valid city centre.
     * 
     * Evaluate terrain flatness and water proximity around it to decide if 
     * valid enough.
     */
    private static bool IsCityCentreValid(int centreX, int centreY, float[,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold)
    {
        // Check if position is on water
        float centreValue = heightmap[centreY, centreX];
        if (centreValue == 0)
            return false;

        // Evaluate the neighborhood
        float value;
        int waterCounter = 0; // Count the number of position on the water
        for (int i = - originRadius; i <= originRadius; i++)
        {
            for (int j = - originRadius; j <= originRadius; j++)
            {
                // Check if some position is outside map boundaries
                if (IsValidPosition(j + centreX, i + centreY, w, h))
                {
                    value = heightmap[i + centreY, j + centreX];

                    // Check for terrain flatness
                    if (value != 0 && Mathf.Abs(value - centreValue) > slopeThreshold)
                        return false;

                    // Count for position on the water
                    if (value == 0)
                        waterCounter++;
                }
                else
                    return false;
            }
        }

        // Check if the number of position on the water is lower than the threshold
        if (waterCounter > Mathf.RoundToInt(Mathf.Pow(originRadius * 2, 2) * waterThreshold))
            return false;

        Debug.LogFormat("cityCentre = ({0}, {1})", centreX, centreY);

        // If all conditions are met, confirm the current location of the city center
        return true;
    }

    /*
     * "Cosine down to zero" distance function for population density map. 
     * 
     * Apply decay on values the more farther the position is from city centre
     * (on xz 2D plane). If the distance is greater than the city radius, return
     * zero.
     * 
     * The distanceThreshold parameter must be equal to the city radius. 
     */
    private static float DistanceToCityOriginProbability(int centreX, int centreY, int x, int y, float distanceThreshold)
    {
        // Euclidean distance
        float distance = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(centreX - x), 2) + Mathf.Pow(Mathf.Abs(centreY - y), 2));

        // "Cosine down to zero" distance function 
        return distance <= distanceThreshold? Mathf.Clamp01((Mathf.Cos(Mathf.PI / distanceThreshold * distance) + 1) / 2) : 0;
    }

    /*
     * Gaussian distance function for population density map. 
     * 
     * Apply a decay to the higher values the greater the difference between the 
     * height of the location and the height of the city center. 
     * 
     * Compared with "Cosine to zero," this function slowly tends to zero with 
     * values above the threshold. Therefore, the returned value will never be 
     * zero. 
     */
    private static float heightToCityCentreProbability(float cityCentreHeight, float height, float slopeThreshold, float heightTolerance)
    {
        float heightDifference = Mathf.Abs(cityCentreHeight - height);

        float sigma = slopeThreshold * heightTolerance;

        return Mathf.Clamp01(Mathf.Pow(System.MathF.E, - Mathf.Pow(heightDifference, 2) / (2 * Mathf.Pow(sigma, 2))));
    }
}
