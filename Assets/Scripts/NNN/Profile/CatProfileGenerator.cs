using System;
using System.Collections.Generic;
using System.Linq;

namespace NNN
{
    public enum CatProfileAbilityBand { VeryLow, Low, Middle, High, VeryHigh }

    /// <summary>CatDefinitionだけから決定的な表示プロフィールを作る純粋変換。</summary>
    public static class CatProfileGenerator
    {
        public const string EscapeCaution = "出入口が開くと、外へ出ようとすることがある。";

        private enum Anchor { Outdoor, NightHighPlace, ActivePlayful, HighPlaceTimid, QuietIndependent,
            AffectionateLonely, Playful, Timid, Quiet, Night, HighPlace, Ability }
        private enum Ability { Activity, Sociability, Independence, Adaptability }

        private sealed class TagRule
        {
            public readonly string Tag; public readonly int Priority;
            public TagRule(string tag, int priority) { Tag = tag; Priority = priority; }
        }

        private sealed class Context
        {
            public readonly CatDefinition Cat;
            public readonly List<string> Traits;
            public readonly HashSet<string> TraitSet;
            public readonly int Activity, Sociability, Independence, Adaptability;
            public Context(CatDefinition cat)
            {
                Cat = cat; Traits = NormalizeTraits(cat.Traits);
                TraitSet = new HashSet<string>(Traits, StringComparer.Ordinal);
                Activity = Clamp(cat.Activity); Sociability = Clamp(cat.Sociability);
                Independence = Clamp(cat.Independence); Adaptability = Clamp(cat.Adaptability);
            }
            public bool Has(string id) => TraitSet.Contains(id);
            public int Value(Ability a) => a == Ability.Activity ? Activity : a == Ability.Sociability ? Sociability :
                a == Ability.Independence ? Independence : Adaptability;
            public CatProfileAbilityBand Band(Ability a) => GetAbilityBand(Value(a));
        }

        private static readonly Dictionary<string, TagRule> Tags = new Dictionary<string, TagRule>(StringComparer.Ordinal)
        {
            {"affectionate", new TagRule("#人なつっこい", 80)}, {"gentle", new TagRule("#おだやか", 75)},
            {"timid", new TagRule("#慎重", 95)}, {"night", new TagRule("#夜型", 90)},
            {"high_place", new TagRule("#高いところ好き", 90)}, {"active", new TagRule("#活発", 60)},
            {"playful", new TagRule("#遊び好き", 85)}, {"independent", new TagRule("#ひとり上手", 60)},
            {"quiet", new TagRule("#物静か", 85)}, {"lonely", new TagRule("#寂しがり", 95)},
            {"outdoor", new TagRule("#外に興味津々", 100)}
        };

        // 配列順が仕様上の優先順。判定と文章を分離し、将来の並べ替えを局所化する。
        private static readonly KeyValuePair<Anchor, Func<Context, bool>>[] Anchors =
        {
            Rule(Anchor.Outdoor, c => c.Has("outdoor")),
            Rule(Anchor.NightHighPlace, c => c.Has("night") && c.Has("high_place")),
            Rule(Anchor.ActivePlayful, c => c.Has("active") && c.Has("playful")),
            Rule(Anchor.HighPlaceTimid, c => c.Has("high_place") && c.Has("timid")),
            Rule(Anchor.QuietIndependent, c => c.Has("quiet") && c.Has("independent")),
            Rule(Anchor.AffectionateLonely, c => c.Has("affectionate") && c.Has("lonely")),
            Rule(Anchor.Playful, c => c.Has("playful")), Rule(Anchor.Timid, c => c.Has("timid")),
            Rule(Anchor.Quiet, c => c.Has("quiet")), Rule(Anchor.Night, c => c.Has("night")),
            Rule(Anchor.HighPlace, c => c.Has("high_place")), Rule(Anchor.Ability, c => true)
        };

        public static CatProfilePresentation Generate(CatDefinition cat)
        {
            if (ReferenceEquals(cat, null)) throw new ArgumentNullException(nameof(cat));
            var c = new Context(cat);
            var anchor = Anchors.First(x => x.Value(c)).Key;
            return new CatProfilePresentation(cat.DisplayName, cat.Age, BuildTags(c), BuildBehavior(c, anchor),
                c.Has("escape") ? EscapeCaution : null);
        }

        public static CatProfileAbilityBand GetAbilityBand(int value)
        {
            value = Clamp(value);
            if (value <= 19) return CatProfileAbilityBand.VeryLow;
            if (value <= 39) return CatProfileAbilityBand.Low;
            if (value <= 59) return CatProfileAbilityBand.Middle;
            if (value <= 79) return CatProfileAbilityBand.High;
            return CatProfileAbilityBand.VeryHigh;
        }

        private static KeyValuePair<Anchor, Func<Context, bool>> Rule(Anchor a, Func<Context, bool> p) =>
            new KeyValuePair<Anchor, Func<Context, bool>>(a, p);
        private static int Clamp(int value) => Math.Max(0, Math.Min(100, value));

