using System;
using UnityEngine;
using Windows.Kinect;

public class KinectManager : MonoBehaviour
{

    public static KinectManager instance = null;
    private DataCapture dataCapture;

    public Action OnStartTracking;
    public Action OnStopTracking;

    private KinectSensor kinectSensor = null;

    private Body currentTrackedBody = null;
    private ulong currentTrackingId = 0;

    private MultiSourceFrameReader multiSourceReader = null;

    private Texture2D colorImg;
    private byte[] colorBuffer;
    private Texture2D depthImg;
    private ushort[] depthData;
    private byte[] depthBuffer;
    private Texture2D infraredImg;
    private ushort[] infraredData;
    private byte[] infraredBuffer;
    private Texture2D indexImg;
    private byte[] indexData;
    private byte[] indexBuffer;

    private int infraredWidth;
    private int infraredHeight;

    public enum ImageType
    {
        Color,
        Depth,
        Infrared,
        BodyIndex
    }

    void Awake()
    {
        //Check if instance already exists
        if (instance == null) instance = this;
        //If instance already exists and it's not this:
        else if (instance != this) Destroy(gameObject);

        //Sets this to not be destroyed when reloading scene
        DontDestroyOnLoad(gameObject);

        Init();
    }

    void Init()
    {
        dataCapture = new DataCapture();

        kinectSensor = KinectSensor.GetDefault();

        FrameDescription colorFrameDescription = kinectSensor.ColorFrameSource.FrameDescription;
        FrameDescription depthFrameDescription = kinectSensor.DepthFrameSource.FrameDescription;
        FrameDescription irFrameDescription = kinectSensor.InfraredFrameSource.FrameDescription;
        FrameDescription indexFrameDescription = kinectSensor.BodyIndexFrameSource.FrameDescription;

        multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);

        colorImg = new Texture2D(colorFrameDescription.Width, colorFrameDescription.Height, TextureFormat.RGBA32, false);
        colorBuffer = new byte[colorFrameDescription.BytesPerPixel * colorFrameDescription.LengthInPixels];

        depthImg = new Texture2D(depthFrameDescription.Width, depthFrameDescription.Height, TextureFormat.RGBA32, false);
        depthBuffer = new byte[depthFrameDescription.Width * depthFrameDescription.Height * colorFrameDescription.BytesPerPixel];
        depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

        infraredImg = new Texture2D(irFrameDescription.Width, irFrameDescription.Height, TextureFormat.RGBA32, false);
        infraredBuffer = new byte[irFrameDescription.Width * irFrameDescription.Height * colorFrameDescription.BytesPerPixel];
        infraredData = new ushort[irFrameDescription.Width * irFrameDescription.Height];

        indexImg = new Texture2D(indexFrameDescription.Width, indexFrameDescription.Height, TextureFormat.RGBA32, false);
        indexBuffer = new byte[indexFrameDescription.Width * indexFrameDescription.Height * colorFrameDescription.BytesPerPixel];
        indexData = new byte[indexFrameDescription.Width * indexFrameDescription.Height];

        infraredWidth = irFrameDescription.Width;
        infraredHeight = irFrameDescription.Height;

