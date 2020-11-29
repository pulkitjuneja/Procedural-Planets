using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEditor;

public class BiomeGenerator : MonoBehaviour {

  public ComputeShader moistureCompute;

  [Header("Noise Settings")]

  // how much should the height affect the moisture Value
  [Header("Temperature and moisture Settings")]

  public float moistureHeightInfluence;
  public float temperatureHeightInfluence;
  public int moistureSeed;
  public int temperatureSeed;
  public SimpleNoise01Settings moistureNoise;
  public SimpleNoise01Settings temperatureNoise;

  [Header("Biome setttings")]

  [Range(0,5)]
  public int numMoistureRegions;

  [Range(0,5)]
  public int numTemperatureRegions;
  public Biome [] biomes;
  public Biome waterBiome;
  public Action onSettingsUpdated;
  public Material terrainMaterial;

  Texture2D biomeTexture;

  List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();
  
  void OnValidate() {
    // check that there are no duplicate biomes
    Array.Resize(ref biomes, numMoistureRegions * numMoistureRegions);
    bool areBiomesValid = true;

    for(int i = 0 ; i< biomes.Length; i++) {
      for (int j = 0; j< biomes.Length; j++) {
        if (i == j ) 
          continue;
        else if (biomes[i].Equals(biomes[j])) {
          areBiomesValid = false;
          break;
        }
      }
    }  

    if(!areBiomesValid) {
      Debug.Log("Biomes not valid");
    } 

    if(onSettingsUpdated != null && !EditorApplication.isPlayingOrWillChangePlaymode) {
       onSettingsUpdated.Invoke();
    }
  }

  public Vector2[] generateMoistureAndTemperatureData (ComputeBuffer vertexBuffer, ComputeBuffer heightMapBuffer, float minHeight, float maxHeight) {
    ComputeBuffer moistureTemperatureBuffer = new ComputeBuffer(vertexBuffer.count,2 * sizeof(float));
    buffersToRelease.Add(moistureTemperatureBuffer); 

    SimpleNoise01Settings []  moisteureNoiseData = new SimpleNoise01Settings [] {moistureNoise, temperatureNoise};
    ComputeBuffer moistureNoiseSettingsBUffer = new ComputeBuffer(moisteureNoiseData.Length, sizeof(int) + 5*sizeof(float));
    moistureNoiseSettingsBUffer.SetData(moisteureNoiseData);
    buffersToRelease.Add(moistureNoiseSettingsBUffer);

    RNGHelper random = new RNGHelper(moistureSeed);
    Vector2 moistureSeedOffset = new Vector3(random.nextDouble(), random.nextDouble(), random.nextDouble());
    random = new RNGHelper(temperatureSeed);
    Vector3 temperatureSeedOffset = new Vector3(random.nextDouble(), random.nextDouble(), random.nextDouble());

    float [] temperatureMoistureBiomeMap = new float [numMoistureRegions*numTemperatureRegions];
    for(int i = 0 ; i< biomes.Length; i++) {
      int position = biomes[i].moistureRegionIndex * numTemperatureRegions + biomes[i].temperatureRegionIndex;
      temperatureMoistureBiomeMap[position] = i;
    }

    ComputeBuffer temperatureMoistureBiomeMapBuffer = new ComputeBuffer(temperatureMoistureBiomeMap.Length, sizeof(float));
    temperatureMoistureBiomeMapBuffer.SetData(temperatureMoistureBiomeMap);
    buffersToRelease.Add(temperatureMoistureBiomeMapBuffer); 

    moistureCompute.SetBuffer(0, "vertices", vertexBuffer);
    moistureCompute.SetBuffer(0, "heights", heightMapBuffer);
    moistureCompute.SetBuffer(0, "moisture", moistureTemperatureBuffer);
    moistureCompute.SetBuffer(0, "moistureNoiseSettings", moistureNoiseSettingsBUffer);
    moistureCompute.SetBuffer(0, "temperatureMoistureBiomeMap", temperatureMoistureBiomeMapBuffer);
    moistureCompute.SetVector("moistureSeedOffset", moistureSeedOffset);
    moistureCompute.SetVector("temperatureSeedOffset", temperatureSeedOffset);
    moistureCompute.SetFloat("minHeight", minHeight);
    moistureCompute.SetFloat("maxHeight", maxHeight);
    moistureCompute.SetFloat("numMoistureRegions", numMoistureRegions);
    moistureCompute.SetFloat("numTemperatureRegions", numTemperatureRegions);
    moistureCompute.SetFloat("moistureHeightInfluence", moistureHeightInfluence);
    moistureCompute.SetFloat("temperatureHeightInfluence", temperatureHeightInfluence);
    moistureCompute.SetVector("planetUp", transform.up);

    uint threadDimensionX, threadDimensionY, threadDimensionZ;
    moistureCompute.GetKernelThreadGroupSizes(0, out threadDimensionX, out threadDimensionY, out threadDimensionZ);
    int threadGroupSizeX = Mathf.CeilToInt(vertexBuffer.count/(float)threadDimensionX);

    moistureCompute.Dispatch(0, threadGroupSizeX, 1, 1);

    Vector2 [] moistureTemperatureData = new Vector2[vertexBuffer.count];
    moistureTemperatureBuffer.GetData(moistureTemperatureData);
    releaseBuffers();

    return moistureTemperatureData;
  }
  
  public void updateTerrainMaterial () {
    int textureResolution = 256;
    biomeTexture = new Texture2D(textureResolution, numMoistureRegions * numTemperatureRegions + 1, TextureFormat.ARGB32, false, false);
    biomeTexture.filterMode = FilterMode.Point;
    for(int i = 0; i < biomes.Length; i ++) {
      for(int j = 0 ; j <textureResolution ; j++ ) {
        biomeTexture.SetPixel(j,i, biomes[i].biomeColors.Evaluate((float)j/(float)256));
      }
    }
    // add water biome to texture
    for(int j = 0 ; j <textureResolution ; j++ ) {
        biomeTexture.SetPixel(j,biomes.Length, waterBiome.biomeColors.Evaluate((float)j/(float)256));
    } 
    terrainMaterial.SetTexture("biomeTexture", biomeTexture);
    biomeTexture.anisoLevel++;
    terrainMaterial.SetFloat("numMoistureRegions", numMoistureRegions);
    terrainMaterial.SetFloat("numTemperatureRegions", numTemperatureRegions);
  }

  void releaseBuffers () {
    foreach(ComputeBuffer buffer in buffersToRelease) {
      buffer.Release();
    }
  }
}