namespace Halen.Application.Attributes;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireFeatureAttribute(string featureKey) : Attribute
{
    public string FeatureKey { get; } = featureKey;
}
