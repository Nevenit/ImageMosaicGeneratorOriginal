using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;

namespace ImageMosaicGenerator
{
    public static class Globals
    {
        public const int ImageSize = 200; //512:55 / 905:32  / 450:64 / 225:128 max
        public const int SourceImgResolution = 100;
        public const float SourceImageColouring = 0.25f;
        public const float OriginalImageOverlay = 0.25f;
        public const bool PreventImageRepetition = true;
        public const int ImageRepetitionDistance = 3;
        public const int ThreadCount = 12;
        public const bool LoadPreProcessedImages = false;
        public const bool PreProcessImages = false;
        public const bool SingleThreadedPixelProcessing = true;
        //public const int memoryCacheMinImgCount = 5;
    }

    class Program
    {
        [DllImport("user32.dll")]
        public static extern int DeleteMenu(IntPtr hMenu, int nPosition, int wFlags);
        [DllImport("user32.dll")]
        private static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();

        static void Main(string[] args)
        {
            IntPtr handle = GetConsoleWindow();
            IntPtr sysMenu = GetSystemMenu(handle, false);
            if (handle != IntPtr.Zero)
            {
                DeleteMenu(sysMenu, 0xF030, 0x00000000);
                DeleteMenu(sysMenu, 0xF000, 0x00000000);
            }

            Console.CursorVisible = false;

            var ext = new List<string> { ".jpg", ".png" };
            string[] directories = Directory.GetFiles("C:\\Users\\Nevenit\\Pictures\\rips", "*.*", SearchOption.AllDirectories).Where(s => ext.Contains(Path.GetExtension(s))).ToArray();
            

            using (Bitmap img = new Bitmap("image.jpg"))
            {
                ImageMosaicStorage imgStorage = new ImageMosaicStorage(img, directories);
                GenerateFinalImage(imgStorage, "output2.bmp");
            }

            Console.WriteLine("finished");
            Console.ReadKey();
        }

