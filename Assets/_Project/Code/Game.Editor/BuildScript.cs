using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UnityBuilderAction
{
    public static class BuildScript
    {
        public static void Build()
        {
            var args = ParseCommandLineArguments();

            if (!args.TryGetValue("buildTarget", out var buildTargetName) ||
                !Enum.TryParse(buildTargetName, out BuildTarget buildTarget))
            {
                Debug.LogError("Missing or invalid -buildTarget argument");
                EditorApplication.Exit(120);
                return;
            }

            if (!args.TryGetValue("customBuildPath", out var buildPath))
            {
                Debug.LogError("Missing -customBuildPath argument");
                EditorApplication.Exit(121);
                return;
            }

            if (args.TryGetValue("buildVersion", out var buildVersion) && buildVersion != "none")
                PlayerSettings.bundleVersion = buildVersion;

            var scenes = EditorBuildSettings.scenes
                .Where(scene => scene.enabled)
                .Select(scene => scene.path)
                .ToArray();

            var buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = buildPath,
                target = buildTarget,
            };

            var report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            var summary = report.summary;

            Debug.Log($"Build {summary.result}: {summary.totalSize} bytes, " +
                      $"{summary.totalErrors} errors, {summary.totalWarnings} warnings, {summary.totalTime}");

            EditorApplication.Exit(summary.result == BuildResult.Succeeded ? 0 : 1);
        }

        private static Dictionary<string, string> ParseCommandLineArguments()
        {
            var args = Environment.GetCommandLineArgs();
            var result = new Dictionary<string, string>();

            for (var i = 0; i < args.Length; i++)
            {
                if (!args[i].StartsWith("-", StringComparison.Ordinal))
                    continue;

                var key = args[i].Substring(1);
                var value = i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal)
                    ? args[i + 1]
                    : string.Empty;
                result[key] = value;
            }

            return result;
        }
    }
}
