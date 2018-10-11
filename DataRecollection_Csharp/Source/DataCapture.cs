using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Media.Imaging;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;
using Newtonsoft.Json;
using NAudio.Wave;

namespace DataRecollection.Source
{
    class DataCapture
    {
        WaveIn sourceStream;
        WaveFileWriter waveWriter;
        private static string BASE_DIR = @"CollectedData\";
        private string subject;
        private int currentSubject;

        public DataCapture()
        {
            currentSubject = 0;

            while (Directory.Exists(BASE_DIR + "sbj-" + currentSubject))
            {
                currentSubject += 1;
            }
        }

        public void NewSubject()
        {
            subject = "sbj-" + currentSubject;
            Directory.CreateDirectory(BASE_DIR + subject);
            currentSubject += 1;
        }

        public void CaptureImage(WriteableBitmap image, string imageType, int capture)
        {
            string filename = BASE_DIR + subject + "\\cpt_" + capture + "_" + imageType + ".png";
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }

        public void CaptureFaceMesh(IReadOnlyList<CameraSpacePoint> mesh, IReadOnlyList<uint> indices, int capture)
        {
            string filename = BASE_DIR + subject + "\\cpt_" + capture + "_FaceMesh.obj";
            using (StreamWriter outputFile = new StreamWriter(filename))
            {
                outputFile.WriteLine("# Vertices");
                foreach (var ver in mesh)
                {
                    String line = "v " + ver.X + " " + ver.Y + " " + ver.Z + " 1.0";
                    outputFile.WriteLine(line);
                }

                outputFile.WriteLine("# Indices");
                for (int i = 0; i < indices.Count; i += 3)
                {
                    uint index01 = indices[i];
                    uint index02 = indices[i + 1];
                    uint index03 = indices[i + 2];

                    String line = "f " + index01 + " " + index02 + " " + index03;
                    outputFile.WriteLine(line);
                }
            }
        }

        public void WriteCaptureInfo(FaceFrameResult result, int capture)
        {
            var faceData = new FaceData(result);
            string filename = BASE_DIR + subject + "\\cpt_" + capture + "_FaceData.json";
            using (StreamWriter outputFile = new StreamWriter(filename))
            {
                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(outputFile, faceData);
            }
        }

        public void StartAudioRecording(string audioType)
        {
            sourceStream = new WaveIn
            {
                DeviceNumber = 0,
                WaveFormat =
                    new WaveFormat(44100, WaveIn.GetCapabilities(0).Channels)
            };

            sourceStream.DataAvailable += this.AudioStreamDataAvailable;

            string filename = BASE_DIR + subject + "\\_" + audioType + ".wav";

            waveWriter = new WaveFileWriter(filename, sourceStream.WaveFormat);
            sourceStream.StartRecording();
        }

        public void EndAudioRecording()
        {
            if (sourceStream != null)
            {
                sourceStream.StopRecording();
                sourceStream.Dispose();
                sourceStream = null;
            }
            if (this.waveWriter == null)
            {
                return;
            }
            this.waveWriter.Dispose();
            this.waveWriter = null;
        }

        public void Incomplete(string stage)
        {
            using (StreamWriter outputFile = new StreamWriter("INCOMPLETE.txt"))
            {
                outputFile.WriteLine("This capture was stopped at " + stage);
            }
        }

        private void AudioStreamDataAvailable(object sender, WaveInEventArgs e)
        {
            if (waveWriter == null) return;
            waveWriter.Write(e.Buffer, 0, e.BytesRecorded);
            waveWriter.Flush();
        }
    }
}
