namespace WeatherSystem.Infrastructure.Data;

/// <summary>
/// Render видає адресу бази у вигляді postgres://user:password@host:port/dbname.
/// Npgsql такий формат не читає, тому перетворюємо його на звичайний рядок підключення.
/// Якщо рядок уже у форматі "Host=...;Database=...", він повертається без змін.
/// </summary>
public static class DatabaseUrl
{
    public static string ToConnectionString(string value)
    {
        if (!value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return value;
        }

        var uri = new Uri(value);
        var userInfo = uri.UserInfo.Split(':', 2);
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
               $"Username={Uri.UnescapeDataString(userInfo[0])};" +
               $"Password={Uri.UnescapeDataString(userInfo.Length > 1 ? userInfo[1] : string.Empty)};" +
               "SSL Mode=Prefer";
    }

    /// <summary>Адреса сервера бази без логіна й пароля — для журналу.</summary>
    public static string SafeDescription(string connectionString)
    {
        var parts = connectionString.Split(';', StringSplitOptions.RemoveEmptyEntries)
            .Select(part => part.Split('=', 2))
            .Where(pair => pair.Length == 2)
            .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim(), StringComparer.OrdinalIgnoreCase);

        parts.TryGetValue("Host", out var host);
        parts.TryGetValue("Database", out var database);
        return $"{host}/{database}";
    }
}
