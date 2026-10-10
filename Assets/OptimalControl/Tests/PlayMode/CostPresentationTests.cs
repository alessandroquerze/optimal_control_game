#if UNITY_EDITOR
using System.Collections;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Presentation;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class CostPresentationTests
    {
        private AppBootstrap bootstrap;
        private LessonHudPresenter Hud => Object.FindFirstObjectByType<LessonHudPresenter>();
        private SimulationSession Session => bootstrap.ActiveHost.Session;
        private VisualElement Root => Hud.GetComponent<UIDocument>().rootVisualElement;
        [UnitySetUp] public IEnumerator SetUp()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null; yield return SceneManager.LoadSceneAsync("Assets/OptimalControl/Scenes/Bootstrap.unity", LoadSceneMode.Single); yield return Ready();
            Assert.That(bootstrap.Navigation.OpenLesson(bootstrap.Navigation.Course.Chapters[0].Lessons[0].Id), Is.True); yield return Ready();
            bootstrap.ActiveHost.Lesson.Explore();
        }
        private IEnumerator Ready()
        {
            float end = Time.realtimeSinceStartup + 20;
            do { yield return null; Assert.That(Time.realtimeSinceStartup, Is.LessThan(end)); bootstrap = Object.FindFirstObjectByType<AppBootstrap>(); }
            while (bootstrap == null || bootstrap.Navigation == null || !bootstrap.Navigation.IsReady);
        }
        [UnityTearDown] public IEnumerator TearDown()
        {
            foreach (var old in Object.FindObjectsByType<AppBootstrap>(FindObjectsInactive.Include, FindObjectsSortMode.None)) Object.Destroy(old.gameObject);
            yield return null;
            foreach (var path in new[] { "Assets/OptimalControl/Scenes/MainMenu.unity", "Assets/OptimalControl/Scenes/LessonWorkspace.unity" }) { var scene = SceneManager.GetSceneByPath(path); if (scene.IsValid() && scene.isLoaded) yield return SceneManager.UnloadSceneAsync(scene); }
        }
        private void CostFormula() { var choice = Root.Q<DropdownField>("formula-choice"); choice.value = choice.choices.Single(c => c.Contains("cost.additive")); }
        private void Submit(string name) { var button = Root.Q<Button>(name); using (var e = NavigationSubmitEvent.GetPooled()) { e.target = button; button.SendEvent(e); } }
        [UnityTest] public IEnumerator CostHudGraphAndTargetsUseTheSameRevisionBeforeAnySeek()
        {
            CostFormula(); var result = Session.State.Result; var graph = bootstrap.ActiveHost.GetComponentInChildren<CostContributionVisualizer>();
            Assert.That(graph.DisplayedResult, Is.SameAs(result)); Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(result));
            Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar, Is.EqualTo(result.Costs.Total));
            Assert.That(graph.GetComponentsInChildren<TextMesh>().Single(t => t.name == "TotalLabel").text,
                Does.Contain("J=" + result.Costs.Total.ToString("G6", System.Globalization.CultureInfo.InvariantCulture) + " [1]"));
            Assert.That(graph.StageLine.positionCount, Is.EqualTo(100)); Assert.That(graph.CumulativeLine.positionCount, Is.EqualTo(101));
            for (int t = 0; t < 100; t++) Assert.That(graph.StageLine.GetPosition(t).y, Is.EqualTo((float)(result.Costs.Stages[t].Total / graph.ContributionScaleMaximum)).Within(1e-6));
            for (int t = 0; t <= 100; t++) Assert.That(graph.CumulativeLine.GetPosition(t).y, Is.EqualTo(1.9f + (float)(result.Costs.Cumulative[t] / graph.CumulativeScaleMaximum)).Within(1e-6));
            Assert.That(graph.Terminal.SampleIndex, Is.EqualTo(100));
            foreach (var view in bootstrap.ActiveHost.GetComponentsInChildren<TargetVisualizer>(true)) { Assert.That(view.DisplayedResult, Is.SameAs(result)); Assert.That(view.Target.SampleIndex, Is.Zero); Assert.That(view.Terminal.SampleIndex, Is.EqualTo(100)); }
            yield return null;
        }
        [UnityTest] public IEnumerator TerminalSelectionHighlightsTheFinalStateWhileCursorIsElsewhere()
        {
            Session.Execute(SimulationCommand.Seek(7)); CostFormula(); Submit("term-cost.terminal");
            Assert.That(Session.State.Sample.Timestep, Is.EqualTo(7)); Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TerminalCost].SampleIndex, Is.EqualTo(100));
            var targets = bootstrap.ActiveHost.GetComponentsInChildren<HighlightTarget>(true).Where(t => t.SemanticRole == "state.terminal").ToArray();
            Assert.That(targets.Length, Is.EqualTo(3)); Assert.That(targets.All(t => t.IsHighlighted && t.SampleIndex == 100 && t.RevisionId == Session.State.Result.RevisionId), Is.True);
            Submit("term-state.terminal"); Assert.That(Hud.Formula.Resolutions["state.terminal"].Value.Vector, Is.SameAs(Session.State.Result.History.States[100])); yield return null;
        }
        [UnityTest] public IEnumerator WeightTargetAndInputEditsUpdateCostsButDoNotDirtyAssetsOrReinterpretA()
        {
            var experiment = bootstrap.Navigation.ActiveLesson.Experiment; string before = EditorJsonUtility.ToJson(experiment), costBefore = EditorJsonUtility.ToJson(experiment.Cost);
            Submit("comparison-save"); var a = Session.State.Comparison.Result;
            Root.Q<DoubleField>("parameter-cost.q.0").value = 30; var b = Session.State.Result;
            for (int t = 0; t <= 100; t++) Assert.That(b.History.States[t], Is.SameAs(a.History.States[t]));
            Assert.That(b.Costs.Total, Is.Not.EqualTo(a.Costs.Total)); Assert.That(Session.State.Comparison.Result, Is.SameAs(a));
            var renderer = bootstrap.ActiveHost.GetComponentInChildren<ComparisonTrajectoryRenderer>(); Assert.That(renderer.DisplayedComparison.Result, Is.SameAs(a)); Assert.That(renderer.DisplayedResult, Is.SameAs(b));
            Assert.That(renderer.GetComponentsInChildren<TrajectoryRenderer>().Single(t => t.name == "RetainedA").Source, Is.SameAs(a.History));
            Root.Q<DoubleField>("parameter-cost.target.0").value = .2;
            Assert.That(Session.State.Result.Configuration.ReferenceState[0], Is.EqualTo(.2));
            Root.Q<DoubleField>("input-0").value = 1; Assert.That(Session.State.Result.Costs.Stages[0].Effort, Is.EqualTo(.05).Within(1e-12));
            CostFormula(); Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar, Is.EqualTo(Session.State.Result.Costs.Total));
            Assert.That(Root.Q<Label>("comparison-status").text, Does.Contain("cost.q.0=10")); Assert.That(Root.Q<Label>("comparison-status").text, Does.Contain("cost.q.0=30"));
            Assert.That(EditorJsonUtility.ToJson(experiment), Is.EqualTo(before)); Assert.That(EditorJsonUtility.ToJson(experiment.Cost), Is.EqualTo(costBefore)); yield return null;
        }
        [UnityTest] public IEnumerator TerminalAndShorterHorizonHaveNoInventedStageOrInputAndCompareCommonSamples()
        {
            Submit("comparison-save"); Root.Q<IntegerField>("horizon").value = 2; Root.Q<SliderInt>("timeline-seek").value = 2; CostFormula();
            Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.StageCost].Validity, Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions[SimulationMetricIds.AppliedInput].Validity, Is.EqualTo(MetricValidity.NotApplicable));
            Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TerminalCost].Validity, Is.EqualTo(MetricValidity.Available));
            Assert.That(Hud.Formula.Resolutions["state.terminal"].SampleIndex, Is.EqualTo(2));
            Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.CumulativeCost].Value.Scalar + Session.State.Result.Costs.Terminal.Total, Is.EqualTo(Session.State.Result.Costs.Total));
            var comparison = bootstrap.ActiveHost.GetComponentInChildren<ComparisonTrajectoryRenderer>(); foreach (var line in comparison.GetComponentsInChildren<LineRenderer>()) Assert.That(line.positionCount, Is.EqualTo(3));
            var graph = bootstrap.ActiveHost.GetComponentInChildren<CostContributionVisualizer>(); Assert.That(graph.StageLine.positionCount, Is.EqualTo(2)); Assert.That(graph.Terminal.SampleIndex, Is.EqualTo(2)); yield return null;
        }
        [UnityTest] public IEnumerator CostFailureKeepsHudGraphAndWholeValidResult()
        {
            CostFormula(); var valid = Session.State.Result; Assert.That(Session.Execute(SimulationCommand.SetParameter("cost.q.0", double.MaxValue)), Is.False);
            Assert.That(Hud.Formula.DisplayedResult, Is.SameAs(valid)); Assert.That(bootstrap.ActiveHost.GetComponentInChildren<CostContributionVisualizer>().DisplayedResult, Is.SameAs(valid));
            Assert.That(Session.State.Result.Costs, Is.SameAs(valid.Costs)); Assert.That(Root.Q<Label>("timeline-status").text, Does.Contain("rifiutato")); yield return null;
        }
        [UnityTest] public IEnumerator IndexOnlyCostHudDisplaysThirtyEightAndTerminalWithoutSeconds()
        {
            var lesson = Object.Instantiate(bootstrap.Navigation.ActiveLesson); var so = new SerializedObject(lesson); var formulas = so.FindProperty("formulas"); formulas.arraySize = 1; formulas.GetArrayElementAtIndex(0).objectReferenceValue = AssetDatabase.LoadAssetAtPath<FormulaDefinition>("Assets/OptimalControl/Data/Formulas/AdditiveCost.asset"); so.ApplyModifiedPropertiesWithoutUndo();
            var cost = new QuadraticTrackingCost(new NumericVector(0), new NumericVector(0), new NumericVector(2), new NumericVector(2), new NumericVector(2), new[] { "1" }, new[] { "1" });
            using (var session = new SimulationSession(new SimulationConfiguration("fixture.cost", new IndexOnlyConfiguration(), new NumericVector(2), new[] { new NumericVector(3) }, cost: cost)))
            {
                try { Hud.Bind(lesson, session, null); Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TotalCost].Value.Scalar, Is.EqualTo(38)); Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.StageCost].Value.Scalar, Is.EqualTo(13)); Assert.That(session.State.Sample.PhysicalTimeSeconds, Is.Null); Root.Q<SliderInt>("timeline-seek").value = 1; Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.StageCost].Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(Hud.Formula.Resolutions[RuntimeMetricIds.TerminalCost].Value.Scalar, Is.EqualTo(25)); Assert.That(session.State.Sample.DiscretizationStepSeconds, Is.Null); }
                finally { Hud.Bind(null, null, null); Object.Destroy(lesson); }
            }
            yield return null;
        }
        [UnityTest] public IEnumerator ClosingReopeningAndDisableReleaseComparisonAndHudCallbacks()
        {
            Submit("comparison-save"); var old = Session; Hud.enabled = false; Hud.enabled = true; Submit("comparison-clear"); Assert.That(Session.State.Comparison, Is.Null); Submit("comparison-save");
            bootstrap.Navigation.ReturnToMenu(); yield return Ready(); Assert.That(old.State.IsClosed, Is.True); Assert.That(old.State.Comparison, Is.Null);
            bootstrap.Navigation.OpenLesson(bootstrap.Navigation.Course.Chapters[0].Lessons[0].Id); yield return Ready(); Assert.That(Session.State.Comparison, Is.Null); Assert.That(Session.State.Result.Configuration.Cost.Parameters.Single(p => p.Target == "cost.q.0").Value, Is.EqualTo(10));
        }
        [UnityTest]
        public IEnumerator ComparisonOfDifferentPhysicalStepsUsesTheCommonPhysicalInterval()
        {
            Session.Execute(SimulationCommand.CaptureComparison());
            var a = Session.State.Comparison.Result;
            Assert.That(Session.Execute(SimulationCommand.SetPhysicalStep(.04)), Is.True);
            var b = Session.State.Result;
            var comparison = bootstrap.ActiveHost.GetComponentInChildren<ComparisonTrajectoryRenderer>();
            var lines = comparison.GetComponentsInChildren<TrajectoryRenderer>();
            var saved = lines.Single(line => line.name == "RetainedA");
            var current = lines.Single(line => line.name == "CurrentB");
            Assert.That(saved.Source, Is.SameAs(a.History));
            Assert.That(current.Source, Is.SameAs(b.History));
            Assert.That(saved.GetComponent<LineRenderer>().positionCount, Is.EqualTo(101));
            Assert.That(current.GetComponent<LineRenderer>().positionCount, Is.EqualTo(51));
            Assert.That(a.History.PhysicalTimeAt(100), Is.EqualTo(b.History.PhysicalTimeAt(50)));
            Assert.That(a.Costs.Configuration, Is.SameAs(a.Configuration.Cost));
            Assert.That(b.Costs.Configuration, Is.SameAs(b.Configuration.Cost));
            yield return null;
        }
    }
}
#endif
