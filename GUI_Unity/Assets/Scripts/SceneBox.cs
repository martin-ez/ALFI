using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneBox : MonoBehaviour {

    public Light sceneLight;

    public float ligthAnimationTime = 1f;

    public void LigthsOn()
    {
        StartCoroutine(ChangeLigthIntensity(1f, 10f));
    }

    public void LigthsOff()
    {
        StartCoroutine(ChangeLigthIntensity(10f, 1f));
    }

    IEnumerator ChangeLigthIntensity(float start, float end)
    {
        sceneLight.range = start;

        float currentTime = 0;
        float i = 0;

        while (i < 1)
        {
            currentTime += Time.deltaTime;
            i = currentTime / ligthAnimationTime;
            sceneLight.range = Mathf.Lerp(start, end, i);
            yield return null;
        }
        sceneLight.range = end;
    }
}
