#if UNITY_EDITOR
using NUnit.Framework;
using OptCon.Application;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using OptCon.Content;

namespace OptCon.Tests
{
    /// <summary>Existing runtime suites never write the user's actual application profile.</summary>
    [SetUpFixture]
    public sealed class ProgressStoreIsolation
    {
        // Historical suites explicitly exercise the prototype. Production tests use the saved Bootstrap.
        public static bool UseSavedCourse { get; set; }
        [OneTimeSetUp] public void Install() => SceneManager.sceneLoaded += Configure;
        [OneTimeTearDown] public void Remove() => SceneManager.sceneLoaded -= Configure;
        private static void Configure(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != "Assets/OptimalControl/Scenes/Bootstrap.unity") return;
            var bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
            if (bootstrap != null && bootstrap.Navigation == null)
            {
                if (!UseSavedCourse)
                {
                    var serialized = new SerializedObject(bootstrap);
                    serialized.FindProperty("course").objectReferenceValue = AssetDatabase.LoadAssetAtPath<CourseDefinition>("Assets/OptimalControl/Data/Courses/PrototypeCourse.asset");
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }
                bootstrap.Configure(store: new MemoryProgressStore());
            }
        }
    }
}
#endif
