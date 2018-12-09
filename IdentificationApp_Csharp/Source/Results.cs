using System;
using System.IO;
using System.Text;

namespace IdentificationApp.Source
{
    class Results
    {
        private static readonly string filePath = Path.Combine(Path.GetPathRoot(Environment.CurrentDirectory), "ALFI_Data", "Results.txt");

        private int truePositive = 0;
        private int trueNegative = 0;
        private int falsePositive = 0;
        private int falseNegative = 0;

        public Results()
        {
            const Int32 BufferSize = 128;
            using (var fileStream = File.OpenRead(filePath))
            {
                using (var reader = new StreamReader(fileStream, Encoding.UTF8, true, BufferSize))
                {
                    String line = reader.ReadLine();
                    truePositive = int.Parse(line.Split(':')[1]);
                    line = reader.ReadLine();
                    trueNegative = int.Parse(line.Split(':')[1]);
                    line = reader.ReadLine();
                    falsePositive = int.Parse(line.Split(':')[1]);
                    line = reader.ReadLine();
                    falseNegative = int.Parse(line.Split(':')[1]);
                }
            }
        }

        public void WriteFile()
        {
            using (var writer = new StreamWriter(filePath))
            {
                writer.WriteLine("truePositive:" + truePositive);
                writer.WriteLine("trueNegative:" + trueNegative);
                writer.WriteLine("falsePositive:" + falsePositive);
                writer.WriteLine("falseNegative:" + falseNegative);
            }
        }

        public void Mark(bool positive, bool correct)
        {
            if (positive)
            {
                if (correct)
                {
                    truePositive += 1;
                }
                else
                {
                    falsePositive += 1;
                }
            }
            else
            {
                if (correct)
                {
                    trueNegative += 1;
                }
                else
                {
                    falseNegative += 1;
                }
            }
        }
    }
}
