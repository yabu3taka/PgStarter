using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

using Microsoft.VisualBasic.FileIO;

namespace PgStarter
{
    static class FileUtil
    {
        public static string GetAssemblyFolder()
        {
            Assembly myAssembly = Assembly.GetEntryAssembly();
            return Path.GetDirectoryName(myAssembly.Location);
        }

        public static string GetMyFolder(string orig)
        {
            return Path.Combine(GetAssemblyFolder(), Path.GetFileName(orig));
        }

        public static void MoveSafe(string sourceDir, string destinationDir)
        {
            FileSystem.MoveDirectory(sourceDir, destinationDir, true);
        }
    }
}
