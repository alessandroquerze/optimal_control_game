using OptCon.Application;
using OptCon.Content;
using UnityEditor;
using UnityEngine;

namespace OptCon.Editor
{
    /// <summary>Single editor entry point for full catalog/provider/binding validation and localized diagnostics.</summary>
    public static class CourseContentValidator
    {
        public static CatalogValidationResult Validate(CourseDefinition course, CourseRuntimeServices services = null, string projectRoot = null, bool verifySourceFiles = true)
            => CatalogValidator.ValidateExecutionConfiguration(course, projectRoot, verifySourceFiles, services);
        [MenuItem("Tools/Optimal Control/Validate Selected Course/Full F09 Summary")]
        private static void ValidateSelected()
        {
            var course = Selection.activeObject as CourseDefinition;
            var result = Validate(course);
            if (result.IsValid) Debug.Log("F09: catalog, prerequisites, sources, scenes, providers, entry domains and bindings valid. Runtime conditions still require runtime verification.", course);
            else foreach (var issue in result.Issues) Debug.LogError(issue.ToString(), issue.Asset);
            EditorUtility.DisplayDialog("Course validation", result.IsValid ? "All static checks passed." : result.Issues.Count + " issue(s). Console entries identify asset, code and field.", "OK");
        }
    }
}
