using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;


public class ChunkedPlanetBodyGenerator : MonoBehaviour {

  public ComputeShader heightMapCompute;
  public int resolution;
  public int chunkResolution;
  public int seed;
  public float ridgeMaskMin;
  public float oceanDepthMultiplier;
  public float oceanFloorThreshold;

  public SimpleNoiseSettings flatlandNoiseSettings;
  public SimpleNoiseSettings ridgeMaskNoiseSettings;
  public RidgeNoiseSettings ridgeNoiseSettings;

  [SerializeField, HideInInspector]
  MeshFilter[] meshFilters;
  Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

  FaceChunk [] FaceChunks;
  BiomeGenerator biomeGenerator;
  ComputeBuffer heightMapBuffer;
  ComputeBuffer vertexBuffer;

  List<Vector3> vegetationPlacementPoints;
  List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();


  void OnValidate () {
    if(biomeGenerator == null) {
      biomeGenerator = GetComponent<BiomeGenerator>();
    }
    biomeGenerator.onSettingsUpdated = Run;
    Run();
  }

  void Run () {
    UpdateChunks();
    var (minHeight, maxHeight) = generateTerrain();
    float startTime = Time.realtimeSinceStartup;
    Vector2 [] moistureTemperatureData = biomeGenerator.generateMoistureAndTemperatureData(vertexBuffer, heightMapBuffer, minHeight, maxHeight);
    biomeGenerator.updateShadingData(ref moistureTemperatureData, minHeight, maxHeight);
    float endTime = Time.realtimeSinceStartup;
    setFaceChunkUVs(moistureTemperatureData);
    vegetationPlacementPoints = new List<Vector3>();
    for (int i = 0; i < FaceChunks.Length; i++) {
      List<Vector3> pointsToAdd = FaceChunks[i].getPointsForObjectPlacement(resolution, meshFilters[i].gameObject.transform, 5, 40);
      vegetationPlacementPoints.AddRange(pointsToAdd);
    }
      Debug.Log((endTime-startTime)* 1000);
    releaseBuffers();
  }

  void UpdateChunks() {
    int chunkArraySize = chunkResolution*chunkResolution*6;
    if (meshFilters == null || meshFilters.Length != chunkArraySize) {
      for (int i = this.transform.childCount; i > 0; --i) {
        UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(this.transform.GetChild(0).gameObject);
      }
      meshFilters = new MeshFilter[chunkArraySize]; 
    }
    FaceChunks = new FaceChunk[chunkArraySize];

    for (int i = 0; i < 6; i++)
    {
      for(int j = 0 ; j < chunkResolution; j++) {
        for(int k = 0; k < chunkResolution; k++) {
          int chunkLocationinArray = i*chunkResolution*chunkResolution + j*chunkResolution +k;
          FaceChunks[chunkLocationinArray] = new FaceChunk(directions[i],j,k);
          if (meshFilters[chunkLocationinArray] == null) {
            GameObject meshObj = new GameObject("chunk-"+i+"-"+j+"-"+k);
            meshObj.transform.parent = transform;
            meshObj.transform.localScale = new Vector3(1,1,1);
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = biomeGenerator.terrainMaterial;
            meshFilters[chunkLocationinArray] = meshObj.AddComponent<MeshFilter>();
          }
        }
      }
    }
  }

  void setFaceChunkUVs (Vector2 [] moistureTemperatureData) {
    List<Vector2> moistureTemperatureList = new List<Vector2>(moistureTemperatureData);
    int currentMeshStartIndex = 0;
    for(int i = 0; i < FaceChunks.Length; i++) {
      FaceChunks[i].updateUVs(3, moistureTemperatureList.GetRange(currentMeshStartIndex, FaceChunks[i].vertexCount).ToArray());
      currentMeshStartIndex += FaceChunks[i].vertexCount;
    }
  } 

