using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum orientationAxes  {
    up,
    right,
    forward
}

public class placeableObject : MonoBehaviour
{
    public Vector3 positionOffset;
    public orientationAxes orientationAxis;

    public void placeObject(Vector3 worldPosition, Vector3 upNormal, Vector3 localScale) {
        this.transform.position = worldPosition + positionOffset;
        switch(orientationAxis) {
            case orientationAxes.up : transform.up = upNormal;
            break;
            case orientationAxes.forward : transform.forward = upNormal;
            break;
            case orientationAxes.right : transform.right = upNormal;
            break;
        }
        transform.localScale = localScale;
    }
}
