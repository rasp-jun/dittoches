using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Validates imported authored assets and preserves the portable player's original build.</summary>
public static class AuthoredModelBuild
{
    static readonly string[] ShaderPaths = {
        "Assets/Resources/ArenaSurface.shader",
        "Assets/Resources/CharacterToon.shader"
    };

    [MenuItem("Dittoches Multi/Validate Authored Model and Shaders")]
    public static void Validate()
    {
        var asset = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Resources/Models/Agumon/Agumon.bytes");
        if (asset == null || asset.bytes.Length == 0)
            throw new BuildFailedException("Agumon.bytes was not imported as a non-empty TextAsset.");
        AuthoredModelValidation.Validate();
        foreach(string id in DigimonModelLibrary.Roster)AuthoredRosterValidation.ValidateModel(id);

        if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Null)
            throw new BuildFailedException("Shader validation needs a graphics device. Run Unity without -nographics.");
        foreach (string path in ShaderPaths)
        {
            Shader shader = LoadShader(path);
            var material = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
            try
            {
                for (int pass = 0; pass < material.passCount; pass++)
                    ShaderUtil.CompilePass(material, pass, true);
                CheckShaderErrors(shader, path);
                if (!shader.isSupported || material.passCount == 0)
                    throw new BuildFailedException("Shader has no supported passes: " + path);
            }
            finally { Object.DestroyImmediate(material); }
        }
        Debug.Log("AUTHORED ASSET VALIDATION PASS: Agumon TextAsset, playback, and both shaders.");
    }

    [MenuItem("Dittoches Multi/Build Validated Windows Client")]
    public static void Build()
    {
        const BuildTarget target = BuildTarget.StandaloneWindows64;
        if (EditorUserBuildSettings.activeBuildTarget != target)
            throw new BuildFailedException("Select Windows x64, or launch Unity with -buildTarget Win64 before this method.");
        if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.Standalone, target))
            throw new BuildFailedException("Windows x64 build support is not installed.");
        Validate();

        string output = Path.Combine(Directory.GetParent(Application.dataPath).FullName,
            "Builds", "UnityWindows", "DittochesMulti.exe");
        Directory.CreateDirectory(Path.GetDirectoryName(output));
        var buildTarget = NamedBuildTarget.Standalone;
        var previousBackend = PlayerSettings.GetScriptingBackend(buildTarget);
        try
        {
            if (previousBackend != ScriptingImplementation.Mono2x)
                PlayerSettings.SetScriptingBackend(buildTarget, ScriptingImplementation.Mono2x);
            var report = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                scenes = new[] { "Assets/Scenes/Bootstrap.unity" },
                locationPathName = output,
                target = target,
                options = BuildOptions.None
            });
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("Validated Windows build failed: " + report.summary.result +
                    " (" + report.summary.totalErrors + " errors).");
            foreach (string path in ShaderPaths) CheckShaderErrors(LoadShader(path), path);
            Debug.Log("AUTHORED WINDOWS BUILD PASS: " + output + " (" + report.summary.totalSize + " bytes).");
        }
        finally
        {
            if (PlayerSettings.GetScriptingBackend(buildTarget) != previousBackend)
                PlayerSettings.SetScriptingBackend(buildTarget, previousBackend);
        }
    }

    static Shader LoadShader(string path)
    {
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
        if (shader == null) throw new BuildFailedException("Required shader was not imported: " + path);
        return shader;
    }

    static void CheckShaderErrors(Shader shader, string path)
    {
        foreach (var message in ShaderUtil.GetShaderMessages(shader))
        {
            if (message.severity == ShaderCompilerMessageSeverity.Error)
                throw new BuildFailedException(path + ": " + message.message);
            Debug.LogWarning(path + ": " + message.message);
        }
        if (ShaderUtil.ShaderHasError(shader))
            throw new BuildFailedException("Shader compilation failed: " + path);
    }
}
