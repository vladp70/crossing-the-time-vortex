using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
    int width = 256;
    int height = 256;
    int depth = 20;
    float scale = 20f;

    // Start is called before the first frame update
    void Start()
    {
        Terrain terrain = GetComponent<Terrain>();
        terrain.terrainData = GenerateTerrain(terrain.terrainData);
    }

    TerrainData GenerateTerrain(TerrainData terrainData)
    {
        terrainData.heightmapResolution = width + 1;
        terrainData.size = new Vector3(width, depth, height);
        terrainData.SetHeights(0, 0, GenerateHeights());
        return terrainData;
    }

    float CalculateFractalNoise(int x, int y)
    {
        int octaves = 4;
        float persistence = 0.5f;
        float lacunarity = 2.0f;
        float frequency = 1.0f;
        float amplitude = 1.0f;
        float noiseHeight = 0.0f;
        for (int i = 0; i < octaves; i++)
        {
            float xCoord = (float)x / width * scale * frequency;
            float yCoord = (float)y / height * scale * frequency;

            float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
            noiseHeight += perlinValue * amplitude;

            amplitude *= persistence;
            frequency *= lacunarity;
        }

        return (noiseHeight + 1) / 2; // Normalize to 0 - 1
    }

    float[,] GenerateHeights()
    {
        float[,] heights = new float[width, height];
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                heights[x, y] = CalculateFractalNoise(x, y);
            }
        }
        return heights;
    }
}
