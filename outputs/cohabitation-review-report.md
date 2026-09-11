# 同居状態モデル レビュー反映報告

対象ブランチ: `feature/cohabitation-state-model`

## 1. 変更ファイル

- `Assets/Scripts/NNN/Observation/ObservationDefinitions.cs`: 遷移制約、世界条件、ルート別ACTION・CAT REPORT、日次出力。
- `Assets/Scripts/NNN/Observation/NNNActionCatalog.cs`: 複数Knowledge、発見条件、世界効果、調査結果、表示状態、フェーズ・レポート型。
- `Assets/Scripts/NNN/Observation/ObservationSimulation.cs`: 日末ACTION、翌朝の工作反映、候補表示、レポート選択。
- `Assets/Scripts/NNN/Observation/SatoSuzuVisitObservationFactory.cs`: 世界条件に反応する通常イベント4件とCAT REPORT3件。
- `Assets/Scripts/NNN/Observation/ObservationDebugRunner.cs`: 猫の一言・調査結果・新規情報・発見／解禁工作の表示。
- `Assets/Scripts/NNN/Editor/CohabitationStateVerification.cs`: 全9通りの同居遷移と拒否時の原子性を追加検証。
- `Assets/Scripts/NNN/Editor/NNNActionVerification.cs`: 新しいコアループに合わせて検証を更新。
- `Assets/Scripts/NNN/Editor/NNNObservationBatchRunner.cs`: 通常のバッチ検証にもACTION検証を接続。
- `outputs/cohabitation-review-report.md`: 本報告。

作業開始前から存在した `Assets/test.unity` と `UserSettings/Layouts/default-6000.dwlt` の変更は作業対象外。

## 2. 新規クラス・enum

クラス: `OperationEffect`, `PendingOperationEffect`, `InvestigationResult`, `CatReportDefinition`, `CatReportResult`。
定数クラス: `WorldFlag`。
enum: `NNNActionVisibility`（Available / VisibleLocked / Hidden）、`ObservationDayPhase`（Observing / CatReport / ActionSelection / Completed）。
既存条件enumに `HasWorldFlag`, `MissingWorldFlag`, `HasKnowledgeTag` を追加。
旧 `ObservationSimulationModifier` と特定EventIdへの優先度加算経路を削除。

## 3. RelationshipState

同居・人間の受容・猫の適応・住居準備・警戒・人間の接触行動は独立したまま維持。
`CanTransitionTo` を候補評価と `RelationshipStateChange.Apply` で共用。
通常の変更は同一状態の再設定または Outside → Visiting → LivingTogether の一段階進行だけを許可。
LivingTogetherへの進行は変更前のVisiting、Welcoming以上、HomePreparation.Allが必要。
不正な遷移は他の軸を変更する前に拒否。同居解消API自体は今回導入していない。

## 4. Investigation / Operationのデータフロー

- 調査 → AddedKnowledgeTagsをPlayerKnowledgeFlagsへ追加 → InvestigationResultを返す。
- 結果には本文、実際に追加した情報、新規発見工作、新しく全Knowledge条件を満たした工作を別々に記録。
- 工作の発見はDiscoveryKnowledgeTags、解禁はRequiredKnowledgeTagsの全件ANDで判定。
- 「遊ぶきっかけ」は生活リズムの調査で発見され、猫の距離感も調査すると解禁される。
- 工作選択 → PendingOperationsへ予約 → 翌日のBeginDayでWorldFlags、Knowledge、住居準備へ反映。
- 世界条件は削除されるまで保持。旧3日間のEvent優先度加算は廃止。期間付き汎用Modifierは今回導入していない。
- 「猫のサインを伝える」はHUMAN_KNOWS_CAT_BOUNDARY_SIGNALを付与。翌朝から「サインに気づく」「見逃す」の両通常イベントが候補になり、発生はDirectorが選ぶ。

GetNNNActionOptions()は通常候補最大3件とSKIPを返す。Hidden・調査済み・効果成立済みを省き、利用可能な工作を優先する。
全カタログの状態確認はGetNNNActionOptions(true)。部分的に情報を得た工作にはVisibleLockedと不足タグを返す。
候補が尽きた場合はSKIPだけになる。

