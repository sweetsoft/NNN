using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>全猫評価から、プレイヤーが意味のある迷いを持てる三匹セットを選ぶ。</summary>
    public sealed class CandidateSetBuilder
    {
        public const float DefaultViabilityThreshold = 25f;
        public const float QualityWindow = 2.5f;
        private const float DrawTemperature = 5f;

        /// <summary>閾値通過猫の全3組を評価し、上位候補からSeed付き重み抽選する。</summary>
        public CandidateSetResult Build(
            IReadOnlyList<CandidateEvaluation> evaluations,
            int seed,
            float viabilityThreshold = DefaultViabilityThreshold)
        {
            var ranked = EvaluateAllSets(evaluations, viabilityThreshold);
            if (ranked.Count == 0) return null;

            float topScore = ranked[0].totalScore;
            var pool = ranked.Where(x => topScore - x.totalScore <= QualityWindow).ToList();
            if (pool.Count == 0) pool.Add(ranked[0]);
            var weights = pool.Select(x => Math.Exp((x.totalScore - topScore) / DrawTemperature)).ToArray();
            double totalWeight = weights.Sum();
            // 同じSeedでも人間（評価値）が違えば同じ抽選順位へ偏らない一方、
            // 同じ案件・同じSeedなら必ず再現できる乱数系列を作る。
            double roll = new System.Random(BuildDrawSeed(seed, evaluations)).NextDouble() * totalWeight;
            int selectedIndex = 0;
            for (int i = 0; i < weights.Length; i++)
            {
                roll -= weights[i];
                if (roll <= 0d) { selectedIndex = i; break; }
            }

            var selected = pool[selectedIndex];
            selected.seed = seed;
            return selected;
        }

        private static int BuildDrawSeed(int seed, IReadOnlyList<CandidateEvaluation> evaluations)
        {
            unchecked
            {
                int hash = seed;
                foreach (var evaluation in evaluations.Where(x => x != null && x.Cat != null).OrderBy(x => x.Cat.Id))
                {
                    foreach (char character in evaluation.Cat.Id ?? "") hash = hash * 31 + character;
                    hash = hash * 31 + Mathf.RoundToInt(evaluation.Viability * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(evaluation.StableScore * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(evaluation.SlowBuildScore * 10f);
                    hash = hash * 31 + Mathf.RoundToInt(evaluation.TransformativeScore * 10f);
                }
                return hash;
            }
        }

        /// <summary>閾値を通過した猫から全組み合わせを列挙し、得点順で返す。</summary>
        public List<CandidateSetResult> EvaluateAllSets(
            IReadOnlyList<CandidateEvaluation> evaluations,
            float viabilityThreshold = DefaultViabilityThreshold)
        {
            var viable = evaluations
                .Where(x => x != null && x.Cat != null && x.Viability >= viabilityThreshold)
                .ToList();
            var results = new List<CandidateSetResult>();
            for (int i = 0; i < viable.Count - 2; i++)
                for (int j = i + 1; j < viable.Count - 1; j++)
                    for (int k = j + 1; k < viable.Count; k++)
                        results.Add(EvaluateSet(viable[i], viable[j], viable[k]));

            results = results.OrderByDescending(x => x.totalScore).ToList();
            for (int i = 0; i < results.Count; i++)
            {
                results[i].rank = i + 1;
                results[i].evaluatedSets = results.Count;
            }
            return results;
        }

        private static CandidateSetResult EvaluateSet(CandidateEvaluation a, CandidateEvaluation b, CandidateEvaluation c)
        {
            float viability = EvaluateViability(a, b, c);
            float diversity = EvaluateDiversity(a, b, c);
            float distinctiveness = (Distinctiveness(a) + Distinctiveness(b) + Distinctiveness(c)) / 3f;
            float tension = EvaluateTension(a, b, c);
            int coverage = new[] { HighestRole(a), HighestRole(b), HighestRole(c) }.Distinct().Count();
            float coverageBonus = coverage == 3 ? 15f : coverage == 2 ? 7f : 0f;
            return new CandidateSetResult
            {
                catA = a, catB = b, catC = c,
                viability = viability,
                diversity = diversity,
                distinctiveness = distinctiveness,
                tension = tension,
                roleCoverage = coverage,
                roleCoverageBonus = coverageBonus,
                totalScore = viability * 0.35f + diversity * 0.30f +
                    distinctiveness * 0.20f + tension * 0.15f + coverageBonus
            };
        }

        private static float EvaluateViability(CandidateEvaluation a, CandidateEvaluation b, CandidateEvaluation c)
        {
            float minimum = Mathf.Min(a.Viability, Mathf.Min(b.Viability, c.Viability));
            float average = (a.Viability + b.Viability + c.Viability) / 3f;
            return minimum * 0.6f + average * 0.4f;
        }

        private static float EvaluateDiversity(CandidateEvaluation a, CandidateEvaluation b, CandidateEvaluation c)
            => Mathf.Clamp((RoleDistance(a, b) + RoleDistance(a, c) + RoleDistance(b, c)) / 3f, 0f, 100f);

        private static float RoleDistance(CandidateEvaluation a, CandidateEvaluation b)
        {
            float stable = a.StableScore - b.StableScore;
            float slow = a.SlowBuildScore - b.SlowBuildScore;
            float transformative = a.TransformativeScore - b.TransformativeScore;
            return Mathf.Sqrt(stable * stable + slow * slow + transformative * transformative);
        }

        private static float Distinctiveness(CandidateEvaluation evaluation)
        {
            var scores = new[] { evaluation.StableScore, evaluation.SlowBuildScore, evaluation.TransformativeScore };
            Array.Sort(scores);
            return scores[2] - scores[1];
        }

        private static float EvaluateTension(CandidateEvaluation a, CandidateEvaluation b, CandidateEvaluation c)
        {
            float maximum = Mathf.Max(a.Viability, Mathf.Max(b.Viability, c.Viability));
            float minimum = Mathf.Min(a.Viability, Mathf.Min(b.Viability, c.Viability));
            return Mathf.Clamp(100f - (maximum - minimum) * 1.5f, 0f, 100f);
        }

        public static CandidateRole HighestRole(CandidateEvaluation evaluation)
        {
            if (evaluation.SlowBuildScore > evaluation.StableScore && evaluation.SlowBuildScore >= evaluation.TransformativeScore)
                return CandidateRole.SlowBuild;
            if (evaluation.TransformativeScore > evaluation.StableScore && evaluation.TransformativeScore > evaluation.SlowBuildScore)
                return CandidateRole.Transformative;
            return CandidateRole.Stable;
        }
    }
}
