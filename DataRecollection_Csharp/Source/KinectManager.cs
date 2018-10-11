using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

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
        private readonly WriteableBitmap depthBitmap = null;
        private readonly WriteableBitmap infraredBitmap = null;
        private readonly WriteableBitmap indexBitmap = null;

        private FaceFrameSource faceFrameSource = null;
        private FaceFrameReader faceFrameReader = null;
        private FaceFrameResult faceTrackedData = null;

        private HighDefinitionFaceFrameSource highDefinitionFaceFrameSource = null;
        private HighDefinitionFaceFrameReader highDefinitionFaceFrameReader = null;
        private FaceModelBuilder faceModelBuilder = null;
        private FaceModel faceModel = null;
        private readonly FaceAlignment faceAlignment = null;
        private IReadOnlyList<CameraSpacePoint> mesh;
        private IReadOnlyList<uint> meshIndices;

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

            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.depthBitmap = new WriteableBitmap(depthFrameDescription.Width, depthFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.infraredBitmap = new WriteableBitmap(irFrameDescription.Width, irFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);
            this.indexBitmap = new WriteableBitmap(indexFrameDescription.Width, indexFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.highDefinitionFaceFrameSource = new HighDefinitionFaceFrameSource(this.kinectSensor);
            this.highDefinitionFaceFrameSource.TrackingIdLost += HDFaceFrameSource_TrackingIdLost;
            this.highDefinitionFaceFrameReader = this.highDefinitionFaceFrameSource.OpenReader();
            this.highDefinitionFaceFrameReader.FrameArrived += Reader_HDFaceFrameArrived;
            this.faceModel = new FaceModel();
            this.faceAlignment = new FaceAlignment();

            FaceFrameFeatures faceFrameFeatures =
                FaceFrameFeatures.BoundingBoxInColorSpace
                | FaceFrameFeatures.PointsInColorSpace
                | FaceFrameFeatures.BoundingBoxInInfraredSpace
                | FaceFrameFeatures.PointsInInfraredSpace
                | FaceFrameFeatures.RotationOrientation
                | FaceFrameFeatures.FaceEngagement
                | FaceFrameFeatures.Glasses
                | FaceFrameFeatures.Happy
                | FaceFrameFeatures.LeftEyeClosed
                | FaceFrameFeatures.RightEyeClosed
                | FaceFrameFeatures.LookingAway
                | FaceFrameFeatures.MouthMoved
                | FaceFrameFeatures.MouthOpen;

            this.faceFrameSource = new FaceFrameSource(this.kinectSensor, 0, faceFrameFeatures);
            this.faceFrameReader = this.faceFrameSource.OpenReader();
            this.faceFrameReader.FrameArrived += Reader_FaceFrameArrived;

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

        public FaceFrameResult GetTrackedFaceData()
        {
            return this.faceTrackedData;
        }

        public IReadOnlyList<CameraSpacePoint> GetMesh()
        {
            return this.mesh;
        }

        public IReadOnlyList<uint> GetMeshIndices()
        {
            return this.meshIndices;
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
                this.faceFrameSource.TrackingId = selectedBody.TrackingId;

                this.highDefinitionFaceFrameSource.TrackingId = this.currentTrackingId;
            }
        }

        private void ColorToBitmap(ColorFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] pixels = new byte[width * height * ((PixelFormats.Bgr32.BitsPerPixel + 7) / 8)];

            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(pixels);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(pixels, ColorImageFormat.Bgra);
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.colorBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixels, stride, 0);
        }

        private void DepthToBitmap(DepthFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort minDepth = frame.DepthMinReliableDistance;
            ushort maxDepth = frame.DepthMaxReliableDistance;

            ushort[] depthData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(depthData);

            int colorIndex = 0;
            for (int depthIndex = 0; depthIndex < depthData.Length; ++depthIndex)
            {
                ushort depth = depthData[depthIndex];
                double interpolation = (double)(depth - minDepth) / (double)(maxDepth - minDepth);
                byte intensity = (byte)(depth >= minDepth && depth <= maxDepth ? interpolation * 255 : 0);
                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.depthBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }

        private void InfraredToBitmap(InfraredFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            ushort[] infraredData = new ushort[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(infraredData);

            int colorIndex = 0;
            for (int infraredIndex = 0; infraredIndex < infraredData.Length; ++infraredIndex)
            {
                ushort ir = infraredData[infraredIndex];
                byte intensity = (byte)(ir >> 8);

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green   
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.infraredBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }

        private void IndexToBitmap(BodyIndexFrame frame)
        {
            int width = frame.FrameDescription.Width;
            int height = frame.FrameDescription.Height;

            byte[] indexData = new byte[width * height];
            byte[] pixelData = new byte[width * height * (PixelFormats.Bgr32.BitsPerPixel + 7) / 8];

            frame.CopyFrameDataToArray(indexData);

            int colorIndex = 0;
            for (int i = 0; i < indexData.Length; ++i)
            {
                byte intensity = 0;
                if (indexData[i] < 6)
                {
                    intensity = 255;
                }

                pixelData[colorIndex++] = intensity; // Blue
                pixelData[colorIndex++] = intensity; // Green   
                pixelData[colorIndex++] = intensity; // Red

                ++colorIndex;
            }

            int stride = width * PixelFormats.Bgr32.BitsPerPixel / 8;

            this.indexBitmap.WritePixels(new Int32Rect(0, 0, width, height), pixelData, stride, 0);
        }

        private void Reader_FaceFrameArrived(object sender, FaceFrameArrivedEventArgs e)
        {
            using (FaceFrame faceFrame = e.FrameReference.AcquireFrame())
            {
                if (faceFrame != null)
                {
                    // check if this face frame has valid face frame results
                    var tracking = ValidateFaceBoxAndPoints(faceFrame.FaceFrameResult);
                    if (tracking)
                    {
                        this.faceTrackedData = faceFrame.FaceFrameResult;
                    }
                    else
                    {
                        this.faceTrackedData = null;
                    }
                }
            }
        }

        private void Reader_HDFaceFrameArrived(object sender, HighDefinitionFaceFrameArrivedEventArgs e)
        {
            using (var frame = e.FrameReference.AcquireFrame())
            {
                if (frame == null || !frame.IsFaceTracked)
                {
                    return;
                }

                frame.GetAndRefreshFaceAlignmentResult(this.faceAlignment);
                this.mesh = this.faceModel.CalculateVerticesForAlignment(this.faceAlignment);
                this.meshIndices = this.faceModel.TriangleIndices;
            }
        }

        private void HDFaceFrameSource_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            var lostTrackingID = e.TrackingId;

            if (this.currentTrackingId == lostTrackingID)
            {
                this.currentTrackingId = 0;
                this.currentTrackedBody = null;
                if (this.faceModelBuilder != null)
                {
                    this.faceModelBuilder.Dispose();
                    this.faceModelBuilder = null;
                }

                this.highDefinitionFaceFrameSource.TrackingId = 0;
            }
        }

        private void StartCapture()
        {
            this.StopFaceCapture();

            this.faceModelBuilder = null;

            this.faceModelBuilder = this.highDefinitionFaceFrameSource.OpenModelBuilder(FaceModelBuilderAttributes.None);

            this.faceModelBuilder.BeginFaceDataCollection();

            this.faceModelBuilder.CollectionCompleted += this.HdFaceBuilder_CollectionCompleted;
        }

        private void StopFaceCapture()
        {
            if (this.faceModelBuilder != null)
            {
                this.faceModelBuilder.Dispose();
                this.faceModelBuilder = null;
            }
        }

        private void HdFaceBuilder_CollectionCompleted(object sender, FaceModelBuilderCollectionCompletedEventArgs e)
        {
            var modelData = e.ModelData;

            this.faceModel = modelData.ProduceFaceModel();

            this.faceModelBuilder.Dispose();
            this.faceModelBuilder = null;
        }

        private bool ValidateFaceBoxAndPoints(FaceFrameResult faceResult)
        {
            bool isFaceValid = faceResult != null;

            if (isFaceValid)
            {
                var faceBox = faceResult.FaceBoundingBoxInColorSpace;
                if (faceBox != null)
                {
                    // check if we have a valid rectangle within the bounds of the screen space
                    isFaceValid = (faceBox.Right - faceBox.Left) > 0 &&
                                  (faceBox.Bottom - faceBox.Top) > 0 &&
                                  faceBox.Right <= this.displayRectColor.Width &&
                                  faceBox.Bottom <= this.displayRectColor.Height;

                    if (isFaceValid)
                    {
                        var facePoints = faceResult.FacePointsInColorSpace;
                        if (facePoints != null)
                        {
                            foreach (PointF pointF in facePoints.Values)
                            {
                                // check if we have a valid face point within the bounds of the screen space
                                bool isFacePointValid = pointF.X > 0.0f &&
                                                        pointF.Y > 0.0f &&
                                                        pointF.X < this.displayRectColor.Width &&
                                                        pointF.Y < this.displayRectColor.Height;

                                if (!isFacePointValid)
                                {
                                    isFaceValid = false;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return isFaceValid;
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
            if (this.faceFrameReader != null)
            {
                this.faceFrameReader.Dispose();
                this.faceFrameReader = null;
            }

            if (this.faceFrameSource != null)
            {
                this.faceFrameSource.Dispose();
                this.faceFrameSource = null;
            }

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