        private static List<string> NormalizeTraits(IEnumerable<TraitDefinition> traits)
        {
            var result = new List<string>(); var seen = new HashSet<string>(StringComparer.Ordinal);
            if (traits == null) return result;
            foreach (var trait in traits)
                if (!ReferenceEquals(trait, null) && !string.IsNullOrEmpty(trait.Id) && seen.Add(trait.Id)) result.Add(trait.Id);
            return result;
        }

        private static List<string> BuildTags(Context c)
        {
            var combined = c.Has("affectionate") && c.Has("lonely");
            var candidates = new List<Tuple<int, int, string>>(); var seen = new HashSet<string>();
            for (var i = 0; i < c.Traits.Count; i++)
            {
                var id = c.Traits[i];
                if (id == "escape" || !Tags.TryGetValue(id, out var rule)) continue;
                var tag = combined && id == "affectionate" ? "#甘えん坊" : rule.Tag;
                if (seen.Add(tag)) candidates.Add(Tuple.Create(i, rule.Priority, tag));
            }
            if (candidates.Count <= 3) return candidates.Select(x => x.Item3).ToList();
            return candidates.OrderByDescending(x => x.Item2).ThenBy(x => x.Item1).Take(3).Select(x => x.Item3).ToList();
        }

        private static string BuildBehavior(Context c, Anchor a)
        {
            switch (a)
            {
                case Anchor.Outdoor: return Outdoor(c);
                case Anchor.NightHighPlace: return NightHigh(c);
                case Anchor.ActivePlayful: return ActivePlay(c);
                case Anchor.HighPlaceTimid: return "高い場所へ登って安全を確かめてから、気になる方向へ動き始める。新しい状況では、周囲を見ながら時間をかける。";
                case Anchor.QuietIndependent: return QuietIndependent(c);
                case Anchor.AffectionateLonely: return AffectionateLonely(c);
                case Anchor.Playful: return Playful(c);
                case Anchor.Timid: return Timid(c);
                case Anchor.Quiet: return "静かにくつろぐ時間を多く持ち、ときどき人の近くへ来る。環境が変わっても、落ち着ける場所を比較的見つけやすい。";
                case Anchor.Night: return "夜になると場所を移したり、周囲を見回ったりする。" + Distance(c);
                case Anchor.HighPlace: return "高い場所へ登り、周囲を眺めることを好む。" + Distance(c);
                default: return AbilityText(c, Salient(c));
            }
        }

        private static string Outdoor(Context c)
        {
            if (c.Has("independent") && c.Band(Ability.Activity) == CatProfileAbilityBand.VeryHigh)
                return "窓の外の匂いや気配へ関心を向けながら、自分で室内を広く動く。人から離れていても、自分の時間を保ちやすい。";
            // 外への関心とescapeが同居する場合は、Behaviorでは環境確認だけを述べ、注意文とは重複させない。
            if (c.Has("escape"))
                return "窓の外の匂いや気配へ反応し、気になる方向を確かめに行く。新しい状況では、安全を確かめながら時間をかける。";
            return "窓の外の匂いや気配へ反応し、気になる方向を確かめに行く。" + Adapt(c);
        }

        private static string NightHigh(Context c) => c.Has("timid")
            ? "夜になると高い場所へ移り、少し離れたところから周囲を眺める。初めての状況では、すぐに近づかず様子を見る。"
            : "夜になると高い場所へ移り、周囲を眺めながら自分の時間を過ごす。" + Adapt(c);

        private static string ActivePlay(Context c)
        {
            if (c.Has("high_place")) return "おもちゃや気になるものへすぐ反応し、高い場所も使いながら次の刺激を探す。交流と自分の探索を行き来する。";
            if (c.Has("night")) return "人やおもちゃの動きへすぐ反応し、そのまま遊び続けようとする。夜にも交流や刺激を求めることがある。";
            return "人やおもちゃの動きへすぐ反応し、交流を交えながら遊び続けようとする。遊びの合間には自分で気になる場所を確かめることもある。";
        }

        private static string QuietIndependent(Context c)
        {
            if (c.Has("night")) return "自分の場所で静かに過ごし、夜になると周囲を見回るために場所を移す。長いひとり時間も、自分のペースで過ごせる。";
            if (c.Has("timid")) return "静かな場所から周囲を観察し、すぐには近づかず様子を見る。新しい状況では、十分に安全を確かめながら少しずつ馴染む。";
            if (c.Band(Ability.Activity) == CatProfileAbilityBand.VeryLow && c.Has("gentle"))
                return "落ち着ける場所で静かに過ごし、必要なときにだけ人の近くへ来る。" + Adapt(c);
            if (c.Band(Ability.Activity) == CatProfileAbilityBand.VeryLow)
                return "落ち着ける場所を見つけ、長く静かに過ごすことが多い。必要なときにだけ場所を移し、自分の時間を保つ。";
            return "自分の場所で静かに過ごし、必要なときにだけ場所を移す。人と離れていても、自分の時間を保ちやすい。";
        }

