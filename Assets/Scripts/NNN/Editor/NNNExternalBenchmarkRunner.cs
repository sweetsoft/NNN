using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>既存データへ混ぜずに、未知の人間・猫で現行候補生成を観測する。</summary>
    public static class NNNExternalBenchmarkRunner
    {
        private static readonly int[] Seeds = { 11111, 22222 };

        [MenuItem("NNN/Test Data/Run External Quality Window Benchmark")]
        public static void Run()
        {
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            var output = new StringBuilder();
            RunGroup("Test A: new humans x existing cats", external.Humans, existing.Cats, output);
            RunGroup("Test B: existing humans x new cats", existing.HumanBenchmarks, external.Cats, output);
            RunGroup("Test C: new humans x new cats", external.Humans, external.Cats, output);
            Debug.Log(output.ToString());
        }

        [MenuItem("NNN/Test Data/Run BadOverlap Diagnostics")]
        public static void RunBadOverlapDiagnostics()
        {
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            var output = new StringBuilder();
            RunBadOverlapGroup("Test B", existing.HumanBenchmarks, external.Cats, output);
            RunBadOverlapGroup("Test C", external.Humans, external.Cats, output);
            string text = output.ToString();
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_BadOverlapDiagnostics.txt");
            File.WriteAllText(path, text, new UTF8Encoding(true));
            Debug.Log(text);
            Debug.Log("BadOverlap diagnostics exported: " + path);
        }

        [MenuItem("NNN/Test Data/Run BadOverlap Generalization Benchmark")]
        public static void RunBadOverlapGeneralizationBenchmark()
        {
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            var allCats = existing.Cats.Concat(external.Cats).ToList();
            var output = new StringBuilder();
            RunBadOverlapGroup("BadOverlap generalization B01-B12", external.BadOverlapHumans, allCats, output);
            string text = output.ToString();
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_BadOverlapGeneralization.txt");
            File.WriteAllText(path, text, new UTF8Encoding(true));
            Debug.Log(text);
            Debug.Log("BadOverlap generalization benchmark exported: " + path);
        }

        [MenuItem("NNN/Test Data/Run BadOverlap Penalty Experiment")]
        public static void RunBadOverlapPenaltyExperiment()
        {
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            var allCats = existing.Cats.Concat(external.Cats).ToList();
            var allHumans = existing.HumanBenchmarks.Concat(external.Humans).Concat(external.BadOverlapHumans).ToList();
            var selector = new CatCandidateSelector();
            var defaultBuilder = new CandidateSetBuilder();
            var explicitOffBuilder = new CandidateSetBuilder(false);
            var onBuilder = new CandidateSetBuilder(true);
            var output = new StringBuilder();
            int regressionFailures = 0;
            int badSelections = 0;
            int totalSelections = 0;
            int eligibleTotal = 0;
            int uniqueSetTotal = 0;
            double normalizedEntropyTotal = 0d;
            double selectedGapTotal = 0d;
            var catAppearances = allCats.ToDictionary(cat => cat.DisplayName, cat => 0);

            output.AppendLine("BadOverlap Penalty Experiment");
            output.AppendLine(string.Format("DefaultEnabled={0} Penalty={1:0.00}",
                CandidateSetBuilder.EnableBadOverlapPenalty, CandidateSetBuilder.BadOverlapPenalty));

            foreach (var human in allHumans)
            {
                var evaluations = selector.EvaluateAll(allCats, human.CreateCase(0));
                var baseline = defaultBuilder.EvaluateAllSets(evaluations);
                var explicitOff = explicitOffBuilder.EvaluateAllSets(evaluations);
                bool offMatch = SameRankedResults(baseline, explicitOff);
                foreach (int seed in Seeds)
                    offMatch &= SameCats(defaultBuilder.Build(evaluations, seed), explicitOffBuilder.Build(evaluations, seed));
                if (!offMatch) regressionFailures++;

                var adjusted = onBuilder.EvaluateAllSets(evaluations);
                var eligible = EligibleBySelectionScore(adjusted);
                eligibleTotal += eligible.Count;
                output.AppendLine(string.Format("{0} {1} OFFMatch={2} BaseBest={3} AdjustedBest={4} Eligible={5}",
                    human.BenchmarkId, human.HumanName, offMatch,
                    CatNames(baseline[0]), CatNames(adjusted[0]), eligible.Count));

                foreach (int seed in Seeds)
                {
                    var selected = onBuilder.Build(evaluations, seed);
                    output.AppendLine(string.Format(
                        "Seed={0} Rank={1} Cats={2} BaseScore={3:0.00} AdjustedScore={4:0.00} Gap={5:0.00} Bad={6}",
                        seed, selected.rank, CatNames(selected), selected.totalScore, selected.selectionScore,
                        adjusted[0].selectionScore - selected.selectionScore,
                        selected.badOverlapDiagnostic.BadOverlapCandidate));
                }

                var selectionCounts = new Dictionary<string, int>();
                double gapTotal = 0d;
                for (int seed = 1; seed <= 1000; seed++)
                {
                    var selected = onBuilder.Build(evaluations, seed);
                    string key = CatNames(selected);
                    selectionCounts[key] = selectionCounts.TryGetValue(key, out int count) ? count + 1 : 1;
                    gapTotal += adjusted[0].selectionScore - selected.selectionScore;
                    selectedGapTotal += adjusted[0].selectionScore - selected.selectionScore;
                    foreach (var cat in selected.Cats) catAppearances[cat.Cat.DisplayName]++;
                    if (selected.badOverlapDiagnostic.BadOverlapCandidate) badSelections++;
                    totalSelections++;
                }
                double entropy = 0d;
                foreach (int count in selectionCounts.Values)
                {
                    double probability = count / 1000d;
                    entropy -= probability * Math.Log(probability);
                }
                double normalizedEntropy = eligible.Count > 1 ? entropy / Math.Log(eligible.Count) : 0d;
                uniqueSetTotal += selectionCounts.Count;
                normalizedEntropyTotal += normalizedEntropy;
                output.AppendLine(string.Format("Seeds1To1000 Unique={0} NormalizedEntropy={1:0.000000} AverageGap={2:0.000000}",
                    selectionCounts.Count, normalizedEntropy, gapTotal / 1000d));

                AppendFocusSet(output, human.BenchmarkId, adjusted, eligible);
                output.AppendLine();
            }

            output.AppendLine(string.Format("RegressionFailures={0}", regressionFailures));
            output.AppendLine(string.Format("Seeds1To1000 BadOverlap={0}/{1} Rate={2:0.000}%",
                badSelections, totalSelections, totalSelections > 0 ? badSelections * 100d / totalSelections : 0d));
            output.AppendLine(string.Format(
                "Seeds1To1000 AverageEligible={0:0.000} AverageUnique={1:0.000} AverageNormalizedEntropy={2:0.000000} AverageGap={3:0.000000}",
                eligibleTotal / (double)allHumans.Count, uniqueSetTotal / (double)allHumans.Count,
                normalizedEntropyTotal / allHumans.Count, selectedGapTotal / totalSelections));
            foreach (var appearance in catAppearances.OrderByDescending(x => x.Value).ThenBy(x => x.Key).Take(10))
                output.AppendLine(string.Format("CatAppearance {0}={1}", appearance.Key, appearance.Value));
            string text = output.ToString();
            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_BadOverlapPenaltyExperiment.txt");
            File.WriteAllText(path, text, new UTF8Encoding(true));
            Debug.Log(text);
            Debug.Log("BadOverlap penalty experiment exported: " + path);
        }

        private static List<CandidateSetResult> EligibleBySelectionScore(IReadOnlyList<CandidateSetResult> ranked)
        {
            if (ranked.Count == 0) return new List<CandidateSetResult>();
            float best = ranked[0].selectionScore;
            return ranked.Where(x => best - x.selectionScore <= CandidateSetBuilder.QualityWindow).ToList();
        }

        private static bool SameRankedResults(
            IReadOnlyList<CandidateSetResult> expected,
            IReadOnlyList<CandidateSetResult> actual)
            => expected.Count == actual.Count && expected.Zip(actual, (a, b) =>
                a.rank == b.rank && a.totalScore == b.totalScore && a.selectionScore == b.selectionScore &&
                a.badOverlapPenaltyApplied == b.badOverlapPenaltyApplied && CatNames(a) == CatNames(b)).All(x => x);

        private static string CatNames(CandidateSetResult set)
            => set.catA.Cat.DisplayName + "/" + set.catB.Cat.DisplayName + "/" + set.catC.Cat.DisplayName;

        private static void AppendFocusSet(
            StringBuilder output,
            string humanId,
            IReadOnlyList<CandidateSetResult> ranked,
            IReadOnlyList<CandidateSetResult> eligible)
        {
            string[] cats = humanId == "E03" || humanId == "E07"
                ? new[] { "ナギ", "コハル", "リン" }
                : humanId == "E05"
                    ? new[] { "レオ", "アラシ", "ツムギ" }
                    : humanId == "B06" ? new[] { "トラ", "サバ", "ソラ" } : null;
            if (cats == null) return;
            var focus = ranked.FirstOrDefault(set => cats.All(name => set.Cats.Any(cat => cat.Cat.DisplayName == name)));
            if (focus == null) return;
            output.AppendLine(string.Format(
                "Focus Cats={0} Rank={1} BaseScore={2:0.00} PenaltyApplied={3:0.00} AdjustedScore={4:0.00} Eligible={5} Bad={6}",
                CatNames(focus), focus.rank, focus.totalScore, focus.badOverlapPenaltyApplied,
                focus.selectionScore, eligible.Contains(focus), focus.badOverlapDiagnostic.BadOverlapCandidate));
        }

        private static void RunBadOverlapGroup(
            string title,
            IReadOnlyList<HumanBenchmarkDefinition> humans,
            IList<CatDefinition> cats,
            StringBuilder output)
        {
            var selector = new CatCandidateSelector();
            var builder = new CandidateSetBuilder();
            output.AppendLine(title);
            foreach (var human in humans)
            {
                var target = human.CreateCase(0);
                var evaluations = selector.EvaluateAll(cats, target);
                var ranked = builder.EvaluateAllSets(evaluations);
                float best = ranked[0].selectionScore;
                var eligible = ranked.Where(x => best - x.selectionScore <= CandidateSetBuilder.QualityWindow).ToList();
                int badSetCount = eligible.Count(x => x.badOverlapDiagnostic != null && x.badOverlapDiagnostic.BadOverlapCandidate);
                var badPairs = eligible.SelectMany(x => x.badOverlapDiagnostic.Pairs)
                    .Where(x => x.BadOverlapCandidate)
                    .GroupBy(x => x.PairName)
                    .Select(x => x.First())
                    .ToList();

                output.AppendLine(string.Format("{0} {1} EligibleSets={2} BadOverlapSetCount={3} BadOverlapPairCount={4}",
                    human.BenchmarkId, human.HumanName, eligible.Count, badSetCount, badPairs.Count));
                foreach (var set in eligible)
                {
                    var diagnostic = set.badOverlapDiagnostic;
                    var pair = diagnostic.RepresentativePair;
                    output.AppendLine(string.Format(
                        "Rank={0} Cats={1}/{2}/{3} BaseScore={4:0.00} AdjustedScore={5:0.00} PenaltyApplied={6:0.00} Gap={7:0.00} Bad={8} Pair={9} Dominant={10} NearDominated={11} FS={12:0.000} Near={13} Tradeoff={14:0.00}",
                        set.rank, set.catA.Cat.DisplayName, set.catB.Cat.DisplayName, set.catC.Cat.DisplayName,
                        set.totalScore, set.selectionScore, set.badOverlapPenaltyApplied,
                        best - set.selectionScore, diagnostic.BadOverlapCandidate,
                        pair != null ? pair.PairName : "-",
                        pair != null && pair.DominantCat != null ? pair.DominantCat.Cat.DisplayName : "-",
                        pair != null && pair.NearDominatedCat != null ? pair.NearDominatedCat.Cat.DisplayName : "-",
                        pair != null ? pair.FunctionalSimilarity : 0f,
                        pair != null && pair.NearDominance, pair != null ? pair.LowSideTradeoffValue : 0f));
                }

                foreach (var pair in badPairs)
                    output.AppendLine(string.Format("BadPair={0} Dominant={1} NearDominated={2} FS={3:0.000} Tradeoff={4:0.00}",
                        pair.PairName, pair.DominantCat.Cat.DisplayName, pair.NearDominatedCat.Cat.DisplayName,
                        pair.FunctionalSimilarity, pair.LowSideTradeoffValue));

                foreach (int seed in Seeds)
                {
                    var selected = builder.Build(evaluations, seed);
                    output.AppendLine(string.Format("Seed={0} SelectedCats={1}/{2}/{3} SelectedRank={4} BadOverlapCandidate={5}",
                        seed, selected.catA.Cat.DisplayName, selected.catB.Cat.DisplayName, selected.catC.Cat.DisplayName,
                        selected.rank, selected.badOverlapDiagnostic.BadOverlapCandidate));
                }

                VerifyDiagnosticIsolation(human, cats, evaluations, ranked, eligible.Count, builder, selector, output);
                output.AppendLine();
            }
        }

        private static void VerifyDiagnosticIsolation(
            HumanBenchmarkDefinition human,
            IList<CatDefinition> cats,
            IReadOnlyList<CandidateEvaluation> diagnosticEvaluations,
            IReadOnlyList<CandidateSetResult> diagnosticRanked,
            int diagnosticEligibleCount,
            CandidateSetBuilder builder,
            CatCandidateSelector selector,
            StringBuilder output)
        {
            var shadow = selector.EvaluateAll(cats, human.CreateCase(0));
            foreach (var evaluation in shadow) evaluation.DiagnosticReasons.Clear();
            var shadowRanked = builder.EvaluateAllSets(shadow);
            float shadowBest = shadowRanked[0].selectionScore;
            int shadowEligible = shadowRanked.Count(x => shadowBest - x.selectionScore <= CandidateSetBuilder.QualityWindow);
            bool sameRanked = diagnosticRanked.Count == shadowRanked.Count &&
                diagnosticRanked.Zip(shadowRanked, (a, b) =>
                    a.rank == b.rank && a.totalScore == b.totalScore && a.selectionScore == b.selectionScore &&
                    a.catA.Cat.Id == b.catA.Cat.Id && a.catB.Cat.Id == b.catB.Cat.Id && a.catC.Cat.Id == b.catC.Cat.Id).All(x => x);
            bool sameSeeds = Seeds.All(seed => SameCats(builder.Build(diagnosticEvaluations, seed), builder.Build(shadow, seed)));
            output.AppendLine(string.Format("Regression Ranked={0} Eligible={1} Seeds={2}",
                sameRanked, diagnosticEligibleCount == shadowEligible, sameSeeds));
        }

        private static bool SameCats(CandidateSetResult a, CandidateSetResult b)
            => a != null && b != null && a.rank == b.rank && a.totalScore == b.totalScore &&
               a.selectionScore == b.selectionScore &&
               a.catA.Cat.Id == b.catA.Cat.Id && a.catB.Cat.Id == b.catB.Cat.Id && a.catC.Cat.Id == b.catC.Cat.Id;

        private static void RunGroup(
            string title,
            IReadOnlyList<HumanBenchmarkDefinition> humans,
            IList<CatDefinition> cats,
            StringBuilder output)
        {
            var selector = new CatCandidateSelector();
            var builder = new CandidateSetBuilder();
            var eligibleCounts = new List<int>();
            var appearances = cats.ToDictionary(cat => cat.DisplayName, cat => 0);
            output.AppendLine(title);

            foreach (var human in humans)
            {
                var target = human.CreateCase(0);
                var evaluations = selector.EvaluateAll(cats, target);
                var ranked = builder.EvaluateAllSets(evaluations);
                float bestScore = ranked[0].totalScore;
                int eligible = ranked.Count(set => bestScore - set.totalScore <= CandidateSetBuilder.QualityWindow);
                eligibleCounts.Add(eligible);

                foreach (int seed in Seeds)
                {
                    var selected = builder.Build(evaluations, seed);
                    foreach (var cat in selected.Cats) appearances[cat.Cat.DisplayName]++;
                    output.AppendLine(string.Format(
                        "{0} {1} Seed={2} Best={3:0.00} Eligible={4} Rank={5} Cats={6}/{7}/{8} Score={9:0.00} Gap={10:0.00}",
                        human.BenchmarkId, human.HumanName, seed, bestScore, eligible, selected.rank,
                        selected.catA.Cat.DisplayName, selected.catB.Cat.DisplayName, selected.catC.Cat.DisplayName,
                        selected.totalScore, bestScore - selected.totalScore));
                }
            }

            var ordered = eligibleCounts.OrderBy(value => value).ToList();
            float median = ordered.Count % 2 == 0
                ? (ordered[ordered.Count / 2 - 1] + ordered[ordered.Count / 2]) / 2f
                : ordered[ordered.Count / 2];
            output.AppendLine(string.Format("EligibleSets Min={0} Max={1} Average={2:0.00} Median={3:0.00}",
                ordered[0], ordered[ordered.Count - 1], ordered.Average(), median));
            foreach (var appearance in appearances.OrderByDescending(item => item.Value).ThenBy(item => item.Key))
                output.AppendLine(string.Format("Appearance {0}={1}", appearance.Key, appearance.Value));
            output.AppendLine();
        }
    }

    public sealed class NNNExternalTestProfile
    {
        public readonly List<HumanBenchmarkDefinition> Humans = new List<HumanBenchmarkDefinition>();
        public readonly List<HumanBenchmarkDefinition> BadOverlapHumans = new List<HumanBenchmarkDefinition>();
        public readonly List<CatDefinition> Cats = new List<CatDefinition>();
    }

    /// <summary>外部検証専用データ。既存FactoryやResourcesへは追加しない。</summary>
    public static class NNNExternalTestDataFactory
    {
        public static NNNExternalTestProfile Create()
        {
            var profile = new NNNExternalTestProfile();
            var traits = CreateTraits();

            profile.Humans.Add(Human("E01", "青木 澪", "極小住居の猫初心者", 15, 25, 20,
                true, false, false, false, 45, traits, null, "quiet_life", "nervous", "regular"));
            profile.Humans.Add(Human("E02", "石井 蓮", "長時間留守で接触少なめ", 50, 45, 25,
                false, true, false, false, 20, traits, null, "often_away", "non_intrusive", "regular"));
            profile.Humans.Add(Human("E03", "上田 杏", "交流を強く求める猫初心者", 48, 40, 20,
                true, false, false, false, 80, traits, null, "caretaker", "contact_heavy", "regular"));
            profile.Humans.Add(Human("E04", "遠藤 誠", "保護猫経験が豊富な不規則勤務", 45, 55, 30,
                false, false, false, true, 40, traits, null, "cat_experienced", "non_intrusive", "caretaker", "irregular"));
            profile.Humans.Add(Human("E05", "大野 葵", "活動的だが猫経験なし", 70, 65, 25,
                false, false, false, false, 70, traits, null, "active_person", "playful", "regular"));
            profile.Humans.Add(Human("E06", "金子 静", "低活動で非常に静かな生活", 35, 30, 15,
                true, false, false, false, 20, traits, new[] { "monotony" }, "quiet_life", "non_intrusive", "regular", "monotony"));
            profile.Humans.Add(Human("E07", "木村 翼", "子どもがいる接触の多い家庭", 65, 55, 20,
                true, false, false, false, 90, traits, null, "caretaker", "contact_heavy", "regular"));
            profile.Humans.Add(Human("E08", "斎藤 真", "多頭飼い経験のある世話好き", 75, 70, 20,
                true, false, false, false, 65, traits, null, "cat_experienced", "caretaker", "regular"));
            profile.Humans.Add(Human("E09", "高田 凛", "夜型で留守もある不規則生活", 42, 60, 25,
                false, true, true, true, 25, traits, new[] { "improvement" }, "irregular", "night", "often_away", "non_intrusive", "improvement"));
            profile.Humans.Add(Human("E10", "中川 悠", "広い住居だが猫との接触時間が少ない", 95, 90, 35,
                false, true, false, false, 15, traits, null, "often_away", "non_intrusive", "regular"));

            // BadOverlap汎化診断専用。既存Test A/B/CのHuman母集団には混ぜない。
            profile.BadOverlapHumans.Add(Human("B01", "松本 ひなた", "猫初心者で在宅・高接触", 55, 45, 30,
                true, false, false, false, 95, traits, null, "caretaker", "contact_heavy", "regular"));
            profile.BadOverlapHumans.Add(Human("B02", "藤井 奏", "猫経験者で在宅・高接触", 60, 50, 25,
                true, false, false, false, 85, traits, null, "cat_experienced", "caretaker", "contact_heavy", "regular"));
            profile.BadOverlapHumans.Add(Human("B03", "岡田 澄", "猫初心者で干渉の少ない生活", 50, 40, 20,
                false, false, false, false, 15, traits, null, "non_intrusive", "regular"));
            profile.BadOverlapHumans.Add(Human("B04", "前田 湊", "猫経験者で留守が多く低接触", 65, 55, 20,
                false, true, false, false, 20, traits, null, "cat_experienced", "often_away", "non_intrusive", "regular"));
            profile.BadOverlapHumans.Add(Human("B05", "福田 陽", "活動的で広い住居", 90, 85, 20,
                false, false, false, false, 60, traits, null, "active_person", "playful", "regular"));
            profile.BadOverlapHumans.Add(Human("B06", "西村 遼", "活動的だが狭い住居", 25, 60, 35,
                true, false, false, false, 70, traits, null, "active_person", "playful", "regular"));
            profile.BadOverlapHumans.Add(Human("B07", "三浦 凪", "静かな生活で広い住居", 90, 75, 15,
                true, false, false, false, 30, traits, null, "quiet_life", "non_intrusive", "regular"));
            profile.BadOverlapHumans.Add(Human("B08", "森川 灯", "静かな生活で狭い住居", 20, 25, 25,
                true, false, false, false, 25, traits, null, "quiet_life", "nervous", "non_intrusive", "regular"));
            profile.BadOverlapHumans.Add(Human("B09", "橋本 玲", "長時間留守で接触要求が低い", 60, 50, 30,
                false, true, false, false, 10, traits, null, "often_away", "non_intrusive", "regular"));
            profile.BadOverlapHumans.Add(Human("B10", "石川 迅", "夜型の不規則勤務", 55, 50, 25,
                false, false, true, true, 40, traits, null, "irregular", "night", "non_intrusive"));
            profile.BadOverlapHumans.Add(Human("B11", "小川 結", "世話好きで接触要求は中程度", 70, 60, 20,
                true, false, false, false, 55, traits, null, "caretaker", "regular"));
            profile.BadOverlapHumans.Add(Human("B12", "長谷川 岳", "猫経験豊富だが狭く脱走対策が難しい住居", 28, 70, 45,
                false, false, false, false, 50, traits, null, "cat_experienced", "caretaker", "non_intrusive", "regular"));

            profile.Cats.Add(Cat("EXT_CAT_01", "ナギ", 5, 5, 25, 90, 65, traits, "quiet", "independent"));
            profile.Cats.Add(Cat("EXT_CAT_02", "レオ", 3, 95, 20, 80, 55, traits, "active", "outdoor", "independent"));
            profile.Cats.Add(Cat("EXT_CAT_03", "コハル", 6, 15, 95, 20, 80, traits, "affectionate", "gentle", "lonely"));
            profile.Cats.Add(Cat("EXT_CAT_04", "リン", 4, 30, 80, 35, 60, traits, "affectionate", "gentle", "escape"));
            profile.Cats.Add(Cat("EXT_CAT_05", "キリ", 2, 75, 35, 70, 45, traits, "timid", "independent", "escape"));
            profile.Cats.Add(Cat("EXT_CAT_06", "スズ", 8, 10, 50, 75, 85, traits, "quiet", "independent", "gentle"));
            profile.Cats.Add(Cat("EXT_CAT_07", "アラシ", 2, 100, 55, 45, 70, traits, "active", "high_place", "playful"));
            profile.Cats.Add(Cat("EXT_CAT_08", "ツムギ", 7, 35, 20, 90, 35, traits, "timid", "independent", "quiet"));
            profile.Cats.Add(Cat("EXT_CAT_09", "ポン", 1, 85, 75, 55, 65, traits, "active", "playful"));
            profile.Cats.Add(Cat("EXT_CAT_10", "ヨル", 5, 60, 30, 85, 90, traits, "night", "high_place", "independent"));
            return profile;
        }

        private static Dictionary<string, TraitDefinition> CreateTraits()
        {
            var ids = new[]
            {
                "affectionate", "gentle", "timid", "night", "high_place", "active", "playful",
                "independent", "quiet", "lonely", "escape", "outdoor", "mostly_home", "non_intrusive",
                "often_away", "regular", "caretaker", "quiet_life", "contact_heavy", "nervous",
                "withdrawn", "active_person", "cat_experienced", "irregular", "improvement", "monotony"
            };
            var traits = new Dictionary<string, TraitDefinition>();
            foreach (string id in ids)
            {
                var trait = Make<TraitDefinition>("ExternalTrait_" + id);
                trait.Id = id;
                trait.DisplayName = id;
                traits[id] = trait;
            }
            return traits;
        }

        private static HumanBenchmarkDefinition Human(
            string id, string name, string description, int space, int vertical, int escapeRisk,
            bool mostlyHome, bool oftenAway, bool nightOwl, bool irregular, int interactionDemand,
            Dictionary<string, TraitDefinition> traits, string[] problemIds, params string[] traitIds)
        {
            var archetype = Make<HumanArchetype>("ExternalHuman_" + id);
            archetype.Id = id.ToLowerInvariant() + "_human";
            archetype.DisplayName = description;
            archetype.Description = description;
            foreach (string traitId in traitIds) archetype.PreferredTraits.Add(traits[traitId]);

            var lifestyle = Make<LifestyleDefinition>("ExternalLifestyle_" + id);
            lifestyle.Id = id.ToLowerInvariant() + "_lifestyle";
            lifestyle.DisplayName = description;
            lifestyle.MostlyHome = mostlyHome;
            lifestyle.OftenAway = oftenAway;
            lifestyle.NightOwl = nightOwl;
            lifestyle.IrregularSchedule = irregular;
            lifestyle.InteractionDemand = interactionDemand;

            var residence = Make<ResidenceDefinition>("ExternalResidence_" + id);
            residence.Id = id.ToLowerInvariant() + "_residence";
            residence.DisplayName = name + "宅";
            residence.ResidenceType = description;
            residence.Space = space;
            residence.Vertical = vertical;
            residence.EscapeRisk = escapeRisk;

            var result = Make<HumanBenchmarkDefinition>(id + "_" + name.Replace(" ", ""));
            result.Id = id.ToLowerInvariant();
            result.BenchmarkId = id;
            result.DisplayName = id + " " + name;
            result.HumanName = name;
            result.Archetype = archetype;
            result.Lifestyle = lifestyle;
            result.Residence = residence;
            if (problemIds != null)
            {
                foreach (string problemId in problemIds)
                {
                    var problem = Make<ProblemDefinition>("ExternalProblem_" + problemId);
                    problem.Id = problemId;
                    problem.DisplayName = problemId;
                    result.Problems.Add(problem);
                }
            }
            return result;
        }

        private static CatDefinition Cat(
            string id, string name, int age, int activity, int sociability, int independence, int adaptability,
            Dictionary<string, TraitDefinition> traits, params string[] traitIds)
        {
            var result = Make<CatDefinition>(id + "_" + name);
            result.Id = id;
            result.DisplayName = name;
            result.Age = age;
            result.Activity = activity;
            result.Sociability = sociability;
            result.Independence = independence;
            result.Adaptability = adaptability;
            foreach (string traitId in traitIds) result.Traits.Add(traits[traitId]);
            return result;
        }

        private static T Make<T>(string objectName) where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            value.name = objectName;
            return value;
        }
    }
}
