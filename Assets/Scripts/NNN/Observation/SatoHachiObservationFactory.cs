using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NNN
{
    /// <summary>佐藤美咲×ハチ。分岐は既存のイベント条件・工作効果に定義し、Simulatorへ猫固有判定を持ち込まない。</summary>
    public static class SatoHachiObservationFactory
    {
        public const string WantsOutside = "KNOW_CAT_WANTS_OUTSIDE";
        public const string IndoorOpportunity = "KNOW_INDOOR_VERTICAL_OPPORTUNITY";
        public const string FamiliarStreets = "KNOW_CAT_FAMILIAR_SHOPPING_STREETS";
        public const string HarnessUncertain = "KNOW_HARNESS_ADAPTATION_UNCERTAIN";
        public const string Walkable = "KNOW_ROUTE_WALKABLE";
        public const string BusyRoad = "KNOW_ROUTE_HAS_BUSY_ROAD";
        public const string CatOnlyPaths = "KNOW_SHOPPING_STREET_HAS_CAT_ONLY_PATHS";
        public const string Tower = "CAT_TOWER_INSTALLED";
        public const string VerticalRoute = "INDOOR_VERTICAL_ROUTE_AVAILABLE";
        public const string Harness = "HARNESS_OWNED";
        public const string Carrier = "CARRIER_OWNED";
        public const string TripPrepared = "SHOPPING_STREET_SHORT_TRIP_PREPARED";
        public const string PlayPrepared = "INDOOR_PLAY_PREPARED";
        public const string WindowPrepared = "SAFE_WINDOW_PERCH_AVAILABLE";
        public const string InvestigateIndoor = "INVESTIGATE_INDOOR_ACTIVITY";
        public const string InvestigatePast = "INVESTIGATE_FORMER_LIVING_AREA";
        public const string InvestigateHarness = "INVESTIGATE_HARNESS_RESPONSE";
        public const string InvestigateRoute = "INVESTIGATE_SHOPPING_STREET_ROUTE";
        public const string InstallTower = "OP_INSTALL_INDOOR_VERTICAL_ROUTE";
        public const string PrepareGear = "OP_PREPARE_SAFE_OUTDOOR_GEAR";
        public const string ShortTrip = "OP_SHOPPING_STREET_SHORT_TRIP";
        public const string ArrangePlay = "OP_PREPARE_INDOOR_PLAY";
        public const string PrepareWindow = "OP_PREPARE_SAFE_WINDOW_PERCH";
        public const string FirstContact = "VISIT_FIRST_CONTACT";
        public const string EnterHome = "REL_ENTER_HOME";
        public const string Cohabitation = "REL_START_COHABITATION";
        public const string OutsideRequest = "REL_OUTSIDE_REQUEST";
        public const string TowerResponse = "REL_USE_TOWER_AND_RETURN_TO_DOOR";
        public const string SafetySearch = "REL_SEARCH_SAFE_OUTDOOR_METHOD";
        public const string FirstHarness = "REL_FIRST_HARNESS";
        public const string HarnessSteps = "REL_HARNESS_FEW_STEPS";
        public const string ShortTripObservation = "REL_SHOPPING_STREET_SHORT_TRIP";
        public const string TripPartiallyWorks = "KNOW_SHORT_TRIP_PARTIALLY_WORKS";
        public const string RouteMismatch = "KNOW_SHOPPING_STREET_ROUTE_MISMATCH";

        public static ObservationRouteDefinition CreateRoute()
        {
            var route = ScriptableObject.CreateInstance<ObservationRouteDefinition>();
            route.name = "SatoHachiDay1To11";
            route.Human = NNNTestDataFactory.CreateRuntimeProfile().HumanBenchmarks.First(x => x.BenchmarkId == "H02");
            route.Cat = ScriptableObject.CreateInstance<CatDefinition>();
            route.Cat.Id = "CAT_HACHI"; route.Cat.name = "ハチ"; route.Cat.DisplayName = "ハチ";
            route.Cat.Age = 5; route.Cat.Activity = 68; route.Cat.Sociability = 76;
            route.Cat.Independence = 65; route.Cat.Adaptability = 58;
            route.Actions = Actions();
            AddEvents(route.Events);
            AddShortTripDays(route.Events);
            AddReports(route.CatReports);
            return route;
        }

        private static List<NNNActionDefinition> Actions()
        {
            return new List<NNNActionDefinition>
            {
                new NNNActionDefinition(InvestigateIndoor, "室内での過ごし方を調べる", "休息と活動、玄関へ向かうタイミングを調べる。", NNNActionKind.Investigation,
                    addedKnowledgeTags: new[] { WantsOutside, IndoorOpportunity },
                    resultText: "食事・水・寝床・トイレは利用している。休んだあとも玄関へ向かう。高低差を使える場所は少ない。活動や探索の不足、以前の生活圏への関心が考えられ、まだ理由は絞れない。",
                    discoveryConditions: new[] { Happened(OutsideRequest) }),
                new NNNActionDefinition(InvestigatePast, "以前の生活圏を調べる", "派遣前の足取りと、繰り返し立ち寄った場所を確認する。", NNNActionKind.Investigation,
                    addedKnowledgeTags: new[] { FamiliarStreets }, resultText: "以前は商店街の軒下や店の間を繰り返し通っていた。室内の寝床にも戻って休む。家を避けたいだけとは言い切れず、慣れた場所への関心も残る。",
                    discoveryConditions: new[] { Day(6), Happened(OutsideRequest) }),
                new NNNActionDefinition(InvestigateHarness, "装着時の反応を調べる", "止まり方と身体の動かし方を観察記録から確認する。", NNNActionKind.Investigation,
                    addedKnowledgeTags: new[] { HarnessUncertain }, resultText: "強い逃避反応は見られない。身体を拘束される違和感が強そうだが、嫌悪や適応可能性はまだ判断できない。",
                    discoveryConditions: new[] { Happened(FirstHarness) }),
                new NNNActionDefinition(InvestigateRoute, "佐藤宅から商店街までの環境", "道幅、交通量、現地での移動経路を調べる。", NNNActionKind.Investigation,
                    requiredKnowledgeTags: new[] { FamiliarStreets }, discoveryKnowledgeTags: new[] { FamiliarStreets },
                    addedKnowledgeTags: new[] { Walkable, BusyRoad, CatOnlyPaths },
                    resultText: "距離は約600mで徒歩圏。住宅街→生活道路→大通り→商店街と続く。大通りはキャリー移動が必要。店の間には猫しか通れない細道もあり、現地でも自由移動は難しい。全行程のハーネス徒歩は安定しない。",
                    discoveryConditions: new[] { Day(9) }),
                new NNNActionDefinition(InstallTower, "タワーと室内巡回路を整える", "登れる場所と窓辺までの高低差を用意する。", NNNActionKind.Operation,
                    new[] { WantsOutside, IndoorOpportunity }, new[] { IndoorOpportunity },
                    effect: new OperationEffect(new[] { Tower, VerticalRoute })),
                new NNNActionDefinition(ArrangePlay, "室内で遊ぶ準備をする", "短い遊びのためにおもちゃを取り出しやすくする。", NNNActionKind.Operation,
                    new[] { IndoorOpportunity }, new[] { IndoorOpportunity }, effect: new OperationEffect(new[] { PlayPrepared })),
                new NNNActionDefinition(PrepareWindow, "安全な窓辺の居場所を整える", "閉じた窓のそばに、座って眺める場所を作る。", NNNActionKind.Operation,
                    new[] { IndoorOpportunity }, new[] { IndoorOpportunity }, effect: new OperationEffect(new[] { WindowPrepared })),
                new NNNActionDefinition(PrepareGear, "安全な外出の道具を揃える", "サイズの合うハーネスと、道路区間で使うキャリーを準備する。", NNNActionKind.Operation,
                    new[] { WantsOutside, FamiliarStreets }, new[] { FamiliarStreets }, effect: new OperationEffect(new[] { Harness, Carrier }),
                    conditions: new[] { Happened(SafetySearch) }),
                new NNNActionDefinition(ShortTrip, "商店街ショートトリップ", "佐藤宅から危険な道路区間はキャリーで移動し、商店街でハーネスを使う短時間の外出を準備する。", NNNActionKind.Operation,
                    new[] { Walkable, BusyRoad, CatOnlyPaths, WantsOutside, HarnessUncertain }, new[] { Walkable, BusyRoad },
                    effect: new OperationEffect(new[] { TripPrepared }),
                    conditions: new[] { World(Carrier, "キャリー所持"), World(Harness, "ハーネス所持") }),
                NNNActionCatalog.Find(NNNActionCatalog.Skip)
            };
        }

        private static void AddEvents(List<ObservationEventDefinition> events)
        {
            var contact = Major(FirstContact, 1, 1, ObservationEventCategory.Milestone,
                Change(null, HumanAcceptanceState.Tolerating, CatAdaptationState.Unfamiliar, CatWarinessState.Medium, HumanToCatState.Approach),
                new ObservationEventCondition[0],
                Log(17, ObservationActor.Cat, "CAT_APPROACH", "ハチが佐藤へ近づき、差し出されていない手の匂いを嗅ぐ。"),
                Log(17.1f, ObservationActor.Human, "HUMAN_CROUCH", "佐藤が足を止めてしゃがむ。ハチはその場に残る。"));
            contact.AddHistoryFlags.AddRange(new[] { RelationshipHistoryFlag.Seen, RelationshipHistoryFlag.Approached, RelationshipHistoryFlag.SniffedHuman });
            events.Add(contact);
            var entry = Major(EnterHome, 2, 2, ObservationEventCategory.Milestone,
                Change(CohabitationState.Visiting, HumanAcceptanceState.Welcoming, CatAdaptationState.Unfamiliar, CatWarinessState.Medium, HumanToCatState.Care),
                new[] { Happened(FirstContact) },
                Log(18, ObservationActor.Cat, "CAT_WALK", "開いた玄関からハチが入り、壁沿いを歩く。"),
                Log(18.1f, ObservationActor.Human, "HUMAN_PLACE", "佐藤が水と食器、トイレ、寝床を離して置き、窓と危険な隙間を確かめる。"));
            entry.StateChange.AddHomePreparation = HomePreparation.All;
            entry.AddHistoryFlags.Add(RelationshipHistoryFlag.EnteredHome); events.Add(entry);
            var living = Major(Cohabitation, 3, 3, ObservationEventCategory.Milestone,
                Change(CohabitationState.LivingTogether, null, CatAdaptationState.Exploring, null, null), new[] { Happened(EnterHome) },
                Log(20, ObservationActor.Human, "HUMAN_PLACE", "佐藤が翌朝の食事を用意し、ハチの寝床の脇を空ける。"),
                Log(20.1f, ObservationActor.Cat, "CAT_REST", "ハチが寝床で丸くなる。玄関で音がすると顔を上げる。"));
            living.AddHistoryFlags.Add(RelationshipHistoryFlag.CohabitationStarted); events.Add(living);
            events.Add(Major(OutsideRequest, 4, 4, ObservationEventCategory.Relationship,
                Change(null, null, CatAdaptationState.Settling, null, null), new[] { Happened(Cohabitation) },
                Log(18, ObservationActor.Cat, "CAT_MEOW", "寝床から起きたハチが玄関へ行き、ドアを見て鳴く。"),
                Log(18.1f, ObservationActor.Human, "HUMAN_DOOR", "佐藤がドアへ手をかけ、外の車を見て閉めたまま手を戻す。")));
            events.Add(Major("REL_INDOOR_PLAY", 5, 5, ObservationEventCategory.Relationship, new RelationshipStateChange(), new[] { Happened(OutsideRequest) },
                Log(19, ObservationActor.Human, "HUMAN_HOLD", "佐藤が紐のおもちゃを小さく動かす。"),
                Log(19.1f, ObservationActor.Cat, "CAT_PAW", "ハチがおもちゃを前足で追う。遊び終えると玄関の方を見る。")));
            events.Add(Major(TowerResponse, 6, 9, ObservationEventCategory.Milestone, new RelationshipStateChange(), new[] { World(Tower), World(VerticalRoute) },
                Log(16, ObservationActor.Cat, "CAT_JUMP", "ハチがタワーへ登り、窓辺を見渡す。"),
                Log(18, ObservationActor.Cat, "CAT_MEOW", "しばらく休んだあと、ハチが玄関のドアを見て鳴く。")));
            // 遅いタワー設置と競合しても、道具の準備に必要な観察を期限切れにしない。
            var safety = Major(SafetySearch, 7, 9, ObservationEventCategory.Milestone, new RelationshipStateChange(), new[] { Happened(OutsideRequest) },
                Log(20, ObservationActor.Human, "HUMAN_PHONE", "佐藤のスマホに猫用ハーネスとキャリーの商品が並ぶ。サイズ表を開き、ハチの身体を見る。"));
            safety.BasePriority = 200; events.Add(safety);
            events.Add(Major(FirstHarness, 8, 9, ObservationEventCategory.Milestone, new RelationshipStateChange(), new[] { World(Harness), Happened(SafetySearch) },
                Log(17, ObservationActor.Human, "HUMAN_HOLD", "佐藤が室内でハーネスを装着し、リードを緩めて待つ。"),
                Log(17.1f, ObservationActor.Cat, "HARNESS_FREEZE", "ハチが足を止める。少し待って、身体を低くして数歩進む。"),
                Log(17.2f, ObservationActor.Cat, "HARNESS_LOW_WALK", "腹を低くしたまま寝床へ向かう。佐藤がハーネスを外す。")));
            events.Add(Major(HarnessSteps, 9, 9, ObservationEventCategory.Relationship, new RelationshipStateChange(), new[] { Happened(FirstHarness), World(Harness) },
                Log(17, ObservationActor.Human, "HUMAN_CROUCH", "佐藤がハーネスをつけたハチの前でしゃがみ、リードを緩める。"),
                Log(17.1f, ObservationActor.Cat, "CAT_WALK", "ハチが数歩進んで止まる。佐藤が待つと、さらに一歩進む。")));

            events.Add(Normal("NORMAL_WATCH_HUMAN", 1, 9, 55, Log(8, ObservationActor.Cat, "CAT_LOOK", "ハチが佐藤の手元を見る。")));
            events.Add(Normal("NORMAL_GROOM", 1, 9, 55, Log(12, ObservationActor.Cat, "CAT_GROOM", "ハチが前足を舐める。")));
            events.Add(Normal("NORMAL_HUMAN_PAUSE", 1, 9, 50, Log(13, ObservationActor.Human, "HUMAN_LOOK", "佐藤が手を止め、ハチのいる方を見る。")));
            events.Add(Normal("NORMAL_HOME_USE", 3, 4, 80, Log(9, ObservationActor.Cat, "CAT_EAT", "食事を終えたハチが水を飲む。トイレを使い、寝床へ戻る。"), Happened(EnterHome)));
            events.Add(Normal("NORMAL_HOME_REST", 4, 9, 60, Log(14, ObservationActor.Cat, "CAT_REST", "ハチが家の寝床へ戻り、横になる。"), Happened(Cohabitation)));
            events.Add(Normal("NORMAL_OUTSIDE_REQUEST", 5, 9, 80, Log(18.5f, ObservationActor.Cat, "CAT_MEOW", "ハチが玄関へ歩き、ドアを見て短く鳴く。"), Happened(OutsideRequest)));
            events.Add(Normal("NORMAL_USE_TOWER", 4, 9, 95, Log(10, ObservationActor.Cat, "CAT_JUMP", "ハチがタワーの上段へ登る。"), World(Tower)));
            events.Add(Normal("NORMAL_VERTICAL_PATROL", 4, 9, 90, Log(11, ObservationActor.Cat, "CAT_WALK", "ハチが棚から窓辺までの巡回路を歩く。"), World(VerticalRoute)));
            events.Add(Normal("NORMAL_LOOK_WINDOW", 4, 9, 85, Log(15, ObservationActor.Cat, "CAT_LOOK", "高い場所でハチが外を眺める。"), World(Tower)));
            events.Add(Normal("NORMAL_PLAY_READY", 4, 9, 80, Log(19.5f, ObservationActor.Cat, "CAT_PAW", "ハチが床のおもちゃを前足で転がす。"), World(PlayPrepared)));
            events.Add(Normal("NORMAL_WINDOW_PERCH", 4, 9, 80, Log(15.5f, ObservationActor.Cat, "CAT_SIT", "ハチが閉じた窓の脇に座る。"), World(WindowPrepared)));
        }

        private static void AddReports(List<CatReportDefinition> reports)
        {
            Report(reports, FirstContact, "そばまで行った。", 100);
            Report(reports, EnterHome, "中、見てきた。", 100);
            Report(reports, Cohabitation, "ここで寝る。", 100);
            Report(reports, OutsideRequest, "外、行きたい。", 100);
            Report(reports, "REL_INDOOR_PLAY", "今日は遊んだ。", 100);
            Report(reports, TowerResponse, "高いところ、いい。", 100);
            Report(reports, "NORMAL_USE_TOWER", "上で休んだ。", 50);
            Report(reports, "NORMAL_OUTSIDE_REQUEST", "外、まだ行きたい。", 40);
            Report(reports, FirstHarness, "あれ、歩きにくい。", 110);
            Report(reports, HarnessSteps, "今日は、あれでも歩けた。", 110);
            reports.Add(new CatReportDefinition { Id = "REPORT_OUTSIDE_PENDING", Text = "外、行きたい。", Priority = 120,
                Conditions = new List<ObservationEventCondition> { Day(10), Happened(OutsideRequest) } });
            Report(reports, ShortTripObservation, "あそこ、行けなかった。", 200);
        }

        private static void AddShortTripDays(List<ObservationEventDefinition> events)
        {
            foreach (var normal in events.Where(x => x.Category == ObservationEventCategory.Normal))
            {
                if (normal.LatestDay == 9) normal.LatestDay = 11;
                // 外出時間に重なる室内描写だけを抑える。帰宅後の外出要求や休息は候補に残す。
                if (normal.Logs.Any(log => log.Time >= 10f && log.Time <= 12.1f))
                    normal.Conditions.Add(new ObservationEventCondition { Type = ObservationConditionType.MissingWorldFlag, StringValue = TripPrepared });
            }
            events.Add(Normal("NORMAL_MORNING_WALK", 10, 11, 55,
                Log(7, ObservationActor.Cat, "CAT_WALK", "ハチが棚の脇から窓辺へ歩く。")));
            events.Add(Normal("NORMAL_MORNING_LOOK", 10, 11, 55,
                Log(7.5f, ObservationActor.Cat, "CAT_LOOK", "ハチが窓の外を見る。")));
            events.Add(Normal("NORMAL_CHECK_OUTDOOR_GEAR", 10, 11, 55,
                Log(8, ObservationActor.Human, "HUMAN_LOOK", "佐藤がハチと外出用品を見る。"), World(Carrier), World(Harness)));
            // 道具がないSKIP経路でも、最低限の通常観察を確保する。
            events.Add(Normal("NORMAL_MORNING_GROOM", 10, 11, 50,
                Log(8.2f, ObservationActor.Cat, "CAT_GROOM", "ハチが前足を舐める。")));

            var trip = Major(ShortTripObservation, 11, 11, ObservationEventCategory.Problem, new RelationshipStateChange(),
                new[] { World(TripPrepared), World(Carrier), World(Harness),
                    new ObservationEventCondition { Type = ObservationConditionType.CohabitationAtLeast, Cohabitation = CohabitationState.LivingTogether },
                    new ObservationEventCondition { Type = ObservationConditionType.HasKnowledgeTag, StringValue = BusyRoad } },
                SceneLog("DEPARTURE", 10, ObservationActor.Cat, "CAT_WALK", "ハチが佐藤宅でキャリーへ入る。佐藤が扉を閉めて確かめる。"),
                SceneLog("DEPARTURE", 10.1f, ObservationActor.Human, "HUMAN_HOLD", "佐藤がキャリーを持ち、生活道路と大通りを渡る。ハチは中にいる。"),
                SceneLog("ARRIVAL", 10.5f, ObservationActor.Human, "HUMAN_PLACE", "商店街の車が入らない場所で、佐藤がキャリーを置く。"),
                SceneLog("ARRIVAL", 10.6f, ObservationActor.Human, "HUMAN_HOLD", "佐藤がキャリーの中でハーネスを装着し、リードをつないでからハチを外へ出す。"),
                SceneLog("ARRIVAL", 10.7f, ObservationActor.Cat, "CAT_WALK", "ハチが店先の匂いを嗅ぎ、歩き始める。"),
                SceneLog("MISMATCH", 11, ObservationActor.Cat, "CAT_WALK", "ハチが迷わず店と店の間の細い隙間へ向かう。"),
                SceneLog("MISMATCH", 11.1f, ObservationActor.Human, "HUMAN_STOP", "佐藤の肩は隙間を通らない。佐藤がその手前で止まり、ハチを追って進めない。"),
                SceneLog("MISMATCH", 11.2f, ObservationActor.Cat, "CAT_MEOW", "リードの先でハチが止まる。隙間の奥を見て短く鳴く。"),
                SceneLog("RETURN", 11.5f, ObservationActor.Human, "HUMAN_PLACE", "佐藤がキャリーを開けて待つ。ハチが戻って入り、佐藤が扉を閉める。"),
                SceneLog("RETURN", 12, ObservationActor.Human, "HUMAN_HOLD", "佐藤がキャリーを持って道路区間を戻り、家の中で扉を開ける。"),
                SceneLog("RETURN", 12.1f, ObservationActor.Cat, "CAT_REST", "ハチが家の寝床で横になる。しばらくして玄関の方へ顔を向ける。"));
            trip.AddKnowledgeTags.AddRange(new[] { TripPartiallyWorks, RouteMismatch });
            events.Add(trip);
        }

        private static ObservationLogTemplate SceneLog(string scene, float time, ObservationActor actor, string action, string text)
        { var log = Log(time, actor, action, text); log.SceneId = scene; return log; }
        private static void Report(List<CatReportDefinition> reports, string today, string text, int priority)
            => reports.Add(new CatReportDefinition { Id = "REPORT_" + today, RequiredTodayEventId = today, Text = text, Priority = priority });
        private static ObservationEventDefinition Major(string id, int first, int last, ObservationEventCategory category,
            RelationshipStateChange change, ObservationEventCondition[] conditions, params ObservationLogTemplate[] logs)
        {
            var e = ScriptableObject.CreateInstance<ObservationEventDefinition>();
            e.Id = id; e.name = id; e.Category = category; e.Role = ObservationEventRole.Core;
            e.EarliestDay = first; e.LatestDay = last; e.BasePriority = 100;
            e.StateChange = change; e.Conditions.AddRange(conditions); e.Logs.AddRange(logs); return e;
        }
        private static ObservationEventDefinition Normal(string id, int first, int last, int priority, ObservationLogTemplate log, params ObservationEventCondition[] conditions)
        {
            var e = Major(id, first, last, ObservationEventCategory.Normal, new RelationshipStateChange(), conditions, log);
            e.Role = ObservationEventRole.Optional; e.Repeatable = true; e.BasePriority = priority; return e;
        }
        private static RelationshipStateChange Change(CohabitationState? living, HumanAcceptanceState? acceptance,
            CatAdaptationState? adaptation, CatWarinessState? wariness, HumanToCatState? human)
            => new RelationshipStateChange { SetCohabitation = living.HasValue, Cohabitation = living.GetValueOrDefault(),
                SetHumanAcceptance = acceptance.HasValue, HumanAcceptance = acceptance.GetValueOrDefault(),
                SetCatAdaptation = adaptation.HasValue, CatAdaptation = adaptation.GetValueOrDefault(),
                SetCatWariness = wariness.HasValue, CatWariness = wariness.GetValueOrDefault(),
                SetHumanToCat = human.HasValue, HumanToCat = human.GetValueOrDefault() };
        private static ObservationLogTemplate Log(float time, ObservationActor actor, string action, string text)
            => new ObservationLogTemplate { Time = time, Actor = actor, ActionId = action, Text = text };
        private static ObservationEventCondition Happened(string id)
            => new ObservationEventCondition { Type = ObservationConditionType.EventOccurred, StringValue = id, Description = "観察: " + id };
        private static ObservationEventCondition World(string flag, string label = null)
            => new ObservationEventCondition { Type = ObservationConditionType.HasWorldFlag, StringValue = flag, Description = label ?? flag };
        private static ObservationEventCondition Day(int day)
            => new ObservationEventCondition { Type = ObservationConditionType.DayAtLeast, IntValue = day, Description = "DAY " + day + "以降" };
    }
}
