namespace OptCon.Core
{
    /// <summary>
    /// Deterministic discrete transition. Implementations own immutable parameters and validate dimensions.
    /// No timestep cursor, playback clock, authoring asset or renderer belongs to this contract.
    /// </summary>
    public interface IDiscreteDynamics
    {
        ModelDescriptor Descriptor { get; }
        NumericVector Evaluate(int timestep, NumericVector state, NumericVector control);
    }
}
