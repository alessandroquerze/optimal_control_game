using System;
using System.Collections.Generic;
using System.Linq;
using OptCon.Content;
using UnityEngine.UIElements;

namespace OptCon.Presentation
{
    /// <summary>One presentation policy adapter. Access is also enforced by ILessonRuntime's command boundary.</summary>
    public sealed class LessonStepPresenter : IDisposable
    {
        private readonly VisualElement root;
        private readonly ILessonRuntime lesson;
        private readonly Button next, previous, restart, explore;
        private long generation = -1;
        private readonly List<(Button button, Action handler)> answerCallbacks = new List<(Button, Action)>();
        public LessonStepPresenter(VisualElement root, ILessonRuntime lesson)
        {
            this.root = root; this.lesson = lesson;
            next = root.Q<Button>("lesson-next"); previous = root.Q<Button>("lesson-previous");
            restart = root.Q<Button>("lesson-restart"); explore = root.Q<Button>("lesson-explore");
            next.clicked += Next; previous.clicked += Previous; restart.clicked += Restart; explore.clicked += Explore;
        }
        private void Next() => lesson.Next();
        private void Previous() => lesson.Previous();
        private void Restart() => lesson.Restart();
        private void Explore() => lesson.Explore();
        public void Render(LessonPresentationState state)
        {
            var panel = root.Q<VisualElement>("lesson-progress"); panel.style.display = state.StepCount == 0 ? DisplayStyle.None : DisplayStyle.Flex;
            var step = state.Step;
            root.Q<Label>("lesson-step-title").text = state.IsFreeExploration ? "Esplorazione libera" : state.IsCompleted ? "Percorso del prototipo completato" :
                (state.StepIndex + 1) + "/" + state.StepCount + " · " + step?.Title;
            root.Q<Label>("lesson-step-objective").text = state.IsFreeExploration ? "Prova gli strumenti. Il completamento didattico richiede il percorso guidato." : step?.Objective;
            root.Q<Label>("lesson-instruction").text = state.IsCompleted ? "Hai collegato stato, input, transizione, traiettoria e costo. Questo completa soltanto il prototipo; nessun'altra lezione è completata." : state.IsFreeExploration ? "Reset/R ripristina il preset e rimuove A; Ricomincia torna al percorso guidato." : step?.Instruction;
            root.Q<Label>("lesson-help").text = step?.Help;
            root.Q<VisualElement>("lesson-help-panel").style.display = !state.IsFreeExploration && !string.IsNullOrEmpty(step?.Help) ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<Label>("lesson-question").text = state.IsFreeExploration || state.IsCompleted ? "" : step?.Question;
            root.Q<Label>("lesson-feedback").text = state.Error == null ? state.Feedback ?? "" : state.Error + " Recupera con Ricomincia o Indietro.";
            root.Q<Label>("lesson-checks").text = state.IsFreeExploration || state.IsCompleted ? "" : string.Join(" · ", state.ConditionsMet.Select((met, i) => (met ? "✓" : "○") + " " + (i + 1))) + (state.CanAdvance ? " · Puoi proseguire." : " · Completa le azioni indicate.");
            next.text = state.StepIndex + 1 == state.StepCount ? "Concludi verifica" : "Avanti →";
            next.SetEnabled(state.CanAdvance); previous.SetEnabled(!state.IsFreeExploration && state.StepIndex > 0);
            next.style.display = previous.style.display = state.IsFreeExploration || state.IsCompleted ? DisplayStyle.None : DisplayStyle.Flex;
            explore.SetEnabled(!state.IsFreeExploration && state.Error == null);
            if (generation != state.Generation)
            {
                generation = state.Generation; ClearAnswers();
                if (!state.IsFreeExploration && step != null) foreach (var answer in step.Answers)
                {
                    string id = answer.Id; Action callback = () => lesson.Answer(id);
                    var button = new Button(callback) { name = "answer-" + id, text = answer.Text };
                    button.AddToClassList("lesson-answer"); root.Q<VisualElement>("lesson-answers").Add(button); answerCallbacks.Add((button, callback));
                }
            }
            root.Q<VisualElement>("lesson-answers").style.display = state.IsFreeExploration || state.IsCompleted ? DisplayStyle.None : DisplayStyle.Flex;
            ApplyAccess(state);
        }
        private void ApplyAccess(LessonPresentationState state)
        {
            void Restrict(string name, bool allowed) { if (!allowed) root.Q<VisualElement>(name)?.SetEnabled(false); }
            Restrict("play-pause", state.Allows(LessonControls.Play));
            Restrict("step-back", state.Allows(LessonControls.Step)); Restrict("step-next", state.Allows(LessonControls.Step));
            Restrict("timeline-seek", state.Allows(LessonControls.Seek)); Restrict("playback-rate", state.Allows(LessonControls.PlaybackRate));
            Restrict("horizon", state.Allows(LessonControls.Horizon)); Restrict("input-scope", state.Allows(LessonControls.Input));
            foreach (var field in root.Query<DoubleField>().ToList())
            {
                if (field.name.StartsWith("parameter-", StringComparison.Ordinal)) Restrict(field.name, state.AllowsParameter(field.name.Substring(10)));
                if (field.name.StartsWith("input-", StringComparison.Ordinal)) Restrict(field.name, state.Allows(LessonControls.Input));
            }
            bool modelParameters = root.Q<VisualElement>("parameter-fields").Query<DoubleField>().ToList().Any(field => state.AllowsParameter(field.name.Substring(10)));
            root.Q<VisualElement>("parameter-panel").style.display = state.Allows(LessonControls.Input) || modelParameters || state.Allows(LessonControls.PlaybackRate) ? DisplayStyle.Flex : DisplayStyle.None;
            if (!state.IsFreeExploration && state.Step != null && !state.Step.ShowCosts) root.Q<VisualElement>("cost-panel").style.display = DisplayStyle.None;
            root.Q<VisualElement>("comparison-controls").style.display = root.Q<VisualElement>("comparison-configuration").style.display = state.Allows(LessonControls.Comparison) ? DisplayStyle.Flex : DisplayStyle.None;
            bool costEditing = state.IsFreeExploration || state.Allows(LessonControls.Horizon) || (state.Step != null && state.Step.EditableParameters.Any(t => t.StartsWith("cost.", StringComparison.Ordinal)));
            root.Q<VisualElement>("cost-configuration").style.display = costEditing ? DisplayStyle.Flex : DisplayStyle.None;
            root.Q<DropdownField>("formula-choice").SetEnabled(state.Allows(LessonControls.Formula));
        }
        private void ClearAnswers()
        { foreach (var callback in answerCallbacks) callback.button.clicked -= callback.handler; answerCallbacks.Clear(); root.Q<VisualElement>("lesson-answers").Clear(); }
        public void Dispose()
        { next.clicked -= Next; previous.clicked -= Previous; restart.clicked -= Restart; explore.clicked -= Explore; ClearAnswers(); }
    }
}
