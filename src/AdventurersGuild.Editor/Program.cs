namespace AdventurersGuild.Editor;

using AdventurersGuild.Data.Compendium;
using AdventurersGuild.Data.IO;
using AdventurersGuild.Editor.Compendium.Loading;
using AdventurersGuild.Editor.Compendium.Schema;
using AdventurersGuild.Editor.Compendium.Schemas;
using AdventurersGuild.Editor.Compendium.Validation;
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
        builder.Services.AddSingleton<EntityValidator>();
        builder.Services.AddSingleton(_ =>
        {
            var reg = new SchemaRegistry();

            // Internal — Species
            reg.RegisterInternal(HumanoidSpeciesSchema.ClassRefSchemaName, HumanoidSpeciesSchema.BuildClassRef());
            reg.RegisterInternal(HumanoidSpeciesSchema.RoleRefSchemaName, HumanoidSpeciesSchema.BuildRoleRef());
            reg.RegisterInternal(BeastSpeciesSchema.ClassRefSchemaName, BeastSpeciesSchema.BuildClassRef());
            reg.RegisterInternal(BeastSpeciesSchema.RoleRefSchemaName, BeastSpeciesSchema.BuildRoleRef());

            // Internal — Role
            reg.RegisterInternal(HumanoidRoleSchema.StateSchemaName, HumanoidRoleSchema.BuildState());
            reg.RegisterInternal(HumanoidRoleSchema.ActionSchemaName, HumanoidRoleSchema.BuildAction());
            reg.RegisterInternal(HumanoidRoleSchema.CheckSchemaName, HumanoidRoleSchema.BuildCheck());

            reg.RegisterInternal(BeastRoleSchema.StateSchemaName, BeastRoleSchema.BuildState());
            reg.RegisterInternal(BeastRoleSchema.ActionSchemaName, BeastRoleSchema.BuildAction());
            reg.RegisterInternal(BeastRoleSchema.CheckSchemaName, BeastRoleSchema.BuildCheck());

            // Публичные kinds
            reg.Register(StatSchema.Build());
            reg.Register(HumanoidSpeciesSchema.Build());
            reg.Register(HumanoidClassSchema.Build());
            reg.Register(HumanoidRoleSchema.Build());
            reg.Register(BeastSpeciesSchema.Build());
            reg.Register(BeastClassSchema.Build());
            reg.Register(BeastRoleSchema.Build());
            reg.Register(PhaseSchema.Build());
            reg.Register(TagSchema.Build());
            reg.Register(LocationSchema.Build());
            reg.Register(GradeSchema.Build());
            reg.Register(TestSchema.Build());

            return reg;
        });

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