        public static void GenerateFinalImage(ImageMosaicStorage imgStorage, string finalFileName)
        {
            bool waitForThrads;
            Stopwatch stopwatch = Stopwatch.StartNew();

            ConsoleUpdater consoleUpdater;
            int[] maxProgressArray;

            //If loading preprocessed images then they dont need to be added to progress counter
            if (Globals.LoadPreProcessedImages)
            {
                maxProgressArray = new int[] { imgStorage.imgHeight * imgStorage.imgWidth, imgStorage.imgHeight * imgStorage.imgWidth };
            }
            else
            {
                maxProgressArray = new int[] { imgStorage.imgHeight * imgStorage.imgWidth, imgStorage.imgHeight * imgStorage.imgWidth, imgStorage.imageDirectories.Length };
            }

            consoleUpdater = new ConsoleUpdater(10, MiscFunctions.IntArrayTotal(maxProgressArray));

            imgStorage.activeThreads = new Thread[Globals.ThreadCount];

            //If image is too big this will throw an exeption
            try
            {
                Image test = new Bitmap(imgStorage.imageWidth, imgStorage.imageHeight, PixelFormat.Format32bppArgb);
            }
            catch
            {
                Console.WriteLine("Image size too big. Either maker the final image smaller or the separate images smaller.");
                return;
            }

            //If there isnt enough images to leave x spaces between the same images
            if (Globals.PreventImageRepetition)
            {
                if (imgStorage.imageDirectories.Length < Math.Pow(Globals.ImageRepetitionDistance + 1, 2))
                {
                    Console.WriteLine("There arent enough images to leave a " + Globals.ImageRepetitionDistance + " space between the same images. \nEither add more images or decrease this number.\nTo have a space of " + Globals.ImageRepetitionDistance + " between images you need at least " + Math.Pow(Globals.ImageRepetitionDistance + 1, 2) + " images while you only have " + imgStorage.imageDirectories.Length + ".");
                    return;
                }
                if (Globals.ImageRepetitionDistance <= 0)
                {
                    Console.WriteLine("The distance between images must be higher than 0.");
                    return;
                }
            }

            Stopwatch stopwatchProcessingImg = Stopwatch.StartNew();
            consoleUpdater.UpdateText("Processing images...", 2, ConsoleColor.White);
            consoleUpdater.WriteToConsole();

            if (Globals.LoadPreProcessedImages)
            {
                ImageProcessing.LoadProcessedImages(imgStorage);
            }
            else
            {

                /*
                 * Process images - Go through all images and get their average color
                 */
                consoleUpdater.ResetProgress(maxProgressArray[2]);

                if (Globals.ThreadCount > 1)
                {
                    for (int i = 0; i < Globals.ThreadCount; i++)
                    {
                        int tempVar = i;
                        imgStorage.activeThreads[tempVar] = new Thread(() => ProcessImages(imgStorage, consoleUpdater, tempVar));
                        imgStorage.activeThreads[tempVar].Start();

                    }
                }
                else
                {
                    imgStorage.activeThreads[0] = new Thread(() => ProcessImages(imgStorage, consoleUpdater, 0));
                    imgStorage.activeThreads[0].Start();
                }

                waitForThrads = true;
                while (waitForThrads)
                {
                    if (Globals.ThreadCount > 1)
                    {
                        int total = 0;
                        for (int i = 0; i < Globals.ThreadCount; i++)
                            total += Convert.ToInt32(imgStorage.activeThreads[i].IsAlive);

                        if (total == 0)
                        {
                            for (int i = 0; i < Globals.ThreadCount; i++)
                                imgStorage.activeThreads[i].Abort();

                            waitForThrads = false;
                        }
                    }
                    else
                    {
                        if (!imgStorage.activeThreads[0].IsAlive)
                        {
                            waitForThrads = false;
                            imgStorage.activeThreads[0].Abort();
                        }
                    }
                    Thread.Sleep(50);
                }


            }

            imgStorage.ClearNullImages();
            imgStorage.averageColorsArrayClab = MiscFunctions.ConvertRGBArrayToCIELAB(imgStorage.averageColorsArray);
            if (Globals.PreProcessImages)
            {
                ImageProcessing.PreProcessImages(imgStorage);
            }
            stopwatchProcessingImg.Stop();

            /*
            * Process pixels - Resize the image to the final resolution (in images) and match it with an image of the closest color
            */
            Stopwatch stopwatchProcessingPix = Stopwatch.StartNew();
            consoleUpdater.ResetProgress(maxProgressArray[0]);
            consoleUpdater.UpdateText("Processing pixels...", 2, ConsoleColor.White);
            consoleUpdater.WriteToConsole();
            //imgStorage.imageBitData = imgStorage.templateImage.LockBits(new Rectangle(0, 0, imgStorage.templateImage.Width, imgStorage.templateImage.Height), ImageLockMode.ReadWrite, imgStorage.templateImage.PixelFormat);
            if (Globals.ThreadCount > 1 && !Globals.SingleThreadedPixelProcessing)
            {
                for (int i = 0; i < Globals.ThreadCount; i++)
                {
                    int tempVar = i;
                    imgStorage.activeThreads[tempVar] = new Thread(() => ProcessPixels(imgStorage, consoleUpdater, tempVar));
                    imgStorage.activeThreads[tempVar].Start();
                }
            }
            else
            {
                imgStorage.activeThreads[0] = new Thread(() => ProcessPixels(imgStorage, consoleUpdater, 0));
                imgStorage.activeThreads[0].Start();
            }

            waitForThrads = true;
            while (waitForThrads)
            {
                if (Globals.ThreadCount > 1 && !Globals.SingleThreadedPixelProcessing)
                {
                    int total = 0;
                    for (int i = 0; i < Globals.ThreadCount; i++)
                        total += Convert.ToInt32(imgStorage.activeThreads[i].IsAlive);

                    if (total == 0)
                    {
                        for (int i = 0; i < Globals.ThreadCount; i++)
                            imgStorage.activeThreads[i].Abort();

                        waitForThrads = false;
                    }
                }
                else
                {
                    if (!imgStorage.activeThreads[0].IsAlive)
                    {
                        waitForThrads = false;
                        imgStorage.activeThreads[0].Abort();
                    }
                }
                Thread.Sleep(50);
            }

            //imgStorage.templateImage.UnlockBits(imgStorage.imageBitData);
            stopwatchProcessingPix.Stop();

            /*
            string imagePaths = "";
            for (int x = 0; x < imgStorage.imgWidth; x++)
            {
                for (int y = 0; y < imgStorage.imgHeight; y++)
                {
                    imagePaths += "[" + x + "," + y + "]" + imgStorage.imagePaths[x, y] + ", ";
                }
            }
            File.WriteAllText("ImageDebug.txt", imagePaths);
            */

            /*
             * Draw image - Using the previous variable render the final image
             */
            Stopwatch stopwatchProcessingDraw = Stopwatch.StartNew();
            consoleUpdater.ResetProgress(maxProgressArray[1]);
            consoleUpdater.UpdateText("Drawing image...", 2, ConsoleColor.White);
            consoleUpdater.WriteToConsole();
            GetDuplicateImages(imgStorage);
            LoadImagesIntoMemory(imgStorage);
            if (Globals.ThreadCount > 1)
            {
                for (int i = 0; i < Globals.ThreadCount; i++)
                {
                    int tempVar = i;
                    imgStorage.activeThreads[tempVar] = new Thread(() => DrawImage(imgStorage, consoleUpdater, tempVar));
                    imgStorage.activeThreads[tempVar].Start();
                }
            }
            else
            {
                imgStorage.activeThreads[0] = new Thread(() => DrawImage(imgStorage, consoleUpdater, 0));
                imgStorage.activeThreads[0].Start();
            }

            waitForThrads = true;
            while (waitForThrads)
            {
                if (Globals.ThreadCount > 1)
                {
                    int total = 0;
                    for (int i = 0; i < Globals.ThreadCount; i++)
                        total += Convert.ToInt32(imgStorage.activeThreads[i].IsAlive);

                    if (total == 0)
                    {
                        for (int i = 0; i < Globals.ThreadCount; i++)
                            imgStorage.activeThreads[i].Abort();

                        waitForThrads = false;
                    }
                }
                else
                {
                    if (!imgStorage.activeThreads[0].IsAlive)
                    {
                        waitForThrads = false;
                        imgStorage.activeThreads[0].Abort();
                    }
                }
                Thread.Sleep(50);
            }


            //Because of using multiple threads the final image is saved in pieces in an array
            //puts all the pieces in a final image
            using (Image img = new Bitmap(imgStorage.imageWidth, imgStorage.imageHeight, PixelFormat.Format32bppArgb))
            {
                using (Graphics gr = Graphics.FromImage(img))
                {
                    for (int y = 0; y < imgStorage.finalImagePart[0].Length; y++)
                    {
                        for (int x = 0; x < imgStorage.finalImagePart.Length; x++)
                        {
                            int height = 0;
                            int width = 0;
                            for (int yy = 0; yy < y; yy++)
                            {
                                height += imgStorage.finalImagePart[x][yy].Height;
                            }
                            for (int xx = 0; xx < x; xx++)
                            {
                                width += imgStorage.finalImagePart[xx][y].Width;
                            }

                            gr.DrawImage(imgStorage.finalImagePart[x][y], new Point(width, height));
                            consoleUpdater.progressArray[0] += 1;
                            consoleUpdater.processingProgressArray[0] += 1;
                        }
                    }

                    if (Globals.OriginalImageOverlay > 0.00f)
                    {
                        //Bitmap transparentImg = SetImageOpacity(ResizeImage(image, imgStorage.imageWidth, imgStorage.imageHeight), 0.05f);
                        //transparentImg.Save(@"finalTEST.jpg", ImageFormat.Jpeg);
                        //transparentImg.MakeTransparent();
                        using (Image imgToDraw = ImageProcessing.SetImageOpacity(ImageProcessing.ResizeImage(imgStorage.originalImage, imgStorage.imageWidth, imgStorage.imageHeight, 1), Math.Min(Math.Abs(Globals.OriginalImageOverlay), 1.00f)))
                        {
                            gr.DrawImage(imgToDraw, new Point(0, 0));
                        }
                    }

                    stopwatchProcessingDraw.Stop();
                    stopwatch.Stop();

                    string[] fileType = finalFileName.Split('.');

                    if (fileType[1] == "png")
                    {
                        img.Save(fileType[0] + ".png", ImageFormat.Png);
                    }
                    else if (fileType[1] == "jpg")
                    {
                        img.Save(fileType[0] + ".jpg", ImageFormat.Jpeg);
                    }
                    else if (fileType[1] == "bmp")
                    {
                        img.Save(fileType[0] + ".bmp", ImageFormat.Bmp);
                    }
                    else
                    {
                        img.Save(fileType[0] + ".jpg", ImageFormat.Jpeg);
                    }

                    //imgStorage.originalImage.Save("1originalIMAGE.png", ImageFormat.Png);
                    //imgStorage.templateImage.Save("1templateIMAGE.png", ImageFormat.Png);
                }
            }

            consoleUpdater.ClearTTP();
            consoleUpdater.UpdateText("Processing images: " + stopwatchProcessingImg.ElapsedMilliseconds / 1000 + "sec", 0, ConsoleColor.White);
            consoleUpdater.UpdateText("Processing pixels: " + stopwatchProcessingPix.ElapsedMilliseconds / 1000 + "sec", 1, ConsoleColor.White);
            consoleUpdater.UpdateText("Drawing image: " + stopwatchProcessingDraw.ElapsedMilliseconds / 1000 + "sec", 2, ConsoleColor.White);
            consoleUpdater.UpdateText("DONE! Total time taken:" + stopwatch.ElapsedMilliseconds / 1000 + "sec", 3, ConsoleColor.White);
            consoleUpdater.WriteToConsole();
        }

