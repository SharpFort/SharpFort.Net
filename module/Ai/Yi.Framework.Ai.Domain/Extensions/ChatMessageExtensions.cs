using System.Reflection;
using OpenAI.Chat;

namespace Yi.Framework.Ai.Domain.Extensions;

public static class ChatMessageExtensions
{
    public static string GetRoleAsString(this ChatMessage message)
    {
        var type = message.GetType();
        var propertyInfo = type.GetProperty("Role", BindingFlags.NonPublic | BindingFlags.Instance);
        
        if (propertyInfo != null)
        {
            var value = propertyInfo.GetValue(message) as ChatMessageRole?;
            return value.ToString().ToLower();
        }
        
        return string.Empty;
    }
}
