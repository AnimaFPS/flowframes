﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Flowframes.IO;
using ImageMagick;
using Flowframes.OS;
using Flowframes.Data;
using System.Drawing;

namespace Flowframes.Magick
{
    class Dedupe
    {
        public enum Mode { None, Info, Enabled, Auto }
        public static Mode currentMode;
        public static float currentThreshold;

        public static async Task Run(string path, bool testRun = false, bool setStatus = true)
        {
            if (path == null || !Directory.Exists(path) || Interpolate.canceled)
                return;

            currentMode = Mode.Auto;

            if(setStatus)
                Program.mainForm.SetStatus("Running frame de-duplication");

            currentThreshold = Config.GetFloat("dedupThresh");
            Logger.Log("Running accurate frame de-duplication...");

            if (currentMode == Mode.Enabled || currentMode == Mode.Auto)
                await RemoveDupeFrames(path, currentThreshold, "png", testRun, false, (currentMode == Mode.Auto));
        }

        public static Dictionary<string, MagickImage> imageCache = new Dictionary<string, MagickImage>();
        static MagickImage GetImage(string path)
        {
            bool allowCaching = true;

            if (!allowCaching)
                return new MagickImage(path);

            if (!imageCache.ContainsKey(path))
                imageCache.Add(path, new MagickImage(path));

            return imageCache[path];
        }

        public static void ClearCache ()
        {
            imageCache.Clear();
        }

        public static async Task RemoveDupeFrames(string path, float threshold, string ext, bool testRun = false, bool debugLog = false, bool skipIfNoDupes = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Restart();
            Logger.Log("Removing duplicate frames - Threshold: " + threshold.ToString("0.00"));

            FileInfo[] framePaths = IOUtils.GetFileInfosSorted(path, false, "*." + ext);
            List<string> framesToDelete = new List<string>();

            int bufferSize = GetBufferSize();

            int currentOutFrame = 1;
            int currentDupeCount = 0;

            int statsFramesKept = 0;
            int statsFramesDeleted = 0;

            int skipAfterNoDupesFrames = Config.GetInt("autoDedupFrames");
            bool hasEncounteredAnyDupes = false;
            bool skipped = false;

            bool hasReachedEnd = false;

            string infoFile = Path.Combine(path.GetParentDir(), $"dupes-magick.ini");
            string fileContent = "";

            for (int i = 0; i < framePaths.Length; i++)     // Loop through frames
            {
                if (hasReachedEnd)
                    break;

                string frame1 = framePaths[i].FullName;
                //if (!File.Exists(framePaths[i].FullName))   // Skip if file doesn't exist (already deleted / used to be a duped frame)
                //    continue;

                int compareWithIndex = i + 1;

                while (true)   // Loop dupes
                {
                    //compareWithIndex++;
                    if (compareWithIndex >= framePaths.Length)
                    {
                        hasReachedEnd = true;
                        break;
                    }

                    if (framesToDelete.Contains(framePaths[compareWithIndex].FullName) || !File.Exists(framePaths[compareWithIndex].FullName))
                    {
                        //Logger.Log($"Frame {compareWithIndex} was already deleted - skipping");
                        compareWithIndex++;
                    }
                    else
                    {
                        //if (compareWithIndex >= framePaths.Length)
                        //    hasReachedEnd = true;

                        string frame2 = framePaths[compareWithIndex].FullName;
                        // if (oldIndex >= 0)
                        //     i = oldIndex;

                        float diff = GetDifference(frame1, frame2);

                        string delStr = "Keeping";
                        if (diff < threshold)     // Is a duped frame.
                        {
                            if (!testRun)
                            {
                                delStr = "Deleting";
                                //File.Delete(frame2);
                                framesToDelete.Add(frame2);
                                if (debugLog) Logger.Log("[FrameDedup] Deleted " + Path.GetFileName(frame2));
                                hasEncounteredAnyDupes = true;
                            }
                            statsFramesDeleted++;
                            currentDupeCount++;
                        }
                        else
                        {
                            fileContent += $"{Path.GetFileNameWithoutExtension(framePaths[i].Name)}:{currentDupeCount}\n";
                            statsFramesKept++;
                            currentOutFrame++;
                            currentDupeCount = 0;
                            break;
                        }

                        if (sw.ElapsedMilliseconds >= 1000 || (i+1) == framePaths.Length)   // Print every 1s (or when done)
                        {
                            sw.Restart();
                            Logger.Log($"[FrameDedup] Difference from {Path.GetFileName(frame1)} to {Path.GetFileName(frame2)}: {diff.ToString("0.00")}% - {delStr}.", false, true);
                            Program.mainForm.SetProgress((int)Math.Round(((float)i / framePaths.Length) * 100f));
                            if (imageCache.Count > bufferSize || (imageCache.Count > 50 && OSUtils.GetFreeRamMb() < 2500))
                                ClearCache();
                        }
                    }
                }

                // int oldIndex = -1; // TODO: Compare with 1st to fix loops?
                // if (i >= framePaths.Length)    // If this is the last frame, compare with 1st to avoid OutOfRange error
                // {
                //     oldIndex = i;
                //     i = 0;
                // }

                if(i % 5 == 0)
                    await Task.Delay(1);

                if (Interpolate.canceled) return;

                if (!testRun && skipIfNoDupes && !hasEncounteredAnyDupes && skipAfterNoDupesFrames > 0 && i >= skipAfterNoDupesFrames)
                {
                    skipped = true;
                    break;
                }
            }

            // File.WriteAllText(infoFile, fileContent);    // DISABLED FOR NOW as we use a single piece of code for mpdec and this code

            foreach (string frame in framesToDelete)
                IOUtils.TryDeleteIfExists(frame);

            string testStr = testRun ? " [TestRun]" : "";

            if (Interpolate.canceled) return;

            int framesLeft = IOUtils.GetAmountOfFiles(path, false, $"*.png");
            int framesDeleted = framePaths.Length - framesLeft;
            float percentDeleted = ((float)framesDeleted / framePaths.Length) * 100f;
            string keptPercent = $"{(100f - percentDeleted).ToString("0.0")}%";

            if (skipped)
                Logger.Log($"[FrameDedup] First {skipAfterNoDupesFrames} frames did not have any duplicates - Skipping the rest!", false, true);
            else
                Logger.Log($"[FrameDedup]{testStr} Done. Kept {framesLeft} ({keptPercent}) frames, deleted {framesDeleted} frames.", false, true);

            if (statsFramesKept <= 0)
                Interpolate.Cancel("No frames were left after de-duplication!\n\nTry decreasing the de-duplication threshold.");
        }

