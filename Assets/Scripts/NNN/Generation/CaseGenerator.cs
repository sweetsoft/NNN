using System;
using UnityEngine;

namespace NNN
{
    /// <summary>生成プロファイルとSeedから、評価可能な一件の案件を組み立てる。</summary>
    public sealed class CaseGenerator
    {
        /// <summary>
        /// 固定テストモードでは田中案件をそのまま返し、通常モードでは
        /// 各マスター一覧からSeed付き疑似乱数で要素を選ぶ。
        /// </summary>
        public GeneratedCase Generate(CaseGenerationProfile profile, int seed)
        {
            if (profile == null) throw new ArgumentNullException(nameof(profile));
            var random = new System.Random(seed);
            var result = new GeneratedCase { Seed = seed };

            if (profile.UseFixedTanakaCase)
            {
                result.Human = new GeneratedHuman
                {
                    Name = "田中 健一",
                    Age = 34,
                    Archetype = First(profile.HumanArchetypes)
                };
                result.Lifestyle = First(profile.Lifestyles);
                result.Residence = First(profile.Residences);
                result.Problems.AddRange(profile.Problems);
                if (result.Human.Archetype != null)
                    result.Human.Traits.AddRange(result.Human.Archetype.PreferredTraits);
                return result;
            }

            var archetype = Pick(profile.HumanArchetypes, random);
            result.Human = new GeneratedHuman
            {
                Name = "生成ユーザー " + random.Next(100, 1000),
                Age = archetype == null ? random.Next(20, 71) : random.Next(archetype.MinAge, archetype.MaxAge + 1),
                Archetype = archetype
            };
            if (archetype != null) result.Human.Traits.AddRange(archetype.PreferredTraits);
            result.Lifestyle = Pick(profile.Lifestyles, random);
            result.Residence = Pick(profile.Residences, random);
            if (profile.Problems.Count > 0) result.Problems.Add(Pick(profile.Problems, random));
            return result;
        }

        /// <summary>固定テストで一覧の先頭要素をnull安全に取得する。</summary>
        private static T First<T>(System.Collections.Generic.List<T> list) where T : UnityEngine.Object
            => list != null && list.Count > 0 ? list[0] : null;

        /// <summary>疑似乱数を使って一覧から一要素をnull安全に取得する。</summary>
        private static T Pick<T>(System.Collections.Generic.List<T> list, System.Random random) where T : UnityEngine.Object
            => list != null && list.Count > 0 ? list[random.Next(list.Count)] : null;
    }
}
