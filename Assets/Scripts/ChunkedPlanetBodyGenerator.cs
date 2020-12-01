using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using ExtensionMethods;
using UnityEditor;

  [System.Serializable]
	public struct LODInfo {
		public int lod;
		public float visibleDstThreshold;
	}

public class ChunkedPlanetBodyGenerator : MonoBehaviour {

  public ComputeShader heightMapCompute;
  public LODInfo [] detailLevels;
  public int chunkResolution;
  public int seed;
  public int radius;
  public float ridgeMaskMin;
  public float oceanDepthMultiplier;
  public float oceanFloorThreshold;

  public SimpleNoiseSettings flatlandNoiseSettings;
  public SimpleNoiseSettings ridgeMaskNoiseSettings;
  public RidgeNoiseSettings ridgeNoiseSettings;

  public Material waterMaterial;

  [SerializeField, HideInInspector]
  MeshFilter[] meshFilters;
  Vector3[] directions = { Vector3.up, Vector3.down, Vector3.left, Vector3.right, Vector3.forward, Vector3.back };

  FaceChunk [] FaceChunks;
  BiomeGenerator biomeGenerator;
  TreeGenerator treeGenerator;
  ComputeBuffer heightMapBuffer;
  ComputeBuffer vertexBuffer;

  List<Vector3> vegetationPlacementPoints;
  List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();

  Vector3 cachedPlayerPosition;
  float currentMaxHeight, currentMinHeight;

  List<Vector3> MeshCalculationVertexAllocation;
  List<int> MeshCalculationTrisAllocation;



  void Awake () {
    cachedPlayerPosition = Camera.main.transform.position;
  }

  void Start () {

    // Terrain generation needs to run again when entering play mode as script references get broken otherwise
    treeGenerator.DestroyAllTrees();
    StartCoroutine(GeneratePlanet());
  }

  void OnValidate () {
    if(biomeGenerator == null) {
      biomeGenerator = GetComponent<BiomeGenerator>();
    }
    if(treeGenerator == null) {
      treeGenerator = GetComponent<TreeGenerator>();
    }
    if(!EditorApplication.isPlayingOrWillChangePlaymode) {
      StartCoroutine(GeneratePlanet());
      Debug.Log("Generated");
     }
  }

  void Update() {
    if(Vector3.Distance(Camera.main.transform.position,cachedPlayerPosition) > 20) {
      checkChunksForLodUpdate();
      cachedPlayerPosition = Camera.main.transform.position;
    }
  }

  void checkChunksForLodUpdate () {
    List<int> faceChunkIdsToUpdate = new List<int>();
    for(int i = 0 ; i < FaceChunks.Length; i++) {
      bool updateRequired = FaceChunks[i].isLodGenerationRequired(chunkResolution, meshFilters[i]);
      if(updateRequired) {
        faceChunkIdsToUpdate.Add(i);
      }
    }
    Debug.Log(faceChunkIdsToUpdate.Count);
    if(faceChunkIdsToUpdate.Count > 0) {
      StartCoroutine(updateFaceChunkLods(faceChunkIdsToUpdate));
    }
  }

  IEnumerator updateFaceChunkLods (List<int> chunkIds) {
    List<Vector3> cumulatedVertices = new List<Vector3>();
    List<int> cumulatedTriangles = new List<int>();
    MeshCalculationVertexAllocation.Clear();
    MeshCalculationTrisAllocation.Clear();
    for (int i = 0; i < chunkIds.Count; i++) {
      if (meshFilters[i].gameObject.activeSelf)
      {
        int resolution = FaceChunks[chunkIds[i]].getResolutionBasedOnCameraDistance(chunkResolution);
        FaceChunks[chunkIds[i]].generateMesh(chunkResolution, resolution, MeshCalculationVertexAllocation, MeshCalculationTrisAllocation);
        FaceChunks[chunkIds[i]].currentResolution = resolution;
        cumulatedVertices.AddRange(MeshCalculationVertexAllocation);
        cumulatedTriangles.AddRange(MeshCalculationTrisAllocation);
      }
    }
    int threadGroupsX = prepareHeightComputeData(cumulatedVertices.ToArray());
    heightMapCompute.Dispatch(0,threadGroupsX,1,1);
    var heights = new float[cumulatedVertices.Count];

    yield return new WaitForSeconds(0.75f);

		heightMapBuffer.GetData (heights);

    for(int i = 0 ;i < heights.Length; i++) {
    //  Debug.Log(heights[i]);
    if(heights[i] > currentMaxHeight) {
      currentMaxHeight = heights[i];
    }
    if (heights[i] < currentMinHeight) {
      currentMinHeight = heights[i];
    }
     cumulatedVertices[i] = cumulatedVertices[i]* heights[i];
    }

    Vector2 [] moistureTemperatureData = biomeGenerator.generateMoistureAndTemperatureData(vertexBuffer, heightMapBuffer, currentMinHeight, currentMaxHeight);
    int currentMeshVertexStartIndex = 0;
    int currentMeshTrisStartIndex = 0;
    for (int i = 0; i < chunkIds.Count; i++) {
      int index = chunkIds[i];
      FaceChunks[index].updateMesh(cumulatedVertices.GetRange(currentMeshVertexStartIndex,FaceChunks[index].vertexCount),
      cumulatedTriangles.GetRange(currentMeshTrisStartIndex, FaceChunks[index].triangleCount));
      FaceChunks[index].updateUVs(4, heights.SubArray(currentMeshVertexStartIndex,FaceChunks[index].vertexCount));
      FaceChunks[index].updateUVs(3, moistureTemperatureData.SubArray(currentMeshVertexStartIndex, FaceChunks[index].vertexCount));
      meshFilters[index].sharedMesh = FaceChunks[index].getCurrentLodMesh();
      currentMeshVertexStartIndex += FaceChunks[index].vertexCount;
      currentMeshTrisStartIndex += FaceChunks[index].triangleCount;
      meshFilters[index].gameObject.transform.localPosition = new Vector3(0,0,0);
    }

    releaseBuffers();
  }

