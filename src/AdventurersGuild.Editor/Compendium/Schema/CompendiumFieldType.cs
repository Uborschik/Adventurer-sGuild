namespace AdventurersGuild.Editor.Compendium.Schema;

public enum CompendiumFieldType
{
    Id,
    Localized,        // { ru, en } — просто строки
    LocalizedCase,    // { ru: { nom, gen, dat, acc, ins, pre }, en: "..." }
    Number,
    Bool,
    Enum,
    Range,            // { min, max }
    Reference,        // id другого kind
    ReferenceList,    // список id
    Dictionary,       // { keyReference: value }
    Nested,           // вложенный объект с подсхемой
    ObjectList,       // список объектов с подсхемой
    Json,             // fallback
}