namespace SharpFort.Ai.Domain.Shared.Attributes;

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public class SfAgentToolAttribute:Attribute
{
    public SfAgentToolAttribute()
    {
    }

    public SfAgentToolAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
