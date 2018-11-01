using UnityEngine;
using System.IO;

public class DataCapture
{
    private readonly string BASE_DIR = Path.Combine(Application.dataPath, "CollectedData");
    private int currentSubject = 0;
    private string subject;

    public DataCapture()
    {
        while (Directory.Exists(Path.Combine(BASE_DIR + "sbj-" + currentSubject)))
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

    public void CaptureImage(Texture2D image, string imageType, int capture)
    {
        byte[] imgData = image.EncodeToPNG();
        string filename = Path.Combine(BASE_DIR, subject, "cpt_" + capture + "_" + imageType + "_i.png");
        File.WriteAllBytes(filename, imgData);
    }

    public void CaptureData(ushort[] data, string dataType, int capture, int width, int height)
    {
        string filename = Path.Combine(BASE_DIR, subject, "cpt_" + capture + "_" + dataType + "_d.dat");
        using (FileStream fs = File.Create(filename))
        {
            using (StreamWriter bw = new StreamWriter(fs))
            {
                for (int i = 0; i<height; i++)
                {
                    for (int j = 0; j<width; j++)
                    {
                        ushort value = data[i * j];
                        bw.Write(value.ToString());
                        bw.Write('\t');
                    }
                    bw.Write("\r\n");
                }
            }
        }
    }
}
