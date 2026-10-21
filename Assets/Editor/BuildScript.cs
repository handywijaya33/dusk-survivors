using UnityEditor;
using UnityEngine;

public static class BuildScript
{
    /// <summary>Builds a Windows player to Builds/Windows. Usable from the menu or via -executeMethod BuildScript.BuildWindows.</summary>
    [MenuItem("Dusk Survivors/Build Windows")]
    public static void BuildWindows()
    {
        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { "Assets/Scenes/Main.unity" },
            locationPathName = "Builds/Windows/DuskSurvivors.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None,
        });
        Debug.Log("Build result: " + report.summary.result);
        if (Application.isBatchMode && report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            EditorApplication.Exit(1);
    }
}
