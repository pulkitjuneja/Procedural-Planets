using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;


public struct ObjectPlacementInfo {
  public Vector3 worldPosition;
  public Vector3 normal;
  public int biomeIndex;

  public Transform parentChunk;

  public ObjectPlacementInfo (Vector3 worldPosition, Vector3 normal, int biomeIndex, Transform parentChunk) {
    this.worldPosition = worldPosition;
    this.normal = normal;
    this.biomeIndex = biomeIndex;
    this.parentChunk = parentChunk;
  }
}