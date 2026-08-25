using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.UIElements;

namespace NNN
{
    /// <summary>固定ベンチマークの個別・一括評価を行うIMGUIデバッグ画面。</summary>
    public sealed class NNNDebugUI : MonoBehaviour
    {
        private CaseGenerationProfile profile;
        private GeneratedCase generatedCase;
        private readonly CatCandidateSelector selector = new CatCandidateSelector();
        private readonly CandidateSetBuilder setBuilder = new CandidateSetBuilder();
        private CandidateSetResult selectedSet;
        private Vector2 scroll;
        private int seed;
        private string seedText;
        private int benchmarkIndex;
        private bool benchmarkMenuOpen;
        private string benchmarkResults;
        private string lastCsvPath;
        private GUIStyle heading;
        private GUIStyle label;

        private void Awake()
        {
            profile = Resources.Load<CaseGenerationProfile>("NNNTestData/Tanaka_TestProfile");
            if (profile == null || profile.HumanBenchmarks == null || profile.HumanBenchmarks.Count == 0)
                profile = NNNTestDataFactory.CreateRuntimeProfile();
            seed = Environment.TickCount & int.MaxValue;
            seedText = seed.ToString();
            SelectBenchmark(0);
        }

        private void SelectBenchmark(int index)
        {
            if (profile.HumanBenchmarks.Count == 0) return;
            benchmarkIndex = (index + profile.HumanBenchmarks.Count) % profile.HumanBenchmarks.Count;
            generatedCase = profile.HumanBenchmarks[benchmarkIndex].CreateCase(seed);
            selectedSet = setBuilder.Build(selector.EvaluateAll(profile.Cats, generatedCase), seed);
            benchmarkMenuOpen = false;
            scroll = Vector2.zero;
        }

        private void Reevaluate(int nextSeed)
        {
            seed = nextSeed;
            seedText = seed.ToString();
            SelectBenchmark(benchmarkIndex);
        }

        private void InitStyles()
        {
            if (heading != null) return;
            label = new GUIStyle(GUI.skin.label) { fontSize = 15, wordWrap = true, richText = true };
            heading = new GUIStyle(label) { fontSize = 21, fontStyle = FontStyle.Bold };
        }

        private void OnGUI()
        {
            InitStyles();
            GUI.Box(new Rect(10, 10, Screen.width - 20, Screen.height - 20), GUIContent.none);
            GUILayout.BeginArea(new Rect(25, 20, Screen.width - 50, Screen.height - 40));
            GUILayout.Label("NNN Human Benchmark Debug", heading);
            DrawControls();
            scroll = GUILayout.BeginScrollView(scroll);
            DrawCase();
            GUILayout.Space(12);
            GUILayout.Label("SELECTED CANDIDATE SET", heading);
            if (selectedSet == null)
                GUILayout.Label("Viability閾値を通過した猫が3匹未満です。", label);
            else
            {
                foreach (var evaluation in selectedSet.Cats) DrawCandidate(evaluation);
                DrawSetScore(selectedSet);
            }
            GUILayout.Space(10);
            DrawAllScores();
            if (!string.IsNullOrEmpty(benchmarkResults))
            {
                GUILayout.Space(14);
                GUILayout.Label("10 Benchmarks Result", heading);
                GUILayout.TextArea(benchmarkResults, label);
                if (!string.IsNullOrEmpty(lastCsvPath)) GUILayout.Label("CSV: " + lastCsvPath, label);
            }
            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        private void DrawControls()
        {
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("< Prev", GUILayout.Width(80))) SelectBenchmark(benchmarkIndex - 1);
            string current = profile.HumanBenchmarks[benchmarkIndex].DisplayName;
            if (GUILayout.Button(current + " ▼", GUILayout.Width(260))) benchmarkMenuOpen = !benchmarkMenuOpen;
            if (GUILayout.Button("Next >", GUILayout.Width(80))) SelectBenchmark(benchmarkIndex + 1);
            GUILayout.Space(15);
            GUILayout.Label("Seed", label, GUILayout.Width(45));
            seedText = GUILayout.TextField(seedText, GUILayout.Width(120));
            if (GUILayout.Button("Use Seed", GUILayout.Width(90)) && int.TryParse(seedText, out var parsed)) Reevaluate(parsed);
            GUILayout.EndHorizontal();
            if (benchmarkMenuOpen)
            {
                GUILayout.BeginVertical(GUI.skin.box, GUILayout.Width(420));
                for (int i = 0; i < profile.HumanBenchmarks.Count; i++)
                    if (GUILayout.Button(profile.HumanBenchmarks[i].DisplayName)) SelectBenchmark(i);
                GUILayout.EndVertical();
            }
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Run 10 Benchmarks", GUILayout.Width(180))) RunBenchmarks(false);
            if (GUILayout.Button("Export CSV", GUILayout.Width(130))) RunBenchmarks(true);
            GUILayout.EndHorizontal();
        }

