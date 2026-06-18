using System.Reflection;
using System.Text.Json;

namespace StS2Shared.Services;

/// <summary>
/// マーチャント（店）の価格データを読む。
/// card-type-extractor が DLL の IL（<c>MerchantCardEntry</c> / <c>MerchantPotionEntry</c> /
/// <c>RelicModel.get_MerchantCost</c> / <c>MerchantCardRemovalEntry</c>）から生成した
/// merchant_prices.json（バージョンフォルダ）を参照する。
/// 価格変動（±%）はランごとの乱数で実数は不定のため、レンジ（min/max）のみ保持する。
/// </summary>
public static class MerchantPriceService
{
    /// <summary>価格変動レンジ（基本価格 × [Min, Max]）。</summary>
    public readonly record struct Variance(double Min, double Max);

    /// <summary>カード価格: レアリティ別基本価格・無色マークアップ・セール率・変動レンジ。</summary>
    public readonly record struct CardPrices(
        IReadOnlyDictionary<string, int> Base, double ColorlessMarkup, double SaleMultiplier, Variance Variance);

    /// <summary>ポーション/レリック価格: レアリティ別基本価格・変動レンジ。</summary>
    public readonly record struct ItemPrices(IReadOnlyDictionary<string, int> Base, Variance Variance);

    /// <summary>カード除去: アセンション閾値・基本値・増分（通常 / アセンション以上）。</summary>
    public readonly record struct RemovalPrices(
        int AscensionThreshold, int BaseCost, int BaseCostAscension, int PriceIncrease, int PriceIncreaseAscension);

    public static CardPrices Card { get; }
    public static ItemPrices Potion { get; }
    public static ItemPrices Relic { get; }
    public static RemovalPrices CardRemoval { get; }

    /// <summary>データが読めたか（DLL 未生成バージョン等では false）。</summary>
    public static bool Available { get; }

    static MerchantPriceService()
    {
        var asm = Assembly.GetExecutingAssembly();
        var name = ResourceResolver.ResolveVersioned(asm, "merchant_prices.json");
        if (name is null) return;

        using var stream = asm.GetManifestResourceStream(name)!;
        using var doc = JsonDocument.Parse(stream);
        var root = doc.RootElement;

        static IReadOnlyDictionary<string, int> Bases(JsonElement parent)
        {
            var d = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            if (parent.TryGetProperty("base", out var b))
                foreach (var p in b.EnumerateObject())
                    d[p.Name] = p.Value.GetInt32();
            return d;
        }
        static Variance Var(JsonElement parent)
        {
            if (parent.TryGetProperty("variance", out var v))
                return new Variance(v.GetProperty("min").GetDouble(), v.GetProperty("max").GetDouble());
            return new Variance(1, 1);
        }

        var card = root.GetProperty("card");
        Card = new CardPrices(Bases(card),
            card.GetProperty("colorlessMarkup").GetDouble(),
            card.GetProperty("saleMultiplier").GetDouble(),
            Var(card));

        var potion = root.GetProperty("potion");
        Potion = new ItemPrices(Bases(potion), Var(potion));

        var relic = root.GetProperty("relic");
        Relic = new ItemPrices(Bases(relic), Var(relic));

        var rm = root.GetProperty("cardRemoval");
        CardRemoval = new RemovalPrices(
            rm.GetProperty("ascensionThreshold").GetInt32(),
            rm.GetProperty("baseCost").GetInt32(),
            rm.GetProperty("baseCostAscension").GetInt32(),
            rm.GetProperty("priceIncrease").GetInt32(),
            rm.GetProperty("priceIncreaseAscension").GetInt32());

        Available = true;
    }

    /// <summary>基本価格 b に変動レンジを適用した最小〜最大の整数レンジ（四捨五入）。</summary>
    public static (int Min, int Max) Range(int b, Variance v) =>
        ((int)Math.Round(b * v.Min), (int)Math.Round(b * v.Max));
}
