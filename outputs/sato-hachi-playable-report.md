# SatoHachi Playable Vertical Slice DAY1–11

追記：観察理解UI追加後の仕様・検証は `sato-hachi-insight-report.md` と `sato-hachi-insight-displays.md` を参照。本書の入力数はReview追加前の記録。

## 起動と構成

Unity 6000.2.2f1で `Assets/Scenes/SatoHachiVerticalSlice.unity` を開いてPlay。
メニュー `NNN > Playable > Play Sato Hachi DAY1-11` からも起動できる。
Game Viewは1280×900基準の可変倍率。小さいDockでは文字も縮小するため、Game Viewを広げて確認する。

Sceneには固定Camera、GameController、ObservationView、HumanActor、CatActor、HOMEとSHOPPING_STREET、Scene Markers、Caption/Report/Action/Result/NextDayの実行時UIを配置。
UIはGame Viewで描画するMonoBehaviourのIMGUI。EditorWindow検証画面ではない。

Space / Enter / NEXTで場面を進め、Actionはボタンで選択する。F1でDebugを開き、GuidedをONにするとGでその日の指定Actionを選べる。Auto Advanceは別の自動検証モード。DAY11の結果を閉じるとSlice Completeとなり、再プレイできる。

## 責務と変更ファイル

- `Assets/Scripts/NNN/Playable/SatoHachiPlayableController.cs`: Route生成、既存Simulator呼出し、既存ObservationDayPhaseでの進行、入力、計測。
- `Assets/Scripts/NNN/Playable/ObservationScenePresenter.cs`: Event/Scene単位のログ連続再生、背景、Marker配置、翌日の小道具反映。
- `Assets/Scripts/NNN/Playable/ObservationActionPresenter.cs`: ActorとActionIdからモーションへ変換。
- `Assets/Scripts/NNN/Playable/CharacterActorView.cs`: 差し替え可能なVisualと仮モーション、移動、向き、表示、姿勢リセット。
- `Assets/Scripts/NNN/Playable/ObservationPresentationDefinition.cs`: 演出データ型。
- `Assets/Scripts/NNN/Playable/SatoHachiPlayableUI.cs`: Caption、CAT REPORT、Action、調査/工作結果、Debug。
- `Assets/Scripts/NNN/Editor/SatoHachiPlayableSceneBuilder.cs`: Scene/素材/演出定義の生成と起動メニュー。
- `Assets/Scripts/NNN/Editor/SatoHachiPlayableVerification.cs`: PlayMode完走とSimulation不変性の検証。
- `Assets/Scripts/NNN/UI/NNNDebugBootstrap.cs`: Playable Scene上で旧Debug UIの重複起動を防止。
- `Assets/Scenes/SatoHachiVerticalSlice.unity`、`Assets/Playable/SatoHachiPresentation.asset`、`Assets/Playable/Materials/*`、各meta。

Simulatorのイベント条件・効果・文章には変更なし。ControllerがSatoHachiObservationFactoryからRouteを生成し、ObservationSimulatorのGetObservationScenes/GetNNNActionOptions/ApplyNNNActionなどを呼ぶ。Presentationは返された結果を読む。新しいフェーズenumは追加していない。

## モーションと背景

| ActionId | 仮モーション |
|---|---|
| CAT_WALK / CAT_APPROACH / HARNESS_LOW_WALK | Walk（移動＋上下動） |
| CAT_LOOK / HUMAN_LOOK | Look（頭を左右に向ける） |
| CAT_GROOM | Groom（頭と前足） |
| CAT_REST | Sit（姿勢を低くする） |
| CAT_EAT | Eat（頭を下げる） |
| CAT_MEOW | Meow（頭の動き、音声なし） |
| CAT_PAW | Paw（前足） |
| CAT_JUMP | Jump（Marker移動＋上下動） |
| HUMAN_CROUCH | Crouch |
| HUMAN_PLACE / HUMAN_DOOR | Place（手を動かす） |
| HUMAN_HOLD / HUMAN_PHONE | Hold / Phone（手を上げる） |
| HARNESS_FREEZE / HUMAN_STOP / 未登録ID | Idle |

