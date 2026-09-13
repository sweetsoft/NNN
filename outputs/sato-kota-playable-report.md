# 佐藤 × コタ Playable Vertical Slice DAY1–11

ブランチ：`codex/sato-kota-playable-demo`。基点：`codex/sato-hachi-playable-insights` の `67ef7af`。

Unityメニュー **NNN > Playable > Play Sato Kota DAY1-11** から起動する。Sceneは `Assets/Scenes/SatoKotaVerticalSlice.unity`。Build Settingsの先頭にもコタSceneを登録し、既存テストSceneは保持した。ハチの起動メニューは **NNN > Playable > Legacy Test** に残した。

## 1. 新しい猫プロフィール

コタ / `CAT_KOTA` / 2歳。Activity 88、Sociability 90、Independence 50、Adaptability 85。

既存Trait機構で `HIGH_ACTIVITY`、`HIGH_CURIOSITY`、`HIGH_PLAY_DRIVE`、`HIGH_EXPLORATION`、`HUMAN_AFFINITY`、`INDOOR_ORIENTED`、`WANTS_HUMAN_HOME`、`LOW_OUTDOOR_DRIVE` を定義。好奇心・遊び・探索には今回新しい数値軸を増やしていない。イベントは複数のTraitと観察履歴・環境を組み合わせる。単一の「やんちゃ」フラグは使わない。

工作で猫プロフィールは書き換えない。遊び・Jump・Pawの候補は最終日にも残る。

## 2. 新規／変更ファイル

新規コード（それぞれUnity metaを含む）：

- `Assets/Scripts/NNN/Observation/SatoKotaObservationFactory.cs`：猫、条件ベースのRoute、Action、CAT REPORT。
- `Assets/Scripts/NNN/Playable/SatoKotaInsightFactory.cs`：コタ専用の事実・変化・問い。
- `Assets/Scripts/NNN/Playable/ObservationPlayableContent.cs`：RouteとInsightの供給元をSceneから指定する共通インターフェース。
- `Assets/Scripts/NNN/Playable/SatoKotaPlayableContent.cs`：コタのFactoryを接続。
- `Assets/Scripts/NNN/Playable/ObservationPropView.cs`：Marker間の小物移動と日次／再プレイのリセット。
- `Assets/Scripts/NNN/Editor/SatoKotaPlayableSceneBuilder.cs`：Scene、Presentation asset、仮素材の生成。
- `Assets/Scripts/NNN/Editor/SatoKotaVerification.cs`：33 Seed × 3経路、条件・先読み・不変性検証。
- `Assets/Scripts/NNN/Editor/SatoKotaPlayableVerification.cs`：PlayMode完走、小物、共通Controller、既存回帰。

新規アセット：`Assets/Scenes/SatoKotaVerticalSlice.unity` と `Assets/Playable/Kota/` 内のContent・Presentation・7種類のMaterial、およびmeta。

変更コード：`SatoHachiPlayableController.cs`、`SatoHachiPlayableUI.cs`、`ObservationPresentationDefinition.cs`、`ObservationScenePresenter.cs`、`SatoHachiPlayableSceneBuilder.cs`。既存Sceneの参照を保つため、共有Controller/UIのクラス名は維持した。Content未設定のハチSceneは従来Factoryを利用する。

その他：`ProjectSettings/EditorBuildSettings.asset`、本報告、`outputs/sato-kota-insight-displays.md`。個人用Unity UserSettingsは実装対象に含めない。

## 3. DAY1–11 Guided進行

日番号条件は追加せず、すべてのイベントはDAY1–30の範囲で、前日までの観察履歴・Trait・Knowledge・WorldFlagから選ぶ。生活の変化を見せる11件には既存Milestone枠を使い、一日最大一件を保証する。下表の日付はGuided経路の結果で、SKIPや別順序では到達日・内容が変わる。

