using System.Collections.Generic;
using static NNN.SatoHachiObservationFactory;

namespace NNN
{
    /// <summary>観察・取得済み情報からのみ選ぶ、佐藤とハチの表示文言。</summary>
    public static class SatoHachiInsightFactory
    {
        public static List<ObservationInsightDefinition> Create()
        {
            var result = new List<ObservationInsightDefinition>();
            var d = Event(result, FirstContact, 100, "この人のそばに、ハチはどこまで近づける？",
                "ハチは自分から佐藤へ近づいた", "佐藤がしゃがんでも、その場を離れなかった");
            Change(d, "NEW 佐藤との接触が始まった");
            d.UpdateLimit = 2;
            d = Event(result, EnterHome, 110, "ハチはこの家を生活場所として使える？",
                "ハチが佐藤宅へ入った", "佐藤は食事・水・トイレ・寝床を準備した");
            Change(d, "NEW 室内へ入った", InsightComparison.CohabitationChanged);
            d.UpdateLimit = 2;
            d = Event(result, Cohabitation, 120, "一緒に暮らし始めたあと、どんな問題が出る？",
                "佐藤は翌朝の世話を準備した", "ハチが家の寝床で丸くなった");
            Change(d, "↑ 滞在から同居へ進んだ", InsightComparison.CohabitationChanged);
            d = Event(result, OutsideRequest, 200, "なぜハチは外へ行きたがる？",
                "休んだあと、ハチは玄関で鳴いた", "佐藤は外を確認したが、ドアを開けなかった");
            Change(d, "NEW 外出要求を確認した");
            d = Event(result, "REL_INDOOR_PLAY", 210, "遊んだあとも、なぜ玄関を気にする？",
                "ハチは室内のおもちゃで遊んだ", "遊び終えたあとも、玄関の方を見た");
            Change(d, "→ 玄関への関心は続いている", InsightComparison.PreviousEvent, OutsideRequest);
            d = Event(result, TowerResponse, 300, "室内が充実しても、なぜ外へ行きたい？",
                "ハチはタワーに登り、窓辺を見渡した", "休んだあとも、玄関のドアを見て鳴いた");
            d.Condition.WorldFlags = new[] { Tower, VerticalRoute };
            Change(d, "↑ 室内の探索場所が増えた", InsightComparison.AfterOperation, InstallTower, new[] { Tower, VerticalRoute });
            Change(d, "→ 外出要求は続いている", InsightComparison.PreviousEvent, "NORMAL_OUTSIDE_REQUEST");
            d = Event(result, SafetySearch, 400, "佐藤と一緒なら、外へ行ける方法はある？",
                "佐藤はハーネスとキャリーのサイズを調べた");
            Change(d, "NEW 佐藤が外出道具を検討している");
            d = Event(result, FirstHarness, 500, "嫌なのか、まだ慣れていないだけか？",
                "装着直後、ハチは動きを止めた", "その後、低い姿勢で数歩進んだ");
            Change(d, "NEW ハーネスを初めて装着した");
            // 強い逃走の有無は調査結果の情報。装着ログだけでは追加しない。
            d = Event(result, HarnessSteps, 600, "ハーネスでの歩き方は、これからどう変わる？",
                "ハチは数歩進み、佐藤が待つとさらに一歩進んだ");
            Change(d, "↑ 装着中に歩く様子が変わった", InsightComparison.PreviousEvent, FirstHarness);
            result.Add(new ObservationInsightDefinition { Id = "HARNESS_ROUTE_QUESTION", Priority = 601,
                CurrentQuestion = "この状態で、安全に商店街まで行ける？",
                Condition = new InsightCondition { TodayEvents = new[] { HarnessSteps }, Knowledge = new[] { FamiliarStreets } } });
            d = Event(result, ShortTripObservation, 1000, "商店街へ行けても、同じように過ごせないならどうする？",
                "キャリーとハーネスで商店街へ行けた", "ハチは店の間の細道へ向かった", "佐藤はその先へ付いていけなかった");
            Change(d, "NEW 商店街への再訪は成立した");
            Change(d, "NEW 人間と猫が通れる範囲の違いを確認した");

            // 通常イベントの要約も実際に表示したEventIdに限定する。
            Event(result, "NORMAL_HOME_USE", 30, null, "ハチは食事・水・トイレ・寝床を利用した");
            Event(result, "NORMAL_HOME_REST", 20, null, "ハチは家の寝床を利用している");
            Event(result, "NORMAL_VERTICAL_PATROL", 35, null, "ハチは室内の巡回路を歩いた");
            d = Event(result, "NORMAL_USE_TOWER", 34, null, "ハチがタワーの上段を使った");
            Change(d, "↑ 高い場所を使うようになった", InsightComparison.NewWorldFlag, Tower, new[] { Tower });
            d = Event(result, "NORMAL_OUTSIDE_REQUEST", 40, null, "ハチは玄関でドアを見て鳴いた");
            Change(d, "→ 外出要求は続いている", InsightComparison.PreviousEvent, "NORMAL_OUTSIDE_REQUEST");
            Event(result, "NORMAL_PLAY_READY", 32, null, "ハチは用意されたおもちゃで遊んだ");
            Event(result, "NORMAL_WINDOW_PERCH", 31, null, "ハチは閉じた窓の脇で過ごした");
            Event(result, "NORMAL_WATCH_HUMAN", 10, null, "ハチが佐藤の手元に目を向けた");
            Event(result, "NORMAL_GROOM", 9, null, "ハチは前足の毛づくろいをした");
            Event(result, "NORMAL_HUMAN_PAUSE", 8, null, "佐藤が手を止め、ハチを見た");
            Event(result, "NORMAL_LOOK_WINDOW", 15, null, "ハチが高い場所から外を眺めた");
            Event(result, "NORMAL_MORNING_WALK", 10, null, "ハチが棚の脇から窓辺へ移動した");
            Event(result, "NORMAL_MORNING_LOOK", 9, null, "ハチが窓の外に目を向けた");
            Event(result, "NORMAL_MORNING_GROOM", 8, null, "ハチは前足の毛づくろいをした");
            Event(result, "NORMAL_CHECK_OUTDOOR_GEAR", 15, null, "佐藤がハチと外出用品を確認した");

            // 経路調査後にだけ成立。DAY9のReviewには出ず、調査結果は既存Result UIが担当する。
            var routeKnown = new InsightCondition { Knowledge = new[] { Walkable, BusyRoad, CatOnlyPaths } };
            d = new ObservationInsightDefinition { Id = "KNOWN_ROUTE", Priority = 650, Condition = routeKnown,
                CurrentQuestion = "安全に商店街まで行く方法は作れる？" };
            d.Updates.Add(new InsightEntry { Id = "ROUTE_FACT", Text = "商店街までの経路上の問題も分かっている", Condition = routeKnown });
            d.Changes.Add(new InsightEntry { Id = "ROUTE_NEW", Text = "NEW 商店街までの移動条件が分かった",
                Condition = new InsightCondition { Knowledge = new[] { Walkable, BusyRoad, CatOnlyPaths }, Comparison = InsightComparison.NewKnowledge, CompareId = BusyRoad } });
            result.Add(d);
            var ready = new InsightCondition { Knowledge = new[] { Walkable, BusyRoad, CatOnlyPaths, HarnessUncertain, WantsOutside },
                WorldFlags = new[] { Harness, Carrier } };
            d = new ObservationInsightDefinition { Id = "TRIP_READY", Priority = 660, Condition = ready, UpdateLimit = 2,
                CurrentQuestion = "安全な移動方法なら、商店街再訪は成立する？" };
            d.Updates.Add(new InsightEntry { Id = "TRIP_READY_FACT", Text = "安全な外出に必要な道具が揃った", Condition = ready });
            result.Add(d);
            return result;
        }

        private static ObservationInsightDefinition Event(List<ObservationInsightDefinition> list, string eventId, int priority, string question, params string[] updates)
        {
            var d = new ObservationInsightDefinition { Id = "INSIGHT_" + eventId, Priority = priority,
                CurrentQuestion = question, Condition = new InsightCondition { TodayEvents = new[] { eventId } } };
            for (int i = 0; i < updates.Length; i++)
                d.Updates.Add(new InsightEntry { Id = eventId + "/" + i, Text = updates[i], Condition = d.Condition });
            list.Add(d); return d;
        }
        private static void Change(ObservationInsightDefinition d, string text, InsightComparison comparison = InsightComparison.None,
            string compareId = null, string[] world = null)
            => d.Changes.Add(new InsightEntry { Id = d.Id + "/CHANGE/" + d.Changes.Count, Text = text,
                Condition = new InsightCondition { TodayEvents = d.Condition.TodayEvents, WorldFlags = world ?? d.Condition.WorldFlags,
                    Comparison = comparison, CompareId = compareId } });
    }
}
