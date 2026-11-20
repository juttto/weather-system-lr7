namespace WeatherSystem.Desktop;

internal static class Program
{
    // Адреса REST API, розгорнутого на Render (ЛР7)
    private const string ApiUrl = "https://weather-api-romanova.onrender.com/";

    [STAThread]
    private static void Main()
    {
        // Безкоштовний сервер Render після простою прокидається до хвилини, тому довгий таймаут
        var http = new HttpClient
        {
            BaseAddress = new Uri(ApiUrl),
            Timeout = TimeSpan.FromSeconds(90)
        };

        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(new DevicesApiClient(http), ApiUrl.TrimEnd('/')));
    }
}
