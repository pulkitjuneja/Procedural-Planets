using System.Collections.Generic;
using UnityEngine;
using ExtensionMethods;


public struct ObjectPlacementInfo {
  public Vector3 worldPosition;
  public Vector3 normal;
  public int biomeIndex;

  public ObjectPlacementInfo (Vector3 worldPosition, Vector3 normal, int biomeIndex) {
    this.worldPosition = worldPosition;
    this.normal = normal;
    this.biomeIndex = biomeIndex;
  }
}