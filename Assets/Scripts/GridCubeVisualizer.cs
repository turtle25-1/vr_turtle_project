using System.Collections.Generic;
using UnityEngine;

public class GridCubeVisualizer : MonoBehaviour
{
    public int sizeX = 3, sizeY = 2, sizeZ = 3;
    public float cellSize = 1.0f;
    public Material lineMaterial;
    private List<GameObject> lines = new();

    void Start()
    {
        for (int x = 0; x < sizeX; x++)
        {
            for (int y = 0; y < sizeY; y++)
            {
                for (int z = 0; z < sizeZ; z++)
                {
                    Vector3 center = new Vector3(x, y, z) * cellSize;
                    CreateWireCube(center, cellSize);
                }
            }
        }
    }

    void CreateWireCube(Vector3 center, float size)
    {
        Vector3[] corners = new Vector3[8];
        float h = size / 2f;

        // 8 corners of the cube
        corners[0] = center + new Vector3(-h, -h, -h);
        corners[1] = center + new Vector3(-h, -h, h);
        corners[2] = center + new Vector3(-h, h, -h);
        corners[3] = center + new Vector3(-h, h, h);
        corners[4] = center + new Vector3(h, -h, -h);
        corners[5] = center + new Vector3(h, -h, h);
        corners[6] = center + new Vector3(h, h, -h);
        corners[7] = center + new Vector3(h, h, h);

        int[,] edges = new int[,]
        {
            {0,1}, {1,3}, {3,2}, {2,0}, // Left face
            {4,5}, {5,7}, {7,6}, {6,4}, // Right face
            {0,4}, {1,5}, {2,6}, {3,7}  // Connecting edges
        };

        for (int i = 0; i < edges.GetLength(0); i++)
        {
            GameObject lineObj = new GameObject("Line");
            lineObj.transform.parent = transform;
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, corners[edges[i, 0]]);
            lr.SetPosition(1, corners[edges[i, 1]]);
            lr.widthMultiplier = 0.01f;
            lr.material = lineMaterial;
            lr.startColor = lr.endColor = Color.black;
            lines.Add(lineObj);
        }
    }
}