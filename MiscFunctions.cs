using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ImageMosaicGenerator
{
    class MiscFunctions
    {
        //This function is bad and a total mess but it works
        public static int[] HighestFactorsOfNumber(int number, int[] imgSize)
        {
            double factorCheck;
            int counter = 0;
            for (int i = 1; i <= number; i++)
            {
                factorCheck = (double)number / i;
                if (factorCheck == Math.Floor(factorCheck))
                {
                    counter++;
                }
            }

            int[,] outputArrayTest = new int[counter, 2];
            counter = 0;
            for (int i = 1; i <= number; i++)
            {
                factorCheck = (double)number / i;
                if (factorCheck == Convert.ToInt32(factorCheck))
                {
                    outputArrayTest[counter, 0] = Convert.ToInt32(factorCheck);
                    outputArrayTest[counter, 1] = i;
                    counter++;
                }
            }
            if (imgSize[0] >= imgSize[1])
            {
                return new int[] { outputArrayTest[Convert.ToInt32(Math.Ceiling((double)outputArrayTest.GetLength(0) / 2)) - 1, 0], outputArrayTest[Convert.ToInt32(Math.Ceiling((double)outputArrayTest.GetLength(0) / 2)) - 1, 1] };
            }
            else
            {
                return new int[] { outputArrayTest[Convert.ToInt32(Math.Ceiling((double)outputArrayTest.GetLength(0) / 2)) - 1, 1], outputArrayTest[Convert.ToInt32(Math.Ceiling((double)outputArrayTest.GetLength(0) / 2)) - 1, 0] };
            }

        }

        public static int IntArrayTotal(int[] inputArray)
        {
            int outputInt = 0;
            for (int i = 0; i < inputArray.Length; i++)
            {
                outputInt += inputArray[i];
            }
            return outputInt;
        }

        public static float[] RGBtoCIELAB(Color colorToConvert)
        {
            float[] inputColor = new float[] { colorToConvert.R / 255f, colorToConvert.G / 255f, colorToConvert.B / 255f };

            for (int i = 0; i < inputColor.Length; i++)
            {
                if (inputColor[i] > 0.04045f)
                {
                    inputColor[i] = (float)Math.Pow(((inputColor[i] + 0.055f) / 1.055f), 2.4f);
                }
                else
                {
                    inputColor[i] = inputColor[i] / 12.92f;
                }
                inputColor[i] *= 100f;
            }

            float X = inputColor[0] * 0.4124f + inputColor[1] * 0.3576f + inputColor[2] * 0.1805f;
            float Y = inputColor[0] * 0.2126f + inputColor[1] * 0.7152f + inputColor[2] * 0.0722f;
            float Z = inputColor[0] * 0.0193f + inputColor[1] * 0.1192f + inputColor[2] * 0.9505f;

            float[] XYZ = new float[] { (float)Math.Round(X, 4), (float)Math.Round(Y, 4), (float)Math.Round(Z, 4) };

            XYZ = new float[] { XYZ[0] / 95.047f, XYZ[1] / 100.0f, XYZ[2] / 108.883f };

            for (int i = 0; i < XYZ.Length; i++)
            {
                if (XYZ[i] > 0.008856f)
                {
                    XYZ[i] = (float)Math.Pow(XYZ[i], 1f / 3f);
                }
                else
                {
                    XYZ[i] = (0.787f * XYZ[i]) + (16f / 116f);
                }
            }
            return new float[] { (float)Math.Round((116f * XYZ[1]) - 16f, 4), (float)Math.Round(500f * (XYZ[0] - XYZ[1]), 4) + 128f, (float)Math.Round(200f * (XYZ[1] - XYZ[2]), 4) + 128f };
        }

        public static float[][] ConvertRGBArrayToCIELAB(Color[] inputArray)
        {
            float[][] finalArray = new float[inputArray.Length][];
            for (int i = 0; i < inputArray.Length; i++)
            {
                float[] convertedColor = RGBtoCIELAB(inputArray[i]);
                finalArray[i] = new float[3];
                finalArray[i][0] = convertedColor[0];
                finalArray[i][1] = convertedColor[1];
                finalArray[i][2] = convertedColor[2];
            }
            return finalArray;
        }

        public static int MaxDistanceCheck(int int1, int int2, int maxDist)
        {
            if (int1 > int2)
            {
                if (int2 <= 0)
                {
                    return 0;
                }
                else
                {
                    return int2;
                }
            }
            else
            {
                if (int2 >= maxDist)
                {
                    return maxDist;
                }
                else
                {
                    return int2;
                }
            }
        }

        public static Bitmap[][] Define2DArrayBitmap(int width, int height)
        {
            Bitmap[][] outputArray = new Bitmap[width][];
            for (int i = 0; i < width; i++)
            {
                outputArray[i] = new Bitmap[height];
            }
            return outputArray;
        }

        public static Color[][] Define2DArrayColor(int width, int height)
        {
            Color[][] outputArray = new Color[width][];
            for (int i = 0; i < width; i++)
            {
                outputArray[i] = new Color[height];
            }
            return outputArray;
        }

        public static string[][] Define2DArrayString(int width, int height)
        {
            string[][] outputArray = new string[width][];
            for (int i = 0; i < width; i++)
            {
                outputArray[i] = new string[height];
            }
            return outputArray;
        }

        public static float[][] Define2DArrayFloat(int width, int height)
        {
            float[][] outputArray = new float[width][];
            for (int i = 0; i < width; i++)
            {
                outputArray[i] = new float[height];
            }
            return outputArray;
        }

        public static int[][] Define2DArrayInt(int width, int height)
        {
            int[][] outputArray = new int[width][];
            for (int i = 0; i < width; i++)
            {
                outputArray[i] = new int[height];
            }
            return outputArray;
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }

        public static byte[] Zip(string str)
        {
            var bytes = Encoding.UTF8.GetBytes(str);

            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(mso, CompressionMode.Compress))
                {
                    //msi.CopyTo(gs);
                    CopyTo(msi, gs);
                }

                return mso.ToArray();
            }
        }

        public static string Unzip(byte[] bytes)
        {
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new GZipStream(msi, CompressionMode.Decompress))
                {
                    //gs.CopyTo(mso);
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static string[] SortStringArrayByNumberArray(string[] stringArray, double[] intArray)
        {
            int n = intArray.Length;

            for (int gap = n / 2; gap > 0; gap /= 2)
            {
                for (int i = gap; i < n; i += 1)
                {
                    double temp = intArray[i];
                    string stringTemp = stringArray[i];

                    int j;
                    for (j = i; j >= gap && intArray[j - gap] > temp; j -= gap)
                    {
                        intArray[j] = intArray[j - gap];
                        stringArray[j] = stringArray[j - gap];
                    }

                    intArray[j] = temp;
                    stringArray[j] = stringTemp;
                }
            }
            return stringArray;
        }
    }
}
