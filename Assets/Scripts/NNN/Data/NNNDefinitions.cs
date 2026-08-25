using System;
using System.Collections.Generic;
using UnityEngine;

namespace NNN
{
    /// <summary>候補猫に担当させる物語上の役割。</summary>
    public enum CandidateRole { Stable, SlowBuild, Transformative }

    /// <summary>すべてのマスターデータに共通する識別情報を持つ基底クラス。</summary>
    public abstract class IdDefinition : ScriptableObject
    {
        /// <summary>保存や照合に使う、表示名に依存しない一意識別子。</summary>
        public string Id;
        /// <summary>デバッグUIやInspectorに表示する人間向け名称。</summary>
        public string DisplayName;
    }

    /// <summary>生成される依頼人の人物像と、年齢・好む性質を定義する。</summary>
    [CreateAssetMenu(menuName = "NNN/Human Archetype")]
    public sealed class HumanArchetype : IdDefinition
    {
        /// <summary>人物像の補足説明。企画意図をInspector上で共有するために使う。</summary>
        [TextArea] public string Description;
        /// <summary>ランダム案件生成時に選ばれる年齢の下限（両端を含む）。</summary>
        public int MinAge = 20;
        /// <summary>ランダム案件生成時に選ばれる年齢の上限（両端を含む）。</summary>
        public int MaxAge = 70;
        /// <summary>この人物像が持つ生活傾向・接し方をTraitとして表した一覧。</summary>
        public List<TraitDefinition> PreferredTraits = new List<TraitDefinition>();
    }

    /// <summary>在宅状況や生活時間、猫への干渉度を定義する生活マスター。</summary>
    [CreateAssetMenu(menuName = "NNN/Lifestyle")]
    public sealed class LifestyleDefinition : IdDefinition
    {
        /// <summary>日中を含め、自宅で過ごす時間が長いか。</summary>
        public bool MostlyHome;
        /// <summary>仕事や旅行などで長時間・頻繁に家を空けるか。</summary>
        public bool OftenAway;
        /// <summary>夜間を主な活動時間とする生活か。</summary>
        public bool NightOwl;
        /// <summary>就寝・外出などの日課が一定せず、猫側に適応力を求める生活か。</summary>
        public bool IrregularSchedule;
        /// <summary>猫との交流を求める強さ。低いほど干渉しすぎない生活を表す。</summary>
        [Range(0, 100)] public int InteractionDemand = 50;
    }

    /// <summary>猫が暮らす物理環境と安全上の条件を定義する住居マスター。</summary>
    [CreateAssetMenu(menuName = "NNN/Residence")]
    public sealed class ResidenceDefinition : IdDefinition
    {
        /// <summary>画面に表示する間取り・住居種別。</summary>
        public string ResidenceType = "2LDKマンション";
        /// <summary>床面積や走り回れる余裕を0～100で抽象化した値。</summary>
        [Range(0, 100)] public int Space = 50;
        /// <summary>棚やキャットタワーなど上下移動の充実度。</summary>
        [Range(0, 100)] public int Vertical = 50;
        /// <summary>玄関・窓・周辺環境を含む脱走発生リスク。</summary>
        [Range(0, 100)] public int EscapeRisk = 50;
    }

    /// <summary>猫の性格タグと人間の生活タグを共通形式で扱う最小マスター。</summary>
    [CreateAssetMenu(menuName = "NNN/Trait")]
    public sealed class TraitDefinition : IdDefinition { }

    /// <summary>案件で解消・変化させたい依頼人側の課題。</summary>
    [CreateAssetMenu(menuName = "NNN/Problem")]
    public sealed class ProblemDefinition : IdDefinition
    {
        /// <summary>課題の内容と、猫との生活で期待する変化の説明。</summary>
        [TextArea] public string Description;
        /// <summary>この課題と特に相性が良い候補役割。将来の係数拡張用。</summary>
        public CandidateRole FavoredRole;
    }

    /// <summary>候補評価に必要な猫一匹分の固定プロフィール。</summary>
    [CreateAssetMenu(menuName = "NNN/Cat")]
    public sealed class CatDefinition : IdDefinition
    {
        /// <summary>猫の年齢。現在は表示用で、将来の健康・活動補正にも利用できる。</summary>
        [Range(0, 20)] public int Age;
        /// <summary>運動量や刺激を求める強さ。</summary>
        [Range(0, 100)] public int Activity;
        /// <summary>人との接触や交流を好む強さ。</summary>
        [Range(0, 100)] public int Sociability;
        /// <summary>単独で落ち着いて過ごせる度合い。</summary>
        [Range(0, 100)] public int Independence;
        /// <summary>新しい人・住居・生活リズムへ馴染む力。</summary>
        [Range(0, 100)] public int Adaptability;
        /// <summary>数値だけでは表しにくい性格・行動特性の一覧。</summary>
        public List<TraitDefinition> Traits = new List<TraitDefinition>();

