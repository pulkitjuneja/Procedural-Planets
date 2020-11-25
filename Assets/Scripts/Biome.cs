using System.Collections.Generic;
using UnityEngine;
using System;


// Defines the properties required to define a  particular biome
[System.Serializable]
public class Biome : IEquatable<Biome>{

  [Range(0,3)]
  public int temperatureRegionIndex;

  [Range(0,3)]
  public int moistureRegionIndex;
  public Gradient biomeColors;

  public GameObject [] TreePrefabs;

  public bool Equals (Biome other) {
    if(other.temperatureRegionIndex == temperatureRegionIndex && other.moistureRegionIndex == moistureRegionIndex) {
      return true;
    }
    return false;
  }

  public override int GetHashCode() {
    return base.GetHashCode();
  }

  public override bool Equals (System.Object other) {
    Biome otherBiome = other as Biome;
    if (otherBiome == null) {
      return false;
    } else {
      return Equals(otherBiome);
    }
  }
}