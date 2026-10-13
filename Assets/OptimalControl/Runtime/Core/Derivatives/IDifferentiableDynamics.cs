namespace OptCon.Core
{
    /// <summary>Optional capability. Standard Jacobians: A is n×n, B is n×m, evaluated at the supplied pair.
    /// The course's gradient-column convention writes these as ∇₁fᵀ and ∇₂fᵀ.</summary>
    public interface IDifferentiableDynamics : IDiscreteDynamics
    {
        NumericMatrix StateJacobian(int timestep, NumericVector state, NumericVector control);
        NumericMatrix InputJacobian(int timestep, NumericVector state, NumericVector control);
    }
}
