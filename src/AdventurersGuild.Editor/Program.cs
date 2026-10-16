namespace AdventurersGuild.Editor;

using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Compendium.Loading;
using AdventurersGuild.Editor.Services;
using AdventurersGuild.Editor.Services.Compendium;
using MudBlazor.Services;

public static class Program
{
    public static void Main(string[] args)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

        builder.WebHost.UseStaticWebAssets();

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

        builder.Services.AddSingleton<DataLoader>();

        builder.Services.AddSingleton<CompendiumJsonLoader>();
        builder.Services.AddSingleton<CompendiumWriter>();
        builder.Services.AddSingleton<CompendiumService>();

        WebApplication app = builder.Build();

        var contentRoot = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..",
            "AdventurersGuild.Content"));

        FileSystem.Current = new SystemFileSystem(contentRoot);

        app.Services.GetRequiredService<DataLoader>().EnsureLoaded();
        app.Services.GetRequiredService<CompendiumService>().LoadAll();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}