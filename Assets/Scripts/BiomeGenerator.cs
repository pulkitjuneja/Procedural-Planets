using System.Collections.Generic;
using UnityEngine;
using System;

public class BiomeGenerator : MonoBehaviour {

  public ComputeShader moistureCompute;

  [Header("Noise Settings")]

  // how much should the height affect the moisture Value
  public float moistureHeightInfluence;
  public float temperatureHeightInfluence;
  public int moistureSeed;
  public int temperatureSeed;
  public SimpleNoiseSettings moistureNoise;
  public SimpleNoiseSettings temperatureNoise;
  public Action onSettingsUpdated;

  List<ComputeBuffer> buffersToRelease = new List<ComputeBuffer>();

  void OnValidate() {
    if(onSettingsUpdated != null) {
       onSettingsUpdated.Invoke();
    }
  }

  public Vector2[] generateMoistureAndTemperatureData (ComputeBuffer vertexBuffer, ComputeBuffer heightMapBuffer, float minHeight, float maxHeight) {
    ComputeBuffer moistureTemperatureBuffer = new ComputeBuffer(vertexBuffer.count,2 * sizeof(float));
    buffersToRelease.Add(moistureTemperatureBuffer); 

    SimpleNoiseSettings []  moisteureNoiseData = new SimpleNoiseSettings [] {moistureNoise, temperatureNoise};
    ComputeBuffer moistureNoiseSettingsBUffer = new ComputeBuffer(moisteureNoiseData.Length, sizeof(int) + 5*sizeof(float));
    moistureNoiseSettingsBUffer.SetData(moisteureNoiseData);
    buffersToRelease.Add(moistureNoiseSettingsBUffer);

    RNGHelper random = new RNGHelper(moistureSeed);
    float [] moistureSeedOffset = new float []{random.nextDouble(), random.nextDouble(), random.nextDouble()};
    random = new RNGHelper(temperatureSeed);
    float [] temperatureSeedOffset = new float []{random.nextDouble(), random.nextDouble(), random.nextDouble()};

    moistureCompute.SetBuffer(0, "vertices", vertexBuffer);
    moistureCompute.SetBuffer(0, "heights", heightMapBuffer);
    moistureCompute.SetBuffer(0, "moisture", moistureTemperatureBuffer);
    moistureCompute.SetBuffer(0, "moistureNoiseSettings", moistureNoiseSettingsBUffer);
    moistureCompute.SetFloats("moistureSeedOffset", moistureSeedOffset);
    moistureCompute.SetFloats("temperatureSeedOffset", temperatureSeedOffset);
    moistureCompute.SetFloat("minHeight", minHeight);
    moistureCompute.SetFloat("maxHeight", maxHeight);
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

    void releaseBuffers () {
    foreach(ComputeBuffer buffer in buffersToRelease) {
      buffer.Release();
    }
  }
}