        public static void ProcessImages(ImageMosaicStorage imgStorage, ConsoleUpdater consoleUpdater, int threadId)
        {
            int loopLength = imgStorage.imageDirectories.Length / Globals.ThreadCount;
            int threadStartPos = threadId * loopLength;
            int finalPos = threadStartPos + loopLength;
            if (threadId == Globals.ThreadCount - 1)
            {
                finalPos += imgStorage.imageDirectories.Length - (loopLength * Globals.ThreadCount);
            }

            for (int i = threadStartPos; i < finalPos; i++)
            {

                //Bitmap bitmap = new Bitmap(Globals.sourceImgResolution, Globals.sourceImgResolution);
                using (Bitmap bitmap = new Bitmap(Globals.SourceImgResolution, Globals.SourceImgResolution))
                {
                    try
                    {
                        using (var gr = Graphics.FromImage(bitmap))
                        {
                            gr.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Rectangle(0, 0, Globals.SourceImgResolution, Globals.SourceImgResolution));
                            using (Bitmap bitmap2 = new Bitmap(imgStorage.imageDirectories[i]))
                            {
                                using (Bitmap bitmap3 = ImageProcessing.SquareImage(bitmap2))
                                {
                                    using (Bitmap bitmap4 = ImageProcessing.ResizeImage(bitmap3, Globals.SourceImgResolution, Globals.SourceImgResolution))
                                    {
                                        gr.DrawImage(bitmap4, new Rectangle(0, 0, Globals.SourceImgResolution, Globals.SourceImgResolution));
                                    }
                                }
                            }
                            imgStorage.averageColorsArray[i] = ImageProcessing.GetAverageBetmapColor(bitmap);
                        }
                    }
                    catch
                    {
                        imgStorage.imageDirectories[i] = "";
                        //imgStorage.brokenImagesArray.Add(i);
                    }

                    consoleUpdater.progressArray[threadId] += 1;
                    consoleUpdater.processingProgressArray[threadId] += 1;

                    if (threadId == 0)
                    {
                        consoleUpdater.UpdateLoadingProgress(0, ConsoleColor.Green);
                    }
                }
            }
        }

