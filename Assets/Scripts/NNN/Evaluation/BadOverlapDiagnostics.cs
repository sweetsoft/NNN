using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>Evaluatorの得点には使わない、machine-readableな診断理由。</summary>
    [Serializable]
    public sealed class CandidateDiagnosticReason
    {
        public string Key;
        public float Weight;
        public float SignedWeight;

        public CandidateDiagnosticReason(string key, float weight, float sign = 1f)
        {
            Key = key;
            Weight = weight;
            SignedWeight = weight * Mathf.Sign(sign);
        }
    }

    /// <summary>猫ペア間のBadOverlap診断。SetScoreや抽選には参照されない。</summary>
    [Serializable]
    public sealed class CandidatePairBadOverlapDiagnostic
    {
        public CandidateEvaluation CatA;
        public CandidateEvaluation CatB;
        public CandidateEvaluation DominantCat;
        public CandidateEvaluation NearDominatedCat;
        public float FunctionalSimilarity;
        public float LowSideTradeoffValue;
        public bool NearDominance;
        public bool BadOverlapCandidate;

        public string PairName => CatA == null || CatB == null
            ? ""
            : CatA.Cat.DisplayName + " / " + CatB.Cat.DisplayName;
    }

    /// <summary>3匹セット内の3ペアをまとめた診断結果。</summary>
    [Serializable]
    public sealed class CandidateSetBadOverlapDiagnostic
    {
        public readonly List<CandidatePairBadOverlapDiagnostic> Pairs = new List<CandidatePairBadOverlapDiagnostic>();
        public CandidatePairBadOverlapDiagnostic RepresentativePair;
        public int BadOverlapPairCount;
        public bool BadOverlapCandidate => BadOverlapPairCount > 0;
    }

    /// <summary>既存Evaluator条件を再判定し、診断Reasonだけを生成する。</summary>
    internal static class CandidateDiagnosticReasonBuilder
    {
        public static void Populate(CandidateEvaluation evaluation, GeneratedCase target)
        {
            if (evaluation == null || evaluation.Cat == null || target == null) return;
            var cat = evaluation.Cat;
            var residence = target.Residence;
            var lifestyle = target.Lifestyle;

            if (cat.HasTrait("high_place") && residence != null && residence.Vertical * 0.2f > 8f) Add(evaluation, "environment.high_place_vertical", 0.6f);
            if (cat.Activity > (residence != null ? residence.Space + 25 : 75)) Add(evaluation, "environment.activity_space_mismatch", 0.8f, -1f);
            if (lifestyle != null && lifestyle.MostlyHome) Add(evaluation, "lifestyle.mostly_home_sociability", 0.6f);
            if (lifestyle != null && lifestyle.NightOwl && cat.HasTrait("night")) Add(evaluation, "lifestyle.night_match", 0.6f);
            if (lifestyle != null && lifestyle.OftenAway) Add(evaluation, "lifestyle.often_away_independence", 0.6f);
            if (lifestyle != null && lifestyle.IrregularSchedule)
                Add(evaluation, cat.Adaptability >= 60 ? "lifestyle.irregular_adaptable" : "lifestyle.irregular_burden", 0.6f, cat.Adaptability >= 60 ? 1f : -1f);

            if (cat.HasTrait("gentle")) Add(evaluation, "human.gentle_low_onboarding", 0.7f);
            if (cat.HasTrait("lonely") && lifestyle != null && lifestyle.MostlyHome) Add(evaluation, "human.lonely_supported_by_home", 0.7f);
            if (HasHumanTrait(target, "quiet_life") && cat.HasTrait("quiet")) Add(evaluation, "human.quiet_life_quiet_cat", 0.7f);
            if (HasHumanTrait(target, "nervous") && cat.Activity > 65) Add(evaluation, "human.nervous_high_activity", 0.8f, -1f);
            if (cat.HasTrait("escape")) Add(evaluation, "risk.escape_residence", 0.8f, -1f);
            if (cat.HasTrait("timid")) Add(evaluation, "risk.timid_initial_load", 0.8f, -1f);
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("escape")) Add(evaluation, "risk.cat_experienced_escape_mitigation", 0.8f);

            AddStable(evaluation, cat, target, residence);
            AddSlowBuild(evaluation, cat, target);
            AddTransformative(evaluation, cat, target, residence);
        }

        private static void AddStable(CandidateEvaluation e, CatDefinition cat, GeneratedCase target, ResidenceDefinition residence)
        {
            if (HasHumanTrait(target, "quiet_life") && cat.HasTrait("quiet")) Add(e, "stable.quiet_life_quiet_cat", 1f);
            if (HasHumanTrait(target, "caretaker") && cat.HasTrait("affectionate")) Add(e, "stable.caretaker_affectionate", 1f);
            if (HasHumanTrait(target, "lonely") && cat.Sociability >= 70) Add(e, "stable.lonely_high_sociability", 1f);
            if (HasHumanTrait(target, "often_away") && cat.Independence >= 70) Add(e, "stable.often_away_high_independence", 1f);
            if (HasHumanTrait(target, "often_away") && cat.Sociability >= 70) Add(e, "stable.often_away_high_sociability_penalty", 1f, -1f);
            if (HasHumanTrait(target, "regular") && cat.Adaptability >= 70) Add(e, "stable.regular_adaptable", 0.7f);
            if (HasHumanTrait(target, "contact_heavy") && cat.Sociability >= 70) Add(e, "stable.contact_heavy_high_sociability", 1f);
            if (HasHumanTrait(target, "irregular") && cat.Independence >= 70) Add(e, "stable.irregular_high_independence", 1f);
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("active")) Add(e, "stable.active_person_active", 1f);
            if (residence != null && residence.Space >= 70f && cat.Activity >= 80f) Add(e, "stable.large_space_high_activity", 0.7f);
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("escape")) Add(e, "stable.cat_experienced_escape", 1f);
        }

        private static void AddSlowBuild(CandidateEvaluation e, CatDefinition cat, GeneratedCase target)
        {
            if (HasHumanTrait(target, "non_intrusive") && cat.HasTrait("timid")) Add(e, "slow.non_intrusive_timid", 1f);
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("timid")) Add(e, "slow.cat_experienced_timid", 1f);
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("independent")) Add(e, "slow.cat_experienced_independent", 1f);
            if (HasHumanTrait(target, "often_away") && cat.Independence >= 70) Add(e, "slow.often_away_high_independence", 1f);
            if (HasHumanTrait(target, "nervous") && cat.HasTrait("timid")) Add(e, "slow.nervous_timid_penalty", 1f, -1f);
            if (HasHumanTrait(target, "contact_heavy") && cat.HasTrait("timid")) Add(e, "slow.contact_heavy_timid_penalty", 1f, -1f);
            if (HasHumanTrait(target, "caretaker") && cat.HasTrait("independent")) Add(e, "slow.caretaker_independent", 1f);
            if (HasHumanTrait(target, "irregular") && cat.Adaptability < 60) Add(e, "slow.irregular_low_adaptability_penalty", 1f, -1f);
            if (HasHumanTrait(target, "irregular") && cat.Independence >= 70) Add(e, "slow.irregular_high_independence", 1f);
            if (HasHumanTrait(target, "irregular") && cat.HasTrait("timid")) Add(e, "slow.irregular_timid_penalty", 1f, -1f);
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("timid")) Add(e, "slow.active_person_timid_penalty", 1f, -1f);
            if (HasHumanTrait(target, "playful") && cat.HasTrait("playful") && cat.Independence >= 60) Add(e, "slow.playful_independent_playful", 1f);
        }

        private static void AddTransformative(CandidateEvaluation e, CatDefinition cat, GeneratedCase target, ResidenceDefinition residence)
        {
            if (HasProblem(target, "monotony") && cat.HasTrait("active")) Add(e, "transform.monotony_active", 1f);
            if (HasHumanTrait(target, "withdrawn") && cat.HasTrait("playful")) Add(e, "transform.withdrawn_playful", 1f);
            if (HasProblem(target, "improvement") && cat.Activity >= 70) Add(e, "transform.improvement_high_activity", 1f);
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("active")) Add(e, "transform.active_person_active_penalty", 1f, -1f);
            if (HasHumanTrait(target, "nervous") && cat.Activity >= 70) Add(e, "transform.nervous_high_activity_penalty", 1f, -1f);
            if (residence != null && residence.Space < 40 && cat.Activity >= 70) Add(e, "transform.small_space_high_activity_penalty", 1f, -1f);
            if (HasHumanTrait(target, "playful") && cat.HasTrait("playful")) Add(e, "transform.playful_pair", 1f);
            if (HasHumanTrait(target, "lonely") && cat.HasTrait("playful") && cat.Sociability >= 60) Add(e, "transform.lonely_social_playful", 1f);
            if (HasHumanTrait(target, "regular") && cat.HasTrait("playful") && cat.Adaptability >= 80) Add(e, "transform.regular_adaptable_playful", 1f);
            if (HasHumanTrait(target, "often_away") && cat.Activity >= 70 && cat.Independence >= 50) Add(e, "transform.often_away_active_independent", 1f);
        }

        private static void Add(CandidateEvaluation e, string key, float weight, float sign = 1f)
        {
            if (!e.DiagnosticReasons.Exists(x => x.Key == key))
                e.DiagnosticReasons.Add(new CandidateDiagnosticReason(key, weight, sign));
        }

        private static bool HasHumanTrait(GeneratedCase target, string id)
            => target != null && target.Human != null && target.Human.Traits.Exists(t => t != null && t.Id == id);

        private static bool HasProblem(GeneratedCase target, string id)
            => target != null && target.Problems.Exists(p => p != null && p.Id == id);
    }

    /// <summary>スコアへ影響しないBadOverlap計算本体。</summary>
    internal static class CandidateBadOverlapAnalyzer
    {
        public const float FunctionalSimilarityThreshold = 0.25f;
        public const float LowSideTradeoffThreshold = 2f;

        public static CandidateSetBadOverlapDiagnostic EvaluateSet(CandidateEvaluation a, CandidateEvaluation b, CandidateEvaluation c)
        {
            var result = new CandidateSetBadOverlapDiagnostic();
            result.Pairs.Add(EvaluatePair(a, b));
            result.Pairs.Add(EvaluatePair(a, c));
            result.Pairs.Add(EvaluatePair(b, c));
            var bad = result.Pairs.Where(x => x.BadOverlapCandidate)
                .OrderByDescending(x => x.FunctionalSimilarity + Mathf.Max(0f, LowSideTradeoffThreshold - x.LowSideTradeoffValue))
                .ToList();
            result.BadOverlapPairCount = bad.Count;
            result.RepresentativePair = bad.Count > 0
                ? bad[0]
                : result.Pairs.OrderByDescending(x => x.FunctionalSimilarity).FirstOrDefault();
            return result;
        }

        private static CandidatePairBadOverlapDiagnostic EvaluatePair(CandidateEvaluation a, CandidateEvaluation b)
        {
            var result = new CandidatePairBadOverlapDiagnostic { CatA = a, CatB = b };
            result.FunctionalSimilarity = FunctionalSimilarity(a, b);

            bool aDominates = NearDominates(a, b);
            bool bDominates = NearDominates(b, a);
            result.NearDominance = aDominates || bDominates;
            if (aDominates) { result.DominantCat = a; result.NearDominatedCat = b; }
            else if (bDominates) { result.DominantCat = b; result.NearDominatedCat = a; }

            var low = a.Viability <= b.Viability ? a : b;
            var high = ReferenceEquals(low, a) ? b : a;
            float viabilityDeficit = high.Viability - low.Viability;
            float bestRoleAdvantage = Mathf.Max(low.StableScore - high.StableScore,
                Mathf.Max(low.SlowBuildScore - high.SlowBuildScore, low.TransformativeScore - high.TransformativeScore));
            float uniqueReasonValue = low.DiagnosticReasons
                .Where(x => !high.DiagnosticReasons.Exists(y => y.Key == x.Key))
                .Sum(x => x.SignedWeight);
            result.LowSideTradeoffValue = Mathf.Max(0f, bestRoleAdvantage) + uniqueReasonValue - viabilityDeficit;
            result.BadOverlapCandidate = result.FunctionalSimilarity >= FunctionalSimilarityThreshold &&
                result.NearDominance && result.LowSideTradeoffValue < LowSideTradeoffThreshold;
            return result;
        }

        private static bool NearDominates(CandidateEvaluation high, CandidateEvaluation low)
        {
            if (high.Viability - low.Viability < 5f) return false;
            var roleDeltas = new[]
            {
                high.StableScore - low.StableScore,
                high.SlowBuildScore - low.SlowBuildScore,
                high.TransformativeScore - low.TransformativeScore
            };
            return roleDeltas.Count(x => x >= 5f) >= 2 && roleDeltas.Min() > -7f;
        }

        private static float FunctionalSimilarity(CandidateEvaluation a, CandidateEvaluation b)
        {
            var shared = a.DiagnosticReasons.Where(x => b.DiagnosticReasons.Exists(y => y.Key == x.Key)).ToList();
            var unionKeys = a.DiagnosticReasons.Select(x => x.Key).Union(b.DiagnosticReasons.Select(x => x.Key)).ToList();
            float sharedWeight = shared.Sum(x => x.Weight);
            float unionWeight = unionKeys.Sum(key =>
            {
                var reason = a.DiagnosticReasons.FirstOrDefault(x => x.Key == key) ?? b.DiagnosticReasons.First(x => x.Key == key);
                return reason.Weight;
            });
            float weightedOverlap = unionWeight > 0f ? sharedWeight / unionWeight : 0f;
            float sharedCountFactor = shared.Count == 0 ? 0f : shared.Count == 1 ? 0.33f : shared.Count == 2 ? 0.67f : 1f;
            float evidenceStrength = Mathf.Min(sharedWeight / 3f, 1f);
            float adjustedOverlap = weightedOverlap * sharedCountFactor * evidenceStrength;
            float traitDistance = (Mathf.Abs(a.Cat.Activity - b.Cat.Activity) +
                Mathf.Abs(a.Cat.Sociability - b.Cat.Sociability) +
                Mathf.Abs(a.Cat.Independence - b.Cat.Independence) +
                Mathf.Abs(a.Cat.Adaptability - b.Cat.Adaptability)) / 400f;
            return adjustedOverlap * (1f - traitDistance);
        }
    }
}
