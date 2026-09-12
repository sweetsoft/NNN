# Playable Slice 観察理解UI

## 実装

観察 → OBSERVATION REVIEW → CAT REPORT → NNN ACTION → RESULTの順。追加操作はReviewを閉じる一回だけ。CURRENT QUESTIONはACTION画面の上部に常時表示し、質問だけの画面は増やしていない。

### 今回の新規ファイル

- `Assets/Scripts/NNN/Playable/ObservationInsightDefinition.cs`：Insight定義、比較条件、表示時点のスナップショット、選択Presenter。
- `Assets/Scripts/NNN/Playable/SatoHachiInsightFactory.cs`：SatoHachiの事実要約・変化・問いを集約。
- `Assets/Scripts/NNN/Editor/SatoHachiInsightVerification.cs`：Guided/SKIP/Alternativeの証拠・先読み・不変性検証。
- 上記のUnity meta。
- `outputs/sato-hachi-insight-displays.md`：検証から生成したDAY1–11の表示内容一覧。

### 今回の変更ファイル

- `SatoHachiPlayableController.cs`：Review表示を追加。既存CatReportフェーズ内の表示切替であり、SimulationのフェーズenumやDAY進行APIは変更していない。
- `SatoHachiPlayableUI.cs`：Review、Question、Intent、根拠Debug表示。Actionカードを拡張。
- `NNNActionCatalog.cs`：表示専用の読み取り専用IntentTextと、既存共通Actionの意図。
- `SatoHachiObservationFactory.cs`：全9件のInvestigation/OperationにIntentを定義。
- `SatoHachiPlayableVerification.cs`：11日すべてのReview表示と、各日1入力だけ増えることを検証。

## 選択と比較

ObservationInsightDefinitionはId、Priority、Condition、Updates、Changes、CurrentQuestionを持つ。各Update/ChangeにもIdと証拠条件がある。条件は今日の実行済みEventId、取得済みKnowledge、WorldFlags、発生履歴をANDで評価する。日番号条件は使っていない。

観察場面をすべて表示し終わった時点でReviewを一度生成する。当日のActionで得られる情報はまだ条件に入らない。優先度順に最大3件の事実と最大2件の変化を選び、前日と同じ要約は新しい事実より後回しにする。適切な定義がなければ、今日のログから重要度順・イベント重複なしで最大2件を抽出する。問いは条件が成立した定義のうち最高優先度の1件。該当する新しい問いがなければ前の問いを維持する。

比較用には前日のReview時点の独立したコピーを保存する。翌日の調査情報や状態変更で過去の証拠は書き換わらない。比較種別は、同居状態の前日差、WorldFlag追加、Knowledge追加、前日の重要Event、特定Operation実行前のWorldFlagsとの比較。工作直前のコピーは選択時に保存し、実際の効果と対象イベントの両方を確認してから変化を出す。

## 先読みを防ぐ文言

- DAY4は寝床・玄関・閉じたドアという事実のみ。商店街への愛着や猫の本心は断定しない。
- DAY8の「強い逃走は見られなかった」は装着ログだけでは保証できないためReviewへ追加していない。既存の調査結果が扱う。
- DAY9のReviewは、商店街が既知の場合「この状態で、安全に商店街まで行ける？」へ問いをつなぐ。経路調査はその後なので、大通り・猫専用細道はまだ表示しない。
- DAY10では取得済みKnowledgeを条件に、道具と経路の準備を2件で要約する。詳細は既存Investigation Resultに残す。
- タワーに関する変化はWorldFlagと当日の利用イベントを必須にする。設置前やSKIP経路へ出ない。
- ACTION INTENTは確かめる仮説／試す条件の短文で、結果を保証しない。VisibleLockedも同じIntentを表示する。Hiddenは表示しない。

## UIと所見

ReviewはUPDATE最大3行、CHANGE最大2行の一画面。Review中は直前のCaptionを重ねて表示しない。CAT REPORTの猫の一言、調査結果の本文・NEW INFORMATION・NEW OPERATION、工作結果は維持した。

ACTIONではCaption領域をCURRENT QUESTIONに使い、下の2×2カードに種別・名前・Intent・不足条件を表示する。F1でSelectedInsightId、CurrentQuestionId、UpdateのEvent/Knowledge根拠、Changeの比較元を確認できる。

Game ViewでDAY1のReview→CAT REPORT→Question、DAY4の調査Intent、DAY5の3工作＋SKIPを確認。Reviewの3行と変化、Questionの1行、Intentの短文が枠内に収まり、Debugの根拠も表示された。Lockedの条件とIntentは自動検証・共通描画経路で確認しており、Locked専用画面の目視確認は行っていない。

DAY1・2・10はUPDATEを2件へ圧縮した。表示上限は選択履歴の保存後に適用し、翌日の要約やCHANGEを変えない。DAY11も3事実＋2変化だが「行けた／付いていけない」の対比は短文で整理している。DAY7は道具を調べる動作、DAY8–9は固まる・低く歩く違いが仮モーションでは弱く、引き続きCaptionへの依存がある。自然なプレイテンポを測る手動3回の完走は、前回の中止指示に従い実施していない。

## Simulationと検証

Simulationの選択条件、イベント、結果文章、効果、Knowledge取得、WorldFlags、Action候補ロジックは変更なし。Observationフォルダの変更はActionの表示メタデータのみ。

- Insight：33 Seed × 3経路 × 11 DAY = 1,089 DAY PASS。表示件数、根拠、DAY4/DAY9の先読み防止、タワー比較、全ActionのIntent、Locked、定義なしFallback、状態・候補・イベント・CAT REPORT不変性。
- SatoHachi：32 Seed × Guided/SKIP/Alternative、再現性、Short Trip回帰 PASS。
- SatoSuzu：1,000 Seed PASS。
- Unity C#コンパイル：PASS。
- PlayModeでのReviewを含むDAY1–11完走：PASS。全日でReviewを1回表示し、従来より各日1入力だけ増えることを確認。

Seed 42 GuidedのScene数は従来どおり合計33。進行・選択入力はReviewの11回を加え合計88（キーボードとクリックを同じ1入力として数える）。

ログ：`Logs/InsightVerification.log`、`Logs/SatoHachiVerification.log`、`Logs/SatoHachiSuzuStress.log`、`Logs/PlayableVerification.log`。
