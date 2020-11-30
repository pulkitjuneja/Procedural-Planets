using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEditor;

public class TreeGenerator : MonoBehaviour {
  public float minDistanceBetweenTrees;
  public int numIterations;
  public GameObject treeParent;
  // List<GameObject> generatedTrees;

  List<ObjectPlacementInfo> vegetationPlacementPoints;

  public void GenerateTrees (FaceChunk[] faceChunks, MeshFilter[] meshFilters, Biome [] biomes) {
    CalculateTreePlacementPositions(faceChunks, meshFilters);
    placeTrees(biomes);
  }

  void CalculateTreePlacementPositions  (FaceChunk[] faceChunks, MeshFilter[] meshFilters) {
    vegetationPlacementPoints = new List<ObjectPlacementInfo>();
    for (int i = 0; i < faceChunks.Length; i++) {
      List<ObjectPlacementInfo> pointsToAdd = faceChunks[i].getPointsForObjectPlacement(meshFilters[i].gameObject.transform, minDistanceBetweenTrees, numIterations);
      vegetationPlacementPoints.AddRange(pointsToAdd);
    }
  }

  void placeTrees (Biome [] biomes) {
    // generate new trees
    foreach(ObjectPlacementInfo point in vegetationPlacementPoints) {
      Biome currentBiome = biomes[point.biomeIndex];
      GameObject treePrefab = currentBiome.TreePrefabs[Random.Range(0,currentBiome.TreePrefabs.Length)];
      Vector3 position = point.worldPosition;
      GameObject tree = Instantiate(treePrefab,position, Quaternion.identity, point.parentChunk);
      tree.GetComponent<placeableObject>().placeObject(position, point.normal.normalized, treePrefab.transform.localScale);
    }
  }

  IEnumerator DestroyTree (GameObject tree) {
    yield return new WaitForEndOfFrame();
    DestroyImmediate(tree); 
  }

  public void DestroyAllTrees () {
    Transform treeParent = transform.Find("Trees");
    if(treeParent!= null ) {
      foreach(Transform child in treeParent) {
        Destroy(child.gameObject);
      }
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