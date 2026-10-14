namespace AdventurersGuild.Editor;

using AdventurersGuild.Data.Compendium;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Compendium.Loading;
using AdventurersGuild.Editor.Compendium.Schema;
using AdventurersGuild.Editor.Compendium.Schemas;
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
        builder.Services.AddSingleton<LocationRepository>();
        builder.Services.AddSingleton<CreatureRepository>();
        builder.Services.AddSingleton(_ =>
        {
            var reg = new SchemaRegistry();
            reg.Register(TagSchema.Build());
            reg.Register(LocationSchema.Build());
            reg.Register(GradeSchema.Build());
            return reg;
        });

        builder.Services.AddSingleton<CompendiumJsonLoader>();
        builder.Services.AddSingleton<CompendiumWriter>();
        builder.Services.AddSingleton<CompendiumService>();

        WebApplication app = builder.Build();

        FileSystem.Current = new SystemFileSystem();

        app.Services.GetRequiredService<DataLoader>().EnsureLoaded();
        app.Services.GetRequiredService<CompendiumService>().LoadAll();

        app.UseStaticFiles();
        app.UseAntiforgery();

        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}