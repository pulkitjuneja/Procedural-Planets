[System.Serializable]
public struct FlatlandNoiseSettings {
    public int octaves;
    public float lacunarity;
    public float persistence;
    public float noiseScale;
    public float noiseStrength;
    public float verticalOffset;
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