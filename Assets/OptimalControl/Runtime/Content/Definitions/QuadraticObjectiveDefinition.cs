using System;
using System.Linq;
using OptCon.Core;
using OptCon.Simulation;
using UnityEngine;

namespace OptCon.Content
{
    [CreateAssetMenu(menuName = "Optimal Control/Providers/Diagonal quadratic objective")]
    public sealed class QuadraticObjectiveDefinition : ExperimentProviderDefinition
    {
        [SerializeField, Min(1)] private int decisionDimension = 2;
        public int DecisionDimension => decisionDimension;
        public override ExperimentCapabilities Capabilities => ExperimentCapabilities.Parameters | ExperimentCapabilities.Objective;
        public ObjectiveConfiguration CreateConfiguration(ExperimentDefinition experiment)
        {
            if (experiment == null || experiment.UsesDiscreteTime || experiment.Model != null || experiment.Cost != null
                || experiment.StateDimension != 0 || experiment.ControlDimension != 0 || experiment.OutputDimension != 0)
                throw new ArgumentException("A static objective must not declare dynamics, trajectory cost or dynamic dimensions.");
            if (decisionDimension < 1 || experiment.Parameters.Count != 2 * decisionDimension) throw new ArgumentException("Decision/weight parameter dimension mismatch.");
            double Read(string target)
            {
                var matches = experiment.Parameters.Where(p => p != null && p.ModelTarget == target).ToArray();
                if (matches.Length != 1) throw new ArgumentException("Exactly one parameter required: " + target);
                var parameter = matches[0];
                if (parameter.Recalculation != ParameterRecalculation.ReevaluateObjective) throw new ArgumentException("Static parameters require ReevaluateObjective.");
                new ParameterRule(target, parameter.Minimum, parameter.Maximum, parameter.ValueKind != ParameterValueKind.Real).Validate(parameter.DefaultValue);
                return parameter.DefaultValue;
            }
            var configuration = new ObjectiveConfiguration(experiment.Id,
                new NumericVector(Enumerable.Range(0, decisionDimension).Select(i => Read("decision." + i)).ToArray()),
                new NumericVector(Enumerable.Range(0, decisionDimension).Select(i => Read("objective.weight." + i)).ToArray()));
            ValidateParameterMetadata(experiment, configuration.Parameters);
            return configuration;
        }
        public override IExperimentSession CreateExperimentSession(ExperimentDefinition experiment) => new ObjectiveSession(CreateConfiguration(experiment),
            experiment.Parameters.Select(p => new ParameterRule(p.ModelTarget, p.Minimum, p.Maximum, p.ValueKind != ParameterValueKind.Real)).ToArray());
    }
}
