using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;

namespace ImageMosaicGenerator
{
    class ImageProcessing
    {

        public static string[] GetSimilarColorImage(Color mainColor, float[][] colorChoices, string[] imageDirectories)
        {
            double[] colorSimilarities = new double[colorChoices.Length];
            float[] mainColorCIALAB = MiscFunctions.RGBtoCIELAB(mainColor);
            for (int i = 0; i < colorChoices.Length; i++)
            {
                //original
                //colorSimilarities[i] = Math.Abs(mainColorCIALAB[0] - colorChoices[i][0]) + Math.Abs(mainColorCIALAB[1] - colorChoices[i][1]) + Math.Abs(mainColorCIALAB[2] - colorChoices[i][2]);

                //new 1
                //colorSimilarities[i] = (Math.Abs(mainColorCIALAB[0] - colorChoices[i][0]) * 2) + Math.Abs(mainColorCIALAB[1] - colorChoices[i][1]) + Math.Abs(mainColorCIALAB[2] - colorChoices[i][2]);

                //new 2
                //colorSimilarities[i] = (Math.Abs(mainColorCIALAB[0] - colorChoices[i][0]) / 2) + Math.Abs(mainColorCIALAB[1] - colorChoices[i][1]) + Math.Abs(mainColorCIALAB[2] - colorChoices[i][2]);

                //new 3 Looks the best
                colorSimilarities[i] = Math.Sqrt(Math.Pow(colorChoices[i][0] - mainColorCIALAB[0], 2) + Math.Pow(colorChoices[i][1] - mainColorCIALAB[1], 2) + Math.Pow(colorChoices[i][2] - mainColorCIALAB[2], 2));
            }
            MiscFunctions.SortStringArrayByNumberArray(imageDirectories, colorSimilarities);
            return imageDirectories;
            //return sortedColorArray[0];
        }

        public static string PreventImageRepetition(string[][] ImageArray, string[] SortedColorArray, int[] pos)
        {
            /*
            int imgRepOffset = Globals.ImageRepetitionDistance + 1;
            int[] posInChunk = new int[] {pos[0] % imgRepOffset, pos[1] % imgRepOffset};
            return SortedColorArray[posInChunk[0] * imgRepOffset + posInChunk[1]];
            */
            if (Globals.PreventImageRepetition)
            {
                int[] maxX = { MiscFunctions.MaxDistanceCheck(pos[0], pos[0] - Globals.ImageRepetitionDistance, ImageArray.Length), MiscFunctions.MaxDistanceCheck(pos[0], pos[0] + Globals.ImageRepetitionDistance, ImageArray.Length - 1) };
                int[] maxY = { MiscFunctions.MaxDistanceCheck(pos[1], pos[1] - Globals.ImageRepetitionDistance, ImageArray[0].Length), MiscFunctions.MaxDistanceCheck(pos[1], pos[1] + Globals.ImageRepetitionDistance, ImageArray[0].Length - 1) };
                bool doLoop;
                int counter = 0;

                for (int y = maxY[0]; y <= maxY[1]; y++)
                {
                    for (int x = maxX[0]; x <= maxX[1]; x++)
                    {
                        if (!String.IsNullOrEmpty(ImageArray[x][y]))
                        {

                            if (SortedColorArray[counter] == "")
                            {
                                int test = 0;
                            }
                            if (ImageArray[x][y] == SortedColorArray[counter])
                            {
                                counter++;
                                y = maxY[0];
                                x = maxX[0];
                            }
                        }
                        //}
                    }
                }

                return SortedColorArray[counter];
            }
            else
            {
                return SortedColorArray[0];
            }
        }

        public static Color GetAverageBetmapColor(Bitmap beatmapIn)
        {
            BitmapData srcData = beatmapIn.LockBits(new Rectangle(0, 0, beatmapIn.Width, beatmapIn.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            int stride = srcData.Stride;

            IntPtr Scan0 = srcData.Scan0;

            long[] totals = new long[] { 0, 0, 0 };

            int width = beatmapIn.Width;
            int height = beatmapIn.Height;

            unsafe
            {
                byte* p = (byte*)(void*)Scan0;

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int color = 0; color < 3; color++)
                        {
                            int idx = (y * stride) + x * 4 + color;

                            totals[color] += p[idx];
                        }
                    }
                }
            }

            long avgB = totals[0] / (width * height);
            long avgG = totals[1] / (width * height);
            long avgR = totals[2] / (width * height);
            return Color.FromArgb((int)avgR, (int)avgG, (int)avgB);
        }

