using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FaceChunk {
  public Mesh mesh;
  public int vertexCount;
  Vector3 localUp;
  Vector3 axisA;
  Vector3 axisB;
  float xIndex;
  float yIndex;
  int [] triangles;
  Vector3 [] vertices;

  public FaceChunk (Vector3 localUp, float xIndex, float yIndex) {
    this.localUp = localUp;

    axisA = new Vector3(localUp.y, localUp.z, localUp.x);
    axisB = Vector3.Cross(localUp, axisA);

      if(mesh ==  null) {
      mesh = new Mesh();
      mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
    }


    this.xIndex = xIndex;
    this.yIndex = yIndex;
  }

  public void generateMesh (int resolution, int chunkResolution) {
    float UVStep = 1f/chunkResolution;
    float step = UVStep/(resolution-1);
    Vector2 offset = new Vector2((-0.5f + xIndex*UVStep), (-0.5f + yIndex*UVStep));
    vertices = new Vector3[resolution * resolution];
    triangles = new int[(resolution - 1) * (resolution - 1) * 6];
    int triIndex = 0;

    for(int y = 0 ; y < resolution; y++) {
      for(int x = 0 ; x < resolution; x++) {
        int i = x + y * resolution;
        Vector2 position = offset + new Vector2(x* step, y*step);
        Vector3 pointOnCube = localUp + position.x*2*axisA + position.y*2*axisB;
        Vector3 pointOnUnitSphere = pointOnCube.normalized;
        vertices[i] = pointOnUnitSphere; 

        if (x != resolution - 1 && y != resolution - 1)
        {
          triangles[triIndex] = i;
          triangles[triIndex + 1] = i + resolution + 1;
          triangles[triIndex + 2] = i + resolution;

          triangles[triIndex + 3] = i;
          triangles[triIndex + 4] = i + 1;
          triangles[triIndex + 5] = i + resolution + 1;
          triIndex += 6;
        }
      }
    }
    vertexCount = vertices.Length;
  }

  public Vector3 [] getVertices () {
    return vertices;
  }

  public void updateMesh (Vector3 [] vertices) {
    mesh.Clear();
    mesh.SetVertices (vertices);
		mesh.SetTriangles (triangles, 0, true);
		mesh.RecalculateNormals ();
  } 

  public void updateUVs (int channel, Vector2 [] uvData) {
    mesh.SetUVs(channel,uvData);
  }

}