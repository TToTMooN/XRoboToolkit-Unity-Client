using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

public static class KiloFloorBuild
{
    // Unity -batchmode -quit -buildTarget Android -executeMethod KiloFloorBuild.Build
    public static void Build()
    {
        var args = Environment.GetCommandLineArgs();
        int outputIndex = Array.IndexOf(args, "-kiloOutput");
        if (outputIndex < 0 || outputIndex + 1 >= args.Length)
            throw new BuildFailedException("Pass -kiloOutput /absolute/path/KILO-XR-Floor.apk");
        string output = Path.GetFullPath(args[outputIndex + 1]);
        if (!output.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            throw new BuildFailedException("KILO floor client output must end in .apk");
        Directory.CreateDirectory(Path.GetDirectoryName(output));

        PlayerSettings.companyName = "KILO";
        PlayerSettings.productName = "KILO XR Floor";
        PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, "com.kilo.xrobotoolkit.floor");
        PlayerSettings.bundleVersion = "1.1.1-kilo-floor.1";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.SetScriptingBackend(BuildTargetGroup.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        // Independent development APK; never rely on upstream's private keystore.
        PlayerSettings.Android.useCustomKeystore = false;
        EditorUserBuildSettings.buildAppBundle = false;
        EditorUserBuildSettings.exportAsGoogleAndroidProject = false;
        var defines = PlayerSettings.GetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android)
            .Split(';').Where(value => value != "AUTOTEST_BUILD");
        PlayerSettings.SetScriptingDefineSymbolsForGroup(BuildTargetGroup.Android, string.Join(";", defines));

        var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray(),
            locationPathName = output,
            target = BuildTarget.Android,
            options = BuildOptions.None,
        });
        if (report.summary.result != BuildResult.Succeeded)
            throw new BuildFailedException($"KILO floor APK failed: {report.summary.result} ({report.summary.totalErrors} errors)");
        Debug.Log($"KILO floor APK built: {output}; package com.kilo.xrobotoolkit.floor");
    }
}
