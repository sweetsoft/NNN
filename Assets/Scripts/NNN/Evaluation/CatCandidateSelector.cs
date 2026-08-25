using System;
using System.Collections.Generic;
using System.Linq;

namespace NNN
{
    /// <summary>役割と、その役割へ選ばれた猫の評価結果を結び付ける。</summary>
    [Serializable]
    public sealed class SelectedCandidate
    {
        /// <summary>この猫が候補一覧で担当する役割。</summary>
        public CandidateRole Role;
        /// <summary>表示と比較に使う、選抜時点の完全な評価結果。</summary>
        public CandidateEvaluation Evaluation;
    }

    /// <summary>全猫を評価し、3役割へ重複しない一匹ずつを割り当てる。</summary>
    public sealed class CatCandidateSelector
    {
        /// <summary>選抜前に各猫の基礎スコアを計算する評価器。</summary>
        private readonly CatCandidateEvaluator evaluator = new CatCandidateEvaluator();

        /// <summary>
        /// Stable→SlowBuild→Transformativeの順で最高点を採用する。
        /// 先に使われた猫を除外するため、一匹が複数役を占有しない。
        /// </summary>
        public List<SelectedCandidate> Select(IList<CatDefinition> cats, GeneratedCase target, int seed)
        {
            var evaluations = cats.Where(c => c != null).Select(c => evaluator.Evaluate(c, target)).ToList();
            var used = new HashSet<CatDefinition>();
            var result = new List<SelectedCandidate>();
            foreach (CandidateRole role in Enum.GetValues(typeof(CandidateRole)))
            {
                // Tiny deterministic jitter exposes seed behavior without overpowering the designed scores.
                var ranked = evaluations
                    .Where(e => !used.Contains(e.Cat))
                    .OrderByDescending(e => e.ScoreFor(role) + Jitter(seed, e.Cat.Id, role))
                    .ToList();
                if (ranked.Count == 0) continue;
                used.Add(ranked[0].Cat);
                result.Add(new SelectedCandidate { Role = role, Evaluation = ranked[0] });
            }
            return result;
        }

        public List<CandidateEvaluation> EvaluateAll(IList<CatDefinition> cats, GeneratedCase target)
            => cats.Where(c => c != null).Select(c => evaluator.Evaluate(c, target)).ToList();

        private static float Jitter(int seed, string id, CandidateRole role)
        {
            unchecked
            {
                int stableIdHash = 17;
                foreach (char character in id ?? "") stableIdHash = stableIdHash * 31 + character;
                int hash = seed * 397 ^ stableIdHash ^ ((int)role * 7919);
                return new Random(hash).Next(-30, 31) / 10f;
            }
        }
    }
}
