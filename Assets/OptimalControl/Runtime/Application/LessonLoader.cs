using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OptCon.Application
{
    /// <summary>Owns one additive content scene. F02 loads only the workspace, not a model or scenario.</summary>
    [DisallowMultipleComponent]
    public sealed class LessonLoader : MonoBehaviour
    {
        public bool IsLoading { get; private set; }
        public Scene CurrentScene { get; private set; }
        public event Action<Scene> SceneReady;

        public bool TryNavigate(string destinationPath, Action<bool, string> completed, out string error)
        {
            error = null;
            if (!isActiveAndEnabled) error = "Il servizio di navigazione non è attivo.";
            else if (IsLoading) error = "Un caricamento è già in corso.";
            else if (string.IsNullOrWhiteSpace(destinationPath) || !UnityEngine.Application.CanStreamedLevelBeLoaded(destinationPath))
                error = $"Scena assente o disabilitata nei Build Settings: '{destinationPath}'.";
            if (error != null) return false;
            IsLoading = true;
            StartCoroutine(Navigate(destinationPath, completed));
            return true;
        }

        private IEnumerator Navigate(string path, Action<bool, string> completed)
        {
            Scene previous = CurrentScene;
            string previousPath = previous.IsValid() ? previous.path : null;
            if (previous.IsValid() && previous.isLoaded && previous.path == path)
            {
                Finish(true, null, completed);
                yield break;
            }
            // Unload before loading to keep at most one additive content scene.
            if (previous.IsValid() && previous.isLoaded)
            {
                var unloading = SceneManager.UnloadSceneAsync(previous);
                if (unloading != null) yield return unloading;
                if (previous.isLoaded)
                {
                    Finish(false, "Impossibile chiudere la scena corrente.", completed);
                    yield break;
                }
            }
            CurrentScene = default;
            var loading = BeginLoad(path, out string error);
            if (loading != null) yield return loading;
            Scene destination = SceneManager.GetSceneByPath(path);
            if (destination.IsValid() && destination.isLoaded)
            {
                CurrentScene = destination;
                SceneManager.SetActiveScene(destination);
                Finish(true, null, completed);
                yield break;
            }
            // Preserve the prior navigation state if a load fails after successful preflight.
            if (!string.IsNullOrEmpty(previousPath))
            {
                var recovery = BeginLoad(previousPath, out string recoveryError);
                if (recovery != null) yield return recovery;
                var restored = SceneManager.GetSceneByPath(previousPath);
                if (restored.IsValid() && restored.isLoaded)
                {
                    CurrentScene = restored;
                    SceneManager.SetActiveScene(restored);
                }
                else error += " Ripristino della scena precedente fallito: " + recoveryError;
            }
            Finish(false, error ?? $"Caricamento della scena fallito: '{path}'.", completed);
        }

        private static AsyncOperation BeginLoad(string path, out string error)
        {
            error = null;
            try
            {
                var existing = SceneManager.GetSceneByPath(path);
                if (existing.IsValid() && existing.isLoaded) return null;
                var operation = SceneManager.LoadSceneAsync(path, LoadSceneMode.Additive);
                if (operation == null) error = $"Impossibile avviare il caricamento: '{path}'.";
                return operation;
            }
            catch (Exception exception) when (exception is ArgumentException || exception is InvalidOperationException)
            {
                error = $"Caricamento fallito: '{path}'. {exception.Message}";
                return null;
            }
        }

        private void Finish(bool success, string error, Action<bool, string> completed)
        {
            IsLoading = false;
            completed?.Invoke(success, error);
            if (CurrentScene.IsValid() && CurrentScene.isLoaded) SceneReady?.Invoke(CurrentScene);
        }

        private void OnDestroy()
        {
            StopAllCoroutines();
            IsLoading = false;
            SceneReady = null;
        }
    }
}
