using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;

namespace DataRecollection.Source
{
    class KinectManager
    {
        public Action OnStartTracking;
        public Action OnStopTracking;

        private KinectSensor kinectSensor = null;

        private Body currentTrackedBody = null;
        private ulong currentTrackingId = 0;

        private MultiSourceFrameReader multiSourceReader = null;

        private readonly WriteableBitmap colorBitmap = null;
        private readonly byte[] colorBuffer = null;

        private readonly WriteableBitmap depthBitmap = null;
        private readonly byte[] depthBuffer = null;
        private readonly ushort[] depthData = null;

        private readonly WriteableBitmap infraredBitmap = null;
        private readonly byte[] infraredBuffer= null;
        private readonly ushort[] infraredData = null;

        private readonly WriteableBitmap indexBitmap = null;
        private readonly byte[] indexBuffer = null;
        private readonly byte[] indexData = null;

        private Rect displayRectColor;
        private Rect displayRectInfrared;

        public enum BitmapType
        {
            Color,
            Depth,
            Infrared,
            BodyIndex
        }

        public KinectManager()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            FrameDescription irFrameDescription = this.kinectSensor.InfraredFrameSource.FrameDescription;
            FrameDescription indexFrameDescription = this.kinectSensor.BodyIndexFrameSource.FrameDescription;
            this.displayRectColor = new Rect(0.0, 0.0, colorFrameDescription.Width, colorFrameDescription.Height);
            this.displayRectInfrared = new Rect(0.0, 0.0, irFrameDescription.Width, irFrameDescription.Height);

            multiSourceReader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
            multiSourceReader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            colorBuffer = new byte[colorFrameDescription.Width * colorFrameDescription.Height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            depthBuffer = new byte[depthFrameDescription.Width * depthFrameDescription.Height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
            depthData = new ushort[depthFrameDescription.Width * depthFrameDescription.Height];

            infraredBitmap = new WriteableBitmap(irFrameDescription.Width, irFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            infraredBuffer = new byte[irFrameDescription.Width * irFrameDescription.Height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
            infraredData = new ushort[irFrameDescription.Width * irFrameDescription.Height];

            indexBitmap = new WriteableBitmap(indexFrameDescription.Width, indexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            indexBuffer = new byte[indexFrameDescription.Width * indexFrameDescription.Height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];
            indexData = new byte[indexFrameDescription.Width * indexFrameDescription.Height];

            // open the sensor
            this.kinectSensor.Open();
        }

        public KinectSensor GetSensor()
        {
            return kinectSensor;
        }

        public bool IsTracking()
        {
            return (this.currentTrackedBody != null && this.currentTrackedBody.IsTracked);
        }

        public WriteableBitmap GetBitmap(BitmapType type)
        {
            switch (type)
            {
                case BitmapType.Color:
                    return this.colorBitmap;
                case BitmapType.Depth:
                    return this.depthBitmap;
                case BitmapType.Infrared:
                    return this.infraredBitmap;
                case BitmapType.BodyIndex:
                    return this.indexBitmap;
            }

            return null;
        }

        public ushort[] GetData(BitmapType type)
        {
            switch (type)
            {
                case BitmapType.Depth:
                    return this.depthData;
                case BitmapType.Infrared:
                    return this.infraredData;
            }

            return null;
        }

        public Rect GetInfraredRect()
        {
            return this.displayRectInfrared;
        }

        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            var reference = e.FrameReference.AcquireFrame();
            using (BodyFrame frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    HandleBodyFrame(frame);
                }
            }

            using (ColorFrame frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    ColorToBitmap(frame);
                }
            }

            using (DepthFrame frame = reference.DepthFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    DepthToBitmap(frame);
                }
            }

            using (InfraredFrame frame = reference.InfraredFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    InfraredToBitmap(frame);
                }
            }

            using (BodyIndexFrame frame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    IndexToBitmap(frame);
                }
            }
        }

        private void HandleBodyFrame(BodyFrame frame)
        {
            if (this.currentTrackedBody != null)
            {
                this.currentTrackedBody = FindBodyWithTrackingId(frame, this.currentTrackingId);

                if (this.currentTrackedBody == null)
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
                this.currentTrackedBody = selectedBody;
                this.currentTrackingId = selectedBody.TrackingId;
            }
        }

        private void ColorToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(colorBuffer);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(colorBuffer, ColorImageFormat.Bgra);
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.colorBitmap.WritePixels(new Int32Rect(0, 0, width, height), colorBuffer, stride, 0);
        }

        private void DepthToBitmap(DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            frame.CopyFrameDataToArray(depthData);

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

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.depthBitmap.WritePixels(new Int32Rect(0, 0, width, height), depthBuffer, stride, 0);
        }

        private void InfraredToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

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

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.infraredBitmap.WritePixels(new Int32Rect(0, 0, width, height), infraredBuffer, stride, 0);
        }

        private void IndexToBitmap(BodyIndexFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

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

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.indexBitmap.WritePixels(new Int32Rect(0, 0, width, height), indexBuffer, stride, 0);
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
}
