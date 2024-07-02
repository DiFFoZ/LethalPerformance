using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class BuildAction
{
    [MenuItem("Window/Lethal Performance/Build")]
    public static void BuildGame()
    {
        // https://docs.unity3d.com/Packages/com.unity.burst@1.8/manual/modding-support.html#simple-create-mod-menu-button

        const string c_ProjectName = "LethalPerformanceMod";

        var projectFolder = new FileInfo(Application.dataPath).DirectoryName;
        var buildPath = Path.Combine(projectFolder, "Build");

        FileUtil.DeleteFileOrDirectory(buildPath);

        var options = new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/SampleScene.unity" },
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
            locationPathName = Path.Combine(buildPath, $"{c_ProjectName}.exe")
        };

        var report = BuildPipeline.BuildPlayer(options);
        if (report.summary.result != BuildResult.Succeeded)
        {
            return;
        }

        var dataFolderPath = Path.Combine(buildPath, $"{c_ProjectName}_Data");

        var burstLibPath = Path.Combine(dataFolderPath, "Plugins", "x86_64", "lib_burst_generated.dll");
        var burstOutputPath = Path.Combine(projectFolder, "..", "LethalPerformance", "Publish", "lib_burst_generated.data");

        FileUtil.DeleteFileOrDirectory(burstOutputPath);
        FileUtil.CopyFileOrDirectory(burstLibPath, burstOutputPath);

        var modLibPath = Path.Combine(dataFolderPath, "Managed", "LethalPerformance.Unity.dll");
        var modOutputPath = Path.Combine(projectFolder, "..", "LethalPerformance", "Publish", "LethalPerformance.Unity.dll");

        FileUtil.DeleteFileOrDirectory(modOutputPath);
        FileUtil.CopyFileOrDirectory(modLibPath, modOutputPath);
    }
}
