using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;


public class FaceChunk {
  
  Dictionary<int, Mesh> lodMeshes;
  GameObject worldParent;
  public int vertexCount;
  public int triangleCount;
  public Bounds chunkBounds;
  public int currentResolution;
  LODInfo [] detailLevels;
  Vector3 localUp;
  Vector3 axisA;
  Vector3 axisB;
  float xIndex;
  float yIndex;
  int [] triangles;
  Vector3 [] vertices;

  List<Vector3> boundsMeshVertexAllocation;
  List<int> boundsMeshTriangleAllocation;




  public FaceChunk (int chunkResolution, Vector3 localUp, float xIndex, float yIndex, LODInfo [] detailLevels, GameObject worldParent) {
    this.localUp = localUp;

    axisA = new Vector3(localUp.y, localUp.z, localUp.x);
    axisB = Vector3.Cross(localUp, axisA);

    lodMeshes = new Dictionary<int, Mesh>(detailLevels.Length);
    for (int i = 0; i < detailLevels.Length; i++) {
      Mesh mesh = new Mesh();
      mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
      lodMeshes.Add(detailLevels[i].lod, mesh);
    }

    this.detailLevels = detailLevels;

    this.xIndex = xIndex;
    this.yIndex = yIndex;

    boundsMeshVertexAllocation = new List<Vector3>(64);
    boundsMeshTriangleAllocation = new List<int>(294);

    this.worldParent = worldParent; 
    updateMeshWorldBounds(chunkResolution);
  }

  public int getResolutionBasedOnCameraDistance (int chunkResolution) {
    updateMeshWorldBounds(chunkResolution);
    float distanceFromCamera = Mathf.Sqrt(chunkBounds.SqrDistance(Camera.main.transform.position));
    float maxViewDistance = detailLevels[detailLevels.Length -1].visibleDstThreshold;

    int lodIndex = 0;
    for(int i = 0; i < detailLevels.Length -1; i++) {
      if(distanceFromCamera > detailLevels[i].visibleDstThreshold) {
        lodIndex += 1;
      } else {
        break;
      }
    }
    return detailLevels[lodIndex].lod;
  }

  void updateMeshWorldBounds (int chunkResolution) {
    generateMesh(chunkResolution, 8, boundsMeshVertexAllocation, boundsMeshTriangleAllocation);

    // TODO find a better way to do this
    Mesh tempMesh = new Mesh();
    tempMesh.SetVertices(boundsMeshVertexAllocation);
    Bounds objectSpaceBounds = tempMesh.bounds;
    var center = worldParent.transform.TransformPoint(objectSpaceBounds.center);

    var extents = objectSpaceBounds.extents;
    var axisX = worldParent.transform.TransformVector(extents.x, 0, 0);
    var axisY = worldParent.transform.TransformVector(0, extents.y, 0);
    var axisZ = worldParent.transform.TransformVector(0, 0, extents.z);

    // sum their absolute value to get the world extents
    extents.x = (axisX.x) + (axisY.x) + (axisZ.x);
    extents.y = (axisX.y) + (axisY.y) + (axisZ.y);
    extents.z = (axisX.z) + (axisY.z) + (axisZ.z);

    chunkBounds = new Bounds { center = center, extents = extents };
    Object.DestroyImmediate(tempMesh);
  }

  public bool isLodGenerationRequired(int chunkResolution, MeshFilter chunkMeshFilter) {
    int newResolution = getResolutionBasedOnCameraDistance(chunkResolution);
    if(newResolution == currentResolution) {
      // no update required
      return false;
    } else if (lodMeshes[newResolution].vertexCount > 0) {
      chunkMeshFilter.sharedMesh = lodMeshes[newResolution];
      return false;
    } else if(Mathf.Sqrt(chunkBounds.SqrDistance(Camera.main.transform.position)) > 120){
      // mesh far away , update not required
      return false;
    } else {
      return true;
    }
  }