        public static void ProcessPixels(ImageMosaicStorage imgStorage, ConsoleUpdater consoleUpdater, int threadId)
        {
            Color pixel;
            string[] SortedColors;
            int threadIdX = 0;
            int threadIdY = 0;
            for (int i = 0; i < threadId; i++)
            {
                threadIdX++;
                if (threadIdX == imgStorage.threadFactorsPublic[0])
                {
                    threadIdX = 0;
                    threadIdY++;
                }
            }

            int startingPointY = 0;
            int startingPointX = 0;
            int endingPointY = 0;
            int endingPointX = 0;

            if (Globals.SingleThreadedPixelProcessing)
            {
                endingPointY = imgStorage.imgHeight;
                endingPointX = imgStorage.imgWidth;
            }
            else
            {
                for (int i = 0; i < threadIdY; i++)
                {
                    startingPointY += imgStorage.finalImagePartSizesHeight[threadIdX][i];
                }
                for (int i = 0; i < threadIdX; i++)
                {
                    startingPointX += imgStorage.finalImagePartSizesWidth[i][threadIdY];
                }

                if (threadIdY < imgStorage.threadFactorsPublic[1])
                {
                    for (int i = 0; i <= threadIdY; i++)
                    {
                        endingPointY += imgStorage.finalImagePartSizesHeight[threadIdX][i];
                    }
                }
                else endingPointY = imgStorage.imgHeight;

                if (threadIdX < imgStorage.threadFactorsPublic[0])
                {
                    for (int i = 0; i <= threadIdX; i++)
                    {
                        endingPointX += imgStorage.finalImagePartSizesWidth[i][threadIdY];
                    }
                }
                else endingPointX = imgStorage.imgWidth;
            }
            string imagePaths = "";
            for (int y = startingPointY; y < endingPointY; y++)
            {
                for (int x = startingPointX; x < endingPointX; x++)
                {
                    //Image class is not thread safe so we gotta lock the image before using it
                    lock (imgStorage.templateImage)
                    {
                        pixel = imgStorage.templateImage.GetPixel(x, y);
                    }
                    imgStorage.templatePixelColors[x][y] = pixel;


                    float[][] tempColorArray = new float[imgStorage.averageColorsArrayClab.Length][];
                    string[] tempDirs = new string[imgStorage.imageDirectories.Length];
                    Array.Copy(imgStorage.averageColorsArrayClab, tempColorArray, imgStorage.averageColorsArrayClab.Length);
                    Array.Copy(imgStorage.imageDirectories, tempDirs, imgStorage.imageDirectories.Length);
                    SortedColors = ImageProcessing.GetSimilarColorImage(imgStorage.templatePixelColors[x][y], tempColorArray, tempDirs);


                    imgStorage.imagePaths[x][y] = ImageProcessing.PreventImageRepetition(imgStorage.imagePaths, SortedColors, new int[] { x, y });
                    imagePaths += "[" + x + "," + y + "](R:" + imgStorage.templatePixelColors[x][y].R + " G:" + imgStorage.templatePixelColors[x][y].G + " B:" + imgStorage.templatePixelColors[x][y].B + "TEST:" + SortedColors[0] + "), \n";
                    consoleUpdater.progressArray[threadId] += 1;
                    consoleUpdater.processingProgressArray[threadId] += 1;
                    if (threadId == 0)
                    {
                        consoleUpdater.UpdateLoadingProgress(0, ConsoleColor.Green);
                    }
                }
            }

            File.WriteAllText("ImageDebug" + threadId + ".txt", imagePaths);

        }

