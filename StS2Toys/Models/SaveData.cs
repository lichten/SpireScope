using System.Text.Json.Serialization;

namespace StS2Toys.Models;

class RunSaveData
{
    [JsonPropertyName("ascension")]
    public int Ascension { get; init; }

    [JsonPropertyName("current_act_index")]
    public int CurrentActIndex { get; init; }

    [JsonPropertyName("schema_version")]
    public int SchemaVersion { get; init; }

    [JsonPropertyName("players")]
    public List<PlayerData> Players { get; init; } = [];
}

class PlayerData
{
    [JsonPropertyName("character_id")]
    public string CharacterId { get; init; } = "";

    [JsonPropertyName("current_hp")]
    public int CurrentHp { get; init; }

    [JsonPropertyName("max_hp")]
    public int MaxHp { get; init; }

    [JsonPropertyName("gold")]
    public int Gold { get; init; }

    [JsonPropertyName("max_energy")]
    public int MaxEnergy { get; init; }

    [JsonPropertyName("deck")]
    public List<CardData> Deck { get; init; } = [];

    [JsonPropertyName("relics")]
    public List<RelicData> Relics { get; init; } = [];
}

class EnchantmentData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("amount")]
    public int Amount { get; init; }
}

class CardData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("floor_added_to_deck")]
    public int FloorAddedToDeck { get; init; }

    [JsonPropertyName("current_upgrade_level")]
    public int? CurrentUpgradeLevel { get; init; }

    [JsonPropertyName("enchantment")]
    public EnchantmentData? Enchantment { get; init; }

    [JsonPropertyName("props")]
    public CardProps? Props { get; init; }

    public int? GetPropInt(string name) =>
        Props?.Ints.FirstOrDefault(x => x.Name == name)?.Value;
}

class CardProps
{
    [JsonPropertyName("ints")]
    public List<NamedInt> Ints { get; init; } = [];
}

class NamedInt
{
    [JsonPropertyName("name")]
    public string Name { get; init; } = "";

    [JsonPropertyName("value")]
    public int Value { get; init; }
}

class RelicData
{
    [JsonPropertyName("id")]
    public string Id { get; init; } = "";

    [JsonPropertyName("floor_added_to_deck")]
    public int FloorAddedToDeck { get; init; }
}
