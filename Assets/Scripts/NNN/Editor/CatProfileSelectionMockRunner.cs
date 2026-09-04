using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>実選出3匹をプレイヤー向け固定プロフィールで並べ、比較可能性だけを観測する。</summary>
    public static class CatProfileSelectionMockRunner
    {
        private const float ProfileSimilarTagJaccard = 0.66f;
        private static readonly int[] Seeds = { 11111, 22222 };
        private static readonly string[,] FocusPairs =
        {
            {"シロ", "スズ"}, {"モモ", "コハル"}, {"ミケ", "リン"},
            {"ゴマ", "ナギ"}, {"トラ", "ポン"}, {"ハチ", "キリ"}
        };

        private sealed class ProfileInfo
        {
            public CatDefinition Cat;
            public CatProfilePresentation Presentation;
            public string Anchor;
            public string Secondary;
            public string Bands;
            public int TagLength;
            public int BehaviorLength;
            public int CautionLength;
            public int TotalLength;
            public int SentenceCount;
        }

        private sealed class PairInfo
        {
            public ProfileInfo A;
            public ProfileInfo B;
            public int SharedTags;
            public int TagDifference;
            public float TagJaccard;
            public bool BehaviorExact;
            public bool AnchorMatch;
            public bool CautionDifference;
            public string Classification;
            public bool Similar;
        }

        private sealed class SetInfo
        {
            public string HumanId;
            public string HumanName;
            public int Seed;
            public CandidateSetResult Selected;
            public List<ProfileInfo> Profiles;
            public List<PairInfo> Pairs;
            public float MaxJaccard;
            public float AverageJaccard;
            public int WeakPairs;
            public bool SimilarPair;
            public bool Monotone;
        }

        [MenuItem("NNN/Test Data/Run Cat Profile Selection Mock")]
        public static void Run()
        {
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            // プロフィールv0.1の対象20匹を同一母集団にし、H01～H10で比較する診断専用構成。
            var cats = existing.Cats.Concat(external.Cats).ToList();
            var selector = new CatCandidateSelector();
            var builder = new CandidateSetBuilder(false);
            var sets = new List<SetInfo>();

            foreach (var human in existing.HumanBenchmarks)
            {
                foreach (var seed in Seeds)
                {
                    var selected = builder.Build(selector.EvaluateAll(cats, human.CreateCase(seed)), seed);
                    sets.Add(AnalyzeSet(human, seed, selected));
                }
            }

            var output = BuildTextReport(sets);
            string root = Directory.GetParent(Application.dataPath).FullName;
            string outputDirectory = Path.Combine(root, "outputs");
            Directory.CreateDirectory(outputDirectory);
            File.WriteAllText(Path.Combine(outputDirectory, "cat-profile-selection-mock.txt"), output, new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(outputDirectory, "cat-profile-selection-mock-summary.csv"), BuildSummaryCsv(sets), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(outputDirectory, "cat-profile-selection-mock-pairs.csv"), BuildPairsCsv(sets), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(outputDirectory, "cat-profile-selection-mock-profiles.csv"), BuildProfilesCsv(sets), new UTF8Encoding(true));
            Debug.Log(output);
            Debug.Log("Cat profile selection mock exported: " + outputDirectory);
        }

        private static SetInfo AnalyzeSet(HumanBenchmarkDefinition human, int seed, CandidateSetResult selected)
        {
            var evaluations = new[] { selected.catA, selected.catB, selected.catC };
            var profiles = evaluations.Select(x => AnalyzeProfile(x.Cat)).ToList();
            var pairs = new List<PairInfo>
            {
                AnalyzePair(profiles[0], profiles[1]), AnalyzePair(profiles[0], profiles[2]),
                AnalyzePair(profiles[1], profiles[2])
            };
            return new SetInfo
            {
                HumanId = human.BenchmarkId,
                HumanName = human.HumanName,
                Seed = seed,
                Selected = selected,
                Profiles = profiles,
                Pairs = pairs,
                MaxJaccard = pairs.Max(x => x.TagJaccard),
                AverageJaccard = pairs.Average(x => x.TagJaccard),
                WeakPairs = pairs.Count(x => x.Classification == "Weak"),
                SimilarPair = pairs.Any(x => x.Similar),
                Monotone = pairs.Count(x => x.Classification == "Weak") >= 2
            };
        }

        private static ProfileInfo AnalyzeProfile(CatDefinition cat)
        {
            var presentation = CatProfileGenerator.Generate(cat);
            string tags = string.Join(" ", presentation.PlayerTags.ToArray());
            int cautionLength = string.IsNullOrEmpty(presentation.Caution) ? 0 : presentation.Caution.Length;
            return new ProfileInfo
            {
                Cat = cat,
                Presentation = presentation,
                Anchor = DiagnoseAnchor(cat),
                Secondary = DiagnoseSecondary(cat),
                Bands = string.Format("A={0},S={1},I={2},D={3}", Band(cat.Activity), Band(cat.Sociability),
                    Band(cat.Independence), Band(cat.Adaptability)),
                TagLength = tags.Length,
                BehaviorLength = presentation.Behavior.Length,
                CautionLength = cautionLength,
                TotalLength = presentation.DisplayName.Length + presentation.Age.ToString(CultureInfo.InvariantCulture).Length +
                    tags.Length + presentation.Behavior.Length + cautionLength,
                SentenceCount = presentation.Behavior.Count(x => x == '。')
            };
        }

        private static PairInfo AnalyzePair(ProfileInfo a, ProfileInfo b)
        {
            var tagsA = new HashSet<string>(a.Presentation.PlayerTags);
            var tagsB = new HashSet<string>(b.Presentation.PlayerTags);
            int shared = tagsA.Intersect(tagsB).Count();
            int union = tagsA.Union(tagsB).Count();
            int differences = tagsA.Except(tagsB).Count() + tagsB.Except(tagsA).Count();
            float jaccard = union == 0 ? 1f : shared / (float)union;
            bool exact = Normalize(a.Presentation.Behavior) == Normalize(b.Presentation.Behavior);
            bool anchorMatch = a.Anchor == b.Anchor;
            bool cautionDifference = string.IsNullOrEmpty(a.Presentation.Caution) != string.IsNullOrEmpty(b.Presentation.Caution);
            bool uniqueBoth = tagsA.Except(tagsB).Any() && tagsB.Except(tagsA).Any();
            string classification;
            if (cautionDifference || uniqueBoth || !anchorMatch || (jaccard < 0.5f && !exact)) classification = "Strong";
            else if (!exact || differences > 0) classification = "Moderate";
            else classification = "Weak";
            return new PairInfo
            {
                A = a, B = b, SharedTags = shared, TagDifference = differences, TagJaccard = jaccard,
                BehaviorExact = exact, AnchorMatch = anchorMatch, CautionDifference = cautionDifference,
                Classification = classification,
                Similar = jaccard >= ProfileSimilarTagJaccard && anchorMatch && !cautionDifference
            };
        }

        private static string BuildTextReport(IList<SetInfo> sets)
        {
            var text = new StringBuilder();
            text.AppendLine("Cat Profile Selection Mock (H01-H10 x Seeds 11111/22222, 20 cats, BadOverlap Penalty OFF)");
            foreach (var set in sets)
            {
                text.AppendLine(); text.AppendLine(new string('=', 50));
                text.AppendLine(set.HumanId + " " + set.HumanName); text.AppendLine("Seed: " + set.Seed);
                text.AppendLine("Selected Cats: " + string.Join(" / ", set.Profiles.Select(x => x.Cat.DisplayName).ToArray()));
                text.AppendLine(new string('=', 50));
                for (var i = 0; i < set.Profiles.Count; i++) AppendPlayerProfile(text, i + 1, set.Profiles[i]);
                text.AppendLine("[Profile diagnostics]");
                foreach (var pair in set.Pairs)
                    text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}/{1}: SharedTags={2} TagJaccard={3:0.000} TagDifference={4} AnchorMatch={5} CautionDifference={6} BehaviorExact={7} Class={8}",
                        pair.A.Cat.DisplayName, pair.B.Cat.DisplayName, pair.SharedTags, pair.TagJaccard,
                        pair.TagDifference, pair.AnchorMatch, pair.CautionDifference, pair.BehaviorExact, pair.Classification));
                text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                    "Set: MaxTagJaccard={0:0.000} AverageTagJaccard={1:0.000} WeakPairs={2} SimilarPair={3} Monotone={4}",
                    set.MaxJaccard, set.AverageJaccard, set.WeakPairs, set.SimilarPair, set.Monotone));
                AppendFocusDetails(text, set);
            }
            AppendAggregates(text, sets);
            return text.ToString();
        }

        private static void AppendPlayerProfile(StringBuilder text, int index, ProfileInfo profile)
        {
            text.AppendLine(); text.AppendLine(string.Format("[{0}] {1}　{2}歳", index, profile.Presentation.DisplayName, profile.Presentation.Age));
            text.AppendLine(string.Join(" ", profile.Presentation.PlayerTags.ToArray())); text.AppendLine();
            foreach (var sentence in SplitSentences(profile.Presentation.Behavior)) text.AppendLine(sentence);
            if (!string.IsNullOrEmpty(profile.Presentation.Caution))
            {
                text.AppendLine(); text.AppendLine("注意:"); text.AppendLine(profile.Presentation.Caution);
            }
            text.AppendLine(string.Format("[Length Tags={0} Behavior={1} Caution={2} Total={3} Sentences={4}]",
                profile.TagLength, profile.BehaviorLength, profile.CautionLength, profile.TotalLength, profile.SentenceCount));
        }

        private static void AppendFocusDetails(StringBuilder text, SetInfo set)
        {
            for (var i = 0; i < FocusPairs.GetLength(0); i++)
            {
                var a = set.Profiles.FirstOrDefault(x => x.Cat.DisplayName == FocusPairs[i, 0]);
                var b = set.Profiles.FirstOrDefault(x => x.Cat.DisplayName == FocusPairs[i, 1]);
                if (a == null || b == null) continue;
                text.AppendLine("[Focus pair] " + a.Cat.DisplayName + " / " + b.Cat.DisplayName);
                foreach (var profile in new[] { a, b })
                    text.AppendLine(string.Format("{0}: Tags={1} Anchor={2} Secondary={3} Bands={4} Behavior={5} Caution={6}",
                        profile.Cat.DisplayName, string.Join("/", profile.Presentation.PlayerTags.ToArray()), profile.Anchor,
                        profile.Secondary, profile.Bands, profile.Presentation.Behavior, profile.Presentation.Caution ?? "なし"));
            }
        }

        private static void AppendAggregates(StringBuilder text, IList<SetInfo> sets)
        {
            var profiles = sets.SelectMany(x => x.Profiles).ToList();
            var lengths = profiles.Select(x => x.BehaviorLength).OrderBy(x => x).ToList();
            text.AppendLine(); text.AppendLine("=== Aggregate ===");
            text.AppendLine("Selected sets: " + sets.Count);
            text.AppendLine("Profile Similar Pair occurrences: " + sets.Sum(x => x.Pairs.Count(p => p.Similar)));
            text.AppendLine("Monotone Set Candidates: " + sets.Count(x => x.Monotone));
            text.AppendLine("Caution profiles: " + profiles.Count(x => !string.IsNullOrEmpty(x.Presentation.Caution)));
            text.AppendLine(string.Format(CultureInfo.InvariantCulture,
                "BehaviorLength Min={0} Max={1} Average={2:0.00} Median={3:0.00} P90={4}", lengths.First(), lengths.Last(),
                lengths.Average(), Percentile(lengths, 0.5f), Percentile(lengths, 0.9f)));
            text.AppendLine("One sentence profiles: " + profiles.Count(x => x.SentenceCount == 1));
            text.AppendLine("Two sentence profiles: " + profiles.Count(x => x.SentenceCount == 2));
            text.AppendLine("Cat selection counts:");
            foreach (var group in profiles.GroupBy(x => x.Cat.DisplayName).OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
                text.AppendLine(group.Key + ": " + group.Count());
            text.AppendLine("Tag combination counts:");
            foreach (var group in profiles.GroupBy(x => string.Join("/", x.Presentation.PlayerTags.ToArray())).OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
                text.AppendLine(group.Key + ": " + group.Count());
            text.AppendLine("Behavior anchor counts:");
            foreach (var group in profiles.GroupBy(x => x.Anchor).OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
                text.AppendLine(group.Key + ": " + group.Count());
            text.AppendLine("Sets containing duplicate tag combinations: " + sets.Count(x => x.Profiles.GroupBy(p => string.Join("/", p.Presentation.PlayerTags.ToArray())).Any(g => g.Count() >= 2)));
            text.AppendLine("Focus pair occurrences:");
            for (var i = 0; i < FocusPairs.GetLength(0); i++)
                text.AppendLine(FocusPairs[i, 0] + "/" + FocusPairs[i, 1] + ": " + sets.Count(s =>
                    s.Profiles.Any(p => p.Cat.DisplayName == FocusPairs[i, 0]) && s.Profiles.Any(p => p.Cat.DisplayName == FocusPairs[i, 1])));
        }

        private static string BuildSummaryCsv(IEnumerable<SetInfo> sets)
        {
            var csv = new StringBuilder("HumanId,HumanName,Seed,Cat1,Cat2,Cat3,MaxTagJaccard,AverageTagJaccard,WeakPairCount,ProfileSimilarPair,MonotoneSetCandidate\n");
            foreach (var s in sets) csv.AppendLine(string.Join(",", Csv(s.HumanId), Csv(s.HumanName), s.Seed.ToString(),
                Csv(s.Profiles[0].Cat.DisplayName), Csv(s.Profiles[1].Cat.DisplayName), Csv(s.Profiles[2].Cat.DisplayName),
                s.MaxJaccard.ToString("0.000", CultureInfo.InvariantCulture), s.AverageJaccard.ToString("0.000", CultureInfo.InvariantCulture),
                s.WeakPairs.ToString(), s.SimilarPair.ToString(), s.Monotone.ToString()));
            return csv.ToString();
        }

        private static string BuildPairsCsv(IEnumerable<SetInfo> sets)
        {
            var csv = new StringBuilder("HumanId,Seed,CatA,CatB,SharedTagCount,TagJaccard,PrimaryAnchorMatch,HasCautionDifference,BehaviorExactMatch,DifferentiationClass,ProfileSimilarPair\n");
            foreach (var s in sets) foreach (var p in s.Pairs) csv.AppendLine(string.Join(",", Csv(s.HumanId), s.Seed.ToString(),
                Csv(p.A.Cat.DisplayName), Csv(p.B.Cat.DisplayName), p.SharedTags.ToString(), p.TagJaccard.ToString("0.000", CultureInfo.InvariantCulture),
                p.AnchorMatch.ToString(), p.CautionDifference.ToString(), p.BehaviorExact.ToString(), p.Classification, p.Similar.ToString()));
            return csv.ToString();
        }

        private static string BuildProfilesCsv(IEnumerable<SetInfo> sets)
        {
            var csv = new StringBuilder("HumanId,Seed,Cat,Age,Tags,Behavior,Caution,PrimaryAnchor,SecondaryFeature,AbilityBands,TagTextLength,BehaviorLength,CautionLength,TotalVisibleCharacters,SentenceCount\n");
            foreach (var s in sets) foreach (var p in s.Profiles) csv.AppendLine(string.Join(",", Csv(s.HumanId), s.Seed.ToString(),
                Csv(p.Cat.DisplayName), p.Presentation.Age.ToString(), Csv(string.Join(" ", p.Presentation.PlayerTags.ToArray())),
                Csv(p.Presentation.Behavior), Csv(p.Presentation.Caution), p.Anchor, p.Secondary, Csv(p.Bands), p.TagLength.ToString(),
                p.BehaviorLength.ToString(), p.CautionLength.ToString(), p.TotalLength.ToString(), p.SentenceCount.ToString()));
            return csv.ToString();
        }

        private static string DiagnoseAnchor(CatDefinition cat)
        {
            if (Has(cat, "outdoor")) return "outdoor";
            if (Has(cat, "night") && Has(cat, "high_place")) return "night+high_place";
            if (Has(cat, "active") && Has(cat, "playful")) return "active+playful";
            if (Has(cat, "high_place") && Has(cat, "timid")) return "high_place+timid";
            if (Has(cat, "quiet") && Has(cat, "independent")) return "quiet+independent";
            if (Has(cat, "affectionate") && Has(cat, "lonely")) return "affectionate+lonely";
            foreach (var id in new[] { "playful", "timid", "quiet", "night", "high_place" }) if (Has(cat, id)) return id;
            int[] values = { cat.Activity, cat.Sociability, cat.Independence, cat.Adaptability };
            string[] names = { "ability.activity", "ability.sociability", "ability.independence", "ability.adaptability" };
            int best = 0;
            for (var i = 1; i < values.Length; i++) if (Math.Abs(values[i] - 50) > Math.Abs(values[best] - 50)) best = i;
            return names[best];
        }

        private static string DiagnoseSecondary(CatDefinition cat)
        {
            string anchor = DiagnoseAnchor(cat);
            if ((cat.Activity <= 19 || cat.Activity >= 80) && !anchor.Contains("activity") && !anchor.Contains("active")) return "activity." + Band(cat.Activity);
            if ((cat.Sociability <= 19 || cat.Sociability >= 80) && !anchor.Contains("sociability")) return "sociability." + Band(cat.Sociability);
            if ((cat.Independence <= 19 || cat.Independence >= 80) && !anchor.Contains("independence") && !anchor.Contains("independent")) return "independence." + Band(cat.Independence);
            foreach (var id in new[] { "night", "high_place", "playful", "outdoor", "timid", "quiet", "lonely" })
                if (Has(cat, id) && !anchor.Contains(id)) return "trait." + id;
            if (cat.Adaptability <= 39 || cat.Adaptability >= 80) return "adaptability." + Band(cat.Adaptability);
            return "none";
        }

        private static bool Has(CatDefinition cat, string id) => cat.Traits != null && cat.Traits.Any(x => x != null && x.Id == id);
        private static string Band(int value) => CatProfileGenerator.GetAbilityBand(value).ToString();
        private static string Normalize(string value) => new string((value ?? string.Empty).Where(x => !char.IsWhiteSpace(x)).ToArray());
        private static IEnumerable<string> SplitSentences(string value) => (value ?? string.Empty).Split(new[] {'。'}, StringSplitOptions.RemoveEmptyEntries).Select(x => x + "。");
        private static int Percentile(IList<int> sorted, float percentile) => sorted[Mathf.Clamp(Mathf.CeilToInt(sorted.Count * percentile) - 1, 0, sorted.Count - 1)];
        private static string Csv(string value) => "\"" + (value ?? string.Empty).Replace("\"", "\"\"") + "\"";
    }
}
