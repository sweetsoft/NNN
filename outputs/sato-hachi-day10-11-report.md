# SatoHachi DAY10–11 追加報告

対象ブランチ: `feature/sato-hachi-vertical-slice`。
DAY1～9の実装を保持し、DAY10の工作選択からDAY11の反応観察まで追加した。

手動プレイ: Unityの **NNN → Observation → Play Sato Hachi DAY1-11**。
「次の観察シーン」→CAT REPORT→NNN ACTION→NEXT DAYという操作順を維持。DAY11まで進められる。
基準の選択順はDAY4室内調査、DAY5タワー、DAY6以前の生活圏調査、DAY7道具準備、DAY8ハーネス反応調査、DAY9経路調査、DAY10 Short Trip、DAY11 SKIP。

## 1. 今回変更したファイル

- `Assets/Scripts/NNN/Observation/SatoHachiObservationFactory.cs`: DAY10の日常、DAY11 Short Trip、4場面、新Knowledge、CAT REPORT。
- `Assets/Scripts/NNN/Observation/ObservationDefinitions.cs`: イベントからのKnowledge付与、ログのSceneId／EventId、ObservationScene、日次結果からの表示場面抽出。
- `Assets/Scripts/NNN/Observation/ObservationSimulation.cs`: 観察KnowledgeをStateと日次結果へ記録し、表示場面を取得するAPIを追加。
- `Assets/Scripts/NNN/Editor/ObservationSliceWindow.cs`: DAY11まで延長。抽出した場面を一つずつ表示し、CAT REPORTの横で観察Knowledgeを確認可能に。
- `Assets/Scripts/NNN/Editor/SatoHachiVerification.cs`: DAY1～11の32 Seed検証、条件欠落、4場面、未解決状態のテスト。
- `outputs/sato-hachi-verification.md`: 最新のDAY1～11表示ログと集計へ更新。
- `outputs/sato-hachi-implementation.md`: DAY1～9報告の冒頭に今回の拡張への案内を追記。
- `outputs/sato-hachi-day10-11-report.md`: 本報告。

以前のDAY1～9追加時からある作業ツリーの差分も保持。候補猫評価、候補選出、CaseGenerator、スズの物語には変更なし。

## 2. DAY10進行

棚から窓辺へ歩く、外を見る、玄関へ向かう、佐藤が道具を見る、といった通常候補を継続する。新しいMajor Eventは置かない。
CAT REPORTは「外、行きたい。」。
基準ルートでは`OP_SHOPPING_STREET_SHORT_TRIP`を選ぶ。既存のPendingOperationEffectにDAY11適用の準備を予約するだけで、その場ではWorldFlagsも観察結果も変わらない。

## 3. DAY11進行

`REL_SHOPPING_STREET_SHORT_TRIP`はCategory=Problem / Role=Coreの単発イベント。
表示は以下の4場面。CAT REPORTを含めて5場面以内。

1. **出発**: 家でキャリーへ入り、佐藤が扉を確かめる。生活道路・大通りをキャリーで通過。
2. **到着**: 商店街の車が入らない場所にキャリーを置く。現地でハーネスを装着し、リードをつないでから歩き始める。
3. **経路の不一致**: ハチが店と店の間へ向かう。佐藤の肩は通らず、先へ付いていけない。ハチが奥を見て鳴く。
4. **帰宅**: キャリーへ戻り、道路区間を安全に帰る。家の寝床で休むが、玄関へ顔を向ける。

通常イベントの内部件数は維持し、DAY11の表示では4つのMajor場面を優先する。生ログは削除しない。
外出時間と重なる室内描写のみ条件で抑え、帰宅後の外出要求は候補に残す。

## 4. Short Tripイベント条件

すべてAND:

- DAY11（EarliestDay=11 / LatestDay=11）
- `HasWorldFlag(SHOPPING_STREET_SHORT_TRIP_PREPARED)`
- `HasWorldFlag(CARRIER_OWNED)`
- `HasWorldFlag(HARNESS_OWNED)`
- `CohabitationAtLeast(LivingTogether)`
- `HasKnowledgeTag(KNOW_ROUTE_HAS_BUSY_ROAD)`
- イベント未発生

既存DirectorのCore期限保護を利用し、ハチ専用のSimulator／Director分岐は追加していない。基準進行ではDAY11に他のMajor候補が競合せず、32 Seedすべてで発生した。

## 5. 追加Knowledge

