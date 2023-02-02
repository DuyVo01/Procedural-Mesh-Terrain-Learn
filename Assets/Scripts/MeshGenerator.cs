using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
   public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _meshHeightCurve, int levelOfDetail)
   {
        AnimationCurve meshHeightCurve = new AnimationCurve(_meshHeightCurve.keys);
        int width = heightMap.GetLength(0);
        int height = heightMap.GetLength(1);

        float halfWidth = (width - 1) / 2f;
        float halfHeight = (height - 1) / 2f;

        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : (levelOfDetail * 2);
        int verticesPerLine = (width - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine, verticesPerLine);
        int vertexIndex = 0;

        for (int x = 0; x < height; x += meshSimplificationIncrement)
        {
            for (int y = 0; y < width; y += meshSimplificationIncrement)
            {
                meshData.Vertices[vertexIndex] = new Vector3(x - halfWidth, meshHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier, y - halfHeight);
                meshData.UVs[vertexIndex] = new Vector2((float)x / width, (float)y / height);
                if(x < width - 1  && y < height - 1)
                {
                    meshData.AddTriangle(vertexIndex, vertexIndex + verticesPerLine + 1, vertexIndex + verticesPerLine);
                    meshData.AddTriangle(vertexIndex + verticesPerLine +1, vertexIndex, vertexIndex + 1);
                }

                vertexIndex++;
            }
        }

        return meshData;
   }
}

public class MeshData
{
    public Vector3[] Vertices { get; set; }
    public int[] Triangles { get; set; }
    public Vector2[] UVs { get; set; }

    private int triangleIndex;
    public MeshData(int meshWidth, int meshHeight)
    {
        Vertices = new Vector3[meshWidth * meshWidth];
        UVs = new Vector2[meshWidth * meshHeight];
        Triangles = new int[(meshWidth - 1) * (meshHeight - 1) * 6];
    }

    public void AddTriangle(int a, int b, int c)
    {
        Triangles[triangleIndex] = a;
        Triangles[triangleIndex + 1] = b;
        Triangles[triangleIndex + 2] = c;

        triangleIndex += 3;
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = Vertices;
        mesh.triangles = Triangles;
        mesh.uv = UVs;

        mesh.RecalculateNormals();
        return mesh;
    }
}
