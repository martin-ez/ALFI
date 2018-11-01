using System.Collections;
using UnityEngine;

public class StateManager : MonoBehaviour {

    private SceneBox scene;

    public enum State
    {
        Idle,
        Welcome,
        Agreedment,
        Setup,
        Capture
    }

    void Start()
    {
        scene = FindObjectOfType<SceneBox>();

        KinectManager.instance.OnStartTracking += OnNewSubject;
        KinectManager.instance.OnStopTracking += OnSubjectLeave;
    }

    void OnNewSubject()
    {
        scene.LigthsOn();
        KinectManager.instance.NewSubject();
        KinectManager.instance.Capture();
    }

    void OnSubjectLeave()
    {
        scene.LigthsOff();
    }
}