        private void RunBenchmarks(bool writeCsv)
        {
            var text = new StringBuilder();
            text.AppendLine("Human,CatA,CatB,CatC,SetScore,Viability,Diversity,Distinctiveness,Tension,RoleCoverage,SelectedRank");
            foreach (var benchmark in profile.HumanBenchmarks)
            {
                var target = benchmark.CreateCase(seed);
                var set = setBuilder.Build(selector.EvaluateAll(profile.Cats, target), seed);
                if (set == null) continue;
                text.AppendLine(string.Format("{0},{1},{2},{3},{4:0.0},{5:0.0},{6:0.0},{7:0.0},{8:0.0},{9},{10}",
                    benchmark.HumanName, set.catA.Cat.DisplayName, set.catB.Cat.DisplayName, set.catC.Cat.DisplayName,
                    set.totalScore, set.viability, set.diversity, set.distinctiveness, set.tension,
                    set.roleCoverage, set.rank));
            }
            benchmarkResults = text.ToString();
            Debug.Log("NNN Human Benchmarks\n" + benchmarkResults);
            if (!writeCsv) return;
            lastCsvPath = Path.Combine(Application.persistentDataPath, "NNN_HumanBenchmarks.csv");
            File.WriteAllText(lastCsvPath, benchmarkResults, new UTF8Encoding(true));
            Debug.Log("NNN benchmark CSV exported: " + lastCsvPath);
        }

        private void DrawCase()
        {
            var benchmark = profile.HumanBenchmarks[benchmarkIndex];
            var h = generatedCase.Human;
            var l = generatedCase.Lifestyle;
            var r = generatedCase.Residence;
            GUILayout.Label(string.Format("<b>{0}</b>  Seed {1}", benchmark.DisplayName, generatedCase.Seed), label);
            GUILayout.Label(string.Format("人間: {0} / {1}歳 / {2}", h.Name, h.Age, h.Archetype.DisplayName), label);
            GUILayout.Label("タグ: " + string.Join(" ", h.Traits.Select(t => t.DisplayName).ToArray()), label);
            GUILayout.Label(string.Format("Lifestyle: 在宅 {0} / 留守多 {1} / 夜型 {2} / 不規則 {3} / 干渉度 {4}",
                YesNo(l.MostlyHome), YesNo(l.OftenAway), YesNo(l.NightOwl), YesNo(l.IrregularSchedule), l.InteractionDemand), label);
            GUILayout.Label(string.Format("住居: {0}  [Space {1} / Vertical {2} / EscapeRisk {3}]", r.ResidenceType, r.Space, r.Vertical, r.EscapeRisk), label);
            GUILayout.Label("問題: " + (generatedCase.Problems.Count == 0 ? "-" : string.Join(" / ", generatedCase.Problems.Select(x => x.DisplayName).ToArray())), label);
        }

        private void DrawCandidate(CandidateEvaluation e)
        {
            var c = e.Cat;
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(string.Format("<b>{0}</b> ({1}, {2}歳) / Highest Role: {3}", c.DisplayName, c.Id, c.Age, CandidateSetBuilder.HighestRole(e)), label);
            GUILayout.Label(string.Format("Activity {0} / Sociability {1} / Independence {2} / Adaptability {3}", c.Activity, c.Sociability, c.Independence, c.Adaptability), label);
            GUILayout.Label(string.Format("Environment {0:0.0} | Lifestyle {1:0.0} | Human {2:0.0} | Risk {3:0.0} | Viability {4:0.0}", e.Environment, e.Lifestyle, e.Human, e.Risk, e.Viability), label);
            GUILayout.Label(string.Format("Role Score  Stable {0:0.0} | SlowBuild {1:0.0} | Transformative {2:0.0}", e.StableScore, e.SlowBuildScore, e.TransformativeScore), label);
            foreach (var reason in e.Reasons) GUILayout.Label("  " + reason, label);
            GUILayout.EndVertical();
        }

        private void DrawSetScore(CandidateSetResult set)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("SET SCORE", heading);
            GUILayout.Label(string.Format("Viability {0:0.0} | Diversity {1:0.0} | Distinctiveness {2:0.0} | Tension {3:0.0}",
                set.viability, set.diversity, set.distinctiveness, set.tension), label);
            GUILayout.Label(string.Format("RoleCoverage {0}/3 (+{1:0}) | TOTAL {2:0.0}",
                set.roleCoverage, set.roleCoverageBonus, set.totalScore), label);
            GUILayout.Label(string.Format("Evaluated Sets: {0} | Selected Rank: {1} | Seed: {2}",
                set.evaluatedSets, set.rank, set.seed), label);
            GUILayout.EndVertical();
        }

        private void DrawAllScores()
        {
            GUILayout.Label("全猫スコア一覧", heading);
            foreach (var e in selector.EvaluateAll(profile.Cats, generatedCase).OrderBy(x => x.Cat.Id))
            {
                Debug.Log($"Cat:{e.Cat.DisplayName}, Activity:{e.Cat.Activity}, Sociability:{e.Cat.Sociability}, Risk:{e.Risk}, Viability:{e.Viability}, StableScore:{e.StableScore}, SlowBuildScore:{e.SlowBuildScore}, TransformativeScore:{e.TransformativeScore}");

                GUILayout.Label(string.Format("{0,-8}  S {1,5:0.0} / B {2,5:0.0} / T {3,5:0.0} / Risk {4,5:0.0}", e.Cat.DisplayName, e.StableScore, e.SlowBuildScore, e.TransformativeScore, e.Risk), label);
            }
        }

        private static string YesNo(bool value) => value ? "Yes" : "No";
    }
}