| DAY | 観察 | 当日のAction | Scene数（Seed 42） |
|---|---|---|---|
| 1 | 自分から接近、匂い、頬寄せ | SKIP | 1 |
| 2 | 初入室と探索、Visiting | SKIP | 2 |
| 3 | 同居開始、LivingTogether | SKIP | 2 |
| 4 | PC作業への乱入、ペン落下、再接近 | 活動時間の調査 | 2 |
| 5 | 夜に走る、棚へ登る、おもちゃを追う | 帰宅後の遊び工作 | 2 |
| 6 | 長く遊ぶと夜の往復が減る、机には来る | 高所・探索の調査 | 3 |
| 7 | 棚を使い、机にも来る | 高所動線の工作 | 4 |
| 8 | タワー・棚・窓辺を使い、机にも来る | SKIP | 3 |
| 9 | 佐藤の移動を追う | 接近タイミングの調査 | 2 |
| 10 | 佐藤が壊れやすい小物を収納する | 机横の猫用スペース工作 | 2 |
| 11 | 猫用スペースで休み、ペン一本を落とす | SKIP | 3 |

合計26 Scene。観察→Review→CAT REPORT→ACTION→結果→翌日の既存入力フローを共有。DAY3の受容はWelcoming、室内適応はExploringであり、同居だけで最大値にはしない。

## 4–5. Investigation / Knowledge

| 調査ID | 意図 | 得るKnowledge |
|---|---|---|
| `INVESTIGATE_ACTIVITY_PATTERN` | 活発な時間と遊び不足の関係を確かめる | `KNOW_KOTA_HIGH_EVENING_ACTIVITY`、`KNOW_KOTA_HIGH_PLAY_DRIVE` |
| `INVESTIGATE_VERTICAL_EXPLORATION` | 登る行動と高所・探索の関係を確かめる | `KNOW_KOTA_VERTICAL_PREFERENCE`、`KNOW_KOTA_EXPLORATION_DRIVE` |
| `INVESTIGATE_HUMAN_PROXIMITY` | 机より佐藤の近くにいたい可能性を確かめる | `KNOW_KOTA_WANTS_HUMAN_PROXIMITY` |

調査本文は観察傾向と未確定部分を併記する。「遊び不足が唯一の原因」「寂しがり」と断定しない。結果本文、NEW INFORMATION、NEW OPERATIONは共通UIを使用。

## 6–7. Operation / WorldFlags

| 工作ID | 発見／実行条件 | 翌朝追加するWorldFlags |
|---|---|---|
| `OP_CREATE_PLAY_ROUTINE` | 夕方の活動を知る／遊び反応を知る | `EVENING_PLAY_ROUTINE_AVAILABLE` |
| `OP_CREATE_ALLOWED_VERTICAL_ROUTE` | 高所の好みを知る／高所と探索の両方を知る | `CAT_TOWER_INSTALLED`、`ALLOWED_VERTICAL_ROUTE_AVAILABLE` |
| `OP_CREATE_DESK_SIDE_CAT_SPOT` | 佐藤への接近傾向を知る | `DESK_SIDE_CAT_SPOT_AVAILABLE`、`FRAGILE_DESK_OBJECTS_STORED` |

遊び工作は激しい夜の往復候補を外し、短い遊びの候補へ変える。机への接近は変えない。高所工作は使えるタワーと動線を増やし、人への接近は消さない。最後の工作は猫の居場所と人間側の物の配置を変える。

人間の自然適応は既存 `HumanAdaptedEnvironment` Memoryにも残る。片付けの観察は接近調査後に成立し、工作をSKIPした場合も、演出上いったん収納した小物が翌朝机へ戻らない。再プレイでは初期配置へ戻す。DAY11のペンは収納済みの壊れやすい小物ではなく、佐藤が現在使っている一本。

## 8–9. CAT REPORT / Insight一覧

[DAY1–11の全表示一覧](sato-kota-insight-displays.md) に、各日のCAT REPORT、UPDATE、CHANGE、CURRENT QUESTION、選択Actionを検証結果から出力した。

CAT REPORT順：近く、平気。／中、面白い。／ここ、好き。／あそこ、面白い。／もっと遊ぶ。／いっぱい走った。／高いところ、好き。／ここもいい。／近くにいたい。／ここ、いい。／ここ、いい。

Reviewは最大3 UPDATE・2 CHANGE、問いは1件。DAY6は「夜の走り回り↓／机や棚は残る」、DAY8は「許可高所↑／作業机にも来る」、DAY11は「近くで過ごせた／物に触れる行動は残る」。変化は実行済みイベントと工作前後の環境、前日の観察を照合する。

## 10. コタRouteから外した要素

