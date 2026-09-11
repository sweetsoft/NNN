# NNN 状態定義 v1

同居の事実、世話を受け入れる姿勢、住居の準備、生活への適応を分離する。
`RelationshipState` は互換性のため名前を残すが、単一の好感度ではない。

| 軸 | 状態 | 意味 |
|---|---|---|
| Cohabitation | Outside / Visiting / LivingTogether | 未入室 / 一時訪問 / 寝食の生活拠点 |
| HumanAcceptance | Reluctant / Tolerating / Welcoming / Committed | ためらい / 一時許容 / 生活と世話の受け入れ / 継続する意思 |
| CatAdaptation | Unfamiliar / Exploring / Settling / AtEase | 未知 / 探索 / 日課を覚える / 落ち着いた生活 |
| CatWariness | High / Medium / Low / Relaxed | 人間との距離・接触への警戒のみ |
| HumanToCat | Avoid / Watch / Approach / Care | 行動選択用。受容の代用にしない |

## 住居準備

`HomeReadiness` は `HomePreparation` のフラグ集合。
FoodAndWaterReady（継続的な食事と水）、ToiletReady（排泄場所）、
RestingPlaceReady（退避して休める場所）、BasicSafetyReady（最低限の安全対策）を個別に保持する。
表示用 `ReadinessStage` は0項目ならUnprepared、一部ならPreparing、全項目ならBasicReady。
家具や上下運動などの追加改善は別途拡張する。BasicReadyは理想環境の完成ではない。

## 遷移の責任

- `CanStartCohabitation` は Visiting、Welcoming以上、準備4項目の成立を要求する。
- 条件成立では自動遷移しない。開始イベント実行時にLivingTogetherへ変更する。
- 未知・高警戒のまま同居してよい。適応と警戒は連動させない。
- 準備の欠落や受容の後退で同居を自動解除しない。後続の問題イベントへつなぐ。
- 変更しない軸にはSetフラグを付けない。住居は項目を追加・削除する。
- 調査はPlayerKnowledgeFlags、工作はModifierを変え、生活状態を直接確定しない。
- 履歴と記憶は状態とは別に保持し、後退時にも消さない。
- 現在地や一時的な驚きはこの長期状態に含めない。一時状態・位置管理は今後の実装範囲。

## デモルート

佐藤×スズ×VisitはDAY1接触（Outside）、DAY2初入室と準備（Visiting）、
DAY3の継続した寝食と世話（LivingTogether）をMilestoneイベントで描く。
日数を見て状態を直接書き換える処理はない。履歴と準備条件が必要。
DAY3もUnfamiliar / Highを維持し、後続の探索、設備利用、日課、世話の継続で成長する。
DAY4以降のイベント期間は既存30日ルートを利用しており、デモ全体の再調整は別作業。

## 移行と検証

旧Settlementは削除。屋内経験の条件はCohabitation、生活への定着条件はCatAdaptationへ移行した。
旧条件enumの数値4・5は欠番とし、他の既存条件の数値を保持する。
対象ブランチに保存済みのSettlementを持つイベントasset・セーブデータはない。
外部で作成した旧データの自動移行は提供しない。

`NNNObservationBatchRunner.RunStressTest` は状態単体の検証、介入APIの回帰検証、
1000 Seedの30日ルートを実行する。全SeedでDAY1〜3、同居後の維持、最終適応を検証する。