  (float, float) generateTerrain (){
    List<Vector3> cumulatedVertices = new List<Vector3>();
    for (int i = 0; i < FaceChunks.Length; i++) {
      if (meshFilters[i].gameObject.activeSelf)
      {
        FaceChunks[i].generateMesh(resolution,chunkResolution);
        cumulatedVertices.AddRange(FaceChunks[i].getVertices());
      }
    }
    int threadGroupsX = prepareHeightComputeData(cumulatedVertices.ToArray());
    heightMapCompute.Dispatch(0,threadGroupsX,1,1);
    var heights = new float[cumulatedVertices.Count];
		heightMapBuffer.GetData (heights);

    float minHeight = float.MaxValue;
    float maxHeight = float.MinValue;

    for(int i = 0 ;i < heights.Length; i++) {
    //  Debug.Log(heights[i]);
    if(heights[i] > maxHeight) {
      maxHeight = heights[i];
    }
    if (heights[i] < minHeight) {
      minHeight = heights[i];
    }
     cumulatedVertices[i] = cumulatedVertices[i]* heights[i];
    }

    int currentMeshStartIndex = 0;
    for(int i = 0 ;i < FaceChunks.Length ; i ++) {
      FaceChunks[i].updateMesh(cumulatedVertices.GetRange(currentMeshStartIndex,FaceChunks[i].vertexCount).ToArray());
      FaceChunks[i].updateUVs(4, heights.SubArray(currentMeshStartIndex,FaceChunks[i].vertexCount));
      meshFilters[i].sharedMesh = FaceChunks[i].mesh;
      currentMeshStartIndex += FaceChunks[i].vertexCount;
    }
    return (minHeight, maxHeight);
  }

  int prepareHeightComputeData (Vector3[] vertices) {
    vertexBuffer = new ComputeBuffer(vertices.Length, 3* sizeof(float));
    vertexBuffer.SetData(vertices);
    buffersToRelease.Add(vertexBuffer);
   
    heightMapBuffer = new ComputeBuffer(vertices.Length, sizeof(float));
    buffersToRelease.Add(heightMapBuffer);
    
    // Noise setting buffers
    SimpleNoiseSettings [] noiseSettingsData = new SimpleNoiseSettings [] {flatlandNoiseSettings, ridgeMaskNoiseSettings};
    RidgeNoiseSettings [] ridgeNoiseData = new RidgeNoiseSettings[] {ridgeNoiseSettings};

    ComputeBuffer noiseSettingsBuffer = new ComputeBuffer(noiseSettingsData.Length, sizeof(int) + 5*sizeof(float));
    noiseSettingsBuffer.SetData(noiseSettingsData);
    buffersToRelease.Add(noiseSettingsBuffer);

    ComputeBuffer ridgeNoiseSettingsBuffer = new ComputeBuffer(ridgeNoiseData.Length, sizeof(int) + 8*sizeof(float));
    ridgeNoiseSettingsBuffer.SetData(ridgeNoiseData);
    buffersToRelease.Add(ridgeNoiseSettingsBuffer);

    RNGHelper random = new RNGHelper(seed);
    Vector3 seedOffset = new Vector3(random.nextDouble(), random.nextDouble(), random.nextDouble());

    heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);
    heightMapCompute.SetBuffer(0, "heights", heightMapBuffer);
    heightMapCompute.SetBuffer(0, "noiseSettings", noiseSettingsBuffer);
    heightMapCompute.SetBuffer(0, "ridgeNoiseSettings", ridgeNoiseSettingsBuffer);
    heightMapCompute.SetVector("seedOffset", seedOffset);
    heightMapCompute.SetFloat("ridgeMaskMin", ridgeMaskMin);
    heightMapCompute.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
    heightMapCompute.SetFloat("oceanFloorThreshold", oceanFloorThreshold);

    // Calculate thread group size based on the number of vertices
    uint threadDimensionX, threadDimensionY, threadDimensionZ;
    heightMapCompute.GetKernelThreadGroupSizes(0, out threadDimensionX, out threadDimensionY, out threadDimensionZ);
    return Mathf.CeilToInt(vertices.Length/(float)threadDimensionX);
  }

  void releaseBuffers () {
    foreach(ComputeBuffer buffer in buffersToRelease) {
      buffer.Release();
    }
  }

  void OnDrawGizmos () {
    foreach (Vector3 vertex in vegetationPlacementPoints) {
      Gizmos.DrawSphere(vertex, 1);
    }
  }

}