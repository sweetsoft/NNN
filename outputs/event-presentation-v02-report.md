# Observation Event Presentation Data v0.2 実装報告

ブランチ: `codex/event-presentation-v02`

## 1. 変更ファイル

- `Assets/Scripts/NNN/Playable/ObservationPresentationDefinition.cs`: Setup / Step / CG拡張。
- `Assets/Scripts/NNN/Playable/ObservationScenePresenter.cs`: 毎Scene再構成、背景Variant、Step再生。
- `Assets/Scripts/NNN/Playable/ObservationPropView.cs`: 一時Prop状態、移動キャンセル、Scene Reset。
- `Assets/Scripts/NNN/Playable/ObservationActionPresenter.cs`: Simulation Logを変更しない演出Action入口、人間の短距離移動。
- `Assets/Scripts/NNN/Playable/CharacterActorView.cs`: 動作切替時の向きを維持。
- `Assets/Scripts/NNN/Editor/SatoKotaPlayableSceneBuilder.cs`: DAY4 / 9 / 11等の新データ、World由来Prop配置、背景Variant。
- `Assets/Scripts/NNN/Editor/SatoHachiPlayableSceneBuilder.cs`: 改名フィールド対応。
- `Assets/Scripts/NNN/Editor/SatoHachiPlayableVerification.cs`: 保存済み旧Sceneを再生成しない検証入口。
- `Assets/Scripts/NNN/Playable/SatoHachiPlayableController.cs`: Resetに関するコメントを新仕様へ更新。
- `Assets/Scripts/NNN/Editor/CatSpriteSceneSetup.cs`: CAT_FOLLOWを確定WALKへ割り当て。
- `Assets/Scripts/NNN/Editor/SatoKotaPlayableVerification.cs`: Propの永続位置を前提とした検証を更新。
- `Assets/Scripts/NNN/Editor/PresentationV02Verification.cs`（新規、meta含む）: 独立性、隔離、SceneId、CG、追従のPlayMode検証。
- `Assets/Playable/Kota/SatoKotaPresentation.asset`、`Assets/Scenes/SatoKotaVerticalSlice.unity`: 新仕様を保存した実データ。

## 2. 新規クラス / enum

`ActorFacing`、`ScenePropState`、`ScenePropSetup`、`PresentationStep`、`BackgroundVariantBinding`。
Editor検証用に `PresentationV02Verification`。別のPresentationシステムは作っていない。

## 3. Scene Setup

`SceneStageBinding` は EventId / SceneId / BackgroundId / HumanStartPoint / HumanFacing / CatStartPoint / CatFacing / PropSetups / EventCgId / TriggerStep を保持。
既存CatDestinationもfallback用に維持。HumanMarker・CatMarkerはFormerlySerializedAsで読み込む。
未定義SceneはHOME / Human_Default / Cat_Defaultへ戻る。背景VariantはIdとGameObjectの対応表で、HOME_DAY / HOME_EVENING / HOME_NIGHTは今回同じ仮背景を共有する。

## 4. Prop Setup / State

`ScenePropSetup`: PropId / PointId / State / Visible / RequiredWorldFlag。
StateはNormal / Moved / Fallen / Tipped / Stored / Hidden。Stored / Hiddenは非表示。Tippedは意味だけで物理傾斜を実装しない。
再構築の優先順は、RestMarker → WorldPropSetups → Scene PropSetups → WorldVisibilityの表示制約。
`Moves`は検証用セッション累計だけで、次Sceneの表示を決める状態には使わない。

## 5. EventId + SceneId + StepIndex

`LogStageBinding.StepIndex` はScene内の0始まり観察ログ番号。旧LogIndexから自動移行する。
空SceneIdは空SceneIdにのみ一致し、名前付きSceneのワイルドカードにはしない。
1つのBinding内に複数PresentationStepを順序指定できる。StepにはActor / ActionId / TargetPointId / TargetPropId / PropState / DurationSecondsがある。
旧配置フィールドもfallback用に保持するが、新Sequenceがある場合はそのAction列を使う。

## 6. Resetタイミング

PlayScene冒頭のSetupSceneで前Coroutine、Actor移動、Prop移動を停止し、Caption / CG表示Id / 進捗をクリア。
Propを初期化し、Worldスナップショット、背景、Actor初期位置・向き、Scene Prop Setupを適用してから再生する。
ResetDayも同じSetup経路を使う。イベント間を移動でつながない。

## 7. Worldとの分離

