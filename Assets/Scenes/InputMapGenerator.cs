using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMapGenerator
{
    private const float E = 2.7182818284590451f;

    private static bool isValidPosition(int x, int y, int w, int h)
    {
        if (x < 0 || x >= w)
            return false;
        if (y < 0 || y >= h)
            return false;
        return true;
    }

    // PERLIN NOISE PROCEDURAL GENERATION

    public static float[,] generatePerlinNoiseMap(int w, int h, float scaleFactor, int nOctaves, float exponent, float waterThreshold)
    {
        // float amplifier = Mathf.Pow(1 / Random.value, 1 / Random.value);
        float amplifier = Random.value * 1000;
        Vector2 origin = Random.insideUnitCircle * amplifier;

        Debug.LogFormat("origin x = {0} | origin y = {1} | amplifier = {2}", origin.x, origin.y, amplifier);

        // UnityEditor.EditorApplication.isPlaying = false;

        float[,] map = new float[w,h];

        float value, xCoord, yCoord, normalizer, amplitude;
        int alpha;

        float maxValue = 0f;
        float minValue = Mathf.Infinity;
        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                xCoord = origin.x + (float) j / w * scaleFactor;
                yCoord = origin.y + (float) i / h * scaleFactor;

                value = 0f;
                normalizer = 0f;

                for (int k = 0; k < nOctaves; k++)
                {
                    alpha = (int) Mathf.Pow(2, k);
                    amplitude = 1 / ((float) alpha);

                    normalizer += amplitude;

                    value += amplitude * Mathf.Clamp01(Mathf.PerlinNoise(alpha * xCoord, alpha * yCoord));

                    // Debug.LogFormat("alpha = {3}, amplitude = {4} | perlin noise value at coords [{0}, {1}] = {2}", xCoord, yCoord, value, alpha, amplitude);
                }
                value /= normalizer;
                value = Mathf.Pow(value, exponent);

                // Debug.LogFormat("map[{0}, {1}] = {2}", i , j , value);

                map[i,j] = value;

                if (maxValue < value) maxValue = value;
                if (minValue > value) minValue = value;
            }
        }

        maxValue -= minValue;
        waterThreshold = waterThreshold * maxValue;

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                value = map[i,j];

                // value = (value - minValue) / (maxValue - minValue);
                value = value - minValue;

                value = value > waterThreshold ? value : waterThreshold; 
                value -= waterThreshold;
                map[i,j] = value;
            }
        }

        Debug.LogFormat("minValue = {0}, maxValue = {1}, waterThreshold = {2}", minValue, maxValue, waterThreshold * maxValue);

        return map;
    }


    // POPULATION DENSITY MAP GENERATION

    public static (float [,] populationMap, int cityCentreX, int cityCentreY) createPopulationDensityMap(float [,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold, float mapBoundaryScale, float cityRadius, float heightTolerance, float PNScaling, int PNOctaves, float perlinNoiseExponent)
    {
        int cityCentreX, cityCentreY;

        // CITY ORIGIN DECISION

        do
        {
            cityCentreX = Mathf.RoundToInt((w - 1) * mapBoundaryScale * Random.value + (w - 1) * (1 - mapBoundaryScale) / 2);
            cityCentreY = Mathf.RoundToInt((h - 1) * mapBoundaryScale * Random.value + (h - 1) * (1 - mapBoundaryScale) / 2);
        } while(!isCityCentreValid(cityCentreX, cityCentreY, heightmap, w, h, slopeThreshold, originRadius, waterThreshold));

        float[,] populationMap = new float[h,w];

        // POPULATION DENSITY GENERATION

        float value, PNvalue, xCoord, yCoord, normalizer, amplitude;
        int alpha;

        float amplifier = Random.value * 1000;
        Vector2 perlinNoiseOrigin = Random.insideUnitCircle * amplifier;

        for(int i = 0; i < h; i++)
        {
            for(int j = 0; j < w; j++)
            {
                value = 0f;

                xCoord = perlinNoiseOrigin.x + (float) j / w * PNScaling;
                yCoord = perlinNoiseOrigin.y + (float) i / h * PNScaling;

                PNvalue = 0f;
                normalizer = 0f;

                if (heightmap[i,j] != 0)
                {
                    value = distanceToCityOriginProbability(cityCentreX, cityCentreY, j, i, cityRadius);
                    value *= heightToCityCentreProbability(heightmap[cityCentreY, cityCentreX], heightmap[i,j], slopeThreshold, heightTolerance);

                    for (int k = 0; k < PNOctaves; k++)
                    {
                        alpha = (int) Mathf.Pow(2, k);
                        amplitude = 1 / ((float) alpha);

                        normalizer += amplitude;

                        PNvalue += amplitude * Mathf.Clamp01(Mathf.PerlinNoise(alpha * xCoord, alpha * yCoord));
                    }
                    if (PNvalue > 0)
                    {
                        PNvalue /= normalizer;
                        PNvalue = Mathf.Pow(PNvalue, perlinNoiseExponent);
                        value *= (PNvalue);
                    }

                    populationMap[i,j] = value;
                }                    
            }
        } 

        return (populationMap, cityCentreX, cityCentreY);
    }

    private static bool isCityCentreValid(int centreX, int centreY, float[,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold)
    {
        float centreValue = heightmap[centreY, centreX];
        if (centreValue == 0)
            return false;

        float value;
        int waterCounter = 0;
        for (int i = - originRadius; i <= originRadius; i++)
        {
            for (int j = - originRadius; j <= originRadius; j++)
            {
                if (isValidPosition(j + centreX, i + centreY, w, h))
                {
                    value = heightmap[i + centreY, j + centreX];

                    if (value != 0 && Mathf.Abs(value - centreValue) > slopeThreshold)
                        return false;
                    if (value == 0)
                        waterCounter++;
                }
                else
                    return false;
            }
        }
        if (waterCounter > Mathf.RoundToInt(Mathf.Pow(originRadius * 2, 2) * waterThreshold))
            return false;

        Debug.LogFormat("cityCentre = ({0}, {1})", centreX, centreY);
        return true;
    }

    private static float distanceToCityOriginProbability(int centreX, int centreY, int x, int y, float distanceThreshold)
    {
        // Euclidean distance
        float distance = Mathf.Sqrt(Mathf.Pow(Mathf.Abs(centreX - x), 2) + Mathf.Pow(Mathf.Abs(centreY - y), 2));

        // "Cosine down to zero" distance function 
        return distance <= distanceThreshold? Mathf.Clamp01((Mathf.Cos(Mathf.PI / distanceThreshold * distance) + 1) / 2) : 0;
    }

    private static float heightToCityCentreProbability(float cityCentreHeight, float height, float slopeThreshold, float heightTolerance)
    {
        float heightDifference = Mathf.Abs(cityCentreHeight - height);

        float sigma = slopeThreshold * heightTolerance;

        return Mathf.Clamp01(Mathf.Pow(E, - Mathf.Pow(heightDifference, 2) / Mathf.Pow(2 * sigma, 2)));
    }
}
