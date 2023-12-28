using System.Drawing;
using System.Linq;
using System.Threading;

namespace ImageMosaicGenerator
{
    class ImageMosaicStorage
    {
        public readonly int imgWidth, imgHeight, imageWidth, imageHeight;
        public readonly Bitmap templateImage;
        public string[] imageDirectories;
        public Color[] averageColorsArray;
        public float[][] averageColorsArrayClab;
        public readonly Color[][] templatePixelColors;
        public readonly string[][] imagePaths;
        public readonly int[] threadFactorsPublic;
        public Thread[] activeThreads;
        public readonly Bitmap[][] finalImagePart;
        public readonly int[][] finalImagePartSizesWidth;
        public readonly int[][] finalImagePartSizesHeight;
        public readonly Bitmap originalImage;
        public Bitmap[] usedImages;
        public string[] usedImagesPaths;
        //public List<int> brokenImagesArray = new List<int>();

        public ImageMosaicStorage(Bitmap image, string[] directories)
        {
            originalImage = image;
            if (image.Width == image.Height)
            {
                imgWidth = Globals.ImageSize;
                imgHeight = Globals.ImageSize;
            }
            else if (image.Width < image.Height)
            {
                imgWidth = Globals.ImageSize * image.Width / image.Height;
                imgHeight = Globals.ImageSize;
            }
            else
            {
                imgWidth = Globals.ImageSize;
                imgHeight = Globals.ImageSize * image.Height / image.Width;
            }

            imageDirectories = directories;
            templateImage = ImageProcessing.ResizeImage(image, imgWidth, imgHeight, 1);
            templateImage.Save("TemplateImg.png");
            averageColorsArray = new Color[directories.Length];
            averageColorsArrayClab = MiscFunctions.Define2DArrayFloat(directories.Length, 3);
            templatePixelColors = MiscFunctions.Define2DArrayColor(imgWidth, imgHeight);
            imagePaths = MiscFunctions.Define2DArrayString(imgWidth, imgHeight);
            imageWidth = imgWidth * Globals.SourceImgResolution;
            imageHeight = imgHeight * Globals.SourceImgResolution;

            int[] threadFactorsLocal = MiscFunctions.HighestFactorsOfNumber(Globals.ThreadCount, new int[] { imgWidth, imgHeight });
            threadFactorsPublic = threadFactorsLocal;
            finalImagePart = MiscFunctions.Define2DArrayBitmap(threadFactorsLocal[0], threadFactorsLocal[1]);
            finalImagePartSizesWidth = MiscFunctions.Define2DArrayInt(threadFactorsLocal[0], threadFactorsLocal[1]);
            finalImagePartSizesHeight = MiscFunctions.Define2DArrayInt(threadFactorsLocal[0], threadFactorsLocal[1]);

            for (int y = 0; y < finalImagePartSizesHeight[0].Length; y++)
            {
                for (int x = 0; x < finalImagePartSizesWidth.Length; x++)
                {
                    if (x == threadFactorsLocal[0] - 1)
                    {
                        finalImagePartSizesWidth[x][y] = imgWidth / threadFactorsLocal[0] + (imgWidth - (imgWidth / threadFactorsLocal[0] * threadFactorsLocal[0]));
                    }
                    else
                    {
                        finalImagePartSizesWidth[x][y] = imgWidth / threadFactorsLocal[0];
                    }

                    if (y == threadFactorsLocal[1] - 1)
                    {
                        finalImagePartSizesHeight[x][y] = imgHeight / threadFactorsLocal[1] + (imgHeight - (imgHeight / threadFactorsLocal[1] * threadFactorsLocal[1]));
                    }
                    else
                    {
                        finalImagePartSizesHeight[x][y] = imgHeight / threadFactorsLocal[1];
                    }
                }
            }
        }

        public void ClearNullImages()
        {
            var colorList = averageColorsArray.ToList();
            var directoryList = imageDirectories.ToList();
            for (int i = imageDirectories.Length - 1; i >= 0; i--)
            {
                if (imageDirectories[i] == "")
                {
                    colorList.RemoveAt(i);
                    directoryList.RemoveAt(i);
                }
            }
            averageColorsArray = colorList.ToArray();
            imageDirectories = directoryList.ToArray();
        }

        public void ThreadFinished()
        {

        }
    }
}
