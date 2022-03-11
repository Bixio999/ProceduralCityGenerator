using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InputMapGenerator
{

    // CELLULAR AUTOMATA PROCEDURAL GENERATION

    public static float[,] generateCellularAutomataMap(int w, int h, float scalingFactor, int noiseDensity, int iterations, int neighboorThreshold)
    {
        int xCut = ((int)Mathf.Floor (w * scalingFactor / 100));
		int yCut = ((int)Mathf.Floor (h * scalingFactor / 100));

        float [,] noise_map = new float [xCut, yCut];

        for (int i = 0; i < yCut; i++)
        {
            for (int j = 0; j < xCut; j++)
            {
                noise_map[i, j] = (Random.value * 100 >= noiseDensity? 1 : 0);
            }
        }

        noise_map = cellularAutomata(noise_map, xCut, yCut, iterations, neighboorThreshold);

        float [,] output = new float[w, h];

        for (int i = 0; i < h; i++)
        {
            for (int j = 0; j < w; j++)
            {
                int scaled_i = Mathf.Clamp(Mathf.FloorToInt(i * scalingFactor / 100), 0, yCut - 1);
                int scaled_j = Mathf.Clamp(Mathf.FloorToInt(j * scalingFactor / 100), 0, xCut - 1);

                float value = (float) noise_map[scaled_i, scaled_j];

                output[i,j] = value;
            }
        }
        return output;
    }

    private static float[,] cellularAutomata(float[,] map, int w, int h, int iterations, int neighboorThreshold)
    {
        float[,] tempMap;

        int neighboorCounter, x, y;
        
        for (int it = 0; it < iterations; it++)
        {
            tempMap = new float [w, h];
            for (int i = 0; i < h; i++ )
            {
                for (int j = 0; j < w; j++)
                {
                    neighboorCounter = 0;
                    for (int k = -1; k <= 1; k++)
                    {
                        for (int z = -1; z <= 1; z++)
                        {
                            y = i + k;
                            x = j + z;

                            if (isValidPosition(x, y, w, h))
                                neighboorCounter += (int) map[y,x];  
                        }
                    }
                    if (neighboorCounter >= neighboorThreshold)
                        tempMap[i, j] = 1;
                    else
                        tempMap[i,j] = 0;
                }
            }
            map = tempMap;
        }

        return map;
    }

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

    public static float [,] createPopulationDensityMap(float [,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold, float mapBoundaryScale)
    {
        int cityCentreX, cityCentreY;

        do
        {
            cityCentreX = Mathf.RoundToInt((w - 1) * mapBoundaryScale * Random.value + (w - 1) * (1 - mapBoundaryScale) / 2);
            cityCentreY = Mathf.RoundToInt((h - 1) * mapBoundaryScale * Random.value + (h - 1) * (1 - mapBoundaryScale) / 2);
        } while(!isCityCentreValid(cityCentreX, cityCentreY, heightmap, w, h, slopeThreshold, originRadius, waterThreshold));

        float[,] populationMap = new float[h,w];

        for (int i = - originRadius; i <= originRadius; i++)
        {
            for (int j = - originRadius; j <= originRadius; j++)
            {
                if (isValidPosition(j + cityCentreX, i + cityCentreY, w, h))
                {
                    if (heightmap[i + cityCentreY, j + cityCentreX] > 0)
                        populationMap[i + cityCentreY, j + cityCentreX] = 1;
                }
            }
        }        

        return populationMap;
    }

    private static bool isCityCentreValid(int centreX, int centreY, float[,] heightmap, int w, int h, float slopeThreshold, int originRadius, float waterThreshold)
    {
        Debug.LogFormat("cityCentre = ({0}, {1})", centreX, centreY);

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

        return true;
    }

}
