using OptCon.Core;

namespace OptCon.Content
{
    public abstract class CostDefinition : ContentDefinition
    {
        public abstract CostConfiguration CreateConfiguration(ExperimentDefinition experiment);
    }
}