  IEnumerator GeneratePlanet () {
    Debug.Log("Generating terrain");
    UpdateChunks();
    var (minHeight, maxHeight) = generateTerrain();
    currentMaxHeight = maxHeight;
    currentMinHeight = minHeight;
    yield return null;
    Debug.Log("Generating Biomes");
    biomeGenerator.updateTerrainMaterial();
    Vector2 [] moistureTemperatureData = biomeGenerator.generateMoistureAndTemperatureData(vertexBuffer, heightMapBuffer, minHeight, maxHeight);
    setFaceChunkUVs(moistureTemperatureData);
    yield return null;
    vegetationPlacementPoints = new List<Vector3>();
    // generate trees only in play mode because of memory issues
    if(EditorApplication.isPlayingOrWillChangePlaymode) {
      Debug.Log("Generating trees");
      treeGenerator.GenerateTrees(FaceChunks,meshFilters, biomeGenerator.biomes);
    }
    scaleWithRadius();
    addWaterMesh();
    releaseBuffers();
  }
  
  void UpdateChunks() {
    int chunkArraySize = chunkResolution*chunkResolution*6;
    if (meshFilters == null || meshFilters.Length != chunkArraySize) {
      for (int i = this.transform.childCount; i > 0; --i) {
        if(!Application.isPlaying) {
          UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(this.transform.GetChild(0).gameObject); 
        } else {
          Destroy(this.transform.GetChild(0).gameObject);
        }
      }
      meshFilters = new MeshFilter[chunkArraySize]; 
    }
    FaceChunks = new FaceChunk[chunkArraySize];

    for (int i = 0; i < 6; i++)
    {
      for(int j = 0 ; j < chunkResolution; j++) {
        for(int k = 0; k < chunkResolution; k++) {
          int chunkLocationinArray = i*chunkResolution*chunkResolution + j*chunkResolution +k;
          if (meshFilters[chunkLocationinArray] == null) {
            GameObject meshObj = new GameObject("chunk-"+i+"-"+j+"-"+k);
            meshObj.transform.parent = transform;
            meshObj.transform.localScale = new Vector3(1,1,1);
            meshObj.AddComponent<MeshRenderer>().sharedMaterial = biomeGenerator.terrainMaterial;
            meshFilters[chunkLocationinArray] = meshObj.AddComponent<MeshFilter>();
          }
          FaceChunks[chunkLocationinArray] = new FaceChunk(chunkResolution, directions[i],j,k, detailLevels, meshFilters[i].gameObject);
        }
      }
    }
    int maxResolution = detailLevels[0].lod;
    MeshCalculationVertexAllocation = new List<Vector3>(maxResolution*maxResolution);
    MeshCalculationTrisAllocation = new List<int>((maxResolution-1)*(maxResolution-1)*6);
    Debug.Log("Buffers reallocated");
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
    List<int> cumulatedTriangles = new List<int>();
    for (int i = 0; i < FaceChunks.Length; i++) {
      if (meshFilters[i].gameObject.activeSelf)
      {
        int resolution = FaceChunks[i].getResolutionBasedOnCameraDistance(chunkResolution);
        FaceChunks[i].generateMesh(chunkResolution, resolution, MeshCalculationVertexAllocation, MeshCalculationTrisAllocation);
        FaceChunks[i].currentResolution = resolution;
        cumulatedVertices.AddRange(MeshCalculationVertexAllocation);
        cumulatedTriangles.AddRange(MeshCalculationTrisAllocation);
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

    int currentMeshVertexStartIndex = 0;
    int currentMeshTrisStartIndex = 0;
    for(int i = 0 ;i < FaceChunks.Length ; i ++) {
      FaceChunks[i].updateMesh(cumulatedVertices.GetRange(currentMeshVertexStartIndex,FaceChunks[i].vertexCount),
      cumulatedTriangles.GetRange(currentMeshTrisStartIndex,FaceChunks[i].triangleCount));
      FaceChunks[i].updateUVs(4, heights.SubArray(currentMeshVertexStartIndex,FaceChunks[i].vertexCount));
      meshFilters[i].sharedMesh = FaceChunks[i].getCurrentLodMesh();
      currentMeshVertexStartIndex += FaceChunks[i].vertexCount;
      currentMeshTrisStartIndex += FaceChunks[i].triangleCount;
      meshFilters[i].gameObject.transform.localPosition = new Vector3(0,0,0);
    }
    return (minHeight, maxHeight);
  }

  void addWaterMesh () {
    if(transform.Find("Sphere") == null) {
      GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
      sphere.transform.parent = transform;
      sphere.transform.localPosition = new Vector3(0, 0, 0);
      sphere.transform.localScale = new Vector3(2,2,2);
      sphere.GetComponent<MeshRenderer>().sharedMaterial = waterMaterial;
    }
  }

  void scaleWithRadius () {
    this.transform.localScale = new Vector3(radius, radius, radius);
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

}