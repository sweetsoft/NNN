using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>室内の活動・探索・人との距離を調整するデモ。条件評価は既存Simulatorが行う。</summary>
    public static class SatoKotaObservationFactory
    {
        // Traitは猫が元から持つ行動特性。Knowledgeはプレイヤーが調査で知った情報、
        // WorldFlagは工作によって変わった生活環境であり、互いの代用にはしない。
        // Activity等の数値プロフィールは保持し、既存Evaluatorが読めるTraitを条件に使う。
        public const string Activity = "HIGH_ACTIVITY", Curiosity = "HIGH_CURIOSITY", PlayDrive = "HIGH_PLAY_DRIVE",
            Exploration = "HIGH_EXPLORATION", Affinity = "HUMAN_AFFINITY", Indoor = "INDOOR_ORIENTED";
        public const string EveningKnowledge = "KNOW_KOTA_HIGH_EVENING_ACTIVITY", PlayKnowledge = "KNOW_KOTA_HIGH_PLAY_DRIVE",
            VerticalKnowledge = "KNOW_KOTA_VERTICAL_PREFERENCE", ExplorationKnowledge = "KNOW_KOTA_EXPLORATION_DRIVE",
            ProximityKnowledge = "KNOW_KOTA_WANTS_HUMAN_PROXIMITY";
        public const string PlayRoutine = "EVENING_PLAY_ROUTINE_AVAILABLE", VerticalRoute = "ALLOWED_VERTICAL_ROUTE_AVAILABLE",
            Tower = "CAT_TOWER_INSTALLED", DeskSpot = "DESK_SIDE_CAT_SPOT_AVAILABLE", DeskCleared = "FRAGILE_DESK_OBJECTS_STORED";
        public const string InvestigateActivity = "INVESTIGATE_ACTIVITY_PATTERN", InvestigateVertical = "INVESTIGATE_VERTICAL_EXPLORATION",
            InvestigateProximity = "INVESTIGATE_HUMAN_PROXIMITY", CreatePlay = "OP_CREATE_PLAY_ROUTINE",
            CreateVertical = "OP_CREATE_ALLOWED_VERTICAL_ROUTE", CreateDeskSpot = "OP_CREATE_DESK_SIDE_CAT_SPOT";
        public const string Contact = "KOTA_FIRST_CONTACT", Entry = "KOTA_ENTER_HOME", Living = "KOTA_START_COHABITATION",
            DeskTrouble = "KOTA_DESK_INTERRUPTION", Night = "KOTA_EVENING_PLAY", PlayResponse = "KOTA_PLAY_ROUTINE_RESPONSE",
            VerticalInterest = "KOTA_VERTICAL_EXPLORATION", VerticalResponse = "KOTA_ALLOWED_ROUTE_RESPONSE",
            Proximity = "KOTA_FOLLOW_HUMAN", HumanAdaptation = "KOTA_HUMAN_ADAPT_ENVIRONMENT", SharedSpace = "KOTA_SHARED_DESK_SPACE";
        public const string NightRun = "KOTA_NORMAL_NIGHT_RUN", ShortPlay = "KOTA_NORMAL_SHORT_PLAY",
            DeskVisit = "KOTA_NORMAL_DESK_VISIT", TowerJump = "KOTA_NORMAL_TOWER_JUMP", PawToy = "KOTA_NORMAL_PAW_TOY";
        // デバッグ用の選択例であり、イベントをこの日付に固定するスケジュールではない。
        // 手動でSKIPしたり調査順を変えたりすると、条件の成立日も変わる。
        public static readonly string[] GuidedActions = { "SKIP", "SKIP", "SKIP", InvestigateActivity, CreatePlay,
            InvestigateVertical, CreateVertical, "SKIP", InvestigateProximity, CreateDeskSpot, "SKIP" };

        /// <summary>
        /// 一回のシミュレーションに渡す固定プロフィールを作る。
        /// 好奇心・遊び・探索・親和性を分け、工作で性格そのものを弱めない。
        /// ここで作るScriptableObjectは実行時データで、保存済み猫アセットの編集ではない。
        /// </summary>
        public static CatDefinition CreateCat()
        {
            var cat = ScriptableObject.CreateInstance<CatDefinition>();
            cat.Id = "CAT_KOTA"; cat.name = cat.DisplayName = "コタ"; cat.Age = 2;
            cat.Activity = 88; cat.Sociability = 90; cat.Independence = 50; cat.Adaptability = 85;
            foreach (string id in new[] { Activity, Curiosity, PlayDrive, Exploration, Affinity, Indoor, "WANTS_HUMAN_HOME", "LOW_OUTDOOR_DRIVE" })
            {
                var trait = ScriptableObject.CreateInstance<TraitDefinition>(); trait.Id = trait.name = trait.DisplayName = id; cat.Traits.Add(trait);
            }
            return cat;
        }
        /// <summary>
        /// 既存の佐藤ベンチマークH02とコタ専用コンテンツを組み合わせる。
        /// RouteのAction集合を明示的に置き換えるため、共通カタログやハチの外出工作が混入しない。
        /// 条件判定・候補選択・状態変更の実行は既存Simulatorに任せる。
        /// </summary>
        public static ObservationRouteDefinition CreateRoute()
        {
            var route = ScriptableObject.CreateInstance<ObservationRouteDefinition>(); route.name = "SatoKotaDay1To11";
            route.Human = NNNTestDataFactory.CreateRuntimeProfile().HumanBenchmarks.First(x => x.BenchmarkId == "H02");
            route.Cat = CreateCat(); route.Actions = Actions(); AddEvents(route);
            return route;
        }
        // discoveryは候補の発見、requiredは選択可能になる条件。両者を分けることで
        // 調査が足りない工作をVisibleLockedとして提示できる。
        // OperationEffectは環境だけを指定し、ApplyNNNAction当日ではなく翌朝に反映される。
        // 調査結果の文言とIntentは「仮説を確かめる／条件を試す」に留め、完全解決を保証しない。
        private static List<NNNActionDefinition> Actions() => new List<NNNActionDefinition>
        {
            new NNNActionDefinition(InvestigateActivity, "活動時間・遊び行動を調べる", "活動時間と遊びへの反応を確認する。", NNNActionKind.Investigation,
                addedKnowledgeTags: new[] { EveningKnowledge, PlayKnowledge }, discoveryConditions: new[] { Happened(DeskTrouble) },
                resultText: "夕方～夜に活動量が高い。佐藤の帰宅や作業開始時に接近が増え、遊びへの反応も強い。机に来る理由が遊び不足だけかは、まだ分からない。",
                intentText: "活発になる時間帯と、遊び不足との関係を確かめる。"),
            new NNNActionDefinition(CreatePlay, "帰宅後に遊ぶ時間を作る", "帰宅後におもちゃで遊ぶ時間を確保する。", NNNActionKind.Operation,
                discoveryKnowledgeTags: new[] { EveningKnowledge }, requiredKnowledgeTags: new[] { PlayKnowledge },
                effect: new OperationEffect(new[] { PlayRoutine }), intentText: "しっかり遊ぶと、夜の行動が変わるか試す。"),
            new NNNActionDefinition(InvestigateVertical, "高所・探索行動を調べる", "机や棚と、部屋の巡回を確認する。", NNNActionKind.Investigation,
                discoveryConditions: new[] { Happened(Night) }, addedKnowledgeTags: new[] { VerticalKnowledge, ExplorationKnowledge },
                resultText: "高い場所を好んで使い、部屋を巡回する傾向が強い。人間の作業場所にも興味を示す。高さだけが机へ来る理由とは、まだ言い切れない。",
                intentText: "机や棚への登り方と、高所・探索欲求の関係を確かめる。"),
            new NNNActionDefinition(CreateVertical, "登ってよい高所動線を作る", "タワーと空いた棚、窓辺をつなぐ。", NNNActionKind.Operation,
                discoveryKnowledgeTags: new[] { VerticalKnowledge }, requiredKnowledgeTags: new[] { VerticalKnowledge, ExplorationKnowledge },
                effect: new OperationEffect(new[] { Tower, VerticalRoute }), intentText: "自由に登れる場所を増やすと、空間の使い方が変わるか試す。"),
            new NNNActionDefinition(InvestigateProximity, "佐藤への接近タイミングを調べる", "佐藤の居場所と猫の滞在先を照らし合わせる。", NNNActionKind.Investigation,
                discoveryConditions: new[] { Happened(Proximity) }, addedKnowledgeTags: new[] { ProximityKnowledge },
                resultText: "佐藤がいる場所での滞在が多い。PC作業中も近くで休もうとし、別の場所へ移ると後を追うことがある。佐藤の近くにいたい可能性があるが、「寂しがり」とは断定しない。",
                intentText: "机そのものより、佐藤の近くにいたい可能性を確かめる。"),
            new NNNActionDefinition(CreateDeskSpot, "机のそばに猫用の居場所を作る", "机横にクッションを置き、壊れやすい小物を収納する。", NNNActionKind.Operation,
                discoveryKnowledgeTags: new[] { ProximityKnowledge }, requiredKnowledgeTags: new[] { ProximityKnowledge },
                effect: new OperationEffect(new[] { DeskSpot, DeskCleared }), intentText: "居場所と小物の配置で、過ごし方が変わるか試す。"),
            NNNActionCatalog.Find(NNNActionCatalog.Skip)
        };
        private static void AddEvents(ObservationRouteDefinition r)
        {
            // 導入は Contact → Entry → Living の履歴で直列化する。
            // Simulatorは一日の候補を生成した時点の履歴を使うため、同じ日に連鎖して同居しない。
            var e = Major(r, Contact, "近く、平気。", 100, new[] { Trait(Affinity), Trait(Curiosity) },
                L(17, "CAT_LOOK", "コタが佐藤を見て、自分から歩み寄る。"),
                L(17.1f, "CAT_APPROACH", "コタが佐藤の足元まで近づく。"),
                L(17.2f, "CAT_SNIFF", "コタが佐藤の靴の匂いを嗅ぐ。"),
                L(17.3f, "CAT_RUB", "コタが佐藤の脚に頬をこすりつける。"),
                L(17.4f, "HUMAN_CROUCH", "佐藤がしゃがむ。コタはすぐそばに残る。"));
            e.StateChange = new RelationshipStateChange { SetHumanAcceptance = true, HumanAcceptance = HumanAcceptanceState.Tolerating,
                SetCatWariness = true, CatWariness = CatWarinessState.Low, SetHumanToCat = true, HumanToCat = HumanToCatState.Approach };
            e.AddHistoryFlags.AddRange(new[] { RelationshipHistoryFlag.Seen, RelationshipHistoryFlag.Approached, RelationshipHistoryFlag.SniffedHuman });
            e = Major(r, Entry, "中、面白い。", 100, new[] { Happened(Contact), Trait(Indoor), Trait(Exploration) },
                L(18, "CAT_ENTER_HOME", "コタが開いた玄関から家へ入る。"),
                L(18.1f, "HUMAN_PLACE", "佐藤が食事・水・トイレ・寝床を準備し、危険な隙間を確かめる。"),
                L(18.2f, "CAT_EXPLORE", "コタが部屋へ進み、家具の下や棚の匂いを嗅ぐ。"));
            e.StateChange = new RelationshipStateChange { SetCohabitation = true, Cohabitation = CohabitationState.Visiting,
                SetHumanAcceptance = true, HumanAcceptance = HumanAcceptanceState.Welcoming, SetCatAdaptation = true,
                CatAdaptation = CatAdaptationState.Exploring, AddHomePreparation = HomePreparation.All, SetHumanToCat = true, HumanToCat = HumanToCatState.Care };
            e.AddHistoryFlags.Add(RelationshipHistoryFlag.EnteredHome);
            // 同居の成立だけを変更する。受容Welcoming・適応Exploring・警戒Lowは維持し、
            // 同居したという事実から他の状態軸の最大値を推論しない。
            e = Major(r, Living, "ここ、好き。", 100, new[] { Happened(Entry) },
                L(20, "HUMAN_PLACE", "佐藤が翌朝の食事を準備し、コタの寝床の脇を空ける。"),
                L(20.1f, "CAT_REST", "コタが家の寝床で丸くなる。佐藤が通ると顔を上げる。"));
            e.StateChange = new RelationshipStateChange { SetCohabitation = true, Cohabitation = CohabitationState.LivingTogether };
            e.AddHistoryFlags.Add(RelationshipHistoryFlag.CohabitationStarted);
            Major(r, DeskTrouble, "あそこ、面白い。", 100, new[] { Happened(Living), Trait(Curiosity), Trait(Affinity), Trait(Exploration) },
                L(18, "HUMAN_PC", "佐藤がPCで作業を始める。"), L(18.1f, "CAT_APPROACH", "コタが佐藤の作業机へ近づく。"),
                L(18.2f, "CAT_JUMP", "コタが机へ飛び乗り、キーボードの脇を歩く。"),
                L(18.3f, "CAT_PAW", "コタが前足でペンを押し、床へ落とす。"),
                L(18.4f, "HUMAN_HOLD", "佐藤が手を止め、コタをそっと床へ下ろす。"),
                L(18.5f, "CAT_APPROACH", "しばらくすると、コタはまた机へ近づく。"));
            Major(r, Night, "もっと遊ぶ。", 100, new[] { Happened(DeskTrouble), Trait(Activity), Trait(PlayDrive) },
                L(21, "CAT_RUN", "コタが部屋を往復し、紙袋へ飛び込む。"), L(21.1f, "CAT_JUMP", "紙袋から出たコタが棚へ登る。"),
                L(21.2f, "HUMAN_PLAY", "佐藤がおもちゃを動かす。"), L(21.3f, "CAT_PLAY", "コタが勢いよくおもちゃを追う。"),
                L(21.4f, "CAT_REST", "少し遊んだあと、コタが床に伏せる。"));
            // 遊び工作の観察はNightの後、実際に遊ぶ時間が確保されてから成立する。
            // VerticalInterestより高い優先度で最初の効果を先に見せるが、机へ登る行動は残す。
            Major(r, PlayResponse, "いっぱい走った。", 220, new[] { Happened(Night), World(PlayRoutine), Trait(Activity), Trait(PlayDrive) },
                L(19, "HUMAN_PLAY", "佐藤が帰宅後、前より長くコタと遊ぶ。"), L(19.1f, "CAT_PLAY", "コタが何度もおもちゃへ飛びつく。"),
                L(19.2f, "CAT_RUN", "コタが短く走って、おもちゃを追う。"),
                L(21, "CAT_REST", "遊んだあとの夜、コタは前に走り回った時間を床で休んで過ごす。"),
                L(21.1f, "CAT_JUMP", "休んだあと、コタがまた机へ登る。"));
            // 高所の実験は遊び工作への依存を持たせない。別順序のプレイでも成立可能。
            // 高所が増えたあとも親和性による接近を残し、「高さだけが理由」とは断定しない。
            Major(r, VerticalInterest, "高いところ、好き。", 100, new[] { Happened(Night), Trait(Curiosity), Trait(Exploration) },
                L(17, "CAT_JUMP", "コタが棚の空いた段へ飛び乗る。"), L(17.1f, "CAT_LOOK", "高い位置から部屋を見渡す。"),
                L(17.2f, "CAT_WALK", "棚を降りたあと、コタが佐藤の机へ歩く。"));
            Major(r, VerticalResponse, "ここもいい。", 250, new[] { Happened(VerticalInterest), World(Tower), World(VerticalRoute), Trait(Exploration) },
                L(17, "CAT_JUMP", "コタが新しいタワーへ飛び乗る。"), L(17.1f, "CAT_WALK", "空けた棚から窓辺まで、コタが歩く。"),
                L(18, "HUMAN_PC", "佐藤がPC作業を始める。"), L(18.1f, "CAT_JUMP", "コタがタワーを降り、作業机へ登る。"),
                L(18.2f, "CAT_SIT", "コタがキーボードの横へ座る。"));
            Major(r, Proximity, "近くにいたい。", 200, new[] { Happened(VerticalInterest), Trait(Affinity), Trait(Curiosity) },
                L(18, "HUMAN_PC", "佐藤がPC作業をしている。コタが近くに来る。"),
                L(18.1f, "CAT_SIT", "コタがキーボードの横で休もうとする。"),
                L(18.2f, "HUMAN_LOOK", "佐藤が席を立ち、部屋の反対側へ移る。"),
                L(18.3f, "CAT_APPROACH", "コタが机を降り、佐藤のあとを追う。"));
            // 接近傾向を調べたあと、人間側も配置を変える。これは猫の性格変更ではなく、
            // 観察可能な人間の適応であり、既存Memoryにも履歴を残す。
            e = Major(r, HumanAdaptation, "ここ、いい。", 200, new[] { Happened(Proximity), Knowledge(ProximityKnowledge) },
                L(18, "HUMAN_CLEAR_TABLE", "佐藤が机の壊れやすい小物を収納し、横の椅子を空ける。"),
                L(18.1f, "CAT_LOOK", "コタが机の横から佐藤を見る。"), L(18.2f, "CAT_PAW", "コタがおもちゃを前足で転がす。"));
            e.AddMemories.Add(RelationshipMemory.HumanAdaptedEnvironment);
            // 結末もDAY11判定ではなく、片付けの観察と机横の環境が揃った時に成立する。
            // 収納済みの壊れやすい小物と、作業中のペン一本を区別し、改善とやんちゃさを両立する。
            Major(r, SharedSpace, "ここ、いい。", 300, new[] { Happened(HumanAdaptation), World(DeskSpot), World(DeskCleared), Trait(Affinity), Trait(Curiosity) },
                L(18, "HUMAN_PC", "佐藤がPC作業を始める。壊れやすい小物は収納されている。"),
                L(18.1f, "CAT_APPROACH", "コタが佐藤のそばへ来る。"), L(18.2f, "CAT_JUMP", "今回はキーボードではなく、机横の猫用クッションへ飛び乗る。"),
                L(18.3f, "CAT_LOOK", "コタが猫用スペースから佐藤を見る。"), L(18.4f, "CAT_REST", "コタがしばらく休む。佐藤は作業を続ける。"),
                L(18.5f, "CAT_PAW", "コタが前足を伸ばし、作業中のペンを一本だけ床へ落とす。"),
                L(18.6f, "HUMAN_LOOK", "佐藤がペンを見て、短く息をつく。"));

            Normal(r, "KOTA_NORMAL_LOOK", "そば、見てた。", 40, L(8, "CAT_LOOK", "コタが佐藤の手元を見る。"), Happened(Contact));
            Normal(r, "KOTA_NORMAL_GROOM", "なめた。", 35, L(12, "CAT_GROOM", "コタが前足を舐める。"), Happened(Contact));
            Normal(r, "KOTA_NORMAL_EAT", "食べた。", 40, L(9, "CAT_EAT", "コタがごはんを食べ、水を飲む。"), Happened(Entry));
            Normal(r, "KOTA_NORMAL_REST", "ここで寝た。", 40, L(14, "CAT_REST", "コタが家の寝床で休む。"), Happened(Living));
            // 部分改善を候補条件で表現する。遊ぶ時間ができると激しい夜の往復を外し、
            // 短い遊びを追加する。DeskVisitはDeskSpotだけで切り替え、遊び・タワーでは消さない。
            // TowerJumpやPawToyは環境改善後も候補に残すため、性格が消える終わり方にならない。
            Normal(r, NightRun, "まだ走る。", 90, L(22, "CAT_RUN", "コタが夜の部屋を何度も走り回る。"), Happened(Night), Trait(Activity), Missing(PlayRoutine));
            Normal(r, ShortPlay, "追いかけた。", 75, L(20, "CAT_PLAY", "コタが短くおもちゃを追って遊ぶ。"), Happened(Night), Trait(PlayDrive), World(PlayRoutine));
            Normal(r, DeskVisit, "そばに来た。", 70, L(16, "CAT_JUMP", "コタが佐藤の机へ登り、手元を見る。"), Happened(DeskTrouble), Trait(Affinity), Trait(Curiosity), Missing(DeskSpot));
            Normal(r, TowerJump, "上もいい。", 75, L(10, "CAT_JUMP", "コタがタワーへ勢いよく飛び乗る。"), World(Tower), Trait(Activity), Trait(Exploration));
            Normal(r, PawToy, "転がった。", 60, L(15, "CAT_PAW", "コタが床のおもちゃを前足で転がす。"), Happened(Night), Trait(PlayDrive), Trait(Curiosity));
            Normal(r, "KOTA_NORMAL_NEAR_DESK", "ここ、いい。", 70, L(16, "CAT_SIT", "コタが机横のクッションから佐藤を見ている。"), World(DeskSpot), Trait(Affinity));
        }
        private static ObservationEventDefinition Major(ObservationRouteDefinition route, string id, string report, int priority,
            ObservationEventCondition[] conditions, params ObservationLogTemplate[] logs)
        {
            var e = ScriptableObject.CreateInstance<ObservationEventDefinition>(); e.Id = e.name = id;
            // 生活の変化を一日一場面で見せる。日付指定でなく、履歴と環境が成立した翌観察で選ぶ。
            e.Category = ObservationEventCategory.Milestone; e.Role = ObservationEventRole.Core; e.EarliestDay = 1; e.LatestDay = 30;
            e.BasePriority = priority; e.Conditions.AddRange(conditions); e.Logs.AddRange(logs); route.Events.Add(e);
            route.CatReports.Add(new CatReportDefinition { Id = "REPORT_" + id, RequiredTodayEventId = id, Text = report, Priority = 100 });
            return e;
        }
        /// <summary>共通の登録処理を使い、日常描写だけを反復可能・Optional・低優先度の猫報告へ変更する。</summary>
        private static void Normal(ObservationRouteDefinition r, string id, string report, int priority, ObservationLogTemplate log, params ObservationEventCondition[] conditions)
        {
            var e = Major(r, id, report, priority, conditions, log); e.Category = ObservationEventCategory.Normal; e.Role = ObservationEventRole.Optional; e.Repeatable = true;
            r.CatReports.Last().Priority = 10;
        }
        private static ObservationLogTemplate L(float time, string action, string text) => new ObservationLogTemplate
            { Time = time, Actor = action.StartsWith("HUMAN_") ? ObservationActor.Human : ObservationActor.Cat, ActionId = action, Text = text };
        private static ObservationEventCondition Happened(string id) => new ObservationEventCondition { Type = ObservationConditionType.EventOccurred, StringValue = id };
        private static ObservationEventCondition Trait(string id) => new ObservationEventCondition { Type = ObservationConditionType.CatTrait, StringValue = id };
        private static ObservationEventCondition World(string id) => new ObservationEventCondition { Type = ObservationConditionType.HasWorldFlag, StringValue = id };
        private static ObservationEventCondition Missing(string id) => new ObservationEventCondition { Type = ObservationConditionType.MissingWorldFlag, StringValue = id };
        private static ObservationEventCondition Knowledge(string id) => new ObservationEventCondition { Type = ObservationConditionType.HasKnowledgeTag, StringValue = id };
    }
}
