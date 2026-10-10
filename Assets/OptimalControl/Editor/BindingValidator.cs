using System.Linq;
using OptCon.Content;
using OptCon.Simulation;

namespace OptCon.Editor
{
    /// <summary>Static binding check delegates all type/unit/scope rules to the authoritative runtime resolver.</summary>
    public static class BindingValidator
    {
        public static void Validate(LessonDefinition lesson, IExperimentSnapshot snapshot, RuntimeMetricResolver resolver, CatalogValidationResult report)
        {
            foreach (var formula in lesson.Formulas.Where(f => f != null)) foreach (var term in formula.Terms.Where(t => t != null))
            {
                var binding = resolver.Resolve(term, snapshot);
                if (binding.Validity == MetricValidity.Invalid) report.Add(formula, "RuntimeBinding", "terms." + term.Id, binding.Diagnostic);
            }
        }
    }
}