        private static string AffectionateLonely(Context c) => c.Has("gentle")
            ? "人の気配を探し、静かに近くへ寄って触れ合いを求める。" + Adapt(c)
            : "人の気配を探し、自分から近くへ寄って触れ合いを求める。ひとりで長く過ごすより、人のそばを選ぶことが多い。";

        private static string Playful(Context c)
        {
            if (c.Has("high_place")) return "人やおもちゃの動きへ反応し、高い場所へ移って周囲を見ることも好む。" + Adapt(c);
            if (c.Has("independent")) return "気になるものへ反応して遊び始め、満足すると自分の場所へ戻る。人と離れていても、自分の時間を保ちやすい。";
            return "おもちゃや人の動きによく反応する。" + Distance(c);
        }

        private static string Timid(Context c) => c.Has("independent") && c.Band(Ability.Activity) == CatProfileAbilityBand.High
            ? "動く量は多いが、初めての状況では距離を取り、安全を確かめてから行動する。人と離れていても、自分の時間を保ちやすい。"
            : "初めての状況では、すぐに近づかず様子を見る。" + Adapt(c);

        private static Ability Salient(Context c)
        {
            var all = new[] { Ability.Activity, Ability.Sociability, Ability.Independence, Ability.Adaptability };
            var best = all[0]; var score = Math.Abs(c.Value(best) - 50);
            for (var i = 1; i < all.Length; i++)
            {
                var next = Math.Abs(c.Value(all[i]) - 50);
                if (next > score) { best = all[i]; score = next; }
            }
            return best;
        }

        private static string AbilityText(Context c, Ability a)
        {
            if (c.Has("affectionate") && c.Has("gentle"))
                return c.Band(Ability.Adaptability) == CatProfileAbilityBand.VeryHigh
                    ? "人の近くへ自分から寄り、触れ合う時間を持つ。" + Adapt(c)
                    : c.Has("escape")
                        ? "人の近くへ寄って穏やかに過ごす。新しい状況では、様子を見ながら普段の過ごし方へ戻る。"
                        : "人の近くへ寄って穏やかに過ごす。" + Adapt(c, true);
            if (a == Ability.Activity) return ActivityText(c.Band(a));
            if (a == Ability.Sociability) return SocialText(c.Band(a));
            if (a == Ability.Independence) return IndependentText(c.Band(a));
            return Adapt(c, true);
        }

        private static string ActivityText(CatProfileAbilityBand b) => b == CatProfileAbilityBand.VeryLow ? "落ち着ける場所を見つけ、長く過ごすことが多い。" : b == CatProfileAbilityBand.Low ? "静かにくつろぐ時間を多く持つ。" : b == CatProfileAbilityBand.Middle ? "くつろぐ時間と場所を移す時間を行き来する。" : b == CatProfileAbilityBand.High ? "気になるものへ反応し、よく場所を移す。" : "気になるものがあるとすぐ動き、室内を広く使う。";
        private static string SocialText(CatProfileAbilityBand b) => b == CatProfileAbilityBand.VeryLow ? "人とは距離を取り、自分の場所を優先する。" : b == CatProfileAbilityBand.Low ? "人から少し離れた場所で過ごすことが多い。" : b == CatProfileAbilityBand.Middle ? "自分の時間を持ちながら、必要なときに人へ近づく。" : b == CatProfileAbilityBand.High ? "人の近くへ来て、交流する時間を持つ。" : "人の気配を探し、自分から近くへ寄ることが多い。";
        private static string IndependentText(CatProfileAbilityBand b) => b == CatProfileAbilityBand.VeryLow ? "ひとりで過ごすより、人の近くで落ち着こうとする。" : b == CatProfileAbilityBand.Low ? "人のそばを選ぶことが多い。" : b == CatProfileAbilityBand.Middle ? "交流と自分の時間を行き来する。" : b == CatProfileAbilityBand.High ? "人と離れていても、自分の時間を保ちやすい。" : "長いひとり時間も、自分の場所で過ごせる。";

        private static string Adapt(Context c, bool allowHigh = false)
        {
            var b = c.Band(Ability.Adaptability);
            if (b == CatProfileAbilityBand.VeryLow) return "新しい状況では、十分に様子を見ながら少しずつ馴染む。";
            if (b == CatProfileAbilityBand.Low) return "新しい状況では、安全を確かめながら時間をかける。";
            if (b == CatProfileAbilityBand.Middle) return "新しい状況では、様子を見ながら普段の過ごし方へ戻る。";
            if (b == CatProfileAbilityBand.High) return allowHigh ? "環境の変化にも比較的無理なく馴染む。" : string.Empty;
            return "新しい状況でも、落ち着ける場所を比較的見つけやすい。";
        }

        private static string Distance(Context c)
        {
            var b = c.Band(Ability.Independence);
            return b == CatProfileAbilityBand.High || b == CatProfileAbilityBand.VeryHigh ? IndependentText(b) : SocialText(c.Band(Ability.Sociability));
        }
    }
}
