#include "./perlin.hlsl"

struct SimpleNoiseSettings {
  int octaves;
  float lacunarity;
  float persistence;
  float noiseScale;
  float noiseStrength;
  float verticalOffset;
};

struct SimpleNoise01Settings {
  int octaves;
  float lacunarity;
  float persistence;
  float noiseScale;
  float offset;
  float gain;
};

struct RidgeNoiseSettings {
  int octaves;
  float lacunarity;
  float persistence;
  float noiseScale;
  float noiseStrength;
  float verticalOffset;
  float power;
  float gain;
  float samplingDistance;
};