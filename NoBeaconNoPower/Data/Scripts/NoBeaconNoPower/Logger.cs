/*
 * 
 * This open-source project is available on GitHub and licensed under the GNU General Public License v3.0.
 * You are free to use, modify, and distribute this software under the terms of the GPL-3.0 license.
 * 
 * GitHub Repository: [https://github.com/MisterRee/Mods]
 * 
 */
using Sandbox.ModAPI;
using System;
using System.IO;

namespace NoBeaconNoPower
{
    internal static class Logs
    {
        internal const string LOG_PREFIX = "NoBeaconNoPower";
        internal const string LOG_SUFFIX = ".log";
        internal const int LOGS_TO_KEEP = 5;

        internal static TextWriter TextWriter;

        internal static void InitLogs()
        {
            int last = LOGS_TO_KEEP - 1;
            string lastName = LOG_PREFIX + last + LOG_SUFFIX;
            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(lastName, typeof(Logs)))
                MyAPIGateway.Utilities.DeleteFileInLocalStorage(lastName, typeof(Logs));

            if (last > 0)
            {
                for (int i = last; i > 0; i--)
                {
                    string oldName = LOG_PREFIX + (i - 1) + LOG_SUFFIX;
                    string newName = LOG_PREFIX + i + LOG_SUFFIX;
                    RenameFileInLocalStorage(oldName, newName, typeof(Logs));
                }
            }

            string fileName = LOG_PREFIX + 0 + LOG_SUFFIX;
            TextWriter = MyAPIGateway.Utilities.WriteFileInLocalStorage(fileName, typeof(Logs));

            var message = $"{DateTime.Now:dd-MM-yy HH-mm-ss} - Logging Started";
            WriteLine(message);
        }

        internal static void RenameFileInLocalStorage(string oldName, string newName, Type anyObjectInYourMod)
        {
            if (!MyAPIGateway.Utilities.FileExistsInLocalStorage(oldName, anyObjectInYourMod))
                return;

            if (MyAPIGateway.Utilities.FileExistsInLocalStorage(newName, anyObjectInYourMod))
                return;

            using (var read = MyAPIGateway.Utilities.ReadFileInLocalStorage(oldName, anyObjectInYourMod))
            {
                using (var write = MyAPIGateway.Utilities.WriteFileInLocalStorage(newName, anyObjectInYourMod))
                {
                    write.Write(read.ReadToEnd());
                    write.Flush();
                    write.Dispose();
                }
            }

            MyAPIGateway.Utilities.DeleteFileInLocalStorage(oldName, anyObjectInYourMod);
        }

        internal static void WriteLine(string text)
        {
            try
            {
                TextWriter.WriteLine($"{DateTime.Now:HH:mm:ss} - {text}");
                TextWriter.Flush();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        internal static void Close()
        {
            try
            {
                var message = $"{DateTime.Now:dd-MM-yy HH-mm-ss} - Logging Stopped";
                WriteLine(message);
                TextWriter.Flush();
                TextWriter.Close();
                TextWriter.Dispose();
            }
            catch (Exception ex)
            {
                LogException(ex);
            }
        }

        internal static void LogException(Exception ex)
        {
            var hasInner = ex.InnerException != null;
            var text = !hasInner ? $"{ex.Message}\n{ex.StackTrace}" : ex.Message;
            WriteLine($"Exception: {text}");
            if (hasInner)
                LogException(ex.InnerException);
        }
    }
}