        public static void DrawImage(ImageMosaicStorage imgStorage, ConsoleUpdater consoleUpdater, int threadId)
        {
            int[] threadPos = new int[2] { 0, 0 };
            int[] startingPos = new int[2] { 0, 0 };
            int[] endingPos = new int[2] { 0, 0 };

            for (int i = 0; i < threadId; i++)
            {
                threadPos[0]++;
                if (threadPos[0] == imgStorage.threadFactorsPublic[0])
                {
                    threadPos[0] = 0;
                    threadPos[1]++;
                }
            }

            Bitmap imgOut = new Bitmap(imgStorage.finalImagePartSizesWidth[threadPos[0]][threadPos[1]] * Globals.SourceImgResolution, imgStorage.finalImagePartSizesHeight[threadPos[0]][threadPos[1]] * Globals.SourceImgResolution);

            for (int i = 0; i < threadPos[1]; i++)
            {
                startingPos[1] += imgStorage.finalImagePartSizesHeight[threadPos[0]][i];
            }
            for (int i = 0; i < threadPos[0]; i++)
            {
                startingPos[0] += imgStorage.finalImagePartSizesWidth[i][threadPos[1]];
            }
            if (threadPos[1] < imgStorage.threadFactorsPublic[1])
            {
                for (int i = 0; i <= threadPos[1]; i++)
                {
                    endingPos[1] += imgStorage.finalImagePartSizesHeight[threadPos[0]][i];
                }
            }
            else
            {
                endingPos[1] = imgStorage.imgHeight;
            }

            if (threadPos[0] < imgStorage.threadFactorsPublic[0])
            {
                for (int i = 0; i <= threadPos[0]; i++)
                {
                    endingPos[0] += imgStorage.finalImagePartSizesWidth[i][threadPos[1]];
                }
            }
            else
            {
                endingPos[0] = imgStorage.imgWidth;
            }

            int xx = 0;
            int yy;
            using (Graphics gr = Graphics.FromImage(imgOut))
            {
                for (int x = startingPos[0]; x < endingPos[0]; x++)
                {
                    yy = 0;
                    for (int y = startingPos[1]; y < endingPos[1]; y++)
                    {
                        gr.FillRectangle(new SolidBrush(Color.FromArgb(255, 255, 255, 255)), new Rectangle(xx * Globals.SourceImgResolution, yy * Globals.SourceImgResolution, Globals.SourceImgResolution, Globals.SourceImgResolution));
                        int usedImagePos = -1;
                        if (Globals.LoadPreProcessedImages)
                        {
                            usedImagePos = Array.IndexOf(imgStorage.usedImagesPaths, imgStorage.imagePaths[x][y]);
                        }
                        if (usedImagePos != -1)
                        {
                            lock (imgStorage.usedImages)
                            {
                                gr.DrawImage(imgStorage.usedImages[usedImagePos], new Point(xx * Globals.SourceImgResolution, yy * Globals.SourceImgResolution));
                            }
                        }
                        else
                        {

                            using (Bitmap bitmap = new Bitmap(imgStorage.imagePaths[x][y]))
                            {
                                bitmap.SetResolution(96, 96);
                                gr.DrawImage(ImageProcessing.ResizeImage(ImageProcessing.SquareImage(bitmap), Globals.SourceImgResolution, Globals.SourceImgResolution), new Point(xx * Globals.SourceImgResolution, yy * Globals.SourceImgResolution));
                            }
                        }
                        if (Globals.SourceImageColouring > 0.0)
                            gr.FillRectangle(new SolidBrush(Color.FromArgb((int)Math.Min(Math.Abs(255.0 * Globals.SourceImageColouring), 255), imgStorage.templatePixelColors[x][y].R, imgStorage.templatePixelColors[x][y].G, imgStorage.templatePixelColors[x][y].B)), new Rectangle(xx * Globals.SourceImgResolution, yy * Globals.SourceImgResolution, Globals.SourceImgResolution, Globals.SourceImgResolution));

                        //gr.FillRectangle(new SolidBrush(imgStorage.templatePixelColors[x][y]), new Rectangle(xx * Globals.sourceImgResolution, yy * Globals.sourceImgResolution, Globals.sourceImgResolution, Globals.sourceImgResolution));

                        consoleUpdater.progressArray[threadId] += 1;
                        consoleUpdater.processingProgressArray[threadId] += 1;

                        if (threadId == 0)
                            consoleUpdater.UpdateLoadingProgress(0, ConsoleColor.Green);
                        yy++;
                    }
                    xx++;
                }
            }

            imgStorage.finalImagePart[threadPos[0]][threadPos[1]] = imgOut;
            //imgOut.Save("img" + threadId + ".png", ImageFormat.Png);
        }



        public static void GetDuplicateImages(ImageMosaicStorage imgStorage)
        {
            List<string> outputArray = new List<string>();
            for (int x = 0; x < imgStorage.imgWidth; x++)
            {
                for (int y = 0; y < imgStorage.imgHeight; y++)
                {
                    if (!outputArray.Contains(imgStorage.imagePaths[x][y]))
                    {
                        outputArray.Add(imgStorage.imagePaths[x][y]);
                    }
                }
            }
            imgStorage.usedImagesPaths = outputArray.ToArray();
        }

        public static void LoadImagesIntoMemory(ImageMosaicStorage imgStorage)
        {
            List<Bitmap> outputArray = new List<Bitmap>();
            for (int i = 0; i < imgStorage.usedImagesPaths.Length; i++)
            {
                using (Bitmap bitmap = new Bitmap(imgStorage.usedImagesPaths[i]))
                {
                    using (Bitmap bitmap2 = ImageProcessing.SquareImage(bitmap))
                    {
                        outputArray.Add(ImageProcessing.ResizeImage(bitmap2, Globals.SourceImgResolution, Globals.SourceImgResolution));
                    }
                }
            }
            imgStorage.usedImages = outputArray.ToArray();
        }
    }
}
