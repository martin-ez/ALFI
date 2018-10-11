using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using System.Collections.Generic;

namespace DataRecollection.Source
{
    class FaceData
    {
        public IReadOnlyDictionary<FacePointType, PointF> PointsInfraredSpace;
        public IReadOnlyDictionary<FacePointType, PointF> PointsColorSpace;
        public RectI BoundingBoxInfraredSpace;
        public RectI BoundingBoxColorSpace;
        public Vector4 RotationQuaternion;
        public IDictionary<string, string> Properties = new Dictionary<string, string>();

        public FaceData(FaceFrameResult data)
        {
            PointsInfraredSpace = data.FacePointsInInfraredSpace;
            PointsColorSpace = data.FacePointsInColorSpace;
            BoundingBoxInfraredSpace = data.FaceBoundingBoxInInfraredSpace;
            BoundingBoxColorSpace = data.FaceBoundingBoxInColorSpace;
            RotationQuaternion = data.FaceRotationQuaternion;

            var properties = data.FaceProperties;
            foreach (var item in properties)
            {
                string key = item.Key.ToString();

                string value = "";
                // consider a "maybe" as a "no" to restrict 
                // the detection result refresh rate
                if (item.Value == DetectionResult.Maybe)
                {
                    value = DetectionResult.No.ToString();
                }
                else
                {
                    value = item.Value.ToString();
                }
                Properties.Add(key, value);
            }
        }
    }
}
