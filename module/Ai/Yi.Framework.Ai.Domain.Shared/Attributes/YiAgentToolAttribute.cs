namespace Yi.Framework.Ai.Domain.Shared.Attributes;

[AttributeUsage(AttributeTargets.Class|AttributeTargets.Method)]
public class YiAgentToolAttribute:Attribute
{
    public YiAgentToolAttribute()
    {
    }

    public YiAgentToolAttribute(string name)
    {
        Name = name;
    }

    public string Name { get; set; }
}
