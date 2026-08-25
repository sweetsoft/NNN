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
    }
}
