using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace AiCat.Editor
{
    /// <summary>
    /// CI 构建入口（T0-8）。
    /// 供 game-ci/unity-builder 通过 buildMethod 调用：AiCat.Editor.BuildScript.BuildWindows。
    /// 产物输出到 &lt;项目根&gt;/Builds/StandaloneWindows64/，与 scaffold/.gitignore 的 Builds/ 忽略规则一致。
    /// 说明：脚手架 UI 由 ChatBootstrap 的 [RuntimeInitializeOnLoadMethod] 程序化生成，
    ///       不依赖场景内容；若 Build Settings 无启用场景，则临时创建最小空场景保证构建通过。
    /// </summary>
    public static class BuildScript
    {
        public static void BuildWindows()
        {
            Build(BuildTarget.StandaloneWindows64, "StandaloneWindows64");
        }

        private static void Build(BuildTarget target, string dirName)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)!.FullName;
            string outDir = Path.Combine(projectRoot, "Builds", dirName);
            Directory.CreateDirectory(outDir);

            string[] scenes = EditorBuildSettings.scenes
                .Where(s => s.enabled)
                .Select(s => s.path)
                .ToArray();

            if (scenes.Length == 0)
            {
                scenes = new[] { EnsureEmptyScene(projectRoot) };
            }

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = Path.Combine(outDir, "AI-Cat.exe"),
                target = target,
                options = BuildOptions.None,
            };

            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new BuildFailedException(
                    $"构建失败：result={report.summary.result}，errors={report.summary.totalErrors}");
            }

            Debug.Log($"T0-8 CI 构建完成 → {options.locationPathName}");
        }

        /// <summary>
        /// 无启用场景时，创建并保存一个最小空场景（仅 CI 构建用，正常开发仍走 ChatBootstrap）。
        /// </summary>
        private static string EnsureEmptyScene(string projectRoot)
        {
            const string rel = "Assets/Scenes/CI_Build.unity";
            string dir = Path.GetDirectoryName(Path.Combine(projectRoot, rel))!;
            Directory.CreateDirectory(dir);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorSceneManager.SaveScene(scene, Path.Combine(projectRoot, rel));
            return rel;
        }
    }
}
