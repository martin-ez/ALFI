using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class ColorImageRenderer : MonoBehaviour
{
    KinectFaceManager kinectManager;

    void Start()
    {
        kinectManager = FindObjectOfType<KinectFaceManager>();
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        if (kinectManager == null)
        {
            return;
        }

        // gameObject.GetComponent<Renderer>().material.mainTexture = kinectManager.GetColorTexture();
    }
}
