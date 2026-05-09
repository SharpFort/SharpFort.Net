using System.Globalization;
using System.Reflection;
using OpenAI.Chat;

namespace SharpFort.Ai.Domain.Extensions;

public static class ChatMessageExtensions
{
    public static string GetRoleAsString(this ChatMessage message)
    {
        Type type = message.GetType();
        PropertyInfo? propertyInfo = type.GetProperty("Role", BindingFlags.NonPublic | BindingFlags.Instance);

        if (propertyInfo != null)
        {
            ChatMessageRole? value = propertyInfo.GetValue(message) as ChatMessageRole?;
            return value?.ToString()?.ToLower(CultureInfo.InvariantCulture) ?? string.Empty;
        }

        return string.Empty;
    }
}
