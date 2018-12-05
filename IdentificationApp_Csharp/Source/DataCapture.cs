using System;
using System.IO;
using System.Windows.Media.Imaging;

namespace IdentificationApp.Source
{
    class DataCapture
    {
        private static string BASE_DIR_Identify = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "ALFI_Img_Repo", "ToIdentify");
        private static string BASE_DIR_Registry = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "ALFI_Img_Repo", "Registry");
        private static string BASE_DIR_Process = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "ALFI_Img_Repo", "ToProcess");
        private string subjectIdentify;
        private string subjectRegistry;
        private int currentSubjectRegistry;
        private int currentSubjectIdentify;

        public DataCapture()
        {
            currentSubjectIdentify = 0;

            while (Directory.Exists(Path.Combine(BASE_DIR_Identify, "sbj-" + currentSubjectIdentify)))
            {
                currentSubjectIdentify += 1;
            }
            subjectIdentify = "sbj-" + currentSubjectIdentify;

            currentSubjectRegistry = 0;

            while (Directory.Exists(Path.Combine(BASE_DIR_Process, "sbj-" + currentSubjectRegistry)))
            {
                currentSubjectRegistry += 1;
            }
            subjectRegistry = "sbj-" + currentSubjectRegistry;
        }

        public int NewSubjectIdentify()
        {
            int sbj = currentSubjectIdentify;
            subjectIdentify = "sbj-" + sbj;
            Directory.CreateDirectory(Path.Combine(BASE_DIR_Identify, subjectIdentify));
            currentSubjectIdentify += 1;
            return sbj;
        }

        public int NewSubjectRegistry()
        {
            int sbj = currentSubjectRegistry;
            subjectRegistry = "sbj-" + sbj;
            Directory.CreateDirectory(Path.Combine(BASE_DIR_Process, subjectRegistry));
            currentSubjectRegistry += 1;
            return sbj;
        }

        public void CaptureImage(WriteableBitmap image, string imageType, int capture, bool identify)
        {
            string filename = Path.Combine(BASE_DIR_Process, subjectRegistry, "cpt_" + capture + "_" + imageType + "_i.png");
            if (identify)
            {
                filename = Path.Combine(BASE_DIR_Identify, subjectIdentify, "cpt_" + capture + "_" + imageType + "_i.png");
            }
            using (FileStream stream = new FileStream(filename, FileMode.Create))
            {
                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                encoder.Save(stream);
            }
        }

        public void CaptureData(ushort[] data, string dataType, int capture, int width, int height, bool identify)
        {
            string filename = Path.Combine(BASE_DIR_Process, subjectRegistry, "cpt_" + capture + "_" + dataType + "_d.dat");
            if (identify)
            {
                filename = Path.Combine(BASE_DIR_Identify, subjectIdentify, "cpt_" + capture + "_" + dataType + "_d.dat");
            }
            using (FileStream fs = File.Create(filename))
            {
                using (StreamWriter bw = new StreamWriter(fs))
                {
                    for (int i = 0; i < height; i++)
                    {
                        for (int j = 0; j < width; j++)
                        {
                            ushort value = data[(i * width) + j];
                            bw.Write(value.ToString());
                            bw.Write('\t');
                        }
                        bw.Write("\r\n");
                    }
                }
            }
        }

        public BitmapImage GetIdentityBitmap(int subject, int capture)
        {
            return new BitmapImage(new Uri(Path.Combine(BASE_DIR_Registry, "sbj-" + subject, "cpt_" + capture + "_color.png")));
        }
    }
}
