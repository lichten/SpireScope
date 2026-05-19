namespace StS2Shared.Services;

public record MechanicDef(string EnLabel, string JaLabel, Func<string, bool> Filter);
public record CharGroup(string EnLabel, string JaLabel, MechanicDef[] Mechanics);

public static class CharacterMechanics
{
    public static readonly CharGroup[] All =
    [
        new("Necrobinder", "ネクロバインダー",
        [
            new("Osty", "オスティ", CardDatabaseService.IsNecroOsty),
            new("Soul", "ソウル", CardDatabaseService.IsNecroSoul),
            new("Doom", "破滅", CardDatabaseService.IsNecroDoom),
        ]),
        new("Ironclad", "アイアンクラッド",
        [
            new("Strength", "筋力",           CardDatabaseService.IsIroncladStrength),
            new("Exhaust",  "廃棄",           CardDatabaseService.IsIroncladExhaust),
            new("Strike",   "ストライク",     CardDatabaseService.IsIroncladStrike),
        ]),
        new("Silent", "サイレント",
        [
            new("Poison", "毒",   CardDatabaseService.IsSilentPoison),
            new("Shiv",   "ナイフ", CardDatabaseService.IsSilentShiv),
        ]),
        new("Defect", "ディフェクト",
        [
            new("Channel",  "オーブ",       CardDatabaseService.IsDefectChannel),
            new("FocusPower", "集中力",      CardDatabaseService.IsDefectFocus),
            new("0 Energy", "0エナジー",    CardDatabaseService.IsDefectZeroEnergy),
        ]),
        new("Regent", "リージェント",
        [
            new("Forge / Sovereign Blade", "鋳造 / ソヴリン・ブレード",
                id => CardDatabaseService.IsRegentForge(id) || CardDatabaseService.IsRegentBlade(id)),
            new("Card Creation", "カード作成",         CardDatabaseService.IsRegentCreate),
            new("Star Gain",     "Starを得る",         CardDatabaseService.IsRegentStarGain),
            new("Star Spend",    "Starを使用する",     CardDatabaseService.IsRegentStarSpend),
        ]),
        new("Other", "その他", []),
        new("Common", "共通",
        [
            new("Weak",       "脱力", CardDatabaseService.IsWeak),
            new("Vulnerable", "弱体", CardDatabaseService.IsVulnerable),
        ]),
    ];

    public static MechanicDef[] MechanicsFor(string enLabel) =>
        All.FirstOrDefault(g => g.EnLabel == enLabel)?.Mechanics ?? [];
}
