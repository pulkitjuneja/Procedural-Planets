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


float ContinentNoise (float3 position, SimpleNoiseSettings noiseSettings,  float3 seedOffsetVector) {
    float noise = 0;

    float frequency = noiseSettings.noiseScale;
    float amplitude = 1;
    // float3 warp = snoise( TrilinearRepeat, ws*0.004 ).xyz

    for (int j =0; j < noiseSettings.octaves; j ++) {
        float n = cnoise((position) * frequency + seedOffsetVector);
        noise += n * amplitude;
        amplitude *= noiseSettings.persistence;
        frequency *= noiseSettings.lacunarity;
    }

    //float elevation = max(0, (noise * noiseSettings.noiseStrength) - noiseSettings.minValue );
    float elevation = noise * noiseSettings.noiseStrength + noiseSettings.verticalOffset;
    return elevation;
}