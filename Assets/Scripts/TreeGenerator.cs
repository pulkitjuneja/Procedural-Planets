using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using ExtensionMethods;
using Unity.Collections;
using Unity.Jobs;

public class TreeGenerator : MonoBehaviour {
  public float minDistanceBetweenTrees;
  public int numIterations;
  public GameObject treeParent;
  List<GameObject> generatedTrees;

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
    if(treeParent == null) {
      treeParent = new GameObject("Trees");
      treeParent.transform.parent = transform;
      treeParent.transform.localScale = new Vector3(1,1,1);
    }

    // remove existing trees
    if(generatedTrees != null) {
      if(!Application.isPlaying) {
        foreach (GameObject tree in generatedTrees) {
          UnityEditor.EditorApplication.delayCall+=()=>{DestroyImmediate(tree);};
        }
      } else {
        foreach (GameObject tree in generatedTrees) { 
          Destroy(tree);
        }
      }
    } else {
      generatedTrees = new List<GameObject>();
    }
    
    // generate new trees
    foreach(ObjectPlacementInfo point in vegetationPlacementPoints) {
      GameObject treePrefab = biomes[0].TreePrefabs[0];
      Vector3 position = point.worldPosition;
      GameObject tree = Instantiate(treePrefab,position, Quaternion.identity, treeParent.transform);
      tree.transform.localScale = treePrefab.transform.localScale;
      tree.transform.up = point.normal.normalized;
      generatedTrees.Add(tree);
    }
  }

  IEnumerator DestroyTree (GameObject tree) {
    yield return new WaitForEndOfFrame();
    DestroyImmediate(tree); 
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