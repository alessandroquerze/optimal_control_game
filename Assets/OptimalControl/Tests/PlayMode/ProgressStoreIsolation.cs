#if UNITY_EDITOR
using NUnit.Framework;
using OptCon.Application;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OptCon.Tests
{
    /// <summary>Existing runtime suites never write the user's actual application profile.</summary>
    [SetUpFixture]
    public sealed class ProgressStoreIsolation
    {
        [OneTimeSetUp] public void Install() => SceneManager.sceneLoaded += Configure;
        [OneTimeTearDown] public void Remove() => SceneManager.sceneLoaded -= Configure;
        private static void Configure(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != "Assets/OptimalControl/Scenes/Bootstrap.unity") return;
            var bootstrap = Object.FindFirstObjectByType<AppBootstrap>();
            if (bootstrap != null && bootstrap.Navigation == null) bootstrap.Configure(store: new MemoryProgressStore());
        }
    }
}
#endif
