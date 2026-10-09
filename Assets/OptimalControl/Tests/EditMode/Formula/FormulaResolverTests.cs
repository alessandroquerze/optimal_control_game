using System;
using System.Linq;
using NUnit.Framework;
using OptCon.Content;
using OptCon.Core;
using OptCon.Editor;
using OptCon.Presentation;
using OptCon.Simulation;
using OptCon.Tests.Fixtures;
using UnityEditor;
using UnityEngine;

namespace OptCon.Tests
{
    public sealed class FormulaResolverTests
    {
        private readonly RuntimeMetricResolver resolver = new RuntimeMetricResolver();
        private sealed class ScalarSource : IRuntimeMetricSource
        {
            public bool TryGet(SimulationResult result, SimulationSnapshot sample, string id, out RuntimeMetric metric)
            { metric = RuntimeMetric.Number(id, 7, "kg", sample.Timestep); return true; }
        }
        [Test]
        public void AdditionalScalarProviderUsesTheSameContractWithoutVectorWrapping()
        {
            using (var s = IndexOnlyConfiguration.CreateSession())
            {
                var r = new RuntimeMetricResolver(new ScalarSource()).Resolve(s.State.Result, 0, "fixture.scalar", FormulaValueKind.Scalar);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(r.Value.Scalar, Is.EqualTo(7)); Assert.That(r.Value.Vector, Is.Null);
                Assert.That(r.Value.Units.Single(), Is.EqualTo("kg")); Assert.That(r.Result, Is.SameAs(s.State.Result));
                Assert.That(new RuntimeMetricResolver(new ScalarSource()).Resolve(s.State.Result, 0, "fixture.scalar", FormulaValueKind.Vector).Validity, Is.EqualTo(MetricValidity.Invalid));
            }
        }
        [Test]
        public void MetricUnitsAreFrozenAndMissingScalarUnitsAreRejected()
        {
            var units = new[] { "m", "s" }; var vector = new NumericVector(2, 3);
            var metric = RuntimeMetric.FromVector("fixture.vector", vector, units, 0, FormulaScope.CurrentTimestep); units[0] = "kg";
            Assert.That(metric.Units, Is.EqualTo(new[] { "m", "s" })); Assert.That(metric.Vector, Is.SameAs(vector));
            Assert.Throws<ArgumentException>(() => RuntimeMetric.Number("fixture.scalar", 1, null, 0));
        }
        [Test]
        public void FormulaValueAndHighlightScopeMismatchIsAnExplicitError()
        {
            var term = JsonUtility.FromJson<FormulaTermDefinition>("{\"value\":{\"metricId\":\"state.next\",\"kind\":1,\"scope\":1,\"component\":-1},\"highlight\":{\"semanticRole\":\"state.next\",\"scope\":0}}");
            using (var s = Pendulum()) Assert.That(resolver.Resolve(term, s.State.Result, 0).Diagnostic, Does.Contain("Value/highlight scope"));
        }
        [Test]
        public void PhysicalTimeOfARetainedResultNeverUsesTheStepOfB()
        {
            using (var s = Pendulum())
            {
                var a = s.State.Result; s.Execute(SimulationCommand.SetPhysicalStep(.04)); var b = s.State.Result;
                Assert.That(resolver.Resolve(a, 2, RuntimeMetricIds.PhysicalTime, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.04));
                Assert.That(resolver.Resolve(b, 2, RuntimeMetricIds.PhysicalTime, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.08));
                Assert.That(resolver.Resolve(a, 2, RuntimeMetricIds.PhysicalStep, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.02));
                Assert.That(resolver.Resolve(b, 2, RuntimeMetricIds.PhysicalStep, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.04));
            }
        }
        private static SimulationSession Pendulum() => new SimulationSession(new SimulationConfiguration("fixture.pendulum",
            new PendulumConfiguration(new PendulumParameters(1, 1, 9.81), 0.02), new NumericVector(0, 2), new[] { new NumericVector(3), new NumericVector(4) }));
        [Test]
        public void ScalarIndexHasExplicitKindShapeUnitAndSample()
        {
            using (var session = IndexOnlyConfiguration.CreateSession())
            {
                var r = resolver.Resolve(session.State.Result, 0, RuntimeMetricIds.Index, FormulaValueKind.Scalar);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(r.Value.Kind, Is.EqualTo(FormulaValueKind.Scalar));
                Assert.That(r.Value.Rows, Is.EqualTo(1)); Assert.That(r.Value.Columns, Is.EqualTo(1)); Assert.That(r.Value.Scalar, Is.Zero);
                Assert.That(r.SampleIndex, Is.Zero); Assert.That(r.Value.Units, Is.EqualTo(new[] { "1" })); Assert.That(r.Value.Vector, Is.Null);
            }
        }
        [TestCase(1)] [TestCase(2)] [TestCase(4)]
        public void VectorsRetainTheirKindEvenAtDimensionOne(int dimension)
        {
            using (var session = IndexOnlyConfiguration.CreateSession(dimension))
            {
                var r = resolver.Resolve(session.State.Result, 0, "input.applied", FormulaValueKind.Vector);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(r.Value.Rows, Is.EqualTo(dimension));
                Assert.That(r.Value.Scalar, Is.Null); Assert.That(r.Value.Vector, Is.SameAs(session.State.Sample.AppliedControl));
            }
        }
        [TestCase(0, "rad", 0d)] [TestCase(1, "rad/s", 2d)]
        public void ExplicitComponentProducesScalarWithItsOwnUnit(int component, string unit, double value)
        {
            using (var session = Pendulum())
            {
                var r = resolver.Resolve(session.State.Result, 0, "state.current", FormulaValueKind.Scalar, component: component);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(r.ComponentSelection, Is.EqualTo(component));
                Assert.That(r.Value.Scalar, Is.EqualTo(value)); Assert.That(r.Value.Units.Single(), Is.EqualTo(unit)); Assert.That(r.SampleIndex, Is.Zero);
            }
        }
        [TestCase(2)] [TestCase(99)] [TestCase(-2)]
        public void InvalidComponentHasDiagnostic(int component)
        {
            using (var s = Pendulum())
            {
                var r = resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Scalar, component: component);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Invalid)); Assert.That(r.Diagnostic, Does.Contain(component == -2 ? "negativa" : "fuori range"));
            }
        }
        [TestCase(FormulaValueKind.Scalar)] [TestCase(FormulaValueKind.Matrix)] [TestCase(FormulaValueKind.ModelDescriptor)]
        public void AOneComponentVectorNeverConvertsImplicitly(FormulaValueKind expected)
        {
            using (var s = IndexOnlyConfiguration.CreateSession())
            {
                var r = resolver.Resolve(s.State.Result, 0, "input.applied", expected);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Invalid)); Assert.That(r.Diagnostic, Does.Contain("Expected kind"));
            }
        }
        [Test]
        public void SelectedScalarCannotBeRequestedAsVector()
        {
            using (var s = Pendulum()) Assert.That(resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector, component: 0).Validity, Is.EqualTo(MetricValidity.Invalid));
        }
        [Test]
        public void CurrentInputAndNextAreStoredSamplesWithSingleResolvedOffset()
        {
            using (var s = IndexOnlyConfiguration.CreateSession())
            {
                var a = resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector);
                var u = resolver.Resolve(s.State.Result, 0, "input.applied", FormulaValueKind.Vector);
                var b = resolver.Resolve(s.State.Result, 0, "state.next", FormulaValueKind.Vector, FormulaScope.NextTimestep);
                Assert.That(a.Value.Vector[0], Is.EqualTo(2)); Assert.That(u.Value.Vector[0], Is.EqualTo(3)); Assert.That(b.Value.Vector[0], Is.EqualTo(5));
                Assert.That(a.SampleIndex, Is.Zero); Assert.That(u.SampleIndex, Is.Zero); Assert.That(b.SampleIndex, Is.EqualTo(1));
                Assert.That(b.Value.Vector, Is.SameAs(s.State.Result.History.States[1]));
            }
        }
        [Test]
        public void TerminalDoesNotInventInputOrNextAndExplicitTerminalResolvesOnce()
        {
            using (var s = Pendulum())
            {
                foreach (var id in new[] { "input.applied", "state.next" })
                {
                    var r = resolver.Resolve(s.State.Result, 2, id, FormulaValueKind.Vector, id == "state.next" ? FormulaScope.NextTimestep : FormulaScope.CurrentTimestep);
                    Assert.That(r.Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.Value.Vector, Is.Null); Assert.That(r.SampleIndex, Is.Null);
                }
                var terminal = resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector, FormulaScope.Terminal);
                Assert.That(terminal.SampleIndex, Is.EqualTo(2)); Assert.That(terminal.Value.Vector, Is.SameAs(s.State.Result.History.States[2]));
            }
        }
        [Test]
        public void IndexOnlyHasIndexButNoPhysicalTimeEvenAtZeroOrTerminal()
        {
            using (var s = IndexOnlyConfiguration.CreateSession()) foreach (int t in new[] { 0, 1 })
            {
                Assert.That(resolver.Resolve(s.State.Result, t, RuntimeMetricIds.Index, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(t));
                foreach (var id in new[] { RuntimeMetricIds.PhysicalTime, RuntimeMetricIds.PhysicalStep })
                { var r = resolver.Resolve(s.State.Result, t, id, FormulaValueKind.Scalar); Assert.That(r.Validity, Is.EqualTo(MetricValidity.NotApplicable)); Assert.That(r.Value.Scalar, Is.Null); Assert.That(r.SampleIndex, Is.Null); }
            }
        }
        [Test]
        public void PendulumIndexStepAndPhysicalTimeAreDistinctAndConsistent()
        {
            using (var s = Pendulum())
            {
                var result = s.State.Result;
                Assert.That(resolver.Resolve(result, 1, RuntimeMetricIds.Index, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(1));
                Assert.That(resolver.Resolve(result, 1, RuntimeMetricIds.PhysicalStep, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.02));
                Assert.That(resolver.Resolve(result, 2, RuntimeMetricIds.PhysicalTime, FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(.04));
                Assert.That(resolver.Resolve(result, 2, RuntimeMetricIds.PhysicalTime, FormulaValueKind.Scalar).Value.Units.Single(), Is.EqualTo("s"));
            }
        }
        [Test]
        public void UnitsShapeScopeAndUnknownMetricFailExplicitly()
        {
            using (var s = Pendulum())
            {
                Assert.That(resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector, rows: 3).Diagnostic, Does.Contain("Forma"));
                Assert.That(resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector, expectedUnits: new[] { "m", "s" }).Diagnostic, Does.Contain("Unità"));
                Assert.That(resolver.Resolve(s.State.Result, 0, "state.next", FormulaValueKind.Vector).Diagnostic, Does.Contain("Scope"));
                Assert.That(resolver.Resolve(s.State.Result, 0, "absent", FormulaValueKind.Scalar).Diagnostic, Does.Contain("sconosciuta"));
                Assert.That(resolver.Resolve(s.State.Result, 0, "state.current", FormulaValueKind.Vector, FormulaScope.CurrentIteration).Validity, Is.EqualTo(MetricValidity.Invalid));
                Assert.That(resolver.Resolve(s.State.Result, 3, RuntimeMetricIds.Index, FormulaValueKind.Scalar).Diagnostic, Does.Contain("SampleIndex"));
            }
        }
        [Test]
        public void MissingOptionalBindingIsExplicitlyUnavailable()
        {
            using (var s = Pendulum()) Assert.That(resolver.Resolve(new FormulaTermDefinition(), s.State.Result, 0).Validity, Is.EqualTo(MetricValidity.NotApplicable));
        }
        [Test]
        public void RevisionAKeepsModelParametersAndSamplesAfterBAndFailure()
        {
            using (var s = Pendulum())
            {
                var a = s.State.Result; var aNext = resolver.Resolve(a, 0, "state.next", FormulaValueKind.Vector, FormulaScope.NextTimestep);
                s.Execute(SimulationCommand.SetParameter("model.mass", 2)); var b = s.State.Result;
                Assert.That(resolver.Resolve(a, 0, RuntimeMetricIds.Model, FormulaValueKind.ModelDescriptor).Value.Model, Is.SameAs(a.Configuration.Model));
                Assert.That(resolver.Resolve(a, 0, "parameter.model.mass", FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(1));
                Assert.That(resolver.Resolve(b, 0, "parameter.model.mass", FormulaValueKind.Scalar).Value.Scalar, Is.EqualTo(2));
                Assert.That(aNext.Value.Vector[1], Is.EqualTo(2.06).Within(1e-12));
                Assert.That(resolver.Resolve(b, 0, "state.next", FormulaValueKind.Vector, FormulaScope.NextTimestep).Value.Vector[1], Is.EqualTo(2.03).Within(1e-12));
                Assert.That(s.Execute(SimulationCommand.SetParameter("model.mass", 0)), Is.False);
                Assert.That(s.State.Result, Is.SameAs(b)); Assert.That(resolver.Resolve(s.State.Result, 0, RuntimeMetricIds.Model, FormulaValueKind.ModelDescriptor).Value.Model, Is.SameAs(b.Configuration.Model));
                Assert.That(resolver.Resolve(a, 0, "state.next", FormulaValueKind.Vector, FormulaScope.NextTimestep).Value.Vector, Is.SameAs(aNext.Value.Vector));
            }
        }
        [Test]
        public void SeekUpdatesAllBindingsWithoutChangingResult()
        {
            using (var s = Pendulum())
            {
                var result = s.State.Result; s.Execute(SimulationCommand.Seek(1));
                Assert.That(resolver.Resolve(result, 1, "state.current", FormulaValueKind.Vector).Value.Vector, Is.SameAs(s.State.Sample.CurrentState));
                Assert.That(resolver.Resolve(result, 1, "input.applied", FormulaValueKind.Vector).Value.Vector[0], Is.EqualTo(4));
                Assert.That(resolver.Resolve(result, 1, "state.next", FormulaValueKind.Vector, FormulaScope.NextTimestep).SampleIndex, Is.EqualTo(2));
                Assert.That(s.State.Result, Is.SameAs(result));
            }
        }
        [Test]
        public void AuthoredPrototypeAndSpecializationResolveAllTerms()
        {
            var lesson = AssetDatabase.LoadAssetAtPath<LessonDefinition>("Assets/OptimalControl/Data/Lessons/Prototype/PendulumSlice.asset");
            using (var s = lesson.Experiment.Model.CreateSession(lesson.Experiment))
                foreach (var formula in lesson.Formulas) foreach (var term in formula.Terms)
                    Assert.That(resolver.Resolve(term, s.State.Result, 0).Validity, Is.EqualTo(MetricValidity.Available), formula.Id + "/" + term.Id);
        }
        [Test]
        public void GenericFormulaInfersVectorDimensionsAndUnitsFromProvider()
        {
            var formula = AssetDatabase.LoadAssetAtPath<FormulaDefinition>("Assets/OptimalControl/Data/Formulas/DiscreteDynamics.asset");
            using (var s = IndexOnlyConfiguration.CreateSession(4))
            {
                var term = formula.Terms.Single(t => t.Id == "input.applied"); var r = resolver.Resolve(term, s.State.Result, 0);
                Assert.That(r.Validity, Is.EqualTo(MetricValidity.Available)); Assert.That(r.Value.Rows, Is.EqualTo(4)); Assert.That(r.Value.Units, Is.EqualTo(new[] { "1", "1", "1", "1" }));
            }
        }
        [Test]
        public void ExecutionValidatorRejectsFormerImplicitScalarInputBinding()
        {
            var original = AssetDatabase.LoadAssetAtPath<CourseDefinition>("Assets/OptimalControl/Data/Courses/PrototypeCourse.asset");
            var course = UnityEngine.Object.Instantiate(original); var chapter = UnityEngine.Object.Instantiate(original.Chapters[0]);
            var lesson = UnityEngine.Object.Instantiate(chapter.Lessons[0]); var formula = UnityEngine.Object.Instantiate(lesson.Formulas[0]);
            try
            {
                void Reference(UnityEngine.Object owner, string path, UnityEngine.Object value) { var so = new SerializedObject(owner); so.FindProperty(path).objectReferenceValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
                Reference(course, "chapters.Array.data[0]", chapter); Reference(chapter, "lessons.Array.data[0]", lesson); Reference(lesson, "formulas.Array.data[0]", formula);
                var so = new SerializedObject(formula); so.FindProperty("terms.Array.data[1].value.component").intValue = -1; so.ApplyModifiedPropertiesWithoutUndo();
                Assert.That(CatalogValidator.ValidateExecutionConfiguration(course).Issues.Any(i => i.Code == "RuntimeBinding" && i.Message.Contains("Expected kind")), Is.True);
            }
            finally { UnityEngine.Object.DestroyImmediate(formula); UnityEngine.Object.DestroyImmediate(lesson); UnityEngine.Object.DestroyImmediate(chapter); UnityEngine.Object.DestroyImmediate(course); }
        }
    }
}
