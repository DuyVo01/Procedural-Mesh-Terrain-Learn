using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EndlessTerrain : MonoBehaviour
{
    public const float ViewerMoveThresholdForChunkUpdate = 25f;
    public const float SqrViewerMoveThresholdForChunkUpdate = ViewerMoveThresholdForChunkUpdate * ViewerMoveThresholdForChunkUpdate;

    [SerializeField] private LODInfo[] detailLevels;
    [SerializeField] private Transform viewer;
    [SerializeField] private Material mapMaterial;
    public static float MaxViewDst { get; set; }
    public static Vector2 ViewerPosition { get; set; }
    private Vector2 viewerPositionOld;
    public static MapGenerator mapGenerator { get; set; }

    private int chunkSize;
    private int chunkVisibleInViewDst;

    Dictionary<Vector2, TerrainChunk> terrainChunkDictionary = new Dictionary<Vector2, TerrainChunk>();
    public static List<TerrainChunk> terrainChunksVisibleLastUpdate = new List<TerrainChunk>();

    private void Start()
    {
        mapGenerator = FindObjectOfType<MapGenerator>();
        MaxViewDst = detailLevels[detailLevels.Length - 1].visibleDstThreshold;
        chunkSize = MapGenerator.MapChunkSize - 1;
        chunkVisibleInViewDst = Mathf.RoundToInt( MaxViewDst / chunkSize);

        UpdateVisibleChunk();
    }

    private void Update()
    {
        ViewerPosition = new Vector2(viewer.position.x, viewer.position.z);
        if((ViewerPosition - viewerPositionOld).sqrMagnitude > SqrViewerMoveThresholdForChunkUpdate)
        {
            viewerPositionOld = ViewerPosition;
            UpdateVisibleChunk();
        }
    }

    private void UpdateVisibleChunk()
    {
        int currentChunkCoordX = Mathf.RoundToInt(ViewerPosition.x / chunkSize);
        int currentChunkCoordY = Mathf.RoundToInt(ViewerPosition.y / chunkSize);

        for (int i = 0; i < terrainChunksVisibleLastUpdate.Count; i++)
        {
            terrainChunksVisibleLastUpdate[i].SetVisible(false);
        }
        terrainChunksVisibleLastUpdate.Clear();

        for(int yOffset = -chunkVisibleInViewDst; yOffset <= chunkVisibleInViewDst; yOffset++)
        {
            for (int xOffset = -chunkVisibleInViewDst; xOffset <= chunkVisibleInViewDst; xOffset++)
            {
                Vector2 viewedChunkCoord = new Vector2(currentChunkCoordX + xOffset, currentChunkCoordY + yOffset);

                if (terrainChunkDictionary.ContainsKey(viewedChunkCoord))
                {
                    terrainChunkDictionary[viewedChunkCoord].UpdateTerrainChunk();
                }
                else
                {
                    terrainChunkDictionary.Add(viewedChunkCoord, new TerrainChunk(viewedChunkCoord, chunkSize, detailLevels, transform, mapMaterial));
                }
            }
        }
    }
}

public class TerrainChunk
{
    private GameObject meshObject;
    private MeshRenderer meshRenderer;
    private MeshFilter meshFilter;
    private Vector2 position;
    private Bounds bounds;

    private LODMesh[] lodMeshes;
    private LODInfo[] detailLevels;

    private MapData mapData;
    private bool mapDataReceived;
    private int previousLODIndex = -1;
    public TerrainChunk(Vector2 coord, int size, LODInfo[] detailLevels, Transform parent, Material material)
    {
        this.detailLevels = detailLevels;
        position = coord * size;
        bounds = new Bounds(position, Vector3.one * size);
        Vector3 positionV3 = new Vector3(position.x, 0, position.y);
        meshObject = new GameObject("Terrain Chunk");
        meshRenderer = meshObject.AddComponent<MeshRenderer>();
        meshFilter = meshObject.AddComponent<MeshFilter>();
        meshObject.transform.position = positionV3;
        meshRenderer.material = material;
        meshObject.transform.parent = parent;
        SetVisible(false);
        EndlessTerrain.mapGenerator.RequestMapData(position, OnMapDataReceived);

        lodMeshes = new LODMesh[detailLevels.Length];
        for (int i = 0; i < lodMeshes.Length; i++)
        {
            lodMeshes[i] = new LODMesh(detailLevels[i].lod, UpdateTerrainChunk);
        }
    }

    private void OnMapDataReceived(MapData mapData)
    {
        this.mapData = mapData;
        mapDataReceived = true;

        Texture2D texture = TextureGenerator.TextureFromColorMap(mapData.colorMap, MapGenerator.MapChunkSize, MapGenerator.MapChunkSize);
        meshRenderer.material.mainTexture = texture;

        UpdateTerrainChunk();  
    }

    public void UpdateTerrainChunk()
    {
        if (mapDataReceived)
        {
            float viewerDstFromNearestEdge = Mathf.Sqrt(bounds.SqrDistance(EndlessTerrain.ViewerPosition));
            bool visible = viewerDstFromNearestEdge <= EndlessTerrain.MaxViewDst;

            if (visible)
            {
                int lodIndex = 0;
                for (int i = 0; i < detailLevels.Length - 1; i++)
                {
                    if (detailLevels[i].visibleDstThreshold < viewerDstFromNearestEdge )
                    {
                        lodIndex = i + 1;
                    }
                    else
                    {
                        break;
                    }
                }

                if (lodIndex != previousLODIndex)
                {
                    LODMesh lodMesh = lodMeshes[lodIndex];
                    if (lodMesh.hasMesh)
                    {
                        previousLODIndex = lodIndex;
                        meshFilter.mesh = lodMesh.mesh;
                    }
                    else if (!lodMesh.hasRequestedMesh)
                    {
                        lodMesh.RequestMesh(mapData);
                    }
                }
                EndlessTerrain.terrainChunksVisibleLastUpdate.Add(this);
            }
            SetVisible(visible);
        }
        
    }

    public void SetVisible(bool visible)
    {
        meshObject.SetActive(visible);
    }

    public bool isVisible()
    {
        return meshObject.activeSelf;
    }
}

public class LODMesh
{
    public bool hasRequestedMesh { get; set; }
    public Mesh mesh { get; set; }
    public bool hasMesh { get; set; }
    public int lod { get; set; }

    private Action updateCallback;

    public LODMesh(int lod, Action updateCallback)
    {
        this.updateCallback = updateCallback;
        this.lod = lod;
    }

    public void OnMeshReceived(MeshData meshData)
    {
        mesh = meshData.CreateMesh();
        hasMesh = true;
        updateCallback();
    }

    public void RequestMesh(MapData mapData)
    {
        hasRequestedMesh = true;
        EndlessTerrain.mapGenerator.RequestMeshData(mapData, lod, OnMeshReceived);
    }
}

[System.Serializable]
public struct LODInfo
{
    public int lod;
    public float visibleDstThreshold;
}

