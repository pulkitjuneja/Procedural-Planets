using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;
using Unity.Collections;
using Unity.Jobs;

public class TreeGenerator : MonoBehaviour {
  public float minDistanceBetweenTrees;
  public int numIterations;

  FindObjectPlacementPositionsJob getVegetationPositionsJob;

  List<ObjectPlacementInfo> vegetationPlacementPoints;

  public void GenerateTrees (FaceChunk[] faceChunks, int resolution, MeshFilter[] meshFilters, Biome [] biomes) {
    CalculateTreePlacementPositions(faceChunks, resolution, meshFilters);
    placeTrees(biomes);
  }

  void CalculateTreePlacementPositions  (FaceChunk[] faceChunks, int resolution, MeshFilter[] meshFilters) {
    vegetationPlacementPoints = new List<ObjectPlacementInfo>();
    for (int i = 0; i < faceChunks.Length; i++) {
      List<ObjectPlacementInfo> pointsToAdd = faceChunks[i].getPointsForObjectPlacement(resolution, meshFilters[i].gameObject.transform, minDistanceBetweenTrees, numIterations);
      vegetationPlacementPoints.AddRange(pointsToAdd);
    }
  }

  void placeTrees (Biome [] biomes) {
    Transform treeParent = transform.Find("Trees");
    if(treeParent != null) {
      for (int i = treeParent.childCount; i > 0; --i) {
        UnityEditor.EditorApplication.delayCall += () => DestroyImmediate(this.transform.GetChild(0).gameObject);
      }
    }
    treeParent = new GameObject("Trees").transform;
    treeParent.parent = transform;
    treeParent.localScale = new Vector3(1,1,1);
    foreach(ObjectPlacementInfo point in vegetationPlacementPoints) {
      GameObject treePrefab = biomes[0].TreePrefabs[0];
      Vector3 position = point.worldPosition;
      GameObject tree = Instantiate(treePrefab,position, Quaternion.identity, treeParent);
      tree.transform.localScale = treePrefab.transform.localScale;
      tree.transform.up = point.normal.normalized;
    }
  }

  void OnDrawGizmos () {
    if (vegetationPlacementPoints != null) {
      foreach (ObjectPlacementInfo point in vegetationPlacementPoints) {
        Gizmos.DrawSphere(point.worldPosition, 0.01f);
        Gizmos.DrawRay(point.worldPosition, point.normal.normalized);
      }
    }
  }

}