  public void generateMesh (int chunkResolution, int resolution, List<Vector3> vertices, List<int> triangles) {
    float UVStep = 1f/chunkResolution;
    float step = UVStep/(resolution-1);
    Vector2 offset = new Vector2((-0.5f + xIndex*UVStep), (-0.5f + yIndex*UVStep));
    vertexCount = resolution * resolution;
    triangleCount = (resolution - 1) * (resolution - 1) * 6;
    vertices.Clear();
    triangles.Clear();
    int triIndex = 0;

    for(int y = 0 ; y < resolution; y++) {
      for(int x = 0 ; x < resolution; x++) {
        int i = x + y * resolution;
        Vector2 position = offset + new Vector2(x* step, y*step);
        Vector3 pointOnCube = localUp + position.x*2*axisA + position.y*2*axisB;
        Vector3 pointOnUnitSphere = pointOnCube.normalized;
        vertices.Insert(i, pointOnUnitSphere); 

        if (x != resolution - 1 && y != resolution - 1)
        {
          triangles.Insert(triIndex, i);
          triangles.Insert(triIndex + 1, i + resolution + 1);
          triangles.Insert(triIndex + 2, i + resolution);

          triangles.Insert(triIndex + 3, i);
          triangles.Insert(triIndex + 4, i + 1);
          triangles.Insert(triIndex + 5, i + resolution + 1);
          triIndex += 6;
        }
      }
    }
  }

  public Vector3 [] getVertices () {
    return vertices;
  }

public List<ObjectPlacementInfo> getPointsForObjectPlacement (Transform worldTransform, float minDistance, float numIterations) {

    Mesh maxAvailableDetailMesh = null;
    int resolution = -1;
    for(int i = detailLevels.Length-1; i>=0; i--) {
      if (lodMeshes[detailLevels[i].lod].vertexCount > 0) {
        maxAvailableDetailMesh = lodMeshes[detailLevels[i].lod];
        resolution = detailLevels[i].lod;
        break;
      }
    }

    List<Vector3> vertices = new List<Vector3>();
    List<Vector3> normals = new List<Vector3>();
    List<Vector2> heights = new List<Vector2>();
    List<Vector2> biomeData = new List<Vector2>();
    List<ObjectPlacementInfo> vegetationPlacementPoints = new List<ObjectPlacementInfo>();

    maxAvailableDetailMesh.GetVertices(vertices);
    maxAvailableDetailMesh.GetNormals(normals);
    maxAvailableDetailMesh.GetUVs(4,heights);
    maxAvailableDetailMesh.GetUVs(3, biomeData);

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
      float biomeIndex = biomeData[pointIndex].y;

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
      vegetationPlacementPoints.Add(new ObjectPlacementInfo(worldTransform.TransformPoint(pointVertex),worldTransform.TransformVector(pointNormal), (int)biomeIndex, worldParent.transform));
    }
    return vegetationPlacementPoints;
  }

  public void updateMesh (List<Vector3> vertices, List<int> triangles) {
    lodMeshes[currentResolution].Clear();
    lodMeshes[currentResolution].SetVertices (vertices);
		lodMeshes[currentResolution].SetTriangles (triangles, 0, true);
		lodMeshes[currentResolution].RecalculateNormals ();
  } 

  public Mesh getCurrentLodMesh () {
    return lodMeshes[currentResolution];
  }

  public void updateUVs (int channel, Vector2 [] uvData) {
    lodMeshes[currentResolution].SetUVs(channel,uvData);
  }

  public void updateUVs (int channel, float [] uvData) {
    Vector2 [] uvDataVector = new Vector2[uvData.Length];
    for (int i = 0 ;i < uvData.Length; i++) {
      uvDataVector[i] = new Vector2(uvData[i], 0);
    }
    lodMeshes[currentResolution].SetUVs(channel,uvDataVector);
  }

}