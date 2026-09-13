using System.Collections.Generic;
using static NNN.SatoKotaObservationFactory;

namespace NNN
{
    public static class SatoKotaInsightFactory
    {
        public static List<ObservationInsightDefinition> Create()
        {
            var list = new List<ObservationInsightDefinition>();
            var d = Add(list, Contact, "コタはこの人と、どこまで近づける？", "コタは自分から佐藤に近づき、脚に頬を寄せた", "佐藤がしゃがんでも、そばに残った");
            Change(d, "NEW 佐藤との接触が始まった");
            d = Add(list, Entry, "コタはこの家をどう使う？", "コタが佐藤宅へ入り、家具の周りを探索した", "佐藤は食事・水・トイレ・寝床を準備した");
            Change(d, "NEW 室内へ入った", InsightComparison.CohabitationChanged);
            d = Add(list, Living, "一緒に暮らすと、どんなことが起きる？", "佐藤が翌朝の世話を準備した", "コタは家の寝床で休んだ");
            Change(d, "↑ 滞在から同居へ進んだ", InsightComparison.CohabitationChanged);
            d = Add(list, DeskTrouble, "なぜコタは作業中の机へ何度も来る？", "コタは作業机へ何度も近づいた", "机の小物を前足で落とした");
            Change(d, "NEW 同居後の生活トラブルを確認");
            d = Add(list, Night, "遊ぶ時間を増やすと、夜の行動は変わる？", "コタは夜に走り、棚へ登った", "佐藤がおもちゃを動かすと強く反応し、そのあと休んだ");
            Change(d, "NEW 夕方以降に活発な行動を確認");
            d = Add(list, PlayResponse, "コタが机や棚へ登る理由は、遊びだけ？", "佐藤と遊ぶ時間が増えた", "遊んだあとの走り回りは減った", "それでも机には登っている");
            d.Condition.WorldFlags = new[] { PlayRoutine };
            Change(d, "↓ 夜の走り回りが減った", InsightComparison.AfterOperation, CreatePlay);
            Change(d, "→ 机や棚への侵入は残っている", InsightComparison.PreviousEvent, Night);
            d = Add(list, VerticalInterest, "登ってよい場所を増やすと、机への接近は変わる？", "コタは棚に登り、部屋を見渡した", "棚から降りると、佐藤の机へ向かった");
            d = Add(list, VerticalResponse, "高い場所があっても、なぜ佐藤の作業机へ来る？", "コタはタワーと空けた棚、窓辺を使った", "そのあと、佐藤のキーボードの横へ座った");
            d.Condition.WorldFlags = new[] { Tower, VerticalRoute };
            Change(d, "↑ 登ってよい場所を使うようになった", InsightComparison.AfterOperation, CreateVertical);
            Change(d, "→ 佐藤の作業机にも来ている", InsightComparison.PreviousEvent, VerticalInterest);
            d = Add(list, Proximity, "机そのものと、佐藤がいる場所。どちらが気になる？", "コタは作業中の佐藤の近くで休もうとした", "佐藤が移動すると、机を降りてあとを追った");
            d = Add(list, HumanAdaptation, "佐藤のそばに猫用の場所があると、過ごし方は変わる？", "佐藤が壊れやすい小物を収納し、机横を空けた", "コタはそばで見たあと、おもちゃを転がした");
            d.Condition.Knowledge = new[] { ProximityKnowledge };
            Change(d, "NEW 佐藤も生活空間の使い方を変え始めた");
            d = Add(list, SharedSpace, "活発なコタと、この暮らし方をどう続ける？", "コタは机横の猫用スペースで休み、佐藤は作業を続けた", "そのあと、作業中のペンを一本だけ落とした");
            d.Condition.WorldFlags = new[] { DeskSpot, DeskCleared };
            Change(d, "↑ 作業を続けながら、近くで過ごす場面ができた", InsightComparison.AfterOperation, CreateDeskSpot);
            Change(d, "→ 前足で物に触れる行動は残っている");
            // 通常観察のみの日はログを要約する。未実行の工作や未調査の理由は補わない。
            return list;
        }
        private static ObservationInsightDefinition Add(List<ObservationInsightDefinition> list, string id, string question, params string[] facts)
        {
            var d = new ObservationInsightDefinition { Id = "INSIGHT_" + id, Priority = 100, CurrentQuestion = question,
                UpdateLimit = facts.Length, Condition = new InsightCondition { TodayEvents = new[] { id } } };
            for (int i = 0; i < facts.Length; i++) d.Updates.Add(new InsightEntry { Id = id + "/" + i, Text = facts[i], Condition = d.Condition });
            list.Add(d); return d;
        }
        private static void Change(ObservationInsightDefinition d, string text, InsightComparison comparison = InsightComparison.None, string compare = null)
            => d.Changes.Add(new InsightEntry { Id = d.Id + "/CHANGE/" + d.Changes.Count, Text = text,
                Condition = new InsightCondition { TodayEvents = d.Condition.TodayEvents, WorldFlags = d.Condition.WorldFlags,
                    Knowledge = d.Condition.Knowledge, Comparison = comparison, CompareId = compare } });
    }
}
