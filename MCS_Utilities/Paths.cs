using System;
using System.IO;
using System.Text.RegularExpressions;

namespace MCS_Utilities
{
    public class Paths
    {
        //Convert Assets/MajorTom/Outgoing/AlphaInjection/foo.png to ./AlphaInjection/foo.png
        // used specifically for mon file generation so we can replace "./" with the parent dir of the .mon file
        public static string RelativeTo(string baseDir, string filePath)
        {
            UnityEngine.Debug.Log("Orig: " + baseDir  + " => " + filePath);
            if (String.IsNullOrEmpty(filePath))
            {
                return filePath;
            }

            baseDir = baseDir.Replace(@"\", @"/");
            filePath = filePath.Replace(@"\", @"/");
            filePath = filePath.Replace(baseDir, "").TrimStart('/');
            filePath = @"./" + filePath;
            UnityEngine.Debug.Log("Out: " + filePath);

            return filePath;
        }

        //convert /foo/bar/car to /foo/bar
        public static string ConvertFileToDir(string filePath)
        {
            filePath = filePath.Replace(@"\", "/");
            int pos = filePath.LastIndexOf('/');
            if (pos >= 0)
            {
                return filePath.Substring(0, pos);
            }
            throw new Exception("Could not detect dir");
        }

        public static string GetFileNameFromFullPath(string filePath,bool stripExtension = false)
        {
            filePath = filePath.Replace(@"\", "/");
            int pos = filePath.LastIndexOf('/');
            if (pos >= 0)
            {
                string result = filePath.Substring(pos+1);
                if (stripExtension)
                {
                    int posExt = result.LastIndexOf('.');
                    if (posExt >= 0)
                    {
                        result = result.Substring(0, posExt);
                    }
                }

                return result;
            }
            throw new Exception("Could not detect dir");
        }

        public static string ConvertRelativeToAbsolute(string baseDir, string filePath)
        {
            if (String.IsNullOrEmpty(filePath) || String.IsNullOrEmpty(baseDir))
            {
                return filePath;
            }

            /*
            if (!filePath.StartsWith(@"./"))
            {
                return filePath;
            }
            */

            baseDir = baseDir.Replace(@"\", @"/");
            filePath = filePath.Replace(@"\", @"/");

            if (filePath.StartsWith(@"./"))
            {
                filePath = filePath.Replace("./", baseDir + "/");
            } else
            {
                //it doesn't start with a "./" let's assume we would not combine it first, then we would combine it as the default
                if (!File.Exists(filePath))
                {
                    filePath = baseDir.TrimEnd('/') + @"/" + filePath;
                }

            }
            return filePath;
        }

        protected static Regex nameScrub = new Regex("[^a-zA-Z0-9_-]+");
        public static string ScrubKey(string src)
        {
            src = src.Replace(" ", "_");
            src = nameScrub.Replace(src, "");
            return src;
        }

        protected static Regex directoryScrub = new Regex(@"[^a-zA-Z0-9-._/]+");
        public static string ScrubPath(string src)
        {
            src = src.Replace("\\", "/");
            src = src.Replace(" ", "_");
            src = directoryScrub.Replace(src, "");
            return src;
        }

        //Attempts to delete a directory recursively multiple times catching exceptions up to X times
        public static void TryDirectoryDelete(string dirPath, int attemptCount = 0)
        {
            int maxAttempts = 10;

            //delete the loose files which we don't need anymore
            if (!Directory.Exists(dirPath))
            {
                return;
            }

            try
            {
                Directory.Delete(dirPath, true);
            }
            catch (Exception e)
            {
                //if we're below our attempts, try again
                if (attemptCount < maxAttempts)
                {
                    //wait 10 ms and try again
                    System.Threading.Thread.Sleep(10);
                    TryDirectoryDelete(dirPath, attemptCount++);
                }
                else
                {
                    UnityEngine.Debug.LogError("Max attempts at deleting directory: " + dirPath + " Check to see if you're exceeding path name limits!");
                    UnityEngine.Debug.LogException(e);
                    throw;
                }
            }
        }
    }
}
