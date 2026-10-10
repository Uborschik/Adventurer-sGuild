using AdventurersGuild.Data.Balance;

namespace AdventurersGuild.Core;

public readonly struct Level
{
    public readonly int Number;
    public readonly int Experience;
    public readonly int ExpToNext;

    public Level(int number, int experience, int expToNext)
    {
        Number = number;
        Experience = experience;
        ExpToNext = expToNext;
    }

    public bool CanLevelUp => Experience >= ExpToNext;

    public Level SetExperience(int newExperience)
        => new(Number, newExperience, ExpToNext);

    public Level SetAfterLevelUp(int newNumber, int newExperience, int newExpToNext)
        => new(newNumber, newExperience, newExpToNext);

    public static Level New(int number)
        => new(number, 0, AdventurerBalance.ExpCapFor(number));
}