        // open the sensor
        kinectSensor.Open();
    }

    public bool IsTracking()
    {
        return (currentTrackedBody != null && currentTrackedBody.IsTracked);
    }

    public Body GetBody()
    {
        return currentTrackedBody;
    }

    public Texture2D GetImage(ImageType type)
    {
        switch (type)
        {
            case ImageType.Color:
                return colorImg;
            case ImageType.Depth:
                return depthImg;
            case ImageType.Infrared:
                return infraredImg;
            case ImageType.BodyIndex:
                return indexImg;
        }

        return null;
    }

    public void NewSubject()
    {
        dataCapture.NewSubject();
    }

    public void Capture()
    {
        dataCapture.CaptureImage(colorImg, "color", 0);
        dataCapture.CaptureImage(depthImg, "depth", 0);
        dataCapture.CaptureImage(infraredImg, "infrared", 0);
        dataCapture.CaptureImage(indexImg, "index", 0);
        dataCapture.CaptureData(depthData, "depth", 0, infraredWidth, infraredHeight);
        dataCapture.CaptureData(infraredData, "infrared", 0, infraredWidth, infraredHeight);
    }

    void Update()
    {
        if (multiSourceReader != null)
        {
            MultiSourceFrame multiFrame = multiSourceReader.AcquireLatestFrame();

            if (multiFrame != null)
            {
                HandleBodyFrame(multiFrame.BodyFrameReference.AcquireFrame());
                ColorToBitmap(multiFrame.ColorFrameReference.AcquireFrame());
                DepthToBitmap(multiFrame.DepthFrameReference.AcquireFrame());
                InfraredToBitmap(multiFrame.InfraredFrameReference.AcquireFrame());
                IndexToBitmap(multiFrame.BodyIndexFrameReference.AcquireFrame());
            }
        }
    }

    private void HandleBodyFrame(BodyFrame frame)
    {
        if (currentTrackedBody != null)
        {
            currentTrackedBody = FindBodyWithTrackingId(frame, currentTrackingId);

            if (currentTrackedBody == null)
            {
                OnStopTracking?.Invoke();
            }
        }
        else
        {
            Body selectedBody = FindClosestBody(frame);

            if (selectedBody == null)
            {
                return;
            }

            OnStartTracking?.Invoke();
            currentTrackedBody = selectedBody;
            currentTrackingId = selectedBody.TrackingId;
        }
    }

    private void ColorToBitmap(ColorFrame frame)
    {
        if (frame != null)
        {
            frame.CopyConvertedFrameDataToArray(colorBuffer, ColorImageFormat.Rgba);
            colorImg.LoadRawTextureData(colorBuffer);
            colorImg.Apply();

            frame.Dispose();
            frame = null;
        }
    }

    private void DepthToBitmap(DepthFrame frame)
    {
        if (frame != null)
        {
            frame.CopyFrameDataToArray(depthData);

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                double interpolation = (double)(depth - minDepth) / (double)(maxDepth - minDepth);
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? interpolation * 255 : 0);
                depthBuffer[colorIndex++] = intensity; // Blue
                depthBuffer[colorIndex++] = intensity; // Green
                depthBuffer[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            depthImg.LoadRawTextureData(depthBuffer);
            depthImg.Apply();

            frame.Dispose();
            frame = null;
        }
    }

    private void InfraredToBitmap(InfraredFrame frame)
    {
        if (frame != null)
        {
            frame.CopyFrameDataToArray(infraredData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                infraredBuffer[colorIndex++] = intensity; // Blue
                infraredBuffer[colorIndex++] = intensity; // Green   
                infraredBuffer[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            infraredImg.LoadRawTextureData(infraredBuffer);
            infraredImg.Apply();

            frame.Dispose();
            frame = null;
        }
    }

    private void IndexToBitmap(BodyIndexFrame frame)
    {
        if (frame != null)
        {
            frame.CopyFrameDataToArray(indexData);

            int colorIndex = 0;
            for (int i = 0; i < indexData.Length; ++i)
            {
                byte intensity = 0;
                if (indexData[i] < 6)
                {
                    intensity = 255;
                }

                indexBuffer[colorIndex++] = intensity; // Blue
                indexBuffer[colorIndex++] = intensity; // Green   
                indexBuffer[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            indexImg.LoadRawTextureData(indexBuffer);
            indexImg.Apply();

            frame.Dispose();
            frame = null;
        }
    }

    private static Body FindBodyWithTrackingId(BodyFrame bodyFrame, ulong trackingId)
    {
        Body result = null;

        Body[] bodies = new Body[bodyFrame.BodyCount];
        bodyFrame.GetAndRefreshBodyData(bodies);

        foreach (var body in bodies)
        {
            if (body.IsTracked)
            {
                if (body.TrackingId == trackingId)
                {
                    result = body;
                    break;
                }
            }
        }

        return result;
    }

    private static Body FindClosestBody(BodyFrame bodyFrame)
    {
        Body result = null;
        double closestBodyDistance = double.MaxValue;

        Body[] bodies = new Body[bodyFrame.BodyCount];
        bodyFrame.GetAndRefreshBodyData(bodies);

        foreach (var body in bodies)
        {
            if (body.IsTracked)
            {
                var currentLocation = body.Joints[JointType.SpineBase].Position;

                var currentDistance = VectorLength(currentLocation);

                if (result == null || currentDistance < closestBodyDistance)
                {
                    result = body;
                    closestBodyDistance = currentDistance;
                }
            }
        }

        return result;
    }

    private static double VectorLength(CameraSpacePoint point)
    {
        var result = Math.Pow(point.X, 2) + Math.Pow(point.Y, 2) + Math.Pow(point.Z, 2);

        result = Math.Sqrt(result);

        return result;
    }

    public void CloseReaders()
    {
        if (this.multiSourceReader != null)
        {
            this.multiSourceReader.Dispose();
            this.multiSourceReader = null;
        }

        if (this.kinectSensor != null)
        {
            this.kinectSensor.Close();
            this.kinectSensor = null;
        }
    }
}
