using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace Nexus_Launcher.Services
{
    internal static class ClientResetService
    {
        // ------------------------------------------------------------
        // Result
        // ------------------------------------------------------------

        public class ResetResult
        {
            public int FilesDeleted { get; set; }
            public int FoldersCleared { get; set; }
            public int FilesFailed { get; set; }
            public int FoldersFailed { get; set; }

            public List<string> Errors { get; } =
                new List<string>();

            public bool Success
            {
                get
                {
                    return Errors.Count == 0;
                }
            }
        }

        // ------------------------------------------------------------
        // Main reset
        // ------------------------------------------------------------

        public static ResetResult ResetClient(
            string clientName,
            IEnumerable<string> tempFolders,
            IEnumerable<string> cacheFolders,
            IEnumerable<string> logFolders,
            IEnumerable<string> crashFolders,
            IEnumerable<string> foldersToClear,
            IEnumerable<string> repairTools,
            IEnumerable<string> processNames,
            bool clearWindowsTemp = true,
            bool clearDns = false)
        {
            ResetResult result =
                new ResetResult();

            //--------------------------------------------------------
            // Stop client
            //--------------------------------------------------------

            if (processNames != null)
            {
                foreach (string processName in processNames)
                {
                    StopProcess(
                        processName,
                        result);
                }
            }

            //--------------------------------------------------------
            // Windows TEMP
            //--------------------------------------------------------

            if (clearWindowsTemp)
            {
                ClearWindowsTemp(result);
            }

            //--------------------------------------------------------
            // Client TEMP
            //--------------------------------------------------------

            ClearFolders(
                tempFolders,
                result);

            //--------------------------------------------------------
            // Cache
            //--------------------------------------------------------

            ClearFolders(
                cacheFolders,
                result);

            //--------------------------------------------------------
            // Logs
            //--------------------------------------------------------

            ClearFilesInFolders(
                logFolders,
                result,
                new[]
                {
                    "*.log",
                    "*.txt",
                    "*.dmp",
                    "*.mdmp",
                    "*.old",
                    "*.bak"
                });

            //--------------------------------------------------------
            // Crash dumps
            //--------------------------------------------------------

            ClearFilesInFolders(
                crashFolders,
                result,
                new[]
                {
                    "*.dmp",
                    "*.mdmp",
                    "*.hdmp",
                    "*.wer"
                });

            //--------------------------------------------------------
            // Explicit folders
            //--------------------------------------------------------

            ClearFolders(
                foldersToClear,
                result);

            //--------------------------------------------------------
            // Repair tools
            //--------------------------------------------------------

            RunRepairTools(
                repairTools,
                result);

            //--------------------------------------------------------
            // Optional DNS reset
            //--------------------------------------------------------

            if (clearDns)
            {
                FlushDns(result);
            }

            return result;
        }

        // ------------------------------------------------------------
        // Process handling
        // ------------------------------------------------------------

        private static void StopProcess(
            string processName,
            ResetResult result)
        {
            if (string.IsNullOrWhiteSpace(processName))
                return;

            try
            {
                string name =
                    Path.GetFileNameWithoutExtension(
                        processName);

                Process[] processes =
                    Process.GetProcessesByName(name);

                foreach (Process process in processes)
                {
                    try
                    {
                        if (!process.HasExited)
                        {
                            process.CloseMainWindow();

                            if (!process.WaitForExit(2000))
                            {
                                process.Kill();

                                process.WaitForExit(2000);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(
                            "Could not stop " +
                            processName +
                            ": " +
                            ex.Message);
                    }
                    finally
                    {
                        process.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add(
                    "Could not inspect process " +
                    processName +
                    ": " +
                    ex.Message);
            }
        }

        // ------------------------------------------------------------
        // Windows TEMP
        // ------------------------------------------------------------

        private static void ClearWindowsTemp(
            ResetResult result)
        {
            List<string> paths =
                new List<string>();

            string userTemp =
                Path.GetTempPath();

            if (!string.IsNullOrWhiteSpace(userTemp))
            {
                paths.Add(userTemp);
            }

            string windowsTemp =
                Path.Combine(
                    Environment.GetFolderPath(
                        Environment.SpecialFolder.Windows),
                    "Temp");

            paths.Add(windowsTemp);

            ClearFolders(
                paths,
                result);
        }

        // ------------------------------------------------------------
        // Clear entire folders
        // ------------------------------------------------------------

        public static void ClearFolders(
            IEnumerable<string> folders,
            ResetResult result)
        {
            if (folders == null)
                return;

            foreach (string folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                try
                {
                    if (!Directory.Exists(folder))
                        continue;

                    foreach (string file in
                        Directory.GetFiles(
                            folder,
                            "*",
                            SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            File.SetAttributes(
                                file,
                                FileAttributes.Normal);

                            File.Delete(file);

                            result.FilesDeleted++;
                        }
                        catch (Exception ex)
                        {
                            result.FilesFailed++;

                            result.Errors.Add(
                                "Could not delete file: " +
                                file +
                                " - " +
                                ex.Message);
                        }
                    }

                    foreach (string directory in
                        Directory.GetDirectories(
                            folder,
                            "*",
                            SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            Directory.Delete(
                                directory,
                                true);

                            result.FoldersCleared++;
                        }
                        catch (Exception ex)
                        {
                            result.FoldersFailed++;

                            result.Errors.Add(
                                "Could not clear folder: " +
                                directory +
                                " - " +
                                ex.Message);
                        }
                    }
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        "Could not access folder: " +
                        folder +
                        " - " +
                        ex.Message);
                }
            }
        }

        // ------------------------------------------------------------
        // Delete specific file types
        // ------------------------------------------------------------

        public static void ClearFilesInFolders(
            IEnumerable<string> folders,
            ResetResult result,
            IEnumerable<string> patterns)
        {
            if (folders == null ||
                patterns == null)
                return;

            foreach (string folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder))
                    continue;

                if (!Directory.Exists(folder))
                    continue;

                foreach (string pattern in patterns)
                {
                    try
                    {
                        string[] files =
                            Directory.GetFiles(
                                folder,
                                pattern,
                                SearchOption.AllDirectories);

                        foreach (string file in files)
                        {
                            try
                            {
                                File.SetAttributes(
                                    file,
                                    FileAttributes.Normal);

                                File.Delete(file);

                                result.FilesDeleted++;
                            }
                            catch (Exception ex)
                            {
                                result.FilesFailed++;

                                result.Errors.Add(
                                    "Could not delete: " +
                                    file +
                                    " - " +
                                    ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        result.Errors.Add(
                            "Could not scan: " +
                            folder +
                            " - " +
                            ex.Message);
                    }
                }
            }
        }

        // ------------------------------------------------------------
        // Repair tools
        // ------------------------------------------------------------

        private static void RunRepairTools(
            IEnumerable<string> tools,
            ResetResult result)
        {
            if (tools == null)
                return;

            foreach (string tool in tools)
            {
                if (string.IsNullOrWhiteSpace(tool))
                    continue;

                try
                {
                    if (!File.Exists(tool))
                        continue;

                    Process process =
                        new Process();

                    process.StartInfo.FileName =
                        tool;

                    process.StartInfo.UseShellExecute =
                        true;

                    process.Start();

                    process.WaitForExit();

                    process.Dispose();
                }
                catch (Exception ex)
                {
                    result.Errors.Add(
                        "Could not run repair tool: " +
                        tool +
                        " - " +
                        ex.Message);
                }
            }
        }

        // ------------------------------------------------------------
        // DNS
        // ------------------------------------------------------------

        private static void FlushDns(
            ResetResult result)
        {
            try
            {
                Process process =
                    new Process();

                process.StartInfo.FileName =
                    "ipconfig.exe";

                process.StartInfo.Arguments =
                    "/flushdns";

                process.StartInfo.UseShellExecute =
                    false;

                process.StartInfo.CreateNoWindow =
                    true;

                process.Start();

                process.WaitForExit();

                process.Dispose();
            }
            catch (Exception ex)
            {
                result.Errors.Add(
                    "Could not flush DNS: " +
                    ex.Message);
            }
        }
    }
}