using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Windows.Kinect;

[RequireComponent(typeof(Camera))]
public class HolographicCamera : MonoBehaviour
{
    private Camera theCam;
    private KinectManager kinect;

    public Transform target;
    public Transform[] corners;
    public bool drawNearCone, drawFrustum;

    public float screenWidth = 16f;
    public float screenHeight = 9f;
    public float nearPlane = 0.05f;
    public float farPlane = 100f;

    public float screenHeightinMM = 480f;
    public float movementScaling = 1.0f;
    public bool kinectAbove = false;

    public float offsetX;
    public float offsetY;
    public float offsetZ;
    float keyCooldown;

    void Start()
    {
        theCam = Camera.main;

        kinect = KinectManager.instance;
    }

    void Update()
    {
        if (Input.GetKey(KeyCode.Space) && Time.time > keyCooldown)
        {
            offsetX = -transform.position.x;
            offsetY = -transform.position.y;
            offsetZ = -transform.position.z;
            keyCooldown = Time.time + 2;
        }

        if (Input.GetKey(KeyCode.Escape))
        {
            Application.Quit();
        }

        if (kinect.IsTracking())
        {
            UpdateCameraPosition();
            UpdateProjectionMatrix();
        }
    }

    void UpdateCameraPosition()
    {
        Body body = kinect.GetBody();
        Windows.Kinect.Joint head = body.Joints[JointType.Head];
        Vector3 jointPos = GetVector3FromJoint(head);

        float headZ = (jointPos.z * movementScaling + offsetZ);
        float headX = (-jointPos.x * movementScaling + offsetX);
        float headY = (jointPos.y * movementScaling + offsetY);
        if (kinectAbove)
        {
            headY = -headY;
        }

        transform.position = new Vector3(headX, headY, headZ);
        transform.LookAt(new Vector3(headX, headY, 0), Vector3.down);
    }

    void UpdateProjectionMatrix()
    {
        Vector3 pa, pb, pc, pd;
        pa = corners[0].position; //Bottom-Left
        pb = corners[1].position; //Bottom-Right
        pc = corners[2].position; //Top-Left
        pd = corners[3].position; //Top-Right

        Vector3 pe = theCam.transform.position;// eye position

        Vector3 vr = (pb - pa).normalized; // right axis of screen
        Vector3 vu = (pc - pa).normalized; // up axis of screen
        Vector3 vn = Vector3.Cross(vr, vu).normalized; // normal vector of screen

        Vector3 va = pa - pe; // from pe to pa
        Vector3 vb = pb - pe; // from pe to pb
        Vector3 vc = pc - pe; // from pe to pc
        Vector3 vd = pd - pe; // from pe to pd

        float n = -target.InverseTransformPoint(theCam.transform.position).z; // distance to the near clip plane (screen)
        float d = Vector3.Dot(va, vn); // distance from eye to screen
        float left = Vector3.Dot(vr, va) * n / d; // distance to left screen edge from the 'center'
        float right = Vector3.Dot(vr, vb) * n / d; // distance to right screen edge from 'center'
        float bottom = Vector3.Dot(vu, va) * n / d; // distance to bottom screen edge from 'center'
        float top = Vector3.Dot(vu, vc) * n / d; // distance to top screen edge from 'center'

        /*float xHead = transform.position.x;
        float yHead = transform.position.y;
        float zHead = transform.position.z;

        float aspectRatio = screenWidth / screenHeight;
        float left = ((-0.5f * aspectRatio + xHead) / zHead) * nearPlane;
        float right = ((0.5f * aspectRatio + xHead) / zHead) * nearPlane;
        float top = ((-0.5f + yHead) / zHead) * nearPlane;
        float bottom = ((0.5f + yHead) / zHead) * nearPlane;*/

        theCam.projectionMatrix = PerspectiveOffCenter(left, right, bottom, top, -n, farPlane);

        if (drawFrustum) DrawFrustum(theCam); //Draw actual camera frustum
    }

    static Matrix4x4 PerspectiveOffCenter(float left, float right, float bottom, float top, float near, float far)
    {
        float x = 2.0F * near / (right - left);
        float y = 2.0F * near / (top - bottom);
        float a = (right + left) / (right - left);
        float b = (top + bottom) / (top - bottom);
        float c = -(far + near) / (far - near);
        float d = -(2.0F * far * near) / (far - near);
        float e = -1.0F;
        Matrix4x4 m = new Matrix4x4();
        m[0, 0] = x;
        m[0, 1] = 0;
        m[0, 2] = a;
        m[0, 3] = 0;
        m[1, 0] = 0;
        m[1, 1] = y;
        m[1, 2] = b;
        m[1, 3] = 0;
        m[2, 0] = 0;
        m[2, 1] = 0;
        m[2, 2] = c;
        m[2, 3] = d;
        m[3, 0] = 0;
        m[3, 1] = 0;
        m[3, 2] = e;
        m[3, 3] = 0;
        return m;
    }

    Vector3 ThreePlaneIntersection(Plane p1, Plane p2, Plane p3)
    { //get the intersection point of 3 planes
        return ((-p1.distance * Vector3.Cross(p2.normal, p3.normal)) +
                (-p2.distance * Vector3.Cross(p3.normal, p1.normal)) +
                (-p3.distance * Vector3.Cross(p1.normal, p2.normal))) /
            (Vector3.Dot(p1.normal, Vector3.Cross(p2.normal, p3.normal)));
    }

    void DrawFrustum(Camera cam)
    {
        Vector3[] nearCorners = new Vector3[4]; //Approx'd nearplane corners
        Vector3[] farCorners = new Vector3[4]; //Approx'd farplane corners
        Plane[] camPlanes = GeometryUtility.CalculateFrustumPlanes(cam); //get planes from matrix
        Plane temp = camPlanes[1]; camPlanes[1] = camPlanes[2]; camPlanes[2] = temp; //swap [1] and [2] so the order is better for the loop

        for (int i = 0; i < 4; i++)
        {
            nearCorners[i] = ThreePlaneIntersection(camPlanes[4], camPlanes[i], camPlanes[(i + 1) % 4]); //near corners on the created projection matrix
            farCorners[i] = ThreePlaneIntersection(camPlanes[5], camPlanes[i], camPlanes[(i + 1) % 4]); //far corners on the created projection matrix
        }

        for (int i = 0; i < 4; i++)
        {
            Debug.DrawLine(nearCorners[i], nearCorners[(i + 1) % 4], Color.red, Time.deltaTime, false); //near corners on the created projection matrix
            Debug.DrawLine(farCorners[i], farCorners[(i + 1) % 4], Color.red, Time.deltaTime, false); //far corners on the created projection matrix
            Debug.DrawLine(nearCorners[i], farCorners[i], Color.red, Time.deltaTime, false); //sides of the created projection matrix
        }
    }

    private static Vector3 GetVector3FromJoint(Windows.Kinect.Joint joint)
    {
        return new Vector3(joint.Position.X, joint.Position.Y, joint.Position.Z);
    }
}