外出要求中心のイベント、以前の商店街、経路調査、大通り・猫専用細道、ハーネス適応、キャリー、Short Tripをコタのイベント・Action・Knowledge・Insightに含めない。ハチFactory・Insight・Sceneと汎用機能は保持した。

## 11–12. 再利用と責務分離

既存ObservationSimulator、ConditionEvaluator、Director、独立したRelationshipState、Knowledge、翌朝のOperationEffect、日次Action候補選択、CAT REPORT、Insight比較、PlayableのDAY進行・計測・UIを再利用。

**Simulator / Director / Evaluatorの変更、コタ専用分岐はゼロ。** CatIdや日番号で演出を直書きせず、ContentがFactoryを選び、PresentationDefinitionのEventIdとログ番号がScene Marker・小物を指定する。未指定のActionは従来どおりIdle。

## 13–16. 検証

- Kota：33 Seed（0–31、42）× Guided / SKIP / Alternative × 11日 = 1,089 DAY。PASS。
- DAY2 Visiting、DAY3 LivingTogetherと独立軸、日付固定なし、先読み防止、翌朝効果、部分改善、人格不変、Seed再現性、PresentationのSimulation不変性。PASS。
- Traitを一つずつ除去する検証で、好奇心・親和性・活動性・遊び・探索が対応行動の条件に使われることを確認。高所工作を遊び工作より先に行える独立性も確認。
- Kota PlayMode：DAY1–11、全Review、計測、ペン落下2回、片付け、小物表示、Marker・Action mapping、Simulation不変性。PASS。
- Hachi：32 Seed、Guided/SKIP/Alternative、Short Tripと再現性。PASS。
- Hachi Insight：33 Seed × 3経路 × 11日。PASS。
- Suzu：1,000 Seed / 30,000日。PASS。
- Unity 6000.2.2f1 C#コンパイル：PASS。

ログは `Logs/KotaVerification.log` と `Logs/KotaPlayableVerification.log`。入力・Scene数・時間は `Logs/Playable/` のCSVに記録。自動送りと目視確認の時間は通常の手動プレイ時間として扱わない。手動3回の完走計測は以前の中止指示に従い実施していない。

## 17. Game Viewで確認した点と分かりにくさ

DAY1の接近→Review、DAY6・9の問い→調査結果、DAY10の片付け→工作、DAY11の猫用スペース→ペン落下→Reviewを操作・目視確認。中間日はDebug Auto Advanceを併用した。全日を手動評価したとは扱わない。

調査本文とNEW INFORMATION / OPERATIONは一画面に収まる。DAY10の工作Intentが末尾だけ折り返していたため短縮した。DAY11の机横クッションには支えを追加した。

DAY6の「走り回りが減った」は、同じ見た目のWalk/Paw fallbackでは差が小さく、CaptionとCHANGEに頼る。DAY9も人間の移動は仮の配置切替なので、後を追う理由の理解にはCaptionが必要。DAY11は机上と机横の違い、落ちた赤いペンが見えるため、部分的な改善を比較しやすい。

## 18. 冗長さ

DAY7の棚・机とDAY8の高所・机は、意味の差をアニメーションだけで示すには弱い。Seed 42のDAY7は4 Sceneで最多。調査を読むDAY6から連続するため、今後はDAY7の通常観察の選び方と高所動線の見せ方を調整する価値がある。DAY1–3は1・2・2 Sceneで早期同居のテンポを優先した。

## 19. 次に必要なアニメーション／小物

優先順：猫のRunとPlayの区別、机・タワーへ着地するJump、人間のPC作業と歩行、頬寄せRUB、ペンへ伸ばすPawと短い人間リアクション。素材追加前でも現在のSceneは完走する。

現状のfallback：RUN/APPROACH/ENTER/EXPLORE→Walk、SNIFF/RUB→Look、PLAY/HUMAN_PC/HUMAN_PLAY→Paw、REST/SIT→Sit、CLEAR_TABLE→Place。Jump/Groom/Eat/Crouch/Holdは既存仮モーション。PC・keyboard・赤いペン・白い小物・赤いおもちゃ・紙袋・棚・タワー・猫用クッションを配置。

## 20. 新しい汎用Simulation機能の必要性

不要だった。追加はPresentation側のContent供給、ログ単位のMarker／Prop指定、小物の移動・リセットのみ。コタは最終日も活動性88、好奇心と遊び・探索のTraitを保ち、人間側も環境を変える構成となった。