        static float GetDifference (string img1Path, string img2Path)
        {
            MagickImage img2 = GetImage(img2Path);
            MagickImage img1 = GetImage(img1Path);

            double err = img1.Compare(img2, ErrorMetric.Fuzz);
            float errPercent = (float)err * 100f;
            return errPercent;
        }

        static int GetBufferSize ()
        {
            Size res = Interpolate.current.GetScaledRes();
            long pixels = res.Width * res.Height;    // 4K = 8294400, 1440p = 3686400, 1080p = 2073600, 720p = 921600, 540p = 518400, 360p = 230400
            int bufferSize = 100;
            if (pixels < 518400) bufferSize = 2800;
            if (pixels >= 518400) return 2000;
            if (pixels >= 921600) return 1200;
            if (pixels >= 2073600) return 800;
            if (pixels >= 3686400) return 400;
            if (pixels >= 8294400) return 200;
            Logger.Log($"Using magick dedupe buffer size {bufferSize} for frame resolution {res.Width}x{res.Height}", true);
            return bufferSize;
        }

        public static async Task CreateDupesFileMpdecimate (string framesPath, int lastFrameNum)
        {
            string infoFile = Path.Combine(framesPath.GetParentDir(), $"dupes.ini");
            string fileContent = "";

            FileInfo[] frameFiles = IOUtils.GetFileInfosSorted(framesPath, false, "*.png");

            for(int i = 0; i < frameFiles.Length; i++)
            {
                bool isLastItem = (i + 1) == frameFiles.Length;
                int frameNum1 = frameFiles[i].Name.GetInt();
                int frameNum2 = isLastItem ? lastFrameNum : frameFiles[i+1].Name.GetInt();

                int diff = frameNum2 - frameNum1;
                int dupes = diff - 1;

                fileContent += $"{Path.GetFileNameWithoutExtension(frameFiles[i].Name)}:{dupes}\n";
            }

            File.WriteAllText(infoFile, fileContent);
        }
    }
}