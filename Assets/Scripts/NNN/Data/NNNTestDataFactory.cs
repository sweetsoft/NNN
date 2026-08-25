using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>
    /// 会話で確定した田中案件と猫10匹を再構築するFactory。
    /// Editorのアセット生成と実行時フォールバックで共用する。
    /// </summary>
    public static class NNNTestDataFactory
    {
        /// <summary>参照関係まで接続済みのテスト用プロファイルをメモリ上に作る。</summary>
        public static CaseGenerationProfile CreateRuntimeProfile()
        {
            var profile = Make<CaseGenerationProfile>("Tanaka_TestProfile");
            profile.UseFixedTanakaCase = true;

            var traits = new Dictionary<string, TraitDefinition>();
            AddTrait(traits, "affectionate", "#甘えん坊");
            AddTrait(traits, "gentle", "#温厚");
            AddTrait(traits, "timid", "#臆病");
            AddTrait(traits, "night", "#夜型");
            AddTrait(traits, "high_place", "#高所好き");
            AddTrait(traits, "active", "#活発");
            AddTrait(traits, "playful", "#遊び好き");
            AddTrait(traits, "independent", "#自立型");
            AddTrait(traits, "quiet", "#静か");
            AddTrait(traits, "lonely", "#寂しがり");
            AddTrait(traits, "escape", "#脱走傾向");
            AddTrait(traits, "outdoor", "#外好き");
            AddTrait(traits, "mostly_home", "#在宅");
            AddTrait(traits, "non_intrusive", "#干渉しすぎない");
            AddTrait(traits, "often_away", "#留守多");
            AddTrait(traits, "regular", "#規則的");
            AddTrait(traits, "caretaker", "#世話焼き");
            AddTrait(traits, "quiet_life", "#静かな生活");
            AddTrait(traits, "contact_heavy", "#接触多");
            AddTrait(traits, "nervous", "#神経質");
            AddTrait(traits, "withdrawn", "#引きこもり気味");
            AddTrait(traits, "active_person", "#活動的");
            AddTrait(traits, "cat_experienced", "#猫経験あり");
            AddTrait(traits, "irregular", "#不規則");
            AddTrait(traits, "improvement", "#生活改善余地");
            AddTrait(traits, "monotony", "#単調な生活");

            var human = Make<HumanArchetype>("Human_Tanaka");
            human.Id = "tanaka";
            human.DisplayName = "在宅・夜型の単身者";
            human.MinAge = human.MaxAge = 34;
            human.Description = "干渉しすぎず、猫との生活で日常を少し変えたい";
            human.PreferredTraits.AddRange(new[] { traits["mostly_home"], traits["night"], traits["non_intrusive"] });
            profile.HumanArchetypes.Add(human);

            var lifestyle = Make<LifestyleDefinition>("Lifestyle_Tanaka");
            lifestyle.Id = "tanaka_lifestyle";
            lifestyle.DisplayName = "在宅・夜型・干渉少なめ";
            lifestyle.MostlyHome = true;
            lifestyle.OftenAway = false;
            lifestyle.NightOwl = true;
            lifestyle.InteractionDemand = 35;
            profile.Lifestyles.Add(lifestyle);

            var residence = Make<ResidenceDefinition>("Residence_Tanaka");
            residence.Id = "tanaka_2ldk";
            residence.DisplayName = "田中宅";
            residence.ResidenceType = "2LDKマンション";
            residence.Space = 55;
            residence.Vertical = 65;
            residence.EscapeRisk = 25;
            profile.Residences.Add(residence);

            profile.Problems.Add(Problem("improvement", "生活改善余地", "猫を迎えることで生活リズムを整えられる余地がある", CandidateRole.Stable));
            profile.Problems.Add(Problem("monotony", "単調な生活", "毎日の変化が少なく、新しい関係性を必要としている", CandidateRole.Transformative));

            profile.Cats.Add(Cat("CAT_001", "ミケ", 3, 40, 85, 35, 85, traits, "affectionate", "gentle"));
            profile.Cats.Add(Cat("CAT_002", "クロ", 6, 30, 30, 80, 55, traits, "timid", "night", "high_place"));
            profile.Cats.Add(Cat("CAT_003", "トラ", 1, 90, 80, 30, 70, traits, "active", "playful", "night"));
            profile.Cats.Add(Cat("CAT_004", "シロ", 8, 20, 60, 65, 80, traits, "gentle", "quiet"));
            profile.Cats.Add(Cat("CAT_005", "サバ", 4, 65, 45, 75, 65, traits, "playful", "independent"));
            profile.Cats.Add(Cat("CAT_006", "ハチ", 2, 80, 35, 60, 45, traits, "active", "high_place", "timid"));
            profile.Cats.Add(Cat("CAT_007", "モモ", 5, 35, 95, 20, 60, traits, "affectionate", "lonely"));
            profile.Cats.Add(Cat("CAT_008", "ゴマ", 7, 25, 25, 90, 75, traits, "quiet", "independent", "night"));
            profile.Cats.Add(Cat("CAT_009", "チャチャ", 2, 95, 65, 35, 40, traits, "active", "escape", "outdoor"));
            profile.Cats.Add(Cat("CAT_010", "ソラ", 4, 55, 70, 55, 90, traits, "gentle", "playful", "high_place"));

            var improvement = profile.Problems[0];
            var monotony = profile.Problems[1];
            profile.HumanBenchmarks.Add(Benchmark("H01", "田中 健一", 34, "在宅・夜型・干渉少なめ", "2LDKマンション", 55, 65, 25,
                true, false, true, false, 35, traits, new[] { improvement, monotony }, "mostly_home", "night", "non_intrusive", "improvement", "monotony"));
            profile.HumanBenchmarks.Add(Benchmark("H02", "佐藤 美咲", 29, "留守多・規則的・世話焼き", "1LDK", 45, 45, 20,
                false, true, false, false, 70, traits, null, "often_away", "regular", "caretaker"));
            profile.HumanBenchmarks.Add(Benchmark("H03", "山本 和夫", 68, "在宅・静か・規則的", "戸建", 70, 40, 15,
                true, false, false, false, 30, traits, null, "mostly_home", "quiet_life", "regular", "non_intrusive"));
            profile.HumanBenchmarks.Add(Benchmark("H04", "鈴木 彩", 25, "在宅・寂しがり・接触多", "1K", 30, 50, 20,
                true, false, false, false, 90, traits, null, "mostly_home", "lonely", "caretaker", "contact_heavy"));
            profile.HumanBenchmarks.Add(Benchmark("H05", "高橋 直樹", 41, "留守多・夜型・干渉少なめ", "2LDK", 60, 75, 25,
                false, true, true, false, 25, traits, null, "often_away", "night", "non_intrusive"));
            profile.HumanBenchmarks.Add(Benchmark("H06", "伊藤 由美", 38, "神経質・静か・在宅", "1K", 28, 35, 15,
                true, false, false, false, 40, traits, null, "nervous", "quiet_life", "mostly_home"));
            profile.HumanBenchmarks.Add(Benchmark("H07", "中村 浩二", 32, "引きこもり・単調・改善余地", "1LDK", 50, 60, 20,
                true, false, false, false, 65, traits, new[] { improvement, monotony }, "withdrawn", "monotony", "improvement", "lonely", "mostly_home"));
            profile.HumanBenchmarks.Add(Benchmark("H08", "小林 麻衣", 36, "活動的・遊び好き・猫経験あり", "戸建", 80, 75, 30,
                false, false, false, false, 65, traits, null, "active_person", "playful", "regular", "cat_experienced"));
            profile.HumanBenchmarks.Add(Benchmark("H09", "加藤 修", 52, "不規則・夜型・干渉少なめ", "2DK", 48, 70, 20,
                false, false, true, true, 25, traits, null, "irregular", "non_intrusive", "cat_experienced", "night"));
            profile.HumanBenchmarks.Add(Benchmark("H10", "吉田 恵", 44, "在宅・世話焼き・寂しがり", "戸建", 75, 65, 15,
                true, false, false, false, 85, traits, null, "mostly_home", "caretaker", "lonely", "regular"));
            return profile;
        }

        /// <summary>指定型のScriptableObjectを作り、識別しやすい内部名を設定する。</summary>
        private static T Make<T>(string objectName) where T : ScriptableObject
        {
            var value = ScriptableObject.CreateInstance<T>();
            value.name = objectName;
            return value;
        }

        /// <summary>Traitを一件作り、IDで参照できる辞書へ登録する。</summary>
        private static void AddTrait(Dictionary<string, TraitDefinition> map, string id, string display)
        {
            var trait = Make<TraitDefinition>("Trait_" + id);
            trait.Id = id;
            trait.DisplayName = display;
            map[id] = trait;
        }

        /// <summary>田中案件に含める課題マスターを一件作成する。</summary>
        private static ProblemDefinition Problem(string id, string name, string description, CandidateRole role)
        {
            var item = Make<ProblemDefinition>("Problem_" + id);
            item.Id = id;
            item.DisplayName = name;
            item.Description = description;
            item.FavoredRole = role;
            return item;
        }

        /// <summary>4能力値と可変個のTrait IDから猫マスターを一匹作成する。</summary>
        private static CatDefinition Cat(string id, string displayName, int age, int activity, int sociability,
            int independence, int adaptability, Dictionary<string, TraitDefinition> traits, params string[] traitIds)
        {
            var cat = Make<CatDefinition>(id + "_" + displayName);
            cat.Id = id;
            cat.DisplayName = displayName;
            cat.Age = age;
            cat.Activity = activity;
            cat.Sociability = sociability;
            cat.Independence = independence;
            cat.Adaptability = adaptability;
            foreach (var traitId in traitIds) cat.Traits.Add(traits[traitId]);
            return cat;
        }

        /// <summary>固定人間・Lifestyle・住居を一体で作り、ランダム候補から分離する。</summary>
        private static HumanBenchmarkDefinition Benchmark(string id, string humanName, int age, string description,
            string residenceType, int space, int vertical, int escapeRisk, bool mostlyHome, bool oftenAway,
            bool nightOwl, bool irregular, int interactionDemand, Dictionary<string, TraitDefinition> traits,
            ProblemDefinition[] problems, params string[] traitIds)
        {
            var archetype = Make<HumanArchetype>("Human_" + id);
            archetype.Id = id.ToLowerInvariant() + "_human";
            archetype.DisplayName = description;
            archetype.Description = description;
            archetype.MinAge = archetype.MaxAge = age;
            foreach (var traitId in traitIds) archetype.PreferredTraits.Add(traits[traitId]);

            var lifestyle = Make<LifestyleDefinition>("Lifestyle_" + id);
            lifestyle.Id = id.ToLowerInvariant() + "_lifestyle";
            lifestyle.DisplayName = description;
            lifestyle.MostlyHome = mostlyHome;
            lifestyle.OftenAway = oftenAway;
            lifestyle.NightOwl = nightOwl;
            lifestyle.IrregularSchedule = irregular;
            lifestyle.InteractionDemand = interactionDemand;

            var residence = Make<ResidenceDefinition>("Residence_" + id);
            residence.Id = id.ToLowerInvariant() + "_residence";
            residence.DisplayName = humanName + "宅";
            residence.ResidenceType = residenceType;
            residence.Space = space;
            residence.Vertical = vertical;
            residence.EscapeRisk = escapeRisk;

            var item = Make<HumanBenchmarkDefinition>(id + "_" + humanName.Replace(" ", ""));
            item.Id = id.ToLowerInvariant();
            item.BenchmarkId = id;
            item.DisplayName = id + " " + humanName;
            item.HumanName = humanName;
            item.Age = age;
            item.Archetype = archetype;
            item.Lifestyle = lifestyle;
            item.Residence = residence;
            if (problems != null) item.Problems.AddRange(problems);
            return item;
        }
    }
}
