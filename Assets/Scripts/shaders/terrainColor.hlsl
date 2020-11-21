
float temperatureMoistureBiomeMap[50];
float numTemperatureRegions;
float numMoistureRegions;

void terrainColor_float (float3 objectPosition, 
      float moistureInterpolation, 
      float termperatureInterpolation,
      out float biomeIndex,
      out float biomeIndexBlend) {
  float moistureInterval = 1/numMoistureRegions;
  float temperatureInterval = 1/numTemperatureRegions;
  float moistureIndex = -1;
  float temperatureIndex = -1;
  for (int i = 1 ; i <= numMoistureRegions; i++) {
    if(moistureInterpolation < (i*moistureInterval)) {
      moistureIndex = i-1;
      break;
    }
  }
  float moistureBlendWeight = ((moistureIndex + 1)*moistureInterval) * (1 - moistureInterpolation);
  for (i = 1 ; i <= numTemperatureRegions; i++) {
    if(termperatureInterpolation < (i*temperatureInterval)) {
      temperatureIndex = i-1;
      break;
    }
  }
  float temperatureBBlendWeight = ((temperatureIndex + 1)*temperatureInterval) * (1 - termperatureInterpolation);
  int biomeIndexInt = temperatureMoistureBiomeMap[moistureIndex* numTemperatureRegions + temperatureIndex];
  biomeIndex = (float)biomeIndexInt / (float)(numTemperatureRegions *numTemperatureRegions) + 0.15;
  biomeIndexBlend = (float)biomeIndexInt / (float)(numTemperatureRegions *numTemperatureRegions) + 0.15;
}