SyncWorldはHashSetをコピーし、Simulatorのコレクション参照を保持しない。
Prop・Actor・Scene PresenterはSimulatorへ書き込むAPIを持たない。WorldVisibilityは設備の表示条件、Fallen等はScene内だけの状態。
Captionは常に元ObservationLogEntry.Text。演出用Actionを変えても元ログを書き換えない。

## 8. DAY4

HOME_EVENING / Human_Default（PC位置）/ Cat_DeskFloor（机前）/ Pen_Desk・Normal。
元ログに対応して HUMAN_PC → CAT_APPROACH → CAT_JUMP（Cat_Desk）→ 共通PenFall → HUMAN_HOLD（猫を床へ配置）→ CAT_APPROACH。
PenFallは CAT_PAW → PENをPen_Floorへ移動しFallen → HUMAN_REACT_SMALL。
指示書の短い例に加え、元の「床へ下ろす」「また近づく」という観察事実も残す。

## 9. DAY11

HOME_EVENING / Human_Default / Cat_DeskFloor。
PENはPen_Desk・Normal。FRAGILEはDeskCleared成立時にObject_Storage・Stored。CAT_CUSHIONはDeskSpot成立時にVisible。
元ログの接近→クッションへジャンプ→見る→休むを保持し、その後DAY4と同じPenFall → HUMAN_REACT_SMALL。
条件未成立の設備をSetupだけで出現させない。

## 10. DAY9

Cat_Desk / Human_Defaultから開始。元のHUMAN_LOOK行を HUMAN_STAND_UP → HUMAN_WALK（Human_OtherSide）で表示。
次のCAT_APPROACH行を CAT_JUMP_DOWN（Cat_DeskFloor）→ CAT_FOLLOW（Cat_OtherSide）で表示。
人間の移動を先に完了し、猫が降りた後に追う。元CaptionとSimulation Actionは変更しない。

## 11. Event CG

EventCgId / TriggerStepとEventCgRequestedイベントを用意。指定ログの演出が終わった時に一度通知。
次SceneでCurrentEventCgIdをクリア。今回実CGは設定しておらず、素材読み込み・進行停止はしない。

## 12–13. 固有分岐 / Simulation

汎用Presenter内にCAT_KOTA等の個体分岐なし。固有データはKota Scene Builder / Presentation assetのみ。
Simulation / Director / Relationship / Knowledge / NNN ACTION / OperationEffect / CAT REPORT / Insightの実装は変更なし。

## 14–17. 検証

Unity Compile: PASS。Scene独立性 / Prop Reset / World Persistence / Presentation Isolation / SceneId Isolation / Caption維持 / CG通知 / DAY9追従: PASS。

Kota Guided DAY1–11: PASS。Kotaは33 Seeds × 3 Policies = 1089日: PASS。Hachi 32 Seeds × 3 Policies: PASS。Hachi Insight 33 Seeds × 3 Policies = 1089日: PASS。Suzu 1000 Seeds: Passed 1000 / Failed 0。

保存済み旧Hachi SceneのDAY1–11: PASS（再生成なし、`Logs/PresentationV02HachiLegacy.log`）。

ログ: `Logs/PresentationV02Verification.log`。Simulation配下のソース差分なし。Playable結果を同一Seed・Actionの表示なし実行と比較し、一致。自動検証は手動プレイ時間の計測ではない。

## 18. ResetEachDay

フィールド・分岐を廃止。旧Sceneの同フィールドは読み捨てられる。
毎Sceneで再構築し、永続収納はWorldPropSetupsとFRAGILE_DESK_OBJECTS_STOREDから復元する。

## 19. Stage Asset化の拡張点

Marker名、BackgroundId、PropIdはDefinition側の識別子。Scene PresenterのMarkers / Backgrounds / Props参照をStage Assetのインスタンスへ差し替えられる。
PresentationStepとCaptionのアンカーは分離済み。CG通知の購読先も差し替え可能。
NavMesh、経路探索、自律巡回は追加していない。

## 20. 既存構造との衝突と扱い

- LogIndexだけの照合をSceneId込みへ移行。
- Propの前日位置保持を廃止。終了時のペン位置を固定視する旧テストを、演出回数とScene再構成テストへ変更。
- DAY10の片付け演出自体はWorldFlagを生まない。次SceneではWorldFlagが未成立なら初期状態へ戻る。永続収納は既存Operationの翌日効果でFlagが成立した後のみ。
- 1観察ログに複数動作があるため、ログを分割せず演出側の子Stepへ展開。
- HOME固定の旧テストをHOME系Variantへ拡張。背景画像そのものはPlaceholder。
- ActorのPlayActionが毎回向きを消していたため、SceneのFacingを動作切替でも保つよう修正。
