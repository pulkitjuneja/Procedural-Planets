using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ColourGenerator : MonoBehaviour {
    public Material mat;
    public Gradient gradient;
    public float normalOffsetWeight;
    public float minElevation;
    public float maxElevation;
    public float minOffset;

    Texture2D texture;
    const int textureResolution = 50;

    public void Init () {
        if (texture == null || texture.width != textureResolution) {
            texture = new Texture2D (textureResolution, 1, TextureFormat.RGBA32, false);
        }
        minElevation = float.MaxValue;
        maxElevation = float.MinValue;
    }


    public void UpdateColors () {
        UpdateTexture ();

        mat.SetFloat("minElevation", minElevation);
        mat.SetFloat("minOffset", minOffset);
        mat.SetFloat("maxElevation", maxElevation);
        mat.SetFloat ("normalOffsetWeight", normalOffsetWeight);

        mat.SetTexture ("ramp", texture);
    }

    public void AddElevationValue (Vector3 position) {
        float elevation = position.magnitude;
        if(elevation < minElevation) {
            minElevation = elevation;
        }
        if(elevation > maxElevation) {
            maxElevation = elevation;
        }
    }

    void UpdateTexture () {
        if (gradient != null) {
            Color[] colours = new Color[texture.width];
            for (int i = 0; i < textureResolution; i++) {
                Color gradientCol = gradient.Evaluate (i / (textureResolution - 1f));
                colours[i] = gradientCol;
            }

            texture.SetPixels (colours);
            texture.Apply ();
        }
    }
}