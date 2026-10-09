using OptCon.Content;
using UnityEngine;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>Catalog menu and editorial lesson overview. Commands use only the Content contract.</summary>
    [RequireComponent(typeof(UIDocument)), DisallowMultipleComponent]
    public sealed class CourseMenuPresenter : MonoBehaviour
    {
        private ICourseNavigation navigation;
        private bool subscribed;
        private Button backButton;

        public void Bind(ICourseNavigation value)
        {
            Unsubscribe();
            navigation = value;
            if (isActiveAndEnabled) Subscribe();
            Render();
        }

        private void OnEnable() { Subscribe(); Render(); }

        private void OnDisable()
        {
            Unsubscribe();
            if (backButton != null) backButton.clicked -= ReturnToMenu;
            backButton = null;
        }

        private void Subscribe()
        {
            if (navigation == null || subscribed) return;
            navigation.Changed += Render;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (navigation != null && subscribed) navigation.Changed -= Render;
            subscribed = false;
        }

        private void ReturnToMenu() => navigation?.ReturnToMenu();

        private void Render()
        {
            if (navigation == null || !isActiveAndEnabled) return;
            var root = GetComponent<UIDocument>().rootVisualElement;
            var menu = root.Q<VisualElement>("menu-panel");
            var overview = root.Q<VisualElement>("lesson-panel");
            if (menu == null || overview == null)
            {
                Debug.LogError("CourseMenuPresenter requires the CourseMenu visual tree.", this);
                return;
            }
            bool inLesson = navigation.ActiveLesson != null;
            menu.style.display = inLesson ? DisplayStyle.None : DisplayStyle.Flex;
            overview.style.display = inLesson ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<VisualElement>("shell").EnableInClassList("workspace", inLesson);
            root.Q<Label>("navigation-status").text = navigation.StatusMessage;
            if (inLesson)
            {
                root.Q<Label>("lesson-title").text = navigation.ActiveLesson.Title;
                root.Q<Label>("lesson-objective").text = navigation.ActiveLesson.Objective;
                root.Q<Label>("lesson-description").text = navigation.ActiveLesson.Description;
                var button = root.Q<Button>("return-to-menu");
                if (backButton != button)
                {
                    if (backButton != null) backButton.clicked -= ReturnToMenu;
                    backButton = button;
                    backButton.clicked += ReturnToMenu;
                }
                backButton.SetEnabled(!navigation.IsTransitioning);
            }
            else
            {
                root.Q<Label>("course-title").text = navigation.Course.Title;
                root.Q<Label>("course-description").text = navigation.Course.Description;
                root.Q<Label>("catalog-kind").text = navigation.Course.IsPrototype ? "CATALOGO DI PROTOTIPO" : "CORSO";
                var list = root.Q<VisualElement>("chapter-list");
                list.Clear();
                foreach (var chapter in navigation.Course.Chapters)
                {
                    var section = new VisualElement();
                    section.AddToClassList("chapter");
                    var title = new Label(chapter.Title);
                    title.AddToClassList("chapter-title");
                    section.Add(title);
                    foreach (var lesson in chapter.Lessons)
                    {
                        string id = lesson.Id;
                        var button = new Button(() => navigation.OpenLesson(id)) { text = lesson.Title, userData = id };
                        button.AddToClassList("lesson-button");
                        button.SetEnabled(!navigation.IsTransitioning);
                        section.Add(button);
                    }
                    list.Add(section);
                }
            }
        }
    }
}
