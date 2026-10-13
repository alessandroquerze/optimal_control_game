using System;
using System.Linq;
using OptCon.Simulation;

namespace OptCon.Content
{
    /// <summary>Static, shared capability preflight. Does not execute lesson actions or mathematical functions.</summary>
    public static class LessonCapabilityRequirements
    {
        public static void Validate(LessonStepPlan step, IExperimentSession session)
        {
            if ((step.EnabledControls.Contains(LessonControls.InitialPerturbation) || step.EnabledControls.Contains(LessonControls.InputPerturbation)) &&
                (session.Capabilities & ExperimentCapabilities.Linearization) == 0) throw new ArgumentException("Perturbation controls require differentiable dynamics.");
            foreach (var command in step.EntryCommands)
                if (!session.Supports(command.Kind)) throw new ArgumentException("Step entry requires unsupported capability: " + command.Kind);
            if ((session.Capabilities & ExperimentCapabilities.Trajectory) != 0) return;
            foreach (string control in step.EnabledControls)
                if (control != LessonControls.Parameter && control != LessonControls.Formula && control != LessonControls.Layers)
                    throw new ArgumentException("Step control requires a trajectory: " + control);
            if (step.ShowCosts) throw new ArgumentException("Stage/terminal costs require a trajectory.");
        }
    }
}
