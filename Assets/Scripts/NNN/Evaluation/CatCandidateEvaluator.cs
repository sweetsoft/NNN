using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>一匹の猫を一件の案件に当てたときの全評価結果。</summary>
    [Serializable]
    public sealed class CandidateEvaluation
    {
        /// <summary>評価対象となった猫マスター。</summary>
        public CatDefinition Cat;
        /// <summary>広さや上下空間など、住居との適合度。高いほど良い。</summary>
        public float Environment;
        /// <summary>在宅・夜型・干渉度など、生活リズムとの適合度。</summary>
        public float Lifestyle;
        /// <summary>依頼人が無理なく関係を築ける度合い。</summary>
        public float Human;
        /// <summary>脱走、過活動、臆病さなどの懸念度。唯一、低いほど良い指標。</summary>
        public float Risk;
        /// <summary>3適合値からRisk影響を引いた、飼育成立可能性の総合値。</summary>
        public float Viability;
        /// <summary>すぐ安定した関係を築くStable役としての得点。</summary>
        public float StableScore;
        /// <summary>時間をかけて距離を縮めるSlowBuild役としての得点。</summary>
        public float SlowBuildScore;
        /// <summary>依頼人の日常を大きく変えるTransformative役としての得点。</summary>
        public float TransformativeScore;
        /// <summary>デバッグ画面へ表示する、主な加点・減点・集計理由。</summary>
        public readonly List<string> Reasons = new List<string>();

        /// <summary>指定された役割に対応するRole Scoreだけを返す。</summary>
        public float ScoreFor(CandidateRole role)
        {
            switch (role)
            {
                case CandidateRole.Stable: return StableScore;
                case CandidateRole.SlowBuild: return SlowBuildScore;
                default: return TransformativeScore;
            }
        }
    }

    /// <summary>猫一匹と生成済み案件を比較し、適合・Risk・役割別得点を算出する。</summary>
    public sealed class CatCandidateEvaluator
    {
        /// <summary>
        /// 評価を実行する。各中間値を0～100へ収めることで、係数調整時にも
        /// UI上で比較しやすいスケールを維持する。
        /// </summary>
        public CandidateEvaluation Evaluate(CatDefinition cat, GeneratedCase target)
        {
            var e = new CandidateEvaluation { Cat = cat };
            var residence = target.Residence;
            var lifestyle = target.Lifestyle;

            // 活動量が住居のSpaceを超えた分だけ基礎適合を下げる。
            float spaceFit = 100f - Mathf.Max(0f, cat.Activity - (residence != null ? residence.Space : 50));
            // 高所好きだけがVertical設備の恩恵を受ける。最大20点の補助値。
            float verticalBonus = cat.HasTrait("high_place") && residence != null ? residence.Vertical * 0.2f : 0f;
            e.Environment = Clamp(spaceFit * 0.8f + verticalBonus);
            if (verticalBonus > 8f) e.Reasons.Add("+ 高所好きとVerticalの相性が良い");
            if (cat.Activity > (residence != null ? residence.Space + 25 : 75)) e.Reasons.Add("- 活動量に対してSpaceが小さい");

            e.Lifestyle = 55f;
            if (lifestyle != null && lifestyle.MostlyHome)
            {
                e.Lifestyle += cat.Sociability * 0.25f;
                e.Reasons.Add("+ 在宅時間とSociabilityが噛み合う");
            }
            if (lifestyle != null && lifestyle.NightOwl && cat.HasTrait("night"))
            {
                e.Lifestyle += 15f;
                e.Reasons.Add("+ 人と猫がともに夜型");
            }
            if (lifestyle != null && lifestyle.InteractionDemand < 45)
                e.Lifestyle += cat.Independence * 0.15f;
            if (lifestyle != null && lifestyle.OftenAway)
            {
                e.Lifestyle += cat.Independence * 0.25f - cat.Sociability * 0.12f;
                e.Reasons.Add("+ 留守の多さをIndependenceで補う");
            }
            if (lifestyle != null && lifestyle.IrregularSchedule)
            {
                e.Lifestyle += (cat.Adaptability - 50f) * 0.25f;
                e.Reasons.Add(cat.Adaptability >= 60 ? "+ 不規則生活へ適応しやすい" : "- 不規則生活への適応負荷");
            }
            e.Lifestyle = Clamp(e.Lifestyle);

            e.Human = Clamp(35f + cat.Adaptability * 0.35f + cat.Sociability * 0.25f);
            if (cat.HasTrait("gentle")) { e.Human += 8f; e.Reasons.Add("+ 温厚で導入負荷が低い"); }
            if (cat.HasTrait("lonely") && lifestyle != null && lifestyle.MostlyHome) { e.Human += 5f; e.Reasons.Add("+ 寂しがりだが在宅で支えられる"); }
            if (HasHumanTrait(target, "quiet_life") && cat.HasTrait("quiet")) { e.Human += 12f; e.Reasons.Add("+ 静かな人と静かな猫の相性"); }
            if (HasHumanTrait(target, "nervous") && cat.Activity > 65) { e.Human -= (cat.Activity - 65) * 0.45f; e.Reasons.Add("- 神経質な人に対して活動量が高い"); }
            if (HasHumanTrait(target, "caretaker")) e.Human += (cat.Sociability - 50f) * 0.12f;
            e.Human = Clamp(e.Human);

            e.Risk = 8f + Mathf.Max(0, cat.Activity - 75) * 0.45f + Mathf.Max(0, 40 - cat.Adaptability) * 0.35f;
            if (cat.HasTrait("escape"))
            {
                e.Risk += (residence != null ? residence.EscapeRisk : 50) * 0.75f;
                e.Reasons.Add("- 脱走傾向×EscapeRisk");
            }
            if (cat.HasTrait("timid")) { e.Risk += 8f; e.Reasons.Add("- 臆病さによる初期負荷"); }


            // 1. 猫経験者 × 脱走傾向
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("escape"))
            {
                e.Risk -= 10f;
            }

            e.Risk = Clamp(e.Risk);

            // 適合3軸の平均を土台にし、Riskの35%を差し引く。+20は通常案件が
            // 比較可能な中～高得点帯へ収まるようにする基準補正。
            e.Viability = Clamp((e.Environment + e.Lifestyle + e.Human) / 3f - e.Risk * 0.35f + 20f);
            // 猫単体の万能さは土台に留め、Role Scoreの主要な差を
            // 「猫×人間×住居」の相互作用で作る。
            e.StableScore = e.Viability * 0.42f + cat.Adaptability * 0.14f + cat.Sociability * 0.07f - e.Risk * 0.18f;
            AddStableInteractions(e, cat, target);

            // 臆病・自立という猫単体の適性だけでSlowBuildを独占しないよう、
            // 固定加点を小さくし、接し方・経験・留守時間との相性を主因にする。
            e.SlowBuildScore = e.Viability * 0.34f + cat.Independence * 0.16f
                + (100 - cat.Sociability) * 0.07f + (cat.HasTrait("timid") ? 10f : 0f) - e.Risk * 0.10f;
            AddSlowBuildInteractions(e, cat, target);

            // Activity/Sociabilityの絶対値は小さなRole適性だけにし、
            // 生活を変える余地と住居が、その活発さを受け止められるかで順位を変える。
            e.TransformativeScore = e.Viability * 0.30f - e.Risk * 0.22f;
            if (cat.Activity >= 70) e.TransformativeScore += 10f;
            if (cat.Sociability >= 70) e.TransformativeScore += 5f;
            AddTransformativeInteractions(e, cat, target, residence);

            // 2. 活動的な人 × 活発な猫
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("active"))
            {
                e.StableScore += 10f;
                e.Reasons.Add("+ Stable: 活動的な生活×活発な猫 +10");
            }

            // 3. 広い住居 × 高Activity猫
            if (residence.Space >= 70f && cat.Activity >= 80f)
            {
                e.StableScore += 5f;
            }

            e.StableScore = Clamp(e.StableScore);
            e.SlowBuildScore = Clamp(e.SlowBuildScore);
            e.TransformativeScore = Clamp(e.TransformativeScore);

            e.Reasons.Add(string.Format("= Viability {0:0.0} (適合平均 − Risk影響)", e.Viability));
            return e;
        }

        /// <summary>全スコアをデバッグUI共通の0～100範囲に制限する。</summary>
        private static float Clamp(float value) => Mathf.Clamp(value, 0f, 100f);

        private static bool HasHumanTrait(GeneratedCase target, string id)
            => target != null && target.Human != null && target.Human.Traits.Exists(t => t != null && t.Id == id);

        private static bool HasProblem(GeneratedCase target, string id)
            => target != null && target.Problems.Exists(p => p != null && p.Id == id);

        private static void AddStableInteractions(CandidateEvaluation e, CatDefinition cat, GeneratedCase target)
        {
            if (HasHumanTrait(target, "quiet_life") && cat.HasTrait("quiet")) Add(ref e.StableScore, 25f, e, "+ Stable: 静かな生活×静かな猫 +25");
            if (HasHumanTrait(target, "caretaker") && cat.HasTrait("affectionate")) Add(ref e.StableScore, 20f, e, "+ Stable: 世話焼き×甘えん坊 +20");
            if (HasHumanTrait(target, "lonely") && cat.Sociability >= 70) Add(ref e.StableScore, 20f, e, "+ Stable: 寂しがり×高Sociability +20");
            if (HasHumanTrait(target, "often_away") && cat.Independence >= 70) Add(ref e.StableScore, 25f, e, "+ Stable: 留守多×高Independence +25");
            if (HasHumanTrait(target, "often_away") && cat.Sociability >= 70) Add(ref e.StableScore, -20f, e, "- Stable: 留守多×高Sociability -20");
            if (HasHumanTrait(target, "regular") && cat.Adaptability >= 70) Add(ref e.StableScore, 8f, e, "+ Stable: 規則的×馴染みやすさ +8");
            if (HasHumanTrait(target, "contact_heavy") && cat.Sociability >= 70) Add(ref e.StableScore, 12f, e, "+ Stable: 接触多×高Sociability +12");
            if (HasHumanTrait(target, "irregular") && cat.Independence >= 70) Add(ref e.StableScore, 18f, e, "+ Stable: 不規則×高Independence +18");
        }

        private static void AddSlowBuildInteractions(CandidateEvaluation e, CatDefinition cat, GeneratedCase target)
        {
            if (HasHumanTrait(target, "non_intrusive") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, 25f, e, "+ SlowBuild: 干渉しすぎない×臆病 +25");
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, 15f, e, "+ SlowBuild: 猫経験あり×臆病 +15");
            if (HasHumanTrait(target, "cat_experienced") && cat.HasTrait("independent")) Add(ref e.SlowBuildScore, 12f, e, "+ SlowBuild: 猫経験あり×自立型 +12");
            if (HasHumanTrait(target, "often_away") && cat.Independence >= 70) Add(ref e.SlowBuildScore, 20f, e, "+ SlowBuild: 留守多×高Independence +20");
            if (HasHumanTrait(target, "nervous") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, -20f, e, "- SlowBuild: 神経質×臆病 -20");
            if (HasHumanTrait(target, "contact_heavy") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, -25f, e, "- SlowBuild: 接触多×臆病 -25");
            if (HasHumanTrait(target, "caretaker") && cat.HasTrait("independent")) Add(ref e.SlowBuildScore, 10f, e, "+ SlowBuild: 世話を待てる人×自立型 +10");
            if (HasHumanTrait(target, "irregular") && cat.Adaptability < 60) Add(ref e.SlowBuildScore, -15f, e, "- SlowBuild: 不規則×低Adaptability -15");
            if (HasHumanTrait(target, "irregular") && cat.Independence >= 70) Add(ref e.SlowBuildScore, 25f, e, "+ SlowBuild: 不規則×高Independence +25");
            if (HasHumanTrait(target, "irregular") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, -30f, e, "- SlowBuild: 不規則×臆病 -30");
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("timid")) Add(ref e.SlowBuildScore, -15f, e, "- SlowBuild: 活動的×臆病 -15");
            if (HasHumanTrait(target, "playful") && cat.HasTrait("playful") && cat.Independence >= 60) Add(ref e.SlowBuildScore, 15f, e, "+ SlowBuild: 遊び好き×自立した遊び好き +15");
        }

        private static void AddTransformativeInteractions(CandidateEvaluation e, CatDefinition cat, GeneratedCase target, ResidenceDefinition residence)
        {
            if (HasProblem(target, "monotony") && cat.HasTrait("active")) Add(ref e.TransformativeScore, 25f, e, "+ Transformative: 単調な生活×活発 +25");
            if (HasHumanTrait(target, "withdrawn") && cat.HasTrait("playful")) Add(ref e.TransformativeScore, 25f, e, "+ Transformative: 引きこもり気味×遊び好き +25");
            if (HasProblem(target, "improvement") && cat.Activity >= 70) Add(ref e.TransformativeScore, 20f, e, "+ Transformative: 生活改善余地×高Activity +20");
            if (HasHumanTrait(target, "active_person") && cat.HasTrait("active")) Add(ref e.TransformativeScore, -20f, e, "- Transformative: 活動的×活発 -20");
            if (HasHumanTrait(target, "nervous") && cat.Activity >= 70) Add(ref e.TransformativeScore, -25f, e, "- Transformative: 神経質×高Activity -25");
            if (residence != null && residence.Space < 40 && cat.Activity >= 70) Add(ref e.TransformativeScore, -25f, e, "- Transformative: 狭い住居×高Activity -25");
            if (HasHumanTrait(target, "playful") && cat.HasTrait("playful")) Add(ref e.TransformativeScore, 12f, e, "+ Transformative: 遊び好き同士 +12");
            if (HasHumanTrait(target, "lonely") && cat.HasTrait("playful") && cat.Sociability >= 60) Add(ref e.TransformativeScore, 12f, e, "+ Transformative: 寂しがり×社交的な遊び好き +12");
            if (HasHumanTrait(target, "regular") && cat.HasTrait("playful") && cat.Adaptability >= 80) Add(ref e.TransformativeScore, 12f, e, "+ Transformative: 規則的×馴染みやすい遊び好き +12");
            if (HasHumanTrait(target, "often_away") && cat.Activity >= 70 && cat.Independence >= 50) Add(ref e.TransformativeScore, 18f, e, "+ Transformative: 留守多×自立した活発猫 +18");
        }

        private static void Add(ref float score, float amount, CandidateEvaluation evaluation, string reason)
        {
            score += amount;
            evaluation.Reasons.Add(reason);
        }
    }
}
