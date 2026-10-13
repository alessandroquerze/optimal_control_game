using System.Linq;

namespace OptCon.Core
{
    /// <summary>The only running-density to integral-contribution conversion. Terminal bypasses it.</summary>
    public static class LeftRectangleQuadrature
    {
        public static CostContribution IntegrateCell(CostContribution density, double stepSeconds, string costUnit)
        {
            NumericValidation.RequirePositive(stepSeconds, nameof(stepSeconds));
            if (density == null || density.Terms.Any(t => t.Unit != costUnit + "/s"))
                throw new System.ArgumentException("Running density unit must be cost-unit per second.");
            return new CostContribution(density.Terms.Select(t => new CostTerm(t.Id, "Contributo integrale: " + t.Name, "δ" + t.Symbol,
                "Rettangolo sinistro della densità. " + t.Description, costUnit, stepSeconds * t.Value)), density.Role, density.SampleIndex, density.RevisionId);
        }
    }
}