- `KNOW_SHORT_TRIP_PARTIALLY_WORKS`: キャリー移動＋現地ハーネスで商店街へ行き、帰宅できた。
- `KNOW_SHOPPING_STREET_ROUTE_MISMATCH`: ハチが使いたい経路には人間が追従できない区間がある。

イベント定義の`AddKnowledgeTags`から観察確定時に取得。既取得情報は重複追加せず、その日に新しく得た分を`DaySimulationResult.AddedKnowledgeTags`へ保存する。
NNN ACTIONを追加で消費しない。KnowledgeをWorldFlagsへ混ぜない。

## 6. 追加WorldFlags

新規WorldFlagsは追加していない。
既存の`SHOPPING_STREET_SHORT_TRIP_PREPARED`を翌朝反映する。実際に訪問した事実は`OccurredEventIds`の`REL_SHOPPING_STREET_SHORT_TRIP`で保持する。
`SHORT_TRIP_SUCCESS`や`OUTDOOR_PROBLEM_SOLVED`などの完了フラグは設けていない。

## 7. CAT REPORT

- DAY10: 「外、行きたい。」
- DAY11のShort Trip後: 「あそこ、行けなかった。」

1日に1件。質問・原因分析・今後の結論は言わせない。旅行していない別選択経路ではTrip用の一言を選ばない。

## 8. 新規Investigation / Operation

追加なし。既存Short Trip工作をそのまま利用する。
安全な商店街内巡回などの次段階の調査や工作は今回実装していない。

## 9. 32 Seed検証結果

Unity 6000.2.2f1でコンパイル成功。
Seed 0～31 × 基準選択／全日SKIP／表示候補から別選択の3方針でDAY1～11を実行（1,056日）。基準選択を再実行し352日分の再現性も確認。すべてPASS。

- 基準進行のDAY11 Short Trip: **32/32**。別Majorに枠を取られたSeedなし。
- DAY10の即時発火なし。DAY11 BeginDayで準備反映・候補化。
- 出発・到着・人間の追従不可・帰宅の4場面と順序を確認。
- 新Knowledge2件、既存Knowledgeの保持、同居・受容・適応・警戒を旅行だけで変更しないことを確認。
- 外出要求候補、外出要求の情報、商店街の過去の行動の情報を保持。
- CAT REPORT一回性、ACTION一日一回、ACTION後のイベント追加なし。
- 準備／キャリー／ハーネス／同居／経路Knowledgeを一つずつ欠くと候補から外れることを確認。
- 全日SKIPでは旅行せず、新Knowledgeも取得しない。
- 表示場面を抽出しても通常イベントの生ログを保持。

ログ: `Logs/SatoHachiVerification.log`。

## 10. DAY1～9回帰

既存の早期同居、独立状態、室内改善と外出要求存続、ハーネスの不確定性、経路調査、KnowledgeとWorld Stateの解禁条件、遅い室内改善の検証を保持してPASS。
調査候補の非表示は最大1日。工作4＋調査3の競合でも3日以内に全候補を提示するテストはPASS。

## 11. SatoSuzu回帰

既存の代表5 Seedバッチ検証と1000 SeedストレステストがPASS。
1000 Seed・30,000日: Passed 1000 / Warnings 0 / Failed 0、ログ順序違反0。
ログ: `Logs/SatoHachiSuzuStress.log`。

## 12. Short Trip後の未解決問題

商店街への移動と帰宅はできる。しかしハチが向かう細い経路は佐藤が通れず、以前の商店街での動きを再現できない。
ハチの外出要求も消えず、佐藤が最適解を見つけたことにもしていない。
安全な巡回範囲、部分的な再訪でどこまで満たされるか、頻度などは未決定。

## 13. DAY12以降へ進む前の設計課題

今回の一往復は既存基盤＋小さな汎用拡張で成立し、追加の状態モデル再設計は不要だった。
実装した汎用拡張は「イベントからの観察Knowledge」と「イベント数から独立した表示場面」のみ。

次に検討する点:

- 反復する旅行では、準備フラグの消費／再準備と訪問履歴をどう表すか。今回は準備フラグを保持し、旅行イベントは単発。
- 遅い調査・工作では固定DAY11を過ぎる。今回のVertical Slice範囲では許容し、製品進行では前提成立からの猶予や期限を検討する。
- 今回の4場面は内部では一つの原子的なイベント。途中介入や中断が必要になった時点で、場面ごとの実行状態を検討する。
- 4場面を超えるMajorを今後追加する場合、表示上限で重要場面を落とさないルールが必要。今回は4場面すべてが残ることを検証済み。

DAY12のイベント、最終解決、エンディングは追加していない。