AnimationClip/Animator素材の代わりにActor View内のTransformで仮モーションを再生する。SimulatorにはAnimation参照を追加していない。
移動先はScene内Transform Markerで指定。Route固有EventId/SceneIdと背景/位置の対応はPresentationDefinitionに置く。
Short TripはDEPARTURE=HOME、ARRIVAL/MISMATCH=SHOPPING_STREET、RETURN=HOME。タワーとキャリーは既存WorldFlagsをDAY開始時に読み、翌日の画面に反映する。

## UI

CAT REPORTは猫の一言と観察で得た情報。質問UIはなし。次にその日の最大3 Action＋SKIPを表示する。INVESTIGATION/OPERATION/SKIPの文字と色で種別を示し、VisibleLockedは無効ボタンと不足理由、Hiddenは非表示。
調査結果は左側ResultText、右側NEW INFORMATION/NEW OPERATIONの一画面配置。工作結果は手配した工作名と「効果は翌日以降に現れます。」を表示し、成功判定の表現は使わない。

## 計測方法

各DAYのUTC開始/終了時刻、実時間秒数、SelectedActionId、実際に表示したScene数、CatReportId、受け付けた進行/選択入力数、Manual/AutoをCSVへ記録。出力先はEditorで `Logs/Playable/play-*.csv`。
入力数は無効中の連打を含まず、マウスクリックとキーボード進行を同じ1入力として数える。
ツール操作による手動記録は通信・画面確認時間も含むため、人間の自然な読書速度の測定として扱わない。中断した試行は完走平均へ含めない。

## 実際に確認した改善点と残課題

手動確認で背景Cameraの描画範囲外に前画面のボタンが残る問題と、Hover時の文字コントラスト低下を発見し修正。初対面で猫が人間から離れていた移動先、窓辺の高さも修正した。
DAY1–3のSKIPのみでもActionと結果を順に通るため、確認操作が多い。DAY5–9は休息・玄関・タワー巡回が繰り返され、Captionへの依存が強い。工作の翌日にタワーが出現する変化は視認できた。
ハーネス装着の見た目とPhoneの小道具がないため、動作の意味はCaptionを読む必要がある。追加素材の優先は装着/固まる/低い歩行、キャリー出入り、電話、ドア開閉。

改善優先順位は、(1) 読む速度と場面送りの実プレイヤー検証、(2) ハーネス/キャリー動作の区別、(3) 繰り返し場面の演出差、(4) 小さいGame Viewでの文字サイズ調整。Simulationルールの追加は今回不要。

## 検証結果

2026-09-12: SatoHachi 32 Seed PASS（guided/skip/alternative、再現性、DAY11 Short Trip 32/32）。SatoSuzu 1000 Seed PASS。UnityのC#コンパイルPASS。
最新SceneのPlayMode自動完走もPASS。DAY1–11、PresentationによるSimulation結果の不変性、商店街への背景切替と帰宅、計測CSVを検証した。
根拠ログ: `Logs/PlayableVerification.log`、`Logs/SatoHachiVerification.log`、`Logs/SatoHachiSuzuStress.log`。

## DAY別Scene数と入力数

Seed 42、指定Guided RouteのPlayMode完走CSV `Logs/Playable/play-20260911-215925-491.csv` による。手動の途中確認でも同じ場面数を確認した範囲では一致する。

| DAY | Scene数 | 進行・選択入力数 |
|---|---:|---:|
| 1 | 4 | 8 |
| 2 | 2 | 6 |
| 3 | 2 | 6 |
| 4 | 2 | 6 |
| 5 | 4 | 8 |
| 6 | 4 | 8 |
| 7 | 3 | 7 |
| 8 | 2 | 6 |
| 9 | 4 | 8 |
| 10 | 2 | 6 |
| 11 | 4 | 8 |
| 合計 | 33 | 77 |

最多入力はDAY1・5・6・9・11の各8入力。クリック専用の集計ではなく、キー操作も含む。

## 手動プレイ計測の扱い

2026-09-13、ユーザーの指示により手動3回の完走計測を取りやめた。各DAYの手動完走時間、総プレイ時間、最長・最短DAY、3回平均は未算出。途中までの試行と別Actionを選んだ完走記録は保存し、指定Guided Routeの完走計測には含めていない。自動完走の秒数も手動プレイ時間として代用していない。

操作・表示の手動確認と自動回帰は実施済みだが、自然な読書速度での全体テンポ、DAY10–11の山場としての体感は未評価。現状の所見は上記の実際に確認した範囲に限る。
