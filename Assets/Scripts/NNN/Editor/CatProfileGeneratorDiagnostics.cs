using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace NNN.Editor
{
    /// <summary>猫プロフィールv0.1のSnapshot・境界・回帰を一括確認するUI非接続Runner。</summary>
    public static class CatProfileGeneratorDiagnostics
    {
        private static readonly Dictionary<string, string> ExpectedBehaviors = new Dictionary<string, string>
        {
            {"ミケ", "人の近くへ自分から寄り、触れ合う時間を持つ。新しい状況でも、落ち着ける場所を比較的見つけやすい。"},
            {"クロ", "夜になると高い場所へ移り、少し離れたところから周囲を眺める。初めての状況では、すぐに近づかず様子を見る。"},
            {"トラ", "人やおもちゃの動きへすぐ反応し、そのまま遊び続けようとする。夜にも交流や刺激を求めることがある。"},
            {"シロ", "静かにくつろぐ時間を多く持ち、ときどき人の近くへ来る。環境が変わっても、落ち着ける場所を比較的見つけやすい。"},
            {"サバ", "気になるものへ反応して遊び始め、満足すると自分の場所へ戻る。人と離れていても、自分の時間を保ちやすい。"},
            {"ハチ", "高い場所へ登って安全を確かめてから、気になる方向へ動き始める。新しい状況では、周囲を見ながら時間をかける。"},
            {"モモ", "人の気配を探し、自分から近くへ寄って触れ合いを求める。ひとりで長く過ごすより、人のそばを選ぶことが多い。"},
            {"ゴマ", "自分の場所で静かに過ごし、夜になると周囲を見回るために場所を移す。長いひとり時間も、自分のペースで過ごせる。"},
            {"チャチャ", "窓の外の匂いや気配へ反応し、気になる方向を確かめに行く。新しい状況では、安全を確かめながら時間をかける。"},
            {"ソラ", "人やおもちゃの動きへ反応し、高い場所へ移って周囲を見ることも好む。新しい状況でも、落ち着ける場所を比較的見つけやすい。"},
            {"ナギ", "落ち着ける場所を見つけ、長く静かに過ごすことが多い。必要なときにだけ場所を移し、自分の時間を保つ。"},
            {"レオ", "窓の外の匂いや気配へ関心を向けながら、自分で室内を広く動く。人から離れていても、自分の時間を保ちやすい。"},
            {"コハル", "人の気配を探し、静かに近くへ寄って触れ合いを求める。新しい状況でも、落ち着ける場所を比較的見つけやすい。"},
            {"リン", "人の近くへ寄って穏やかに過ごす。新しい状況では、様子を見ながら普段の過ごし方へ戻る。"},
            {"キリ", "動く量は多いが、初めての状況では距離を取り、安全を確かめてから行動する。人と離れていても、自分の時間を保ちやすい。"},
            {"スズ", "落ち着ける場所で静かに過ごし、必要なときにだけ人の近くへ来る。新しい状況でも、落ち着ける場所を比較的見つけやすい。"},
            {"アラシ", "おもちゃや気になるものへすぐ反応し、高い場所も使いながら次の刺激を探す。交流と自分の探索を行き来する。"},
            {"ツムギ", "静かな場所から周囲を観察し、すぐには近づかず様子を見る。新しい状況では、十分に安全を確かめながら少しずつ馴染む。"},
            {"ポン", "人やおもちゃの動きへすぐ反応し、交流を交えながら遊び続けようとする。遊びの合間には自分で気になる場所を確かめることもある。"},
            {"ヨル", "夜になると高い場所へ移り、周囲を眺めながら自分の時間を過ごす。新しい状況でも、落ち着ける場所を比較的見つけやすい。"}
        };

        private static readonly Dictionary<string, string> ExpectedTags = new Dictionary<string, string>
        {
            {"ミケ", "#人なつっこい / #おだやか"}, {"クロ", "#慎重 / #夜型 / #高いところ好き"},
            {"トラ", "#活発 / #遊び好き / #夜型"}, {"シロ", "#おだやか / #物静か"},
            {"サバ", "#遊び好き / #ひとり上手"}, {"ハチ", "#活発 / #高いところ好き / #慎重"},
            {"モモ", "#甘えん坊 / #寂しがり"}, {"ゴマ", "#物静か / #ひとり上手 / #夜型"},
            {"チャチャ", "#活発 / #外に興味津々"}, {"ソラ", "#おだやか / #遊び好き / #高いところ好き"},
            {"ナギ", "#物静か / #ひとり上手"}, {"レオ", "#活発 / #外に興味津々 / #ひとり上手"},
            {"コハル", "#甘えん坊 / #おだやか / #寂しがり"}, {"リン", "#人なつっこい / #おだやか"},
            {"キリ", "#慎重 / #ひとり上手"}, {"スズ", "#物静か / #ひとり上手 / #おだやか"},
            {"アラシ", "#活発 / #高いところ好き / #遊び好き"}, {"ツムギ", "#慎重 / #ひとり上手 / #物静か"},
            {"ポン", "#活発 / #遊び好き"}, {"ヨル", "#夜型 / #高いところ好き / #ひとり上手"}
        };

        [MenuItem("NNN/Test Data/Run Cat Profile v0.1 Diagnostics")]
        public static void Run()
        {
            var output = new StringBuilder(); var failures = new List<string>();
            var existing = NNNTestDataFactory.CreateRuntimeProfile();
            var external = NNNExternalTestDataFactory.Create();
            var cats = existing.Cats.Concat(external.Cats).ToList();

            TestBands(failures); TestTraitSafety(failures); TestCombinations(failures);
            var before = CaptureSelections(existing);
            output.AppendLine("Cat Profile Generation v0.1 - 20 Cat Snapshot");
            foreach (var cat in cats)
            {
                var first = CatProfileGenerator.Generate(cat); var second = CatProfileGenerator.Generate(cat);
                string tags = string.Join(" / ", first.PlayerTags.ToArray());
                bool exact = ExpectedTags[cat.DisplayName] == tags && ExpectedBehaviors[cat.DisplayName] == first.Behavior &&
                    ((cat.HasTrait("escape") ? CatProfileGenerator.EscapeCaution : null) == first.Caution);
                if (!exact) failures.Add("Snapshot mismatch: " + cat.DisplayName);
                if (tags != string.Join(" / ", second.PlayerTags.ToArray()) || first.Behavior != second.Behavior || first.Caution != second.Caution)
                    failures.Add("Non-deterministic: " + cat.DisplayName);
                output.AppendLine(); output.AppendLine(cat.DisplayName + " " + cat.Age + "歳 [" + (exact ? "Exact Match" : "Mismatch") + "]");
                output.AppendLine("Tags: " + tags); output.AppendLine("Behavior: " + first.Behavior);
                output.AppendLine("Caution: " + (first.Caution ?? "なし"));
            }

            TestPairs(cats, failures, output);
            var after = CaptureSelections(existing);
            if (!before.SequenceEqual(after)) failures.Add("Evaluator/CandidateSet/Seed regression changed after profile generation");
            output.AppendLine(); output.AppendLine("Boundary/Trait/Combination tests: " + (failures.Count == 0 ? "PASS" : "FAIL"));
            output.AppendLine("Selection regression (H01-H10 x Seeds 11111/22222): " + (before.SequenceEqual(after) ? "PASS" : "FAIL"));
            foreach (var failure in failures) output.AppendLine("FAIL: " + failure);

            string path = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "NNN_CatProfileV01_Diagnostics.txt");
            File.WriteAllText(path, output.ToString(), new UTF8Encoding(true));
            Debug.Log(output.ToString());
            if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
        }

        private static void TestBands(ICollection<string> failures)
        {
            int[] values = {-1, 0, 19, 20, 39, 40, 59, 60, 79, 80, 100, 101};
            var expected = new[] {CatProfileAbilityBand.VeryLow, CatProfileAbilityBand.VeryLow, CatProfileAbilityBand.VeryLow,
                CatProfileAbilityBand.Low, CatProfileAbilityBand.Low, CatProfileAbilityBand.Middle, CatProfileAbilityBand.Middle,
                CatProfileAbilityBand.High, CatProfileAbilityBand.High, CatProfileAbilityBand.VeryHigh,
                CatProfileAbilityBand.VeryHigh, CatProfileAbilityBand.VeryHigh};
            for (var i = 0; i < values.Length; i++) if (CatProfileGenerator.GetAbilityBand(values[i]) != expected[i]) failures.Add("Band: " + values[i]);
        }

        private static void TestTraitSafety(ICollection<string> failures)
        {
            var cat = Cat("safety", 50, 50, 50, 50); cat.Traits = null;
            if (CatProfileGenerator.Generate(cat).PlayerTags.Count != 0) failures.Add("null traits");
            cat.Traits = new List<TraitDefinition>(); if (CatProfileGenerator.Generate(cat).PlayerTags.Count != 0) failures.Add("empty traits");
            cat.Traits.Add(Trait("unknown")); if (CatProfileGenerator.Generate(cat).PlayerTags.Count != 0) failures.Add("unknown trait");
            cat.Traits = new List<TraitDefinition> {Trait("quiet"), Trait("quiet")};
            if (CatProfileGenerator.Generate(cat).PlayerTags.Count != 1) failures.Add("duplicate trait");
            cat.Traits = new List<TraitDefinition> {Trait("active"), Trait("playful"), Trait("night"), Trait("timid"), Trait("outdoor")};
            if (CatProfileGenerator.Generate(cat).PlayerTags.Count != 3) failures.Add("five trait max");
        }

        private static void TestCombinations(ICollection<string> failures)
        {
            Check(Cat("active", 80, 50, 50, 50, "active"), "#活発", null, failures);
            Check(Cat("independent", 50, 50, 80, 50, "independent"), "#ひとり上手", null, failures);
            Check(Cat("affLonely", 50, 80, 20, 50, "affectionate", "lonely"), "#甘えん坊 / #寂しがり", null, failures);
            Check(Cat("outdoor", 50, 50, 50, 50, "outdoor"), "#外に興味津々", null, failures);
            Check(Cat("escape", 50, 50, 50, 50, "escape"), "", CatProfileGenerator.EscapeCaution, failures);
            Check(Cat("both", 50, 50, 50, 50, "outdoor", "escape"), "#外に興味津々", CatProfileGenerator.EscapeCaution, failures);
            Check(Cat("quietIndependent", 10, 50, 80, 80, "quiet", "independent"), "#物静か / #ひとり上手", null, failures);
            Check(Cat("activePlay", 90, 70, 50, 60, "active", "playful"), "#活発 / #遊び好き", null, failures);
            Check(Cat("nightHigh", 50, 50, 50, 50, "night", "high_place"), "#夜型 / #高いところ好き", null, failures);
            Check(Cat("highTimid", 50, 50, 50, 30, "high_place", "timid"), "#高いところ好き / #慎重", null, failures);
        }

        private static void Check(CatDefinition cat, string tags, string caution, ICollection<string> failures)
        {
            var p = CatProfileGenerator.Generate(cat);
            if (string.Join(" / ", p.PlayerTags.ToArray()) != tags || p.Caution != caution) failures.Add("Combination: " + cat.DisplayName);
        }

        private static void TestPairs(IList<CatDefinition> cats, ICollection<string> failures, StringBuilder output)
        {
            string[,] pairs = {{"シロ","スズ"},{"ゴマ","ナギ"},{"トラ","ポン"},{"モモ","コハル"},{"ミケ","リン"},{"ハチ","キリ"}};
            output.AppendLine(); output.AppendLine("Focus pair regression");
            for (var i = 0; i < pairs.GetLength(0); i++)
            {
                var a = CatProfileGenerator.Generate(cats.First(x => x.DisplayName == pairs[i, 0]));
                var b = CatProfileGenerator.Generate(cats.First(x => x.DisplayName == pairs[i, 1]));
                bool differs = string.Join("|", a.PlayerTags) != string.Join("|", b.PlayerTags) || a.Behavior != b.Behavior || a.Caution != b.Caution;
                if (!differs) failures.Add("Focus pair identical: " + pairs[i, 0] + "/" + pairs[i, 1]);
                output.AppendLine(pairs[i, 0] + " / " + pairs[i, 1] + ": " + (differs ? "PASS" : "FAIL"));
            }
        }

        private static List<string> CaptureSelections(CaseGenerationProfile profile)
        {
            var selector = new CatCandidateSelector(); var builder = new CandidateSetBuilder(); var result = new List<string>();
            foreach (var human in profile.HumanBenchmarks) foreach (var seed in new[] {11111, 22222})
            {
                var set = builder.Build(selector.EvaluateAll(profile.Cats, human.CreateCase(seed)), seed);
                result.Add(human.BenchmarkId + ":" + seed + ":" + set.catA.Cat.Id + "/" + set.catB.Cat.Id + "/" + set.catC.Cat.Id + ":" + set.totalScore.ToString("R"));
            }
            return result;
        }

        private static CatDefinition Cat(string name, int a, int s, int i, int d, params string[] traits)
        {
            var cat = ScriptableObject.CreateInstance<CatDefinition>(); cat.Id = name; cat.DisplayName = name;
            cat.Activity = a; cat.Sociability = s; cat.Independence = i; cat.Adaptability = d;
            foreach (var trait in traits) cat.Traits.Add(Trait(trait)); return cat;
        }

        private static TraitDefinition Trait(string id)
        {
            var trait = ScriptableObject.CreateInstance<TraitDefinition>(); trait.Id = id; return trait;
        }
    }
}
