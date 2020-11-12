using System.Collections.Generic;
using UnityEngine;

public class PlanetBodyGenerator : MonoBehaviour {
  public ComputeShader heightMapCompute;
  public MeshFilter meshFilter;
  public float radius = 1;

  [Header("Noise Settings")]

  [Min(0)]
  public int resolution;
  public int seed;
  public float ridgeMaskMin;
  public float oceanDepthMultiplier;
  public float oceanFloorThreshold;

  public FlatlandNoiseSettings flatlandNoiseSettings;
  public FlatlandNoiseSettings ridgeMaskNoiseSettings;
  public RidgeNoiseSettings ridgeNoiseSettings;

  OctaSphereGenerator sphereGenerator;
  public ColourGenerator colourGenerator;

  ComputeBuffer heightMapBuffer;
  ComputeBuffer vertexBuffer;

  List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();
  Mesh planetMesh;

  void Awake () {
    sphereGenerator = new OctaSphereGenerator();
  }

  void OnValidate () {
    colourGenerator.Init();
    var (minHeight, maxHeight) = generateTerrain();
    colourGenerator.UpdateColors();
  }

  (float, float) generateTerrain () {
    initSphereGenerator();
    var (vertices, triangles) = sphereGenerator.generate(resolution);

    int threadGroupsX = prepareHeightComputeData(ref vertices);
    heightMapCompute.Dispatch(0,threadGroupsX,1,1);

    var heights = new float[vertices.Length];
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
     vertices[i] = vertices[i]* heights[i];
    }

    createMesh(vertices, triangles);
    scaleWithRadius();
    releaseBuffers();

    colourGenerator.maxElevation = maxHeight;
    colourGenerator.minElevation = minHeight;

    return (minHeight, maxHeight);
  }

  int prepareHeightComputeData (ref Vector3[] vertices) {
    vertexBuffer = new ComputeBuffer(vertices.Length, 3* sizeof(float));
    vertexBuffer.SetData(vertices);
    buffersToRelease.Add(vertexBuffer);
   
    heightMapBuffer = new ComputeBuffer(vertices.Length, sizeof(float));
    buffersToRelease.Add(heightMapBuffer);
    
    // Noise setting buffers
    FlatlandNoiseSettings [] noiseSettingsData = new FlatlandNoiseSettings [] {flatlandNoiseSettings, ridgeMaskNoiseSettings};
    RidgeNoiseSettings [] ridgeNoiseData = new RidgeNoiseSettings[] {ridgeNoiseSettings};

    ComputeBuffer noiseSettingsBuffer = new ComputeBuffer(noiseSettingsData.Length, sizeof(int) + 5*sizeof(float));
    noiseSettingsBuffer.SetData(noiseSettingsData);
    buffersToRelease.Add(noiseSettingsBuffer);

    ComputeBuffer ridgeNoiseSettingsBuffer = new ComputeBuffer(ridgeNoiseData.Length, sizeof(int) + 8*sizeof(float));
    ridgeNoiseSettingsBuffer.SetData(ridgeNoiseData);

    RNGHelper random = new RNGHelper(seed);
    float [] seedOffset = new float []{random.nextDouble(), random.nextDouble(), random.nextDouble()};

    heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);
    heightMapCompute.SetBuffer(0, "heights", heightMapBuffer);
    heightMapCompute.SetBuffer(0, "noiseSettings", noiseSettingsBuffer);
    heightMapCompute.SetBuffer(0, "ridgeNoiseSettings", ridgeNoiseSettingsBuffer);
    heightMapCompute.SetFloats("seedOffset", seedOffset);
    heightMapCompute.SetFloat("ridgeMaskMin", ridgeMaskMin);
    heightMapCompute.SetFloat("oceanDepthMultiplier", oceanDepthMultiplier);
    heightMapCompute.SetFloat("oceanFloorThreshold", oceanFloorThreshold);

    // Calculate thread group size based on the number of vertices
    uint threadDimensionX, threadDimensionY, threadDimensionZ;
    heightMapCompute.GetKernelThreadGroupSizes(0, out threadDimensionX, out threadDimensionY, out threadDimensionZ);
    return Mathf.CeilToInt(vertices.Length/(float)threadDimensionX);
  }

  void createMesh (Vector3[] vertices, int[] triangles) {
    
    if(planetMesh == null) {
      planetMesh = new Mesh();
      meshFilter.sharedMesh = planetMesh;
    }

    planetMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

    planetMesh.Clear();
    planetMesh.SetVertices (vertices);
		planetMesh.SetTriangles (triangles, 0, true);
		planetMesh.RecalculateNormals ();
  }

  void scaleWithRadius () {
    this.transform.localScale = new Vector3(radius, radius, radius);
  }

  void initSphereGenerator () {
    if (sphereGenerator == null) {
      sphereGenerator = new OctaSphereGenerator();
    }
  }

  // release compute shader buffers to avoid memory leaks
  void releaseBuffers () {
    foreach(ComputeBuffer buffer in buffersToRelease) {
      buffer.Release();
    }
  }
}