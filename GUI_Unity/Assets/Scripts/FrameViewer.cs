using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FrameViewer : MonoBehaviour
{
    public KinectManager.ImageType frameType;

    void Start()
    {
        gameObject.GetComponent<Renderer>().material.SetTextureScale("_MainTex", new Vector2(-1, 1));
    }

    void Update()
    {
        gameObject.GetComponent<Renderer>().material.mainTexture = KinectManager.instance.GetImage(frameType);
    }
}