## 5. CAT REPORTとDAYフェーズ

当日の発生イベントIDと状態条件から優先度順に一件を選択し、該当なしは「今日はここで休んだ。」。
スズ用には遊び・距離・室内探索の短い一言を用意。質問や未知の人名は含めない。
レポート生成を繰り返し呼んでも同じ結果を返し、ACTION枠は消費しない。
日次結果のCatReportに保持し、通常イベントの数や表示ログ数には混ぜない。

UIからの呼出順:

```csharp
simulator.BeginDay(day);
// GenerateNextEvent / ExecuteNextEventで観察を表示
var report = simulator.CompleteObservation(); // 残りの観察を消化、CatReportへ
// report.Textを表示
simulator.CompleteCatReport();                // ActionSelectionへ
var options = simulator.GetNNNActionOptions();
var investigation = simulator.ApplyNNNAction(selectedId); // Completedへ
var result = simulator.EndDay();
// 翌日のBeginDayで工作効果を反映
```

観察中・レポート中のACTION実行は拒否。ACTION後のイベント取得・実行はnull。
既存バッチ利用との互換性のため、EndDayは未完の観察／レポートを順に完了し、未選択ならSKIPを記録する。
UIでは上記の明示的な呼出順を使用する。時刻付きの旧入口も残すが、選択可能な日末時刻は24時。

## 6. 検証結果

Unity 6000.2.2f1でコンパイルエラーなし。既存のNNNDebugBootstrapにFindObjectOfTypeの非推奨警告あり。

- RunBatchVerification: PASS。代表5 Seedの30日進行、同居開始条件、状態分離、ログ順序、再現性。
- 新規／更新テスト: 同居の9遷移、拒否の原子性、Hidden→VisibleLocked→Available、複数情報AND、未調査拒否、同日二回拒否、SKIPの枠消費、調査結果の発見／解禁差分、日末フェーズ順序、CAT REPORTの一回性、工作の当日不変／翌日反映、複数候補化、世界フラグ削除と準備削除、調査／SKIPによる30日観察結果の不変性。
- RunStressTest: 1000 Seed、30,000日、Failed 0、ログ時刻違反0、480 Seed警告なし／520 Seed警告あり。結果はPASS WITH WARNINGS。代表警告は「導入後のMajor Eventが2日連続」で、今回の必須条件違反ではない。
- NNNExternalBenchmarkRunner.Run: 新人間×既存猫、既存人間×新猫、新人間×新猫の3群を正常完了。候補評価・選出・プロフィール生成のソース変更なし。このベンチマークは診断出力であり、旧版とのスコア完全一致比較ではない。
- 変更したC#ファイルのgit diff --check: エラーなし。

ログ:

- `Logs/CohabitationReviewVerification.log`
- `Logs/CohabitationReviewStress.log`
- `Logs/CohabitationReviewCandidates.log`

## 7. SatoSuzuへの影響

DAY1接触、DAY2初入室、DAY3同居開始と、その後の既存Core進行は維持。
工作後だけ候補になる通常イベント4件とCAT REPORTを追加。通常1～3件＋Major最大1件の枠は維持。
スズ編の接触時期を早める調整や、SatoHachiルートの新規作成は行っていない。

## 8. SatoHachiを追加する際の拡張点

ObservationRouteDefinition.ActionsとCatReportsにルート専用定義を追加する。
調査一件から複数AddedKnowledgeTagsを返せるため、経路・大通り・キャリー情報を別の情報源から取得できる。
DiscoveryKnowledgeTagsとRequiredKnowledgeTagsを分け、途中まで知っている工作をVisibleLockedで提示できる。
工作のOperationEffectにWorldFlagsや住居準備を定義し、複数の通常／MajorイベントがHasWorldFlag等を共有して参照する。
CAT REPORTはRequiredTodayEventIdとConditionsを組み合わせ、当日の体験に合った台詞を追加する。
シーンへの圧縮はDaySimulationResultを読むUI側で行い、シミュレーションのイベント件数から独立させる。
現在のACTION定義はコードから組み立てる形式。専用マスターアセット／セーブ形式への移行は後続作業。
