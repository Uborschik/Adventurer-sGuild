using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Infrastructure;
using AdventurersGuild.Editor.Services;
using MudBlazor.Services;

namespace AdventurersGuild.Editor;

public static class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseStaticWebAssets();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

        builder.Services.AddSingleton<DataLoader>();
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<CreatureRepository>();

        WebApplication app = builder.Build();

        // Установить FileSystem до загрузки данных
        var projectRoot = Path.GetFullPath(
            Path.Combine(app.Environment.ContentRootPath, "..", "AdventurersGuild.Game"));
        FileSystem.Current = new SystemFileSystem(projectRoot);

        app.Services.GetRequiredService<DataLoader>().EnsureLoaded();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}