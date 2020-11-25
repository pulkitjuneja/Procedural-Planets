using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;


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

  public List<ObjectPlacementInfo> getPointsForObjectPlacement (int resolution, Transform worldTransform, float minDistance, float numIterations) {
    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> heights = new List<Vector2>();
    List<Vector2> biomeData = new List<Vector2>();
    List<ObjectPlacementInfo> vegetationPlacementPoints = new List<ObjectPlacementInfo>();

    mesh.GetVertices(vertices);
    mesh.GetNormals(normals);
    mesh.GetUVs(4,heights);
    mesh.GetUVs(3, biomeData);

    int centerVertexPosition;
    if (resolution%2 == 0) {
      centerVertexPosition = resolution*resolution/2 + resolution/2 -1;
    } else {
      centerVertexPosition = resolution*resolution/2;
    }
    int [] directions = new int[]{resolution, -resolution, 1, -1};


    // sample points in all directions
    int currentStartingPoint = centerVertexPosition;
    for(int i = 0; i< numIterations; i ++) {
      int randomDirection = directions[Random.Range(0,4)];
      int randomDistance = Random.Range(0,resolution/2);
      // sample random point
      int pointIndex = currentStartingPoint + randomDirection*randomDistance;

      // check if point is in bounds and avoid edges of chunk
      if(pointIndex < 0 || pointIndex > resolution * resolution || pointIndex > resolution*(resolution - 2) || pointIndex/resolution == 0 || (pointIndex+1)/resolution == 0) {
        continue;
      }

      float nextPointSelection = Random.Range(0,1);
      currentStartingPoint = nextPointSelection > 0.7 ? currentStartingPoint : pointIndex;

      Vector3 pointVertex = vertices[pointIndex];
      Vector3 pointNormal = normals[pointIndex];
      float height = heights[pointIndex].x;
      float biomeIndex = biomeData[pointIndex].x;

      //check if random point is not in min range of any existing point
      bool isValid = true;
      foreach(ObjectPlacementInfo point in vegetationPlacementPoints) {
        float distance = Vector3.Distance(point.worldPosition, worldTransform.TransformPoint(pointVertex));
        if(distance < minDistance) {
          isValid = false;
          break;
        }
      }

      if(isValid == false) {
        continue;
      }

      if(height < 1 || height > 1.05) {
        continue;
      }

      float steepness = 1 - Mathf.Pow(Vector3.Dot(pointVertex.normalized, pointNormal.normalized), 4);
      if(steepness > 0.2) {
        continue;
      }
      vegetationPlacementPoints.Add(new ObjectPlacementInfo(worldTransform.TransformPoint(pointVertex),worldTransform.TransformVector(pointNormal), (int)biomeIndex));
    }
    return vegetationPlacementPoints;
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

  public void updateUVs (int channel, float [] uvData) {
    Vector2 [] uvDataVector = new Vector2[uvData.Length];
    for (int i = 0 ;i < uvData.Length; i++) {
      uvDataVector[i] = new Vector2(uvData[i], 0);
    }
    mesh.SetUVs(channel,uvDataVector);
  }

}