        /// <summary>指定IDのTraitを持つかをnull安全に判定する。</summary>
        public bool HasTrait(string id)
        {
            return Traits.Exists(t => t != null && t.Id == id);
        }
    }

    /// <summary>CaseGeneratorが作成した、案件ごとの依頼人実体。</summary>
    [Serializable]
    public sealed class GeneratedHuman
    {
        /// <summary>案件画面に表示する依頼人名。</summary>
        public string Name;
        /// <summary>生成時点の依頼人年齢。</summary>
        public int Age;
        /// <summary>生成元となった人物像マスター。</summary>
        public HumanArchetype Archetype;
        /// <summary>案件内で確定した依頼人の生活・接し方タグ。</summary>
        public List<TraitDefinition> Traits = new List<TraitDefinition>();
    }

    /// <summary>候補評価へ渡す、一件分の生成済み案件データ。</summary>
    [Serializable]
    public sealed class GeneratedCase
    {
        /// <summary>生成および候補の決定的揺らぎに使ったSeed。</summary>
        public int Seed;
        /// <summary>この案件の依頼人。</summary>
        public GeneratedHuman Human;
        /// <summary>この案件で採用された生活条件。</summary>
        public LifestyleDefinition Lifestyle;
        /// <summary>この案件で採用された住居条件。</summary>
        public ResidenceDefinition Residence;
        /// <summary>この案件が解決対象とする課題一覧。</summary>
        public List<ProblemDefinition> Problems = new List<ProblemDefinition>();
    }

    /// <summary>ランダム案件から独立して保持する、再現可能な評価ベンチマーク一件。</summary>
    [CreateAssetMenu(menuName = "NNN/Human Benchmark")]
    public sealed class HumanBenchmarkDefinition : IdDefinition
    {
        /// <summary>H01～H10の固定識別子。</summary>
        public string BenchmarkId;
        /// <summary>案件画面に表示する氏名。</summary>
        public string HumanName;
        /// <summary>固定年齢。</summary>
        public int Age;
        /// <summary>人物タグを保持する人物像。</summary>
        public HumanArchetype Archetype;
        /// <summary>固定のLifestyleフラグ。</summary>
        public LifestyleDefinition Lifestyle;
        /// <summary>固定の住居条件。</summary>
        public ResidenceDefinition Residence;
        /// <summary>この人物が抱える固定課題。</summary>
        public List<ProblemDefinition> Problems = new List<ProblemDefinition>();

        /// <summary>ランダム生成器を通さず、定義値だけで評価案件を作る。</summary>
        public GeneratedCase CreateCase(int seed)
        {
            var human = new GeneratedHuman { Name = HumanName, Age = Age, Archetype = Archetype };
            if (Archetype != null) human.Traits.AddRange(Archetype.PreferredTraits);
            var result = new GeneratedCase { Seed = seed, Human = human, Lifestyle = Lifestyle, Residence = Residence };
            result.Problems.AddRange(Problems);
            return result;
        }
    }

    /// <summary>案件生成に使用できる各マスターの集合と生成モードを保持する。</summary>
    [CreateAssetMenu(menuName = "NNN/Case Generation Profile")]
    public sealed class CaseGenerationProfile : ScriptableObject
    {
        /// <summary>trueならSeedにかかわらず田中案件の条件を使用する。</summary>
        public bool UseFixedTanakaCase = true;
        /// <summary>依頼人生成時の候補となる人物像マスター。</summary>
        public List<HumanArchetype> HumanArchetypes = new List<HumanArchetype>();
        /// <summary>案件へ割り当て可能な生活マスター。</summary>
        public List<LifestyleDefinition> Lifestyles = new List<LifestyleDefinition>();
        /// <summary>案件へ割り当て可能な住居マスター。</summary>
        public List<ResidenceDefinition> Residences = new List<ResidenceDefinition>();
        /// <summary>案件へ割り当て可能な問題マスター。</summary>
        public List<ProblemDefinition> Problems = new List<ProblemDefinition>();
        /// <summary>評価・選抜対象となる猫マスター。</summary>
        public List<CatDefinition> Cats = new List<CatDefinition>();
        /// <summary>ランダム案件候補へ混ぜず、デバッグ評価からのみ参照する固定10件。</summary>
        public List<HumanBenchmarkDefinition> HumanBenchmarks = new List<HumanBenchmarkDefinition>();
    }
}
