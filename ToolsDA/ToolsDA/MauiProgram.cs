using System.Reflection;
using ToolsDA.Services;

namespace ToolsDA
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });


            /*  
                var a = Assembly.GetExecutingAssembly();
                using var stream = a.GetManifestResourceStream("ToolsDA.appsettings.json");
                var config = new ConfigurationBuilder()
                    .AddJsonStream(stream)
                    .Build();
                builder.Configuration.AddConfiguration(config);
            */

            builder.Services.AddMauiBlazorWebView();
            builder.Services.AddScoped<LogsService, LogsService>();
            builder.Services.AddScoped<ToolService, ToolService>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}
