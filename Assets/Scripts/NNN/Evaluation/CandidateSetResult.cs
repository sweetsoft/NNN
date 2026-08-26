using System;
using System.Collections.Generic;

namespace NNN
{
    /// <summary>三匹の候補セットに含まれる猫と、セット全体の評価結果。</summary>
    [Serializable]
    public sealed class CandidateSetResult
    {
        public CandidateEvaluation catA;
        public CandidateEvaluation catB;
        public CandidateEvaluation catC;

        [Range01To100] public float viability;
        [Range01To100] public float diversity;
        [Range01To100] public float distinctiveness;
        [Range01To100] public float tension;

        /// <summary>三匹の最高Roleに何種類が含まれるか（1～3）。</summary>
        public int roleCoverage;
        /// <summary>Role Coverageによるソフトボーナス（0 / 7 / 15）。</summary>
        public float roleCoverageBonus;
        /// <summary>既存のSetScore式だけで算出した基礎評価値。実験補正では変更しない。</summary>
        public float totalScore;
        /// <summary>順位・Quality Window・抽選に使う値。実験OFF時はtotalScoreと完全一致する。</summary>
        public float selectionScore;
        /// <summary>BadOverlap experimental selection penalty。基本SetScoreの構成要素ではない。</summary>
        public float badOverlapPenaltyApplied;
        /// <summary>全セットをselectionScore降順に並べた順位（1始まり）。</summary>
        public int rank;
        public int evaluatedSets;
        public int seed;
        /// <summary>TotalScore・順位・抽選へ影響しない並列診断結果。</summary>
        public CandidateSetBadOverlapDiagnostic badOverlapDiagnostic;

        public IEnumerable<CandidateEvaluation> Cats
        {
            get
            {
                yield return catA;
                yield return catB;
                yield return catC;
            }
        }
    }

    // Unity属性に依存せず、フィールドの意図をコード上に残すための軽量マーカー。
    [AttributeUsage(AttributeTargets.Field)]
    internal sealed class Range01To100Attribute : Attribute { }
}
