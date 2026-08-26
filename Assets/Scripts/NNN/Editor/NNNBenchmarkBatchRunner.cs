using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>CIやバランス調整時に、固定Seedの10件CSVを非対話で再生成する。</summary>
    public static class NNNBenchmarkBatchRunner
    {
        private const int BenchmarkSeed = 20260825;

        [MenuItem("NNN/Test Data/Export Benchmark CSV")]
        public static void Export()
        {
            var profile = NNNTestDataFactory.CreateRuntimeProfile();
            var selector = new CatCandidateSelector();
            var setBuilder = new CandidateSetBuilder();
            var csv = new StringBuilder();
            var details = new StringBuilder();
            csv.AppendLine("Human,CatA,CatB,CatC,SetScore,Viability,Diversity,Distinctiveness,Tension,RoleCoverage,SelectedRank");
            details.AppendLine("Benchmark,Human,Cat,StableScore,SlowBuildScore,TransformativeScore,Viability,Risk");
            foreach (var benchmark in profile.HumanBenchmarks)
            {
                var generatedCase = benchmark.CreateCase(BenchmarkSeed);
                var evaluations = selector.EvaluateAll(profile.Cats, generatedCase);
                if (benchmark.BenchmarkId == "H08")
                    Debug.Log(BuildH08CandidateSetDebugOutput(benchmark, evaluations, setBuilder));
                var selected = setBuilder.Build(evaluations, BenchmarkSeed);
                if (selected != null)
                    csv.AppendLine(string.Format("{0},{1},{2},{3},{4:0.0},{5:0.0},{6:0.0},{7:0.0},{8:0.0},{9},{10}",
                        benchmark.HumanName, selected.catA.Cat.DisplayName, selected.catB.Cat.DisplayName,
                        selected.catC.Cat.DisplayName, selected.totalScore, selected.viability, selected.diversity,
                        selected.distinctiveness, selected.tension, selected.roleCoverage, selected.rank));
                foreach (var evaluation in evaluations)
                    details.AppendLine(string.Format("{0},{1},{2},{3:0.0},{4:0.0},{5:0.0},{6:0.0},{7:0.0}",
                        benchmark.BenchmarkId, benchmark.HumanName, evaluation.Cat.DisplayName,
                        evaluation.StableScore, evaluation.SlowBuildScore, evaluation.TransformativeScore,
                        evaluation.Viability, evaluation.Risk));
            }

            var path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_HumanBenchmarks.csv");
            File.WriteAllText(path, csv.ToString(), new UTF8Encoding(true));
            File.WriteAllText(Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_HumanBenchmarks_AllScores.csv"), details.ToString(), new UTF8Encoding(true));
            Debug.Log("NNN benchmark CSV exported: " + path + "\n" + csv);
        }

        /// <summary>評価済みセットを変更せず、H08の順位と内訳だけを観測用に整形する。</summary>
        private static string BuildH08CandidateSetDebugOutput(
            HumanBenchmarkDefinition benchmark,
            System.Collections.Generic.IReadOnlyList<CandidateEvaluation> evaluations,
            CandidateSetBuilder setBuilder)
        {
            var ranked = setBuilder.EvaluateAllSets(evaluations);
            var output = new StringBuilder();
            output.AppendLine(string.Format("{0} {1} Candidate Sets", benchmark.BenchmarkId, benchmark.HumanName));

            int topCount = Mathf.Min(5, ranked.Count);
            for (int i = 0; i < topCount; i++)
                AppendSet(output, ranked[i], true);

            CandidateSetResult bestChachaSet = null;
            for (int i = 0; i < ranked.Count; i++)
            {
                foreach (var cat in ranked[i].Cats)
                {
                    if (cat.Cat.DisplayName != "チャチャ") continue;
                    bestChachaSet = ranked[i];
                    break;
                }
                if (bestChachaSet != null) break;
            }

            output.AppendLine("Best set containing チャチャ:");
            if (bestChachaSet != null)
                AppendSet(output, bestChachaSet, false);
            else
                output.AppendLine("Not found");
            return output.ToString();
        }

        private static void AppendSet(StringBuilder output, CandidateSetResult set, bool includeCatScores)
        {
            output.AppendLine(string.Format("Rank {0}", set.rank));
            output.AppendLine(string.Format("Cats: {0} / {1} / {2}",
                set.catA.Cat.DisplayName, set.catB.Cat.DisplayName, set.catC.Cat.DisplayName));
            output.AppendLine(string.Format("BaseScore: {0:0.00}", set.totalScore));
            output.AppendLine(string.Format("PenaltyApplied: {0:0.00}", set.badOverlapPenaltyApplied));
            output.AppendLine(string.Format("AdjustedScore: {0:0.00}", set.selectionScore));
            output.AppendLine(string.Format("Viability: {0:0.00}", set.viability));
            output.AppendLine(string.Format("Diversity: {0:0.00}", set.diversity));
            output.AppendLine(string.Format("Distinctiveness: {0:0.00}", set.distinctiveness));
            output.AppendLine(string.Format("Tension: {0:0.00}", set.tension));
            output.AppendLine(string.Format("RoleCoverage: {0}", set.roleCoverage));
            AppendBadOverlap(output, set);
            if (includeCatScores)
            {
                foreach (var cat in set.Cats)
                    output.AppendLine(string.Format("{0}: V={1:0.00} S={2:0.00} L={3:0.00} T={4:0.00}",
                        cat.Cat.DisplayName, cat.Viability, cat.StableScore,
                        cat.SlowBuildScore, cat.TransformativeScore));
            }
            output.AppendLine();
        }

        private static void AppendBadOverlap(StringBuilder output, CandidateSetResult set)
        {
            var diagnostic = set.badOverlapDiagnostic;
            var pair = diagnostic != null ? diagnostic.RepresentativePair : null;
            output.AppendLine(string.Format("BadOverlapCandidate: {0}", diagnostic != null && diagnostic.BadOverlapCandidate));
            output.AppendLine(string.Format("BadOverlapPairCount: {0}", diagnostic != null ? diagnostic.BadOverlapPairCount : 0));
            if (pair == null) return;
            output.AppendLine(string.Format("DiagnosticPair: {0}", pair.PairName));
            output.AppendLine(string.Format("DominantCat: {0}", pair.DominantCat != null ? pair.DominantCat.Cat.DisplayName : "-"));
            output.AppendLine(string.Format("NearDominatedCat: {0}", pair.NearDominatedCat != null ? pair.NearDominatedCat.Cat.DisplayName : "-"));
            output.AppendLine(string.Format("FunctionalSimilarity: {0:0.000}", pair.FunctionalSimilarity));
            output.AppendLine(string.Format("NearDominance: {0}", pair.NearDominance));
            output.AppendLine(string.Format("LowSideTradeoffValue: {0:0.00}", pair.LowSideTradeoffValue));
        }
    }
}
