#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class ProductionCostReferenceRuntimeTests
    {
        const string A="Assets/OptimalControl/";
        private AppBootstrap bootstrap;
        private MemoryProgressStore store;
        private LessonHudPresenter Hud=>Object.FindFirstObjectByType<LessonHudPresenter>();
        private VisualElement Root=>Hud.GetComponent<UIDocument>().rootVisualElement;
        private VisualElement Menu=>Object.FindFirstObjectByType<CourseMenuPresenter>().GetComponent<UIDocument>().rootVisualElement;
        private SimulationSession Sim=>bootstrap.ActiveHost.Session;
        private LessonSession Lesson=>bootstrap.ActiveHost.Lesson;
        private ExperimentVisualization View=>bootstrap.ActiveHost.GetComponent<ExperimentVisualization>();
        private static void Click(Button button)
        {
            Assert.That(button,Is.Not.Null); Assert.That(button.enabledInHierarchy,Is.True,button.name);
            using var submit=NavigationSubmitEvent.GetPooled(); submit.target=button; button.SendEvent(submit);
        }
        private IEnumerator Ready()
        {
            float limit=Time.realtimeSinceStartup+20;
            do { yield return null; bootstrap=Object.FindFirstObjectByType<AppBootstrap>(); Assert.That(Time.realtimeSinceStartup,Is.LessThan(limit),bootstrap?.Navigation?.StatusMessage); }
            while(bootstrap?.Navigation==null || !bootstrap.Navigation.IsReady);
            yield return null;
        }
        private IEnumerator RestartApp()
        {
            foreach(var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            void Configure(Scene scene,LoadSceneMode mode)
            { if(scene.path==A+"Scenes/Bootstrap.unity") Object.FindFirstObjectByType<AppBootstrap>().Configure(store:store); }
            SceneManager.sceneLoaded+=Configure;
            try { yield return SceneManager.LoadSceneAsync(A+"Scenes/Bootstrap.unity",LoadSceneMode.Single); }
            finally { SceneManager.sceneLoaded-=Configure; }
            yield return Ready();
        }
        [UnitySetUp] public IEnumerator Setup()
        { ProgressStoreIsolation.UseSavedCourse=true; store=new MemoryProgressStore(); yield return RestartApp(); }
        [UnityTearDown] public IEnumerator Cleanup()
        {
            ProgressStoreIsolation.UseSavedCourse=false;
            foreach(var app in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include,FindObjectsSortMode.None)) Object.Destroy(app.gameObject);
            yield return null;
            foreach(string path in new[]{A+"Scenes/MainMenu.unity",A+"Scenes/LessonWorkspace.unity"})
            { var scene=SceneManager.GetSceneByPath(path); if(scene.IsValid()&&scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        }
        private IEnumerator Open(string id="C1.L3")
        { Click(Menu.Query<Button>().ToList().Single(b=>b.userData as string==id)); yield return Ready(); Assert.That(bootstrap.Navigation.ActiveLesson.Id,Is.EqualTo(id)); }
        private void Select(string term)=>Click(Root.Q<Button>("term-"+term));
        private void Next(string answer) { Click(Root.Q<Button>("answer-"+answer)); Click(Root.Q<Button>("lesson-next")); }
        private void Costs()
        {
            Select("total");Next("sum");Select("error");Select("state-part");Next("deviation");
            Root.Q<DoubleField>("parameter-cost.q.0").value=4;Next("criterion");
            Root.Q<DoubleField>("parameter-cost.r.0").value=.4;Next("weight");
            Root.Q<DoubleField>("parameter-cost.qt.0").value=10;Select("terminal");Next("final");
            Root.Q<DoubleField>("parameter-constraint.torque.limit").value=.5;Next("hard");Select("r");Next("endpoint");
            Root.Q<DropdownField>("input-scope").index=1;Root.Q<DoubleField>("input-0").value=.75;Next("infeasible");
            Root.Q<SliderInt>("timeline-seek").value=20;Select("input.applied");Select("stage");Next("terminal");
        }
        private void ToReferenceEdit()
        {
            Select("reference.state");Select("reference.output");Next("different");Select("desired-h");Next("jump");
            Root.Q<DropdownField>("input-scope").index=1;Root.Q<DoubleField>("input-0").value=1;Click(Root.Q<Button>("step-next"));Select("h");Next("manual");
            Select("error");Select("state-part");Next("same-sample");
        }
        private void Desired()
        {
            ToReferenceEdit();Root.Q<DoubleField>("parameter-cost.reference.state.10.0").value=.8;Select("reference.state");Next("reference");
            Select("g");Next("roles");Root.Q<SliderInt>("timeline-seek").value=20;Select("reference.terminal");Select("input.applied");Next("terminal");
        }
        [UnityTest] public IEnumerator NormalBootstrapExposesBothProductionLessonsWithAdvisoryIds()
        {
            Assert.That(Menu.Query<Button>().ToList().Where(b=>b.userData is string).Select(b=>(string)b.userData),Is.EquivalentTo(new[]{"C1.L1","C1.L2","C1.L3","C1.L4","C1.L5","C1.L6"}));
            Assert.That(Menu.Q<Label>("progress-C1.L3").text,Does.Contain("C1.L2"));Assert.That(Menu.Q<Label>("progress-C1.L4").text,Does.Contain("C1.L3"));
            Assert.That(bootstrap.Navigation.OpenLesson("prototype.pendulum"),Is.False);Assert.That(bootstrap.Navigation.OpenLesson("fixture.f10.static"),Is.False);
            yield return Open();Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("decisions"));Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));Assert.That(store.Load(),Is.Null);
        }
        private void ReachWeight()
        { Select("total");Next("sum");Select("error");Select("state-part");Next("deviation"); }
        private void ReachCostComparison()
        {
            ReachWeight();Root.Q<DoubleField>("parameter-cost.q.0").value=4;Next("criterion");
            Root.Q<DoubleField>("parameter-cost.r.0").value=.4;Next("weight");
            Root.Q<DoubleField>("parameter-cost.qt.0").value=10;Select("terminal");Next("final");
            Root.Q<DoubleField>("parameter-constraint.torque.limit").value=.5;Next("hard");Select("r");Next("endpoint");
        }
        [UnityTest] public IEnumerator WeightEvidenceRejectsWrongValueAndUndoThroughUIAndRecoversAfterPrevious()
        {
            yield return Open();ReachWeight();var baseline=Lesson.ActivityBaseline;
            Click(Root.Q<Button>("answer-criterion"));Root.Q<DoubleField>("parameter-cost.q.0").value=3;
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("parameter-cost.q.0").value=4;Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Root.Q<DoubleField>("parameter-cost.q.0").value=2;Assert.That(Lesson.Presentation.CanAdvance,Is.False);
            Assert.That(Root.Q<Button>("lesson-next").enabledInHierarchy,Is.False);Assert.That(store.Load(),Is.Null);
            Root.Q<DoubleField>("parameter-cost.q.0").value=4;Next("criterion");Click(Root.Q<Button>("lesson-previous"));
            Assert.That(Lesson.ActivityBaseline,Is.Not.SameAs(baseline));Click(Root.Q<Button>("answer-criterion"));
            Assert.That(Lesson.Presentation.CanAdvance,Is.False);Root.Q<DoubleField>("parameter-cost.q.0").value=4;
            Assert.That(Lesson.Presentation.CanAdvance,Is.True);Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator FixedCriterionComparisonBlocksIncreasingCostFeasibleInputAndUndoThenPersistsOnlyPositivePath()
        {
            yield return Open();ReachCostComparison();yield return null;var a=Sim.State.Result;
            Assert.That(Sim.State.Comparison.Result,Is.SameAs(a));Assert.That(Lesson.Presentation.Step.EditableParameters,Is.Empty);
            Assert.That(Root.Q<VisualElement>("comparison-configuration").resolvedStyle.display,Is.EqualTo(DisplayStyle.Flex));
            Assert.That(Root.Q<VisualElement>("comparison-controls").resolvedStyle.display,Is.EqualTo(DisplayStyle.None));
            Root.Q<DropdownField>("input-scope").index=1;Click(Root.Q<Button>("answer-infeasible"));
            Root.Q<DoubleField>("input-0").value=2;Assert.That(Sim.State.Result.Costs.Total,Is.GreaterThan(a.Costs.Total));
            Assert.That(Sim.State.Result.Constraints.PathResiduals[0][0],Is.GreaterThan(0));Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("input-0").value=0;Assert.That(Sim.State.Result.Costs.Total,Is.LessThan(a.Costs.Total));
            Assert.That(Lesson.Presentation.CanAdvance,Is.False,"A cheaper candidate with no violated input bound is not this activity's observation.");
            Root.Q<DoubleField>("input-0").value=.75;Assert.That(Lesson.Presentation.CanAdvance,Is.True);
            Root.Q<DoubleField>("input-0").value=1;Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(store.Load(),Is.Null);
            Root.Q<DoubleField>("input-0").value=.75;var b=Sim.State.Result;
            Assert.That(b.Configuration.Cost,Is.SameAs(a.Configuration.Cost));Assert.That(b.Configuration.Constraints,Is.SameAs(a.Configuration.Constraints));
            Assert.That(b.Costs.Total,Is.EqualTo(4.655611848439829).Within(1e-12));Assert.That(b.Constraints.PathResiduals[0][0],Is.EqualTo(.25));
            Assert.That(Hud.DisplayedState,Is.SameAs(View.DisplayedState));Assert.That(Sim.State.Comparison.Result,Is.SameAs(a));
            Next("infeasible");Root.Q<SliderInt>("timeline-seek").value=20;Select("input.applied");Select("stage");Next("terminal");
            Assert.That(store.Load().Completions.Single().LessonId,Is.EqualTo("C1.L3"));Assert.That(store.SaveCount,Is.EqualTo(1));
            Click(Menu.Q<Button>("return-to-menu"));yield return Ready();yield return Open();
            Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Sim.State.Comparison,Is.Null);Assert.That(store.SaveCount,Is.EqualTo(1));
        }
        [UnityTest] public IEnumerator CompleteBothThroughRealUIAndReloadOnlyPersistentCompletion()
        {
            yield return Open();Costs();Assert.That(Lesson.Presentation.IsCompleted,Is.True);var old=Sim;Click(Menu.Q<Button>("return-to-menu"));yield return Ready();
            Assert.That(old.State.IsClosed,Is.True);Assert.That(Menu.Q<Label>("progress-C1.L4").text,Does.Not.Contain("C1.L3"));
            yield return Open("C1.L4");Desired();Assert.That(Lesson.Presentation.IsCompleted,Is.True);Assert.That(store.Load().Completions.Select(c=>c.LessonId),Is.EquivalentTo(new[]{"C1.L3","C1.L4"}));
            Click(Menu.Q<Button>("return-to-menu"));yield return Ready();yield return Open("C1.L4");
            Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Assert.That(Lesson.Presentation.IsCompleted,Is.False);Assert.That(Sim.State.Comparison,Is.Null);Assert.That(store.SaveCount,Is.EqualTo(2));
            yield return RestartApp();Assert.That(bootstrap.Progress.GetStatus("C1.L3").IsCompleted,Is.True);Assert.That(bootstrap.Progress.GetStatus("C1.L4").IsCompleted,Is.True);
            Assert.That(bootstrap.Progress.GetStatus("C1.L1").IsCompleted,Is.False);Assert.That(bootstrap.Progress.GetStatus("C1.L2").IsCompleted,Is.False);
        }
        [UnityTest] public IEnumerator ReferenceEditPublishesSameRevisionToHUDTargetCostAndDesiredViewAndRetainsA()
        {
            yield return Open("C1.L4");ToReferenceEdit();var a=Sim.State;var desired=View.GetComponentInChildren<ReferenceTrajectoryView>(true);
            Assert.That(desired.DisplayedState,Is.SameAs(a));Select("reference.state");Assert.That(View.GetComponentsInChildren<HighlightTarget>(true).Single(h=>h.SemanticRole=="reference.state").IsHighlighted,Is.True);
            Root.Q<DoubleField>("parameter-cost.reference.state.10.0").value=.8;var b=Sim.State;
            Assert.That(Hud.DisplayedState,Is.SameAs(b));Assert.That(View.DisplayedState,Is.SameAs(b));Assert.That(desired.DisplayedState,Is.SameAs(b));
            Assert.That(Hud.Formula.Resolutions["reference.state"].Value.Vector[0],Is.EqualTo(.8));Assert.That(Hud.Formula.Resolutions["stage"].Value.Scalar,Is.EqualTo(b.Result.Costs.Stages[10].Total));
            Assert.That(desired.Current.transform.localPosition.x,Is.EqualTo(3.2f).Within(1e-6));Assert.That(desired.Current.SampleIndex,Is.EqualTo(10));
            Assert.That(b.Comparison.Result,Is.SameAs(a.Comparison.Result));Assert.That(a.Result.Reference.Reference.States[10][0],Is.EqualTo(.5));
            foreach(var target in View.GetComponentsInChildren<TargetVisualizer>(true)) Assert.That(target.DisplayedResult,Is.SameAs(b.Result));
            Root.Q<SliderInt>("timeline-seek").value=9;Assert.That(Hud.Formula.Resolutions["reference.state"].Value.Vector[0],Is.Zero);Assert.That(desired.Current.transform.localPosition.x,Is.Zero);
            Assert.That(Sim.State.Result,Is.SameAs(b.Result));
        }
        [UnityTest] public IEnumerator TightenedLimitChangesSavedResidualAndRegionWithoutClampingOrChangingCost()
        {
            yield return Open();Select("total");Next("sum");Select("error");Select("state-part");Next("deviation");
            Root.Q<DoubleField>("parameter-cost.q.0").value=4;Next("criterion");Root.Q<DoubleField>("parameter-cost.r.0").value=.4;Next("weight");Root.Q<DoubleField>("parameter-cost.qt.0").value=10;Select("terminal");Next("final");
            var a=Sim.State.Result;var view=View.GetComponentInChildren<ConstraintRegionVisualizer>(true);Assert.That(view.DisplayedResidual,Is.EqualTo(-1));
            Root.Q<DoubleField>("parameter-constraint.torque.limit").value=.5;var b=Sim.State.Result;
            Assert.That(view.DisplayedResidual,Is.EqualTo(.5));Assert.That(view.DisplayedState,Is.SameAs(Sim.State));Assert.That(b.Costs.Total,Is.EqualTo(a.Costs.Total));Assert.That(b.History.Controls[0][0],Is.EqualTo(1));
            Assert.That(Hud.Formula.Resolutions["g"].Value.Scalar,Is.EqualTo(.5));Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available));
            Select("g");Assert.That(View.GetComponentsInChildren<HighlightTarget>(true).Single(h=>h.SemanticRole=="constraint.path").IsHighlighted,Is.True);
        }
        [UnityTest] public IEnumerator WrongNoopPreviousRestartResetAndNavigationNeverSaveIncompleteLesson()
        {
            yield return Open();Click(Root.Q<Button>("answer-wrong"));Select("total");Assert.That(Lesson.Next(),Is.False);Next("sum");Select("error");Select("state-part");Next("deviation");
            Root.Q<DoubleField>("parameter-cost.q.0").value=2;Click(Root.Q<Button>("answer-criterion"));Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("parameter-cost.q.0").value=4;Next("criterion");Click(Root.Q<Button>("lesson-previous"));
            Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("weight"));Assert.That(Lesson.Presentation.CanAdvance,Is.False);Assert.That(Root.Q<DoubleField>("parameter-cost.q.0").value,Is.EqualTo(2));
            Click(Root.Q<Button>("lesson-restart"));Assert.That(Lesson.Presentation.StepIndex,Is.Zero);Click(Root.Q<Button>("reset"));Assert.That(Lesson.Presentation.StepIndex,Is.Zero);
            var old=Sim;Click(Menu.Q<Button>("return-to-menu"));yield return Ready();Assert.That(old.State.IsClosed,Is.True);Assert.That(store.Load(),Is.Null);yield return Open();Assert.That(Lesson.Presentation.StepIndex,Is.Zero);
        }
        [UnityTest] public IEnumerator PreviousRebuildsReferenceComparisonAndDoesNotCreditTheRequestedEdit()
        {
            yield return Open("C1.L4");ToReferenceEdit();Root.Q<DoubleField>("parameter-cost.reference.state.10.0").value=.8;Select("reference.state");Next("reference");
            Click(Root.Q<Button>("lesson-previous"));Assert.That(Lesson.Presentation.Step.Id,Is.EqualTo("change-reference"));Assert.That(Sim.State.Comparison,Is.Not.Null);Assert.That(Sim.State.Sample.Timestep,Is.EqualTo(10));
            Assert.That(Root.Q<DoubleField>("parameter-cost.reference.state.10.0").value,Is.EqualTo(.5));Select("reference.state");Click(Root.Q<Button>("answer-reference"));Assert.That(Lesson.Next(),Is.False);
            Root.Q<DoubleField>("parameter-cost.reference.state.10.0").value=.8;Next("reference");Assert.That(store.Load(),Is.Null);
        }
        [UnityTest] public IEnumerator TerminalKeepsReferenceAndCostButNoInputStageOrResidualTransition()
        {
            yield return Open("C1.L4");Click(Root.Q<Button>("lesson-explore"));Root.Q<SliderInt>("timeline-seek").value=20;Hud.Formula.SetFormula("c1.terminal-tracking");Hud.Formula.Render(Sim.State);
            Assert.That(Hud.Formula.Resolutions["reference.terminal"].Value.Vector[0],Is.EqualTo(.5));Assert.That(Hud.Formula.Resolutions["reference.terminal"].SampleIndex,Is.EqualTo(20));Assert.That(Hud.Formula.Resolutions["terminal"].Value.Scalar,Is.EqualTo(.625));
            foreach(var id in new[]{"input.applied","stage"}) Assert.That(Hud.Formula.Resolutions[id].Validity,Is.EqualTo(MetricValidity.NotApplicable));
            var metrics=new RuntimeMetricResolver();foreach(var id in new[]{"reference.input","constraint.h","constraint.desired-h"}) Assert.That(metrics.Resolve(Sim.State,id,FormulaValueKind.Vector,FormulaScope.CurrentTimestep).Validity,Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Sim.State.Sample.PhysicalTimeSeconds,Is.EqualTo(.4));Assert.That(Sim.State.Sample.DiscretizationStepSeconds,Is.EqualTo(.02));
        }
        [UnityTest] public IEnumerator FailedCostCandidateKeepsWholeValidRevisionAndResetRecovers()
        {
            yield return Open("C1.L4");Click(Root.Q<Button>("lesson-explore"));Root.Q<SliderInt>("timeline-seek").value=10;var a=Sim.State.Result;
            Root.Q<DoubleField>("parameter-cost.r.0").value=0;Assert.That(Sim.State.Error,Is.Not.Null);Assert.That(Sim.State.Result,Is.SameAs(a));Assert.That(Hud.DisplayedState.Result,Is.SameAs(a));Assert.That(View.DisplayedState.Result,Is.SameAs(a));
            Assert.That(Root.Q<DoubleField>("input-0").enabledInHierarchy,Is.False);Assert.That(Sim.Execute(SimulationCommand.SetInput(10,new NumericVector(2))),Is.False);
            Click(Root.Q<Button>("reset"));Assert.That(Sim.State.Error,Is.Null);Assert.That(View.Status.Availability,Is.EqualTo(VisualizationAvailability.Available));Assert.That(store.Load(),Is.Null);
        }
    }
}
#endif
