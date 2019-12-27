using System;
using System.IO;
using UnityEngine;
using UnityEditor;
using UnityEditor.Build.Reporting;
using System.Net;
using ICSharpCode.SharpZipLib.Zip;
using ICSharpCode.SharpZipLib.GZip;
using ICSharpCode.SharpZipLib.Tar;

namespace Editor
{
    public class Builder {

        public static string PlayerName = PlayerSettings.productName;

        public const string BuildBasePath = "Build";

        public static readonly string ProjectBasePath = Path.Combine(Application.dataPath, "..", "..");

        [MenuItem("Build/Standalone/Windows + Mac OSX + Linux")]
        public static void BuildAll()
        {
            BuildMacOS();
            BuildWindows();
            BuildLinux();
        }

        [MenuItem("Build/Standalone/MacOS")]
        public static void BuildMacOS()
        {
            Debug.Log("Build MacOS");
            Build(BuildTarget.StandaloneOSX, targetDirName: "MacOS");
        }

        [MenuItem("Build/Standalone/Windows")]
        public static void BuildWindows()
        {
            Debug.Log("Build Windows");
            Build(BuildTarget.StandaloneWindows64, targetDirName: "Windows");
        }

        [MenuItem("Build/Standalone/Linux")]
        public static void BuildLinux()
        {
            Debug.Log("Build Linux");
            Build(BuildTarget.StandaloneLinux64, targetDirName: "Linux");
        }

        [MenuItem("Build/Standalone/MacOS Headless")]
        public static void BuildMacOSHeadless()
        {
            Debug.Log("Build MacOS Headless");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "MacOSHeadless");
        }

        [MenuItem("Build/Standalone/Linux Headless")]
        public static void BuildLinuxHeadless()
        {
            Debug.Log("Build Linux Headless");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.EnableHeadlessMode, "LinuxHeadless");
        }

        [MenuItem("Build/Standalone/Windows Headless")]
        public static void BuildWindowsHeadless()
        {
            Debug.Log("Build Windows Headless");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }

        [MenuItem("Build/Development/Windows + Mac OSX + Linux")]
        public static void BuildAllDevelopment()
        {
            BuildMacOSDevelopment();
            BuildWindowsDevelopment();
            BuildLinuxDevelopment();
        }

        [MenuItem("Build/Development/MacOS")]
        public static void BuildMacOSDevelopment()
        {
            Debug.Log("Build MacOS Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.Development, targetDirName: "MacOS");
        }

        [MenuItem("Build/Development/Windows")]
        public static void BuildWindowsDevelopment()
        {
            Debug.Log("Build Windows Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.Development, targetDirName: "Windows");
        }

        [MenuItem("Build/Development/Linux")]
        public static void BuildLinuxDevelopment()
        {
            Debug.Log("Build Linux Development");
            Build(BuildTarget.StandaloneLinux64, BuildOptions.Development, targetDirName: "Linux");
        }

        [MenuItem("Build/Development/MacOS Headless")]
        public static void BuildMacOSHeadlessDevelopment()
        {
            Debug.Log("Build MacOS Headless Development");
            Build(BuildTarget.StandaloneOSX, BuildOptions.EnableHeadlessMode, "MacOSHeadless");
        }

        [MenuItem("Build/Development/Linux Headless")]
        public static void BuildLinuxHeadlessDevelopment()
        {
            Debug.Log("Build Linux Headless Development");
            Build(
                BuildTarget.StandaloneLinux64,
                BuildOptions.EnableHeadlessMode | BuildOptions.Development,
                "LinuxHeadless");
        }
        
        [MenuItem("Build/Standalone/Windows Headless Development")]
        public static void BuildWindowsHeadlessDevelopment()
        {
            Debug.Log("Build Windows Headless Development");
            Build(BuildTarget.StandaloneWindows64, BuildOptions.EnableHeadlessMode, "WindowsHeadless");
        }


        public static void Build(
            BuildTarget buildTarget,
            BuildOptions options = BuildOptions.None,
            string targetDirName = null)
        {
            string[] scenes = { "Assets/_Scenes/Game.unity" };
            targetDirName = Path.Combine(
                BuildBasePath,
                targetDirName ?? buildTarget.ToString()
            );
            string locationPathName = Path.Combine(
                targetDirName,
                buildTarget.HasFlag(BuildTarget.StandaloneWindows64) ? $"{PlayerName}.exe" : PlayerName);

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = locationPathName,
                target = buildTarget,
                options = EditorUserBuildSettings.development ? options | BuildOptions.Development : options,
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            DownloadSnapshotManager(buildTarget, targetDirName);
            File.Copy(
                Path.Combine(Application.dataPath, "README.txt"),
                Path.Combine(targetDirName, "README.txt")
            );
            
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log("Build succeeded: " + summary.totalSize + " bytes");
            }

            if (summary.result == BuildResult.Failed)
            {
                Debug.LogError("Build failed");
            }
        }

        private static void DownloadSnapshotManager(BuildTarget buildTarget, string targetDir)
        {
            string url;
            
            if (buildTarget == BuildTarget.StandaloneWindows64)
            {
                url = "https://9c-data-snapshots.s3.amazonaws.com/9c-snapshot.win-x64.zip";
            }
            else if (buildTarget == BuildTarget.StandaloneOSX)
            {
                url = "https://9c-data-snapshots.s3.amazonaws.com/9c-snapshot.osx-x64.tar.gz";
            }
            else 
            {
                Debug.LogWarning($"Snapshot Manager for {buildTarget} isn't supported. skipping...");
                return;
            }
            
            using (var client = new WebClient())
            {
                var tempFilePath = Path.GetTempFileName();
                try 
                {
                    client.DownloadFile(url, tempFilePath);
                    
                    if (url.EndsWith(".zip")) 
                    {
                        var fz = new FastZip();
                        fz.ExtractZip(tempFilePath, targetDir, null);
                    }
                    else if (url.EndsWith(".tar.gz"))
                    {
                        using (var tempFile = File.OpenRead(tempFilePath))
                        using (var gz = new GZipInputStream(tempFile))
                        using (var tar = TarArchive.CreateInputTarArchive(gz))
                        {
                            tar.ExtractContents(targetDir);
                        }
                    }
                }
                finally
                {
                    if (File.Exists(tempFilePath)) 
                    {
                        File.Delete(tempFilePath);
                    }
                }
            }
        }
    }
}
