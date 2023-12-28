using System;
using System.Diagnostics;

namespace ImageMosaicGenerator
{
    class ConsoleUpdater
    {
        string[] textToPrint;
        ConsoleColor[] colorToPrint;
        int maxProgress = 0;
        int maxProcessProgress = 0;
        int progressBarLength = 75;
        public int[] progressArray;
        int updateFrequency = 250;
        double lastTimeUpdated = Environment.TickCount;
        public int[] processingProgressArray;
        public Stopwatch processingProgressClock;

        public ConsoleUpdater(int consoleLength, int maxProgressVar)
        {
            maxProgress = maxProgressVar;
            textToPrint = new string[consoleLength];
            colorToPrint = new ConsoleColor[consoleLength];

            progressArray = new int[Globals.ThreadCount];
            for (int i = 0; i < progressArray.Length; i++)
            {
                progressArray[i] = 0;
            }

            processingProgressArray = new int[Globals.ThreadCount];
            for (int i = 0; i < processingProgressArray.Length; i++)
            {
                processingProgressArray[i] = 0;
            }
        }

        public void UpdateLoadingProgress(int position, ConsoleColor textColor)
        {
            UpdateProcessingProgress(position + 1);
            int totalProgress = 0;
            for (int i = 0; i < progressArray.Length; i++)
            {
                totalProgress += progressArray[i];
            }

            if (totalProgress < maxProgress)
            {
                double currentPercentage = (double)totalProgress / maxProgress * 100.000;
                textToPrint[position] = "";
                float currentSteps = 0;
                float progressSteps = 100.0f / progressBarLength;
                for (int i = 0; i < progressBarLength; i++)
                {
                    if (currentSteps < currentPercentage)
                    {
                        textToPrint[position] += "█";
                    }
                    else
                    {
                        textToPrint[position] += "░";
                    }
                    currentSteps += progressSteps;
                }
                textToPrint[position] += " " + Math.Round(currentPercentage, 2) + "%";
                colorToPrint[position] = textColor;
                WriteToConsole();
            }
        }

        public void UpdateProcessingProgress(int position, ConsoleColor textColor = ConsoleColor.White)
        {
            int totalProgress = 0;
            for (int i = 0; i < processingProgressArray.Length; i++)
            {
                totalProgress += processingProgressArray[i];
            }

            if (totalProgress < maxProcessProgress && totalProgress > 0)
            {
                textToPrint[position] = "";
                textToPrint[position] += "ETA for this part: " + (int)Math.Round(processingProgressClock.ElapsedMilliseconds / (double)totalProgress * (maxProcessProgress - totalProgress) / 1000, 0) + "sec";
                colorToPrint[position] = textColor;
                WriteToConsole();
            }
        }

        public void ResetProgress(int maxProgressVar)
        {
            maxProcessProgress = maxProgressVar;
            for (int i = 0; i < processingProgressArray.Length; i++)
            {
                processingProgressArray[i] = 0;
            }
            processingProgressClock = Stopwatch.StartNew();
        }

        public void UpdateText(string text, int position, ConsoleColor textColor)
        {
            textToPrint[position] = text;
            colorToPrint[position] = textColor;
        }

        public void ClearTTP(int position = 99999)
        {
            ClearConsole();
            if (position < textToPrint.Length)
            {
                textToPrint[position] = "";
            }
            else
            {
                for (int i = 0; i < textToPrint.Length; i++)
                {
                    textToPrint[i] = "";
                }
            }
        }

        public void WriteToConsole()
        {
            if (Environment.TickCount - lastTimeUpdated > updateFrequency)
            {
                for (int i = 0; i < textToPrint.Length; i++)
                {
                    if (textToPrint[i] != "" && textToPrint[i] != null)
                    {
                        Console.SetCursorPosition(0, i);
                        //Console.Write("                                                                                                                 ");
                        Console.ForegroundColor = colorToPrint[i];
                        Console.WriteLine("\r" + textToPrint[i] + "                                                                                                                 ");
                    }
                }
                lastTimeUpdated = Environment.TickCount;
            }
        }

        public void ClearConsole()
        {
            for (int i = Console.CursorTop; i >= 0; i--)
            {
                Console.SetCursorPosition(0, i);
                Console.Write("\r                                                                                                                 ");
            }

        }
    }
}
