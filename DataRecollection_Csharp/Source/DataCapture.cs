using System.IO;
using System.Windows.Media.Imaging;
using NAudio.Wave;

namespace DataRecollection.Source
{
    class DataCapture
    {
        WaveIn sourceStream;
        WaveFileWriter waveWriter;
        private static string BASE_DIR = @"CollectedData";
        private string subject;
        private int currentSubject;

        public DataCapture()
        {
            currentSubject = 0;

            while (Directory.Exists(Path.Combine(BASE_DIR, "sbj-" + currentSubject)))
            {
                currentSubject += 1;
            }
        }

        public void NewSubject()
        {
            subject = "sbj-" + currentSubject;
            Directory.CreateDirectory(Path.Combine(BASE_DIR, subject));
            currentSubject += 1;
        }

        public void CaptureImage(WriteableBitmap image, string imageType, int capture)
        {
            string filename = Path.Combine(BASE_DIR, subject, "cpt_" + capture + "_" + imageType + "_i.png");
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }

        public void CaptureData(ushort[] data, string dataType, int capture, int width, int height)
        {
            string filename = Path.Combine(BASE_DIR, subject, "cpt_" + capture + "_" + dataType + "_d.dat");
            using (FileStream fs = File.Create(filename))
            {
                using (StreamWriter bw = new StreamWriter(fs))
                {
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            ushort value = data[(i*width) + j];
                            bw.Write(value.ToString());
                            bw.Write('\t');
                        }
                        bw.Write("\r\n");
                    }
                }
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

            string filename = Path.Combine(BASE_DIR, subject, "_" + audioType + ".wav");

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
