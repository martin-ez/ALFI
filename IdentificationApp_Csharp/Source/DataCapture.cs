using System.IO;
using System.Windows.Media.Imaging;

namespace IdentificationApp.Source
{
    class DataCapture
    {
        private static string BASE_DIR = @"Img_Repo";
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
    }
}
