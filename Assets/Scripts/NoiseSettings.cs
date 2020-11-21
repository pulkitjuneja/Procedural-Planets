[System.Serializable]
public struct SimpleNoiseSettings {
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float noiseScale;
    public float noiseStrength;
    public float verticalOffset;
}

[System.Serializable]
public struct SimpleNoise01Settings {
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float noiseScale;
    public float offset;
    public float gain;
}

[System.Serializable]
public struct RidgeNoiseSettings {
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float noiseScale;
    public float noiseStrength;
    public float verticalOffset;
    public float power;
    public float gain;
    public float samplingDistance;
}