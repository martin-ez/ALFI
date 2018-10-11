using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class FaceIndicator : MonoBehaviour
{
    public Color activeColor;
    public Color inactiveColor;

    KinectFaceManager kinectManager;
    Material mat;

    void Start()
    {
        mat = GetComponent<Renderer>().material;
        kinectManager = FindObjectOfType<KinectFaceManager>();
    }

    void Update()
    {
        if (kinectManager != null)
        {
            if (kinectManager.FaceEngaged())
            {
                mat.color = activeColor;
            }
            else
            {
                mat.color = inactiveColor;
            }
        }
    }
}
