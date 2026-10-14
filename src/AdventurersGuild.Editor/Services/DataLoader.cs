namespace AdventurersGuild.Editor.Services;

using AdventurersGuild.Data;
using AdventurersGuild.Data.Adventurer;
using AdventurersGuild.Data.Balance;
using AdventurersGuild.Data.Compendium;
using AdventurersGuild.Data.Quest;

public sealed class DataLoader
{
    public bool IsLoaded { get; private set; }

    public void EnsureLoaded()
    {
        if (IsLoaded) return;

        // FileSystem.Current должен быть установлен хостом до вызова.
        LocLoader.Load();
        AdventurerBalanceLoader.Load();
        QuestBalanceLoader.Load();
        AdventurerLoader.Load();
        QuestLoader.Load();
        CompendiumLoader.Load();

        IsLoaded = true;
    }
}