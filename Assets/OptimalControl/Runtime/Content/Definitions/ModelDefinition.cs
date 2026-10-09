using OptCon.Core;

namespace OptCon.Content
{
    /// <summary>
    /// Typed authoring boundary for future concrete model providers. F01 only needs dimensions;
    /// the mathematical model and its creation contract are added when implemented in F03/F04.
    /// There is deliberately no placeholder factory or concrete draft provider.
    /// </summary>
    public abstract class ModelDefinition : ContentDefinition
    {
        public abstract ModelDimensions Dimensions { get; }
    }
}
