using UnityEngine;
using System.Collections;
using System.IO;
using Windows.Kinect;

public class HeadTracker : MonoBehaviour
{
    private KinectSensor _Sensor;
    private BodyFrameReader _Reader;
    private Body[] _Data = null;

    public float OffsetX;
    public float OffsetY;
    public float OffsetZ;
    public float MultiplyX = 1f;
    public float MultiplyY = 1f;
    public float MultiplyZ = 1f;

    void Start()
    {
        _Sensor = KinectSensor.GetDefault();

        if (_Sensor != null)
        {
            _Reader = _Sensor.BodyFrameSource.OpenReader();

            if (!_Sensor.IsOpen)
            {
                _Sensor.Open();
            }
        }
    }

    void Update()
    {
        if (_Reader != null)
        {
            var frame = _Reader.AcquireLatestFrame();
            if (frame != null)
            {
                if (_Data == null)
                {
                    _Data = new Body[_Sensor.BodyFrameSource.BodyCount];
                }

                frame.GetAndRefreshBodyData(_Data);

                if (_Data.Length > 0 && _Data[0].IsTracked)
                {
                    Windows.Kinect.Joint head = _Data[0].Joints[JointType.Head];
                    Vector3 jointPos = GetVector3FromJoint(head);

                    float NewOffsetX = jointPos.x * MultiplyX + OffsetX;
                    float NewOffsetY = jointPos.y * MultiplyY + OffsetY;
                    float NewOffsetZ = jointPos.z * MultiplyZ + OffsetZ;
 
                    transform.position = new Vector3(NewOffsetX, NewOffsetY, NewOffsetZ);
                }

                frame.Dispose();
                frame = null;
            }
        }
    }

    private static Vector3 GetVector3FromJoint(Windows.Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
    }

    void OnApplicationQuit()
    {
        if (_Reader != null)
        {
            _Reader.Dispose();
            _Reader = null;
        }

        if (_Sensor != null)
        {
            if (_Sensor.IsOpen)
            {
                _Sensor.Close();
            }

            _Sensor = null;
        }
    }
}