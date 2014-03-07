using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlitPhoto
{
    internal class Program
    {
        [STAThread]
        private static void Main()
        {
            // begin settings block

            const bool isVertical = true;

            var lineNumbers = new List<int> {};
            lineNumbers.AddRange(Enumerable.Range(5, 2));

            var upShiftsList = new List<Tuple<int, int>>
                                   {
                                       new Tuple<int, int>(140, 240),
                                       new Tuple<int, int>(400, 440)
                                   };
            var downShiftsList = new List<Tuple<int, int>>
                                     {
                                         new Tuple<int, int>(510, 635)
                                     };

            // end settings block

            var openFileDialog = new OpenFileDialog
                                     {
                                         Filter = "Image files|*.png",
                                         Title = "Select an Image",
                                         Multiselect = true
                                     };

            string[] fileNames;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                fileNames = openFileDialog.FileNames;
            }
            else
            {
                return;
            }

            PostMessage("Programm will cutt {0} lines to create a slit photo", isVertical ? "vertical" : "horizontal");

            int weigth;
            int height;

            using (var bitmap = new Bitmap(fileNames[0]))
            {
                weigth = bitmap.Width;
                height = bitmap.Height;
            }

            PostMessage("Selected images will be converted from {0}x{1} to {2}x{3} one image",
                        weigth,
                        height,
                        isVertical ? fileNames.Length : weigth,
                        isVertical ? height : fileNames.Length);

            var shifts = new int[fileNames.Length];
            shifts[0] = 0;

            for (int x = 1; x < fileNames.Length; x++)
            {
                if (upShiftsList.Exists(item => x > item.Item1 && x < item.Item2))
                {
                    shifts[x] = shifts[x - 1] + 1;
                }
                else if (downShiftsList.Exists(item => x > item.Item1 && x < item.Item2))
                {
                    shifts[x] = shifts[x - 1] - 1;
                }
                else
                {
                    shifts[x] = shifts[x - 1];
                }
            }

            PostMessage("Number Of Logical Processors: {0}", Environment.ProcessorCount);

            foreach (var lineNumber in lineNumbers)
            {
                using (var resultBitmap = new Bitmap(isVertical ? fileNames.Length : weigth, isVertical ? height : fileNames.Length))
                {
                    var actions = new Action[Environment.ProcessorCount];

                    for (int i = 0; i < Environment.ProcessorCount; i++)
                    {
                        int localI = i;
                        int localLineNumber = lineNumber;
                        actions[i] = () => ProcessLines(localI, Environment.ProcessorCount, fileNames, shifts, resultBitmap, localLineNumber, isVertical);
                    }

                    PostMessage("Starting with line number: {0}...", lineNumber);

                    Parallel.Invoke(actions);

                    resultBitmap.Save(string.Format("Result_{0}.png", lineNumber));
                }
            }

            PostMessage("Done!");
        }

        private static void ProcessLines(int start, int n, string[] fileNames, int[] shifts, Bitmap resultBitmap, int lineNumber, bool isVertical)
        {
            for (int fileNum = start; fileNum < fileNames.Length; fileNum += n)
            {
                ProcessLine(fileNum, fileNames[fileNum], resultBitmap, shifts[fileNum], lineNumber, isVertical);
            }
        }

        private static void ProcessLine(int fileNum, string filename, Bitmap resultBitmap, int shift, int lineNumber, bool isVertical)
        {
            using (var bitmap = new Bitmap(filename))
            {
                for (int i = 0; i < (isVertical ? bitmap.Height : bitmap.Width); i++)
                {
                    var color = isVertical ? bitmap.GetPixel(lineNumber, i) : bitmap.GetPixel(i, lineNumber);
                    lock (resultBitmap)
                    {
                        resultBitmap.SetPixel(isVertical ? fileNum : (bitmap.Width + i + shift)%bitmap.Width,
                                              isVertical ? (bitmap.Height + i - shift)%bitmap.Height : fileNum,
                                              color);
                    }
                }
            }
        }

        private static void PostMessage(string format, params object[] args)
        {
            Console.WriteLine(string.Format("{0}: ", DateTime.Now) + string.Format(format, args));
        }

    }
}