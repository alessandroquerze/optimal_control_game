#if UNITY_EDITOR
using NUnit.Framework;
using OptCon.Application;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEditor;
using OptCon.Content;
using UnityEngine.InputSystem;

namespace OptCon.Tests
{
    /// <summary>Existing runtime suites never write the user's actual application profile.</summary>
    [SetUpFixture]
    public sealed class ProgressStoreIsolation
    {
        // Historical suites explicitly exercise the prototype. Production tests use the saved Bootstrap.
        public static bool UseSavedCourse { get; set; }
        private InputSettings originalInput, testInput;
        [OneTimeSetUp] public void Install()
        {
            // A hidden batch Editor has no OS/Game View focus. Keep synthetic test devices enabled;
            // application HUD focus guards still run normally. Never mutate the project settings asset.
            originalInput = InputSystem.settings; testInput = Object.Instantiate(originalInput);
            testInput.hideFlags = HideFlags.HideAndDontSave;
            testInput.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testInput.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testInput;
            SceneManager.sceneLoaded += Configure;
        }
        [OneTimeTearDown] public void Remove()
        {
            SceneManager.sceneLoaded -= Configure;
            InputSystem.settings = originalInput; Object.DestroyImmediate(testInput);
        }
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
