namespace OptCon.Core
{
    /// <summary>A static scalar objective on decision space; no dynamics, derivatives or optimizer implied.</summary>
    public interface IObjectiveFunction
    {
        int Dimension { get; }
        double Value(NumericVector decision);
    }
}
