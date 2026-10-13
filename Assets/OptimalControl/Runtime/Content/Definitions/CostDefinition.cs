using OptCon.Core;

namespace OptCon.Content
{
    public abstract class CostDefinition : ContentDefinition
    {
        [UnityEngine.SerializeField] private string totalSymbol = "J";
        public string TotalSymbol => string.IsNullOrWhiteSpace(totalSymbol) ? "J" : totalSymbol;
        public abstract CostConfiguration CreateConfiguration(ExperimentDefinition experiment);
    }
}
