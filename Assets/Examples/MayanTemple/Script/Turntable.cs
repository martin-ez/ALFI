using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turntable : MonoBehaviour
{
    public float speed = 10f;

    void Update()
    {
        transform.localEulerAngles = new Vector3(0, transform.localEulerAngles.y + speed * Time.deltaTime, 0);
    }
}