        public static Bitmap SquareImage(Bitmap image)
        {
            image.SetResolution(96, 96);
            Rectangle destRect;

            Bitmap destImage;

            if (image.Width == image.Height)
            {
                return image;
            }
            else if (image.Width < image.Height)
            {
                destRect = new Rectangle(0, 0 - (image.Height - image.Width) / 2, image.Width, image.Height);
                destImage = new Bitmap(image.Width, image.Width);
            }
            else // image.Width > image.Height
            {
                destRect = new Rectangle(0 - (image.Width - image.Height) / 2, 0, image.Width, image.Height);
                destImage = new Bitmap(image.Height, image.Height);
            }

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.DrawImage(image, destRect);
            }

            return destImage;
        }

        public static Image SetImageOpacity(Image image, float opacity)
        {
            Image bmp = new Bitmap(image.Width, image.Height, PixelFormat.Format32bppArgb);
            using (Graphics gfx = Graphics.FromImage(bmp))
            {
                ColorMatrix matrix = new ColorMatrix();
                matrix.Matrix33 = opacity;
                ImageAttributes attributes = new ImageAttributes();
                attributes.SetColorMatrix(matrix, ColorMatrixFlag.Default, ColorAdjustType.Default);
                gfx.DrawImage(image, new Rectangle(0, 0, bmp.Width, bmp.Height), 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, attributes);
            }
            return bmp;
        }

        public static Image Overlap(Image source1, Image source2)
        {
            Bitmap target = new Bitmap(source1.Width, source1.Height, PixelFormat.Format32bppArgb);
            using (Graphics graphics = Graphics.FromImage(target))
            {
                graphics.CompositingMode = CompositingMode.SourceOver; // this is the default, but just to be clear
                graphics.DrawImage(source1, 0, 0);
                graphics.DrawImage(source2, 0, 0);

            }
            return target;
        }

        public static Bitmap ResizeImage(Bitmap image, int width, int height, int mode = 0)
        {
            //Define new bitmap and rectangle
            var destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);
            using (var graphics = Graphics.FromImage(destImage))
            {
                //Set resizing settings/quality
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                if (mode == 0)
                {
                    graphics.InterpolationMode = InterpolationMode.Bicubic;
                }
                else if (mode == 1)
                {
                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic; //HighQualityBicubic is much slower
                }
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                //using the attributes as a variable
                using (var wrapMode = new ImageAttributes())
                {
                    //Set the wraping mode to fip x y
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);

                    //Draw the image causing it to get resized
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }
            return destImage;
        }

        public static void LoadProcessedImages(ImageMosaicStorage imgStorage)
        {
            string fileContents = MiscFunctions.Unzip(File.ReadAllBytes("ProcessedImages.bin"));
            File.WriteAllText("test.txt", fileContents);
            string[] fileContentsLineArray = fileContents.Split('\n');
            string[] singleLineArray;
            List<string> directoriesList = new List<string>();
            List<Color> colorList = new List<Color>();
            for (int i = 0; i < fileContentsLineArray.Length; i++)
            {
                singleLineArray = fileContentsLineArray[i].Split(new string[] { "::" }, StringSplitOptions.None);
                if (singleLineArray[0].Replace("\r", "") != "")
                {
                    directoriesList.Add(singleLineArray[0]);
                    int[] colorArray = singleLineArray[1].Split(',').Select(Int32.Parse).ToList().ToArray();
                    colorList.Add(Color.FromArgb(255, colorArray[0], colorArray[1], colorArray[2]));
                }
            }
            imgStorage.imageDirectories = directoriesList.ToArray();
            imgStorage.averageColorsArray = colorList.ToArray();
        }

        public static void PreProcessImages(ImageMosaicStorage imgStorage)
        {
            //Save settings (img size and location)
            //Temprorary maybe 
            string[] saveImageProcessing = new string[imgStorage.averageColorsArray.Length];
            for (int i = 0; i < imgStorage.averageColorsArray.Length; i++)
            {
                saveImageProcessing[i] = imgStorage.imageDirectories[i] + "::" + imgStorage.averageColorsArray[i].R + "," + imgStorage.averageColorsArray[i].G + "," + imgStorage.averageColorsArray[i].B;
            }
            string output = string.Join("\n", saveImageProcessing);
            File.WriteAllBytes("ProcessedImages.bin", MiscFunctions.Zip(output));
        }

    }
}
