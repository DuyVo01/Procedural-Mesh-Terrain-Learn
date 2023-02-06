using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class MeshGenerator
{
   public static MeshData GenerateTerrainMesh(float[,] heightMap, float heightMultiplier, AnimationCurve _meshHeightCurve, int levelOfDetail)
   {
        AnimationCurve meshHeightCurve = new AnimationCurve(_meshHeightCurve.keys);
        int meshSimplificationIncrement = (levelOfDetail == 0) ? 1 : (levelOfDetail * 2);

        int borderedSize = heightMap.GetLength(0);
        int meshSize = borderedSize - 2*meshSimplificationIncrement;
        int meshSizeUnSimplified = borderedSize - 2;

        float halfWidth = (meshSizeUnSimplified - 1) / -2f;
        float halfHeight = (meshSizeUnSimplified - 1) / 2f;

        int verticesPerLine = (meshSize - 1) / meshSimplificationIncrement + 1;

        MeshData meshData = new MeshData(verticesPerLine);

        int[,] vertexIndiciesMap = new int[borderedSize, borderedSize];
        int meshVertexIndex = 0;
        int borderVertexIndex = -1;

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                bool isBordered = y == 0 || y == borderedSize - 1 || x == 0 || x == borderedSize - 1;
                if (isBordered)
                {
                    vertexIndiciesMap[x, y] = borderVertexIndex;
                    borderVertexIndex--;
                }
                else
                {
                    vertexIndiciesMap[x, y] = meshVertexIndex;
                    meshVertexIndex++;
                }
            }
        }

        for (int y = 0; y < borderedSize; y += meshSimplificationIncrement)
        {
            for (int x = 0; x < borderedSize; x += meshSimplificationIncrement)
            {
                int vertexIndex = vertexIndiciesMap[x,y];
                Vector2 percent = new Vector2( (x - meshSimplificationIncrement) / (float)meshSize, (y - meshSimplificationIncrement) / (float)meshSize);
                float height = meshHeightCurve.Evaluate(heightMap[x, y]) * heightMultiplier;
                Vector3 vertexPosition = new Vector3( halfWidth + percent.x * meshSizeUnSimplified , height, halfHeight - percent.y * meshSizeUnSimplified );

                meshData.AddVertices(vertexPosition, percent, vertexIndex);

                if(x < borderedSize - 1  && y < borderedSize - 1)
                {
                    int a = vertexIndiciesMap[x, y];
                    int b = vertexIndiciesMap[x + meshSimplificationIncrement, y];
                    int c = vertexIndiciesMap[x, y + meshSimplificationIncrement];
                    int d = vertexIndiciesMap[x + meshSimplificationIncrement, y + meshSimplificationIncrement];

                    meshData.AddTriangle(a, d, c);
                    meshData.AddTriangle(d, a, b);
                }
                //vertexIndex++;
            }
        }

        meshData.BakeNormals();

        return meshData;
   }
}

public class MeshData
{
    public Vector3[] Vertices { get; set; }
    public int[] Triangles { get; set; }
    public Vector2[] UVs { get; set; }
    public Vector3[] BakedNormals { get; set; }

    private Vector3[] borderVertices;
    private int[] borderTriangles;

    private int borderTriangleIndex = 0;
    private int triangleIndex = 0;

    public MeshData(int verticesPerLine)
    {
        Vertices = new Vector3[verticesPerLine * verticesPerLine];
        UVs = new Vector2[verticesPerLine * verticesPerLine];
        Triangles = new int[(verticesPerLine - 1) * (verticesPerLine - 1) * 6];

        borderVertices = new Vector3[verticesPerLine * 4 + 4];
        borderTriangles = new int[24 * verticesPerLine];
    }

    public void AddVertices(Vector3 vertexPosition, Vector2 uv, int vertexIndex)
    {
        if(vertexIndex < 0)
        {
            borderVertices[-vertexIndex - 1] = vertexPosition;
        }
        else
        {
            Vertices[vertexIndex] = vertexPosition;
            UVs[vertexIndex] = uv;
        }
    }

    public void AddTriangle(int a, int b, int c)
    {
        if(a < 0 || b < 0 || c < 0)
        {
            borderTriangles[borderTriangleIndex] = a;
            borderTriangles[borderTriangleIndex + 1] = b;
            borderTriangles[borderTriangleIndex + 2] = c;

            borderTriangleIndex += 3;
        }
        else
        {
            Triangles[triangleIndex] = a;
            Triangles[triangleIndex + 1] = b;
            Triangles[triangleIndex + 2] = c;

            triangleIndex += 3;
        }
        
    }

    private Vector3[] CalculateNormals()
    {
        Vector3[] vertexNormals = new Vector3[Vertices.Length];
        int triangleCount = Triangles.Length / 3;
        for(int i = 0; i < triangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = Triangles[normalTriangleIndex];
            int vertexIndexB = Triangles[normalTriangleIndex + 1];
            int vertexIndexC = Triangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC);

            vertexNormals[vertexIndexA] += triangleNormal;
            vertexNormals[vertexIndexB] += triangleNormal;
            vertexNormals[vertexIndexC] += triangleNormal;
        }

        int borderTriangleCount = borderTriangles.Length / 3;
        for (int i = 0; i < borderTriangleCount; i++)
        {
            int normalTriangleIndex = i * 3;
            int vertexIndexA = borderTriangles[normalTriangleIndex];
            int vertexIndexB = borderTriangles[normalTriangleIndex + 1];
            int vertexIndexC = borderTriangles[normalTriangleIndex + 2];

            Vector3 triangleNormal = SurfaceNormalFromIndicies(vertexIndexA, vertexIndexB, vertexIndexC);
            if(vertexIndexA >= 0)
            {
                vertexNormals[vertexIndexA] += triangleNormal;
            }
            if(vertexIndexB >= 0)
            {
                vertexNormals[vertexIndexB] += triangleNormal;
            }
            if(vertexIndexC >= 0)
            {
                vertexNormals[vertexIndexC] += triangleNormal;
            }

        }

        for (int i = 0; i < vertexNormals.Length; i++)
        {
            vertexNormals[i].Normalize();
        }

        return vertexNormals;
    }

    private Vector3 SurfaceNormalFromIndicies(int indexA, int indexB, int indexC)
    {
        Vector3 pointA = (indexA < 0) ? borderVertices[-indexA - 1] : Vertices[indexA];
        Vector3 pointB = (indexB < 0) ? borderVertices[-indexB - 1] : Vertices[indexB];
        Vector3 pointC = (indexC < 0) ? borderVertices[-indexC - 1] : Vertices[indexC];

        Vector3 sideAB = pointB - pointA;
        Vector3 sideAC = pointC - pointA;

        return Vector3.Cross(sideAB, sideAC).normalized;
    }

    public void BakeNormals()
    {
        BakedNormals = CalculateNormals();
    }

    public Mesh CreateMesh()
    {
        Mesh mesh = new Mesh();
        mesh.vertices = Vertices;
        mesh.triangles = Triangles;
        mesh.uv = UVs;

        mesh.normals = BakedNormals;
        return mesh;
    }
}
