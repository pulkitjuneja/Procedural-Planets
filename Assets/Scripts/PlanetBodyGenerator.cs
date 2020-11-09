using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct NoiseSettings {
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float noiseScale;
    public float noiseStrength;
    public float verticalOffset;
    public float weight;
    public float oceanFloorThreshold;
}

public class PlanetBodyGenerator : MonoBehaviour {
  public ComputeShader heightMapCompute;
  public MeshFilter meshFilter;

  [Header("Noise Settings")]

  [Min(0)]
  public int resolution;
  public int seed;

  public NoiseSettings noiseSettings;

  OctaSphereGenerator sphereGenerator;
  ComputeBuffer vertexBuffer;
  ComputeBuffer heightMapBuffer;
  ComputeBuffer noiseSettingsBuffer;
  Mesh planetMesh;

  void Awake () {
      sphereGenerator = new OctaSphereGenerator();
  }

  void OnValidate () {
    generateTerrain();
  }
  void generateTerrain () {
    Debug.Log("Here");
    initSphereGenerator();
    var (vertices, triangles) = sphereGenerator.generate(resolution);
    
    vertexBuffer = new ComputeBuffer(vertices.Length, 3* sizeof(float));
    vertexBuffer.SetData(vertices);
   
    heightMapBuffer = new ComputeBuffer(vertices.Length, sizeof(float));
    
    NoiseSettings [] noiseSettingsData = new NoiseSettings [] {noiseSettings};
    noiseSettingsBuffer = new ComputeBuffer(noiseSettingsData.Length, sizeof(int) + 7*sizeof(float));
    noiseSettingsBuffer.SetData(noiseSettingsData);

    RNGHelper random = new RNGHelper(seed);
    float [] seedOffset = new float []{random.nextDouble(), random.nextDouble(), random.nextDouble()};

    heightMapCompute.SetBuffer(0, "vertices", vertexBuffer);
    heightMapCompute.SetBuffer(0, "heights", heightMapBuffer);
    heightMapCompute.SetBuffer(0, "noiseSettings", noiseSettingsBuffer);
    heightMapCompute.SetFloats("seedOffset", seedOffset);

    heightMapCompute.Dispatch(0,vertexBuffer.count,1,1);

    var heights = new float[vertices.Length];
		heightMapBuffer.GetData (heights);



    for(int i = 0 ;i < heights.Length; i++) {
     vertices[i] = vertices[i]* heights[i];
    }

    if(planetMesh == null) {
      planetMesh = new Mesh();
      meshFilter.sharedMesh = planetMesh;
    }

    planetMesh.Clear();
    planetMesh.SetVertices (vertices);
		planetMesh.SetTriangles (triangles, 0, true);
		planetMesh.RecalculateNormals ();

    releaseBuffers();
  }

  void initSphereGenerator () {
    if (sphereGenerator == null) {
      sphereGenerator = new OctaSphereGenerator();
    }
  }

  // release compute shader buffers to avoid memory leaks
  void releaseBuffers () {
    vertexBuffer.Release();
    heightMapBuffer.Release();
    noiseSettingsBuffer.Release();
  }
}