// Pravljenje Android verzije: meni Tools > Wizard's Fight > Build Android APK
// (ili iz komandne linije: -buildTarget Android -executeMethod MobileBuild.BuildAndroid).
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

public static class MobileBuild
{
    public const string AndroidPath = "Builds/Android/AWizardsFight.apk";

    // Podešavanja koja važe za telefon: položeno, IL2CPP za 64-bitne telefone, Android 7.1 i noviji.
    public static void ConfigurePlayer()
    {
        PlayerSettings.companyName = "Mata";
        PlayerSettings.productName = "A Wizard's Fight";
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.mata.awizardsfight");
        PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, "com.mata.awizardsfight");
        PlayerSettings.bundleVersion = "1.0";
        PlayerSettings.Android.bundleVersionCode = 1;
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.AutoRotation;
        PlayerSettings.allowedAutorotateToLandscapeLeft = true;
        PlayerSettings.allowedAutorotateToLandscapeRight = true;
        PlayerSettings.allowedAutorotateToPortrait = false;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
        PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
        PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel25;
        PlayerSettings.Android.startInFullscreen = true;
        PlayerSettings.Android.renderOutsideSafeArea = false;
        AssetDatabase.SaveAssets();
    }

    // iOS: Unity na Windows-u pravi Xcode projekat; aplikaciju iz njega prevodi i potpisuje Xcode na Mac-u.
    public const string IosPath = "Builds/iOS";

    [MenuItem("Tools/Wizard's Fight/Export iOS Xcode Project")]
    public static void BuildIos()
    {
        ConfigurePlayer();
        PlayerSettings.iOS.targetOSVersionString = "15.0";
        PlayerSettings.iOS.requiresFullScreen = true;
        PlayerSettings.iOS.appleEnableAutomaticSigning = true;
        PlayerSettings.SetArchitecture(NamedBuildTarget.iOS, 1);
        var scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
        var report = BuildPipeline.BuildPlayer(scenes, IosPath, BuildTarget.iOS, BuildOptions.None);
        Debug.Log("BUILD ios " + report.summary.result + " errors=" + report.summary.totalErrors);
    }

    [MenuItem("Tools/Wizard's Fight/Build Android APK")]
    public static void BuildAndroid()
    {
        ConfigurePlayer();
        EditorUserBuildSettings.buildAppBundle = false;
        var scenes = System.Array.ConvertAll(EditorBuildSettings.scenes, scene => scene.path);
        var report = BuildPipeline.BuildPlayer(scenes, AndroidPath, BuildTarget.Android, BuildOptions.None);
        Debug.Log("BUILD android " + report.summary.result + " errors=" + report.summary.totalErrors + " size=" + report.summary.totalSize);
    }
}
