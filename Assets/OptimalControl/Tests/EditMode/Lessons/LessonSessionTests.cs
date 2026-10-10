using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using OptCon.Application;
using OptCon.Content;
using OptCon.Core;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace OptCon.Tests
{
    public sealed class LessonSessionTests
    {
        private readonly List<LessonDefinition> assets = new List<LessonDefinition>();
        private readonly List<IDisposable> runtimes = new List<IDisposable>();
        private LessonDefinition Definition(string steps)
        {
            var lesson = Object.Instantiate(AssetDatabase.LoadAssetAtPath<LessonDefinition>("Assets/OptimalControl/Data/Lessons/Prototype/PendulumSlice.asset"));
            JsonUtility.FromJsonOverwrite("{\"id\":\"fixture.educational\",\"steps\":" + steps + "}", lesson); assets.Add(lesson); return lesson;
        }
        private LessonSession Open(string steps, out SimulationSession simulation)
        { simulation = IndexOnlyConfiguration.CreateSession(); runtimes.Add(simulation); var lesson = new LessonSession(Definition(steps), simulation); runtimes.Add(lesson); return lesson; }
        private const string Plain = "{\"id\":\"one\",\"instruction\":\"Observe.\",\"enabledControlIds\":[\"step\",\"seek\",\"input\",\"comparison\"]}";
        [Test] public void AF08_03_FailedEntryCannotAdvanceOrComplete()
        {
            var lesson = Open("[" + Plain + ",{\"id\":\"bad\",\"instruction\":\"Terminal.\",\"entryActions\":[{\"kind\":4,\"index\":2}],\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]", out var simulation);
            Assert.DoesNotThrow(() => Assert.That(lesson.Next(), Is.False));
            Assert.That(lesson.Presentation.StepIndex, Is.Zero);
            Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(lesson.Next(), Is.False);
            Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(simulation.State.Sample.Timestep, Is.Zero);
            Assert.That(lesson.Presentation.Error, Does.Contain("Seek"));
            Assert.That(lesson.Explore(), Is.False);
            Assert.That(lesson.Restart(), Is.True);
            Assert.That(lesson.Presentation.Error, Is.Null);
        }
        [TestCase(false, false)] [TestCase(false, true)] [TestCase(true, false)] [TestCase(true, true)]
        public void AF08_03_FailedIntermediateOrLastEntryKeepsCommittedIdentityAndSupportsRecovery(bool pendulum, bool intermediate)
        {
            var experiment = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>("Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset");
            var simulation = pendulum ? experiment.Model.CreateSession(experiment) : IndexOnlyConfiguration.CreateSession(); runtimes.Add(simulation);
            int invalid = simulation.State.Sample.Horizon + 1;
            string bad = "{\"id\":\"bad\",\"instruction\":\"Invalid entry.\",\"entryActions\":[{\"kind\":4,\"index\":" + invalid + "}],\"conditions\":[{\"evaluatorId\":\"terminal\"}]}";
            var lesson = new LessonSession(Definition("[" + Plain + "," + Plain.Replace("one", "two") + "," + bad + (intermediate ? "," + Plain.Replace("one", "four") : "") + "]"), simulation); runtimes.Add(lesson);
            Assert.That(lesson.Next(), Is.True); var committed = lesson.Presentation.Step;
            Assert.That(lesson.Next(), Is.False);
            Assert.That(lesson.Presentation.StepIndex, Is.EqualTo(1)); Assert.That(lesson.Presentation.Step, Is.SameAs(committed));
            Assert.That(lesson.Presentation.Error, Does.Contain("bad")); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(simulation.State.Sample.IsTerminal, Is.False);
            for (int i = 0; i < 4; i++) Assert.That(lesson.Next(), Is.False);
            Assert.That(lesson.Presentation.IsCompleted, Is.False); Assert.That(lesson.Execute(SimulationCommand.Seek(1)), Is.False);
            Assert.That(lesson.Previous(), Is.True); Assert.That(lesson.Presentation.StepIndex, Is.Zero); Assert.That(lesson.Presentation.Error, Is.Null);
            lesson.Next(); lesson.Next(); Assert.That(lesson.Restart(), Is.True); Assert.That(lesson.Presentation.StepIndex, Is.Zero);
            Assert.That(lesson.Presentation.Error, Is.Null); Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(simulation.State.Sample.DiscretizationStepSeconds, pendulum ? Is.EqualTo(.02) : Is.Null);
        }
        [Test] public void AF08_03_RuntimeDomainChangeAndPartialEntryDoNotAwardProgress()
        {
            var lesson = Open("[" + Plain + ",{\"id\":\"bad\",\"instruction\":\"Partial entry.\",\"entryActions\":[{\"kind\":6,\"index\":0,\"vector\":[4]},{\"kind\":4,\"index\":2}],\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]", out var simulation);
            Assert.That(lesson.Next(), Is.False);
            Assert.That(simulation.State.Result.History.States[1][0], Is.EqualTo(6), "Successful mathematical edits are retained, not rolled back by the educational layer.");
            Assert.That(lesson.Presentation.StepIndex, Is.Zero); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(lesson.Previous(), Is.False); Assert.That(lesson.Restart(), Is.True);
            Assert.That(simulation.State.Result.History.States[1][0], Is.EqualTo(5));
        }
        [TestCase(false)] [TestCase(true)] public void AF08_03_StaticallyValidEntryCanFailAfterRuntimeHorizonEdit(bool pendulum)
        {
            var experiment = AssetDatabase.LoadAssetAtPath<ExperimentDefinition>("Assets/OptimalControl/Data/Experiments/PendulumPrototype.asset");
            var simulation = pendulum ? experiment.Model.CreateSession(experiment) : IndexOnlyConfiguration.CreateSession(horizon: 3); runtimes.Add(simulation);
            int presetT = simulation.State.Sample.Horizon;
            string terminal = "{\"id\":\"terminal\",\"instruction\":\"Enter terminal.\",\"entryActions\":[{\"kind\":4,\"index\":" + presetT + "}],\"conditions\":[{\"evaluatorId\":\"terminal\"}]}";
            var lesson = new LessonSession(Definition("[" + Plain + "," + terminal + "]"), simulation); runtimes.Add(lesson);
            simulation.Execute(SimulationCommand.SetHorizon(1));
            Assert.That(lesson.Next(), Is.False); Assert.That(lesson.Presentation.Error, Does.Contain("Seek"));
            Assert.That(lesson.Next(), Is.False); Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(lesson.Restart(), Is.True); Assert.That(simulation.State.Sample.Horizon, Is.EqualTo(presetT));
            Assert.That(lesson.Next(), Is.True); Assert.That(simulation.State.Sample.IsTerminal, Is.True);
            Assert.That(lesson.Presentation.CanAdvance, Is.True); Assert.That(lesson.Next(), Is.True); Assert.That(lesson.Presentation.IsCompleted, Is.True);
        }
        [TearDown] public void Cleanup() { for (int i = runtimes.Count - 1; i >= 0; i--) runtimes[i].Dispose(); runtimes.Clear(); foreach (var asset in assets) Object.DestroyImmediate(asset); assets.Clear(); }

        [Test] public void ProductionLessonCompilesSevenFrozenStepsWithoutWritingAssets()
        {
            var definition = AssetDatabase.LoadAssetAtPath<LessonDefinition>("Assets/OptimalControl/Data/Lessons/Prototype/PendulumSlice.asset");
            string before = EditorJsonUtility.ToJson(definition); var plan = new LessonPlan(definition);
            Assert.That(plan.Steps.Count, Is.EqualTo(7)); Assert.That(plan.Steps[0].FormulaId, Is.EqualTo(definition.Formulas[0].Id));
            Assert.That(plan.Steps[4].ShowCosts, Is.True); Assert.That(plan.Steps[4].EditableParameters, Is.EqualTo(new[] { "cost.q.0" }));
            Assert.That(EditorJsonUtility.ToJson(definition), Is.EqualTo(before));
            Assert.Throws<NotSupportedException>(() => ((IList<LessonStepPlan>)plan.Steps).Clear());
        }
        [Test] public void SequenceRequiresExplicitAdvanceAndCompletesOnlyItsOwnLesson()
        {
            var lesson = Open("[" + Plain + "," + Plain.Replace("one", "two") + "]", out var simulation);
            Assert.That(lesson.Presentation.StepIndex, Is.Zero); Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(lesson.Next(), Is.True); Assert.That(lesson.Presentation.StepIndex, Is.EqualTo(1));
            Assert.That(lesson.Next(), Is.True); Assert.That(lesson.Presentation.IsCompleted, Is.True); Assert.That(lesson.Presentation.LessonId, Is.EqualTo("fixture.educational"));
            Assert.That(lesson.Next(), Is.False); Assert.That(simulation.State.Sample.PhysicalTimeSeconds, Is.Null);
        }
        [Test] public void EmptyStepsRemainExploratoryWithoutInventingCompletion()
        {
            var lesson = Open("[]", out var simulation);
            Assert.That(lesson.Presentation.IsFreeExploration, Is.True); Assert.That(lesson.Next(), Is.False); Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(lesson.Execute(SimulationCommand.StepForward()), Is.True); Assert.That(simulation.State.Sample.CurrentState[0], Is.EqualTo(5));
        }
        [Test] public void ForbiddenCommandsCannotBypassTheSharedPolicy()
        {
            var lesson = Open("[{\"id\":\"observe\",\"instruction\":\"Read only.\"}]", out var simulation);
            var result = simulation.State.Result;
            Assert.That(lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(8))), Is.False);
            Assert.That(lesson.Execute(SimulationCommand.CaptureComparison()), Is.False); Assert.That(lesson.Execute(SimulationCommand.Play()), Is.False);
            Assert.That(lesson.Execute(SimulationCommand.Seek(1)), Is.False); Assert.That(simulation.State.Result, Is.SameAs(result));
            Assert.That(lesson.Execute(SimulationCommand.Pause()), Is.True); Assert.That(lesson.Execute(SimulationCommand.Reset()), Is.True);
        }
        [Test] public void TermConditionCountsUserSelectionNotTheAuthoredFocus()
        {
            var lesson = Open("[{\"id\":\"select\",\"instruction\":\"Select x.\",\"selectedTermId\":\"state.current\",\"visibleTermIds\":[\"state.current\"],\"conditions\":[{\"evaluatorId\":\"term\",\"targetId\":\"state.current\"}]}]",out _);
            Assert.That(lesson.Presentation.CanAdvance, Is.False);
            lesson.RecordTermSelection("unrelated.formula", "state.current");
            Assert.That(lesson.Presentation.CanAdvance, Is.False);
            lesson.RecordTermSelection(lesson.Presentation.Step.FormulaId, "state.current");
            Assert.That(lesson.Presentation.CanAdvance, Is.True);
            Assert.That(lesson.Restart(), Is.True); lesson.RecordTermSelection("any", "state.next"); Assert.That(lesson.Presentation.CanAdvance, Is.False);
        }
        [Test] public void InputConditionRequiresAnActualSuccessfulChange()
        {
            var lesson = Open("[{\"id\":\"edit\",\"instruction\":\"Edit.\",\"enabledControlIds\":[\"input\"],\"conditions\":[{\"evaluatorId\":\"action\",\"targetId\":\"input.changed\"}]}]",out _);
            Assert.That(lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(3))), Is.True); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(4))), Is.True); Assert.That(lesson.Presentation.CanAdvance, Is.True);
        }
        [Test] public void InvalidActionDoesNotCreditCompletion()
        {
            var lesson = Open("[{\"id\":\"edit\",\"instruction\":\"Edit.\",\"enabledControlIds\":[\"input\"],\"conditions\":[{\"evaluatorId\":\"action\",\"targetId\":\"input.changed\"}]}]",out var simulation);
            var result = simulation.State.Result; Assert.That(lesson.Execute(SimulationCommand.SetInput(99,new NumericVector(4))), Is.False);
            Assert.That(simulation.State.Result, Is.SameAs(result)); Assert.That(lesson.Presentation.CanAdvance, Is.False);
        }
        [Test] public void AnswerRequiresADeclaredChoiceAndCanBeCorrected()
        {
            var lesson = Open("[{\"id\":\"question\",\"instruction\":\"Explain.\",\"answers\":[{\"id\":\"wrong\",\"text\":\"Wrong\",\"feedback\":\"Try again\"},{\"id\":\"right\",\"text\":\"Right\",\"feedback\":\"Correct\"}],\"conditions\":[{\"evaluatorId\":\"answer\",\"expectedAnswerId\":\"right\"}]}]",out _);
            Assert.That(lesson.Answer("unknown"), Is.False); Assert.That(lesson.Answer("wrong"), Is.True); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            Assert.That(lesson.Presentation.Feedback, Is.EqualTo("Try again")); Assert.That(lesson.Answer("right"), Is.True); Assert.That(lesson.Presentation.CanAdvance, Is.True);
            lesson.Answer("wrong"); Assert.That(lesson.Presentation.CanAdvance, Is.False);
        }
        [Test] public void TerminalConditionTracksTheSavedIndexWithoutPhysicalTime()
        {
            var lesson = Open("[{\"id\":\"terminal\",\"instruction\":\"Seek T.\",\"enabledControlIds\":[\"seek\"],\"conditions\":[{\"evaluatorId\":\"terminal\"}]}]",out var simulation);
            Assert.That(lesson.Presentation.CanAdvance, Is.False); lesson.Execute(SimulationCommand.Seek(1)); Assert.That(lesson.Presentation.CanAdvance, Is.True);
            Assert.That(simulation.State.Sample.AppliedControl, Is.Null); Assert.That(simulation.State.Sample.NextState, Is.Null);
            Assert.That(simulation.State.Sample.DiscretizationStepSeconds, Is.Null); Assert.That(simulation.State.Sample.PhysicalTimeSeconds, Is.Null);
            lesson.Execute(SimulationCommand.Seek(0)); Assert.That(lesson.Presentation.CanAdvance, Is.False);
        }
        [Test] public void ActionFactsAreScopedToEachStepAndRestartClearsThem()
        {
            string step = "{\"id\":\"one\",\"instruction\":\"Step.\",\"enabledControlIds\":[\"step\"],\"conditions\":[{\"evaluatorId\":\"action\",\"targetId\":\"time.advanced\"}]}";
            var lesson = Open("["+step+","+step.Replace("one","two")+"]",out var simulation);
            lesson.Execute(SimulationCommand.StepForward()); lesson.Next(); Assert.That(lesson.Presentation.CanAdvance, Is.False);
            lesson.Restart(); Assert.That(lesson.Presentation.StepIndex, Is.Zero); Assert.That(simulation.State.Sample.Timestep, Is.Zero); Assert.That(lesson.Presentation.CanAdvance, Is.False);
        }
        [Test] public void BackRestoresPresetAndClearsComparisonAndFutureFacts()
        {
            var lesson = Open("[" + Plain + "," + Plain.Replace("one", "two") + "]", out var simulation);
            lesson.Next(); lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(9))); lesson.Execute(SimulationCommand.CaptureComparison());
            Assert.That(lesson.Previous(), Is.True); Assert.That(simulation.State.Result.Configuration.Controls[0][0], Is.EqualTo(3));
            Assert.That(simulation.State.Comparison, Is.Null); Assert.That(simulation.State.IsPlaying, Is.False);
        }
        [Test] public void ExploreUnlocksControlsWithoutAwardingGuidedCompletion()
        {
            var lesson = Open("[{\"id\":\"locked\",\"instruction\":\"Observe.\"}]",out var simulation);
            lesson.Explore(); Assert.That(lesson.Presentation.Allows(LessonControls.Horizon), Is.True); Assert.That(lesson.Presentation.IsCompleted, Is.False);
            Assert.That(lesson.Next(), Is.False); lesson.Execute(SimulationCommand.SetInput(0,new NumericVector(10)));
            lesson.Execute(SimulationCommand.Reset()); Assert.That(lesson.Presentation.IsFreeExploration, Is.True); Assert.That(simulation.State.Sample.CurrentState[0], Is.EqualTo(2));
            lesson.Restart(); Assert.That(lesson.Presentation.IsFreeExploration, Is.False);
        }
        [Test] public void RuntimeStepPlanDoesNotRereadMutatedEditorialData()
        {
            var definition = Definition("["+Plain+"]"); using(var simulation=IndexOnlyConfiguration.CreateSession()) using(var lesson=new LessonSession(definition,simulation))
            { JsonUtility.FromJsonOverwrite("{\"steps\":[]}",definition); Assert.That(lesson.Presentation.StepCount, Is.EqualTo(1)); Assert.That(lesson.Next(), Is.True); }
        }
        [Test] public void UnknownConditionProviderIsAnExplicitCompositionError()
        {
            var definition=Definition("[{\"id\":\"bad\",\"instruction\":\"Observe.\",\"conditions\":[{\"evaluatorId\":\"unknown\"}]}]");
            using(var simulation=IndexOnlyConfiguration.CreateSession()) Assert.Throws<ArgumentException>(()=>new LessonSession(definition,simulation));
        }
        [Test] public void NewConditionProviderNeedsNoLessonSwitch()
        {
            var definition=Definition("[{\"id\":\"custom\",\"instruction\":\"Observe.\",\"conditions\":[{\"evaluatorId\":\"custom.provider\"}]}]"); var registry=new LessonConditionRegistry(); registry.Register("custom.provider",new Always());
            using(var simulation=IndexOnlyConfiguration.CreateSession()) using(var lesson=new LessonSession(definition,simulation,registry)) Assert.That(lesson.Presentation.CanAdvance,Is.True);
        }
        private sealed class Always : ILessonConditionEvaluator { public bool IsMet(LessonConditionPlan condition,LessonConditionContext context)=>true; }
        [Test] public void CostEditsPreserveAAndFailedCandidatesKeepTheirOwnConfiguration()
        {
            var definition=Definition("[{\"id\":\"cost\",\"instruction\":\"Compare.\",\"enabledControlIds\":[\"parameter\",\"comparison\"],\"editableParameterTargets\":[\"cost.q.0\"]}]");
            var cost=new QuadraticTrackingCost(new NumericVector(0),new NumericVector(0),new NumericVector(2),new NumericVector(2),new NumericVector(2),new[]{"1"},new[]{"1"});
            using(var simulation=new SimulationSession(new SimulationConfiguration("fixture.cost",new IndexOnlyConfiguration(),new NumericVector(2),new[]{new NumericVector(3)},cost:cost)))
            using(var lesson=new LessonSession(definition,simulation))
            {
                lesson.Execute(SimulationCommand.CaptureComparison());var a=simulation.State.Comparison.Result;
                Assert.That(lesson.Execute(SimulationCommand.SetParameter("cost.q.0",4)),Is.True);var b=simulation.State.Result;
                Assert.That(a.Costs.Total,Is.EqualTo(38));Assert.That(b.Costs.Total,Is.EqualTo(42));Assert.That(a.Configuration.Cost.Parameters.First(p=>p.Target=="cost.q.0").Value,Is.EqualTo(2));
                Assert.That(lesson.Execute(SimulationCommand.SetParameter("cost.q.0",double.MaxValue)),Is.False);Assert.That(simulation.State.Result,Is.SameAs(b));Assert.That(simulation.State.Comparison.Result,Is.SameAs(a));
                Assert.That(b.Costs.Configuration,Is.SameAs(b.Configuration.Cost));Assert.That(a.History.States[1][0],Is.EqualTo(5));
            }
        }
        [Test] public void UnknownEditableParameterFailsComposition()
        {
            var definition=Definition("[{\"id\":\"bad\",\"instruction\":\"Read.\",\"editableParameterTargets\":[\"missing\"]}]");
            using(var simulation=IndexOnlyConfiguration.CreateSession())Assert.Throws<ArgumentException>(()=>new LessonSession(definition,simulation));
        }
        [Test] public void UnknownLayerInALaterStepFailsBeforeTheFirstStepIsReady()
        {
            var definition=Definition("["+Plain+",{\"id\":\"bad\",\"instruction\":\"Read.\",\"visibleLayerIds\":[\"missing\"]}]");
            using(var simulation=IndexOnlyConfiguration.CreateSession())Assert.Throws<ArgumentException>(()=>new LessonSession(definition,simulation,availableLayers:new[]{"known"}));
        }
        [Test] public void NativeRoundTripPreservesGuidedStepsChoicesActionsAndSharedFormulaReferences()
        {
            string path="Assets/__OptConF08Lesson_"+Guid.NewGuid().ToString("N")+".asset";
            var copy=Object.Instantiate(AssetDatabase.LoadAssetAtPath<LessonDefinition>("Assets/OptimalControl/Data/Lessons/Prototype/PendulumSlice.asset"));
            try
            {
                AssetDatabase.CreateAsset(copy,path);AssetDatabase.SaveAssets();AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
                var reloaded=AssetDatabase.LoadAssetAtPath<LessonDefinition>(path);var plan=new LessonPlan(reloaded);
                Assert.That(plan.Steps.Count,Is.EqualTo(7));Assert.That(plan.Steps[0].Answers[1].Id,Is.EqualTo("angle-speed"));
                Assert.That(reloaded.Steps[4].FormulaFocus,Is.SameAs(reloaded.Formulas[2]));Assert.That(plan.Steps[1].EntryCommands.Single().Kind,Is.EqualTo(SimulationCommandKind.Seek));
                Assert.That(plan.Steps[6].Conditions[2].ExpectedAnswerId,Is.EqualTo("terminal"));
            }
            finally{AssetDatabase.DeleteAsset(path);}
        }
        [TestCase("[{\"id\":\"same\",\"instruction\":\"Read.\"},{\"id\":\"same\",\"instruction\":\"Read.\"}]")]
        [TestCase("[{\"id\":\"bad\",\"instruction\":\"Read.\",\"enabledControlIds\":[\"invented\"]}]")]
        [TestCase("[{\"id\":\"bad\",\"instruction\":\"Read.\",\"visibleTermIds\":[\"invented\"]}]")]
        [TestCase("[{\"id\":\"bad\",\"instruction\":\"Read.\",\"conditions\":[{\"evaluatorId\":\"answer\",\"expectedAnswerId\":\"missing\"}]}]")]
        public void InvalidStepDataFailsBeforeReady(string steps) => Assert.Throws<ArgumentException>(()=>new LessonPlan(Definition(steps)));
        [Test] public void DispatchRejectsReentrantProgressionAndCommands()
        {
            var lesson=Open("["+Plain+","+Plain.Replace("one","two")+"]",out _); var attempts=new List<bool>();
            lesson.Changed+=_=>{attempts.Add(lesson.Next());attempts.Add(lesson.Restart());attempts.Add(lesson.Execute(SimulationCommand.StepForward()));};
            lesson.Next(); Assert.That(attempts,Is.EqualTo(new[]{false,false,false})); Assert.That(lesson.Presentation.StepIndex,Is.EqualTo(1));
        }
        [Test] public void DisposeRemovesSubscriptionsAndRejectsFurtherInteraction()
        {
            var lesson=Open("["+Plain+"]",out var simulation);int publications=0;lesson.Changed+=_=>publications++;lesson.Dispose();simulation.Execute(SimulationCommand.Seek(1));
            Assert.That(publications,Is.Zero);Assert.That(lesson.Next(),Is.False);Assert.That(lesson.Answer("x"),Is.False);Assert.That(lesson.Execute(SimulationCommand.Reset()),Is.False);
        }
        [Test] public void EducationalActionsNeverEvaluateDynamicsOrCopyAResult()
        {
            var model=new CountingModel();using(var simulation=new SimulationSession(new SimulationConfiguration("fixture.count",model,new NumericVector(2),new[]{new NumericVector(3)})))
            using(var lesson=new LessonSession(Definition("["+Plain+","+Plain.Replace("one","two")+"]"),simulation))
            { int calls=model.Calls;var result=simulation.State.Result;lesson.RecordTermSelection("any","state.current");lesson.Next();lesson.Execute(SimulationCommand.Seek(1));lesson.Execute(SimulationCommand.CaptureComparison());lesson.Explore();Assert.That(model.Calls,Is.EqualTo(calls));Assert.That(simulation.State.Result,Is.SameAs(result));Assert.That(simulation.State.Comparison.Result,Is.SameAs(result)); }
        }
        private sealed class CountingModel : ModelConfiguration,IDiscreteDynamics
        {
            private readonly IndexOnlyConfiguration source=new IndexOnlyConfiguration();public int Calls;
            public override ModelDescriptor Descriptor=>source.Descriptor;public override IReadOnlyList<ModelParameterValue> Parameters=>source.Parameters;
            public override IDiscreteDynamics CreateDynamics()=>this; public NumericVector Evaluate(int t,NumericVector x,NumericVector u){Calls++;return source.Evaluate(t,x,u);}
            public override ModelConfiguration WithParameter(string target,double value)=>throw new ArgumentException();public override ModelConfiguration WithPhysicalStep(double? seconds)=>throw new ArgumentException();
        }
    }
}
