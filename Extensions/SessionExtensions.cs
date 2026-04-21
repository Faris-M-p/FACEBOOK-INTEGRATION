using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace FACEBOOK_INTEGRATION.Extensions;

public static class SessionExtensions
{
    public static void SetInt32Required(this ISession session, string key, int value)
        => session.SetInt32(key, value);

    public static int? GetClientId(this ISession session) => session.GetInt32(SessionKeys.ClientId);

    public static int GetClientIdRequired(this ISession session)
        => session.GetInt32(SessionKeys.ClientId) ?? throw new InvalidOperationException("No client selected.");

    public static void SetJson<T>(this ISession session, string key, T value)
        => session.Set(key, Encoding.UTF8.GetBytes(JsonSerializer.Serialize(value)));

    public static T? GetJson<T>(this ISession session, string key)
    {
        var bytes = session.Get(key);
        if (bytes is null || bytes.Length == 0) return default;
        return JsonSerializer.Deserialize<T>(Encoding.UTF8.GetString(bytes));
    }
}

