> 更新: DAY10〜11を追加し、手動プレイメニューは「Play Sato Hachi DAY1-11」になりました。追加内容・最新検証は `outputs/sato-hachi-day10-11-report.md` を参照してください。以下はDAY1〜9実装時の報告です。

# SatoHachi Vertical Slice DAY1–9 実装報告

ブランチ: `feature/sato-hachi-vertical-slice`。origin/mainとmainはいずれも同居状態モデル反映済みのa716c1dだったため、mainから作成。

## 手動プレイ

Unityの **NNN → Observation → Play Sato Hachi DAY1-9** を開く。Seedを設定して「DAY1から開始」。
「次の観察シーン」→CAT REPORT→「NNN ACTIONへ」→調査／工作／SKIP→「NEXT DAY」の順で進む。
シーンやGameObjectの編集、Play Modeへの切替は不要。これは実画像やアニメーションを持たないEditor内の検証画面。
世界条件を含む検証状態は折りたたみ内に分離し、通常表示では未発見工作や商店街への関心を先取りしない。
DAY9の調査も一日一回の枠を使うため、そこで解禁されたShort Tripの実行はDAY10以降。画面のプレイ範囲はDAY9まで。

## 1. 変更／新規ファイル

新規:

- `Assets/Scripts/NNN/Observation/SatoHachiObservationFactory.cs` およびmeta: ハチのプロフィール、イベント、調査、工作、CAT REPORT。
- `Assets/Scripts/NNN/Editor/ObservationSliceWindow.cs` およびmeta: 手動で選べる検証画面。
- `Assets/Scripts/NNN/Editor/SatoHachiVerification.cs` およびmeta: 32 Seedの検証、選択肢の公平性、スズ回帰の実行入口。
- `outputs/sato-hachi-verification.md`: 自動生成したSeed 0の全日ログと検証集計。
- `outputs/sato-hachi-implementation.md`: 本報告。

変更:

- `Assets/Scripts/NNN/Observation/NNNActionCatalog.cs`: ACTIONの実行条件・発見条件に既存ObservationEventConditionを利用。
- `Assets/Scripts/NNN/Observation/ObservationDefinitions.cs`: DayAtLeast条件、条件説明、日次結果のMajor分類メタデータ。
- `Assets/Scripts/NNN/Observation/ObservationSimulation.cs`: 条件判定、World Stateを含む解禁通知、候補の提示履歴と日内固定。
- `Assets/Scripts/NNN/Editor/NNNObservationBatchRunner.cs`: Core／Milestoneを任意イベントの過密警告と区別。

## 2. DAY1～9進行

以下は検証した基準の選択順。プレイヤーがSKIPや別の工作を選ぶと進展は変わる。

| DAY | 主な観察 | 日末の選択 |
|---|---|---|
| 1 | 佐藤へ接近し匂いを嗅ぐ。佐藤がしゃがみ、ハチが残る | SKIP |
| 2 | 初入室。食器、水、トイレ、寝床と安全を準備。Visiting | SKIP |
| 3 | 翌朝の世話の準備と寝床の利用。LivingTogether | SKIP |
| 4 | 家を利用するが玄関へ行って鳴く。佐藤は外の車を見てドアを開けない | 室内での過ごし方を調査 |
| 5 | 室内で遊び、終えると玄関を見る | タワーと室内巡回路を整える |
| 6 | タワーを利用し、休んだ後も玄関へ向かう | 以前の生活圏を調査 |
| 7 | 佐藤がスマホでハーネスとキャリーのサイズを確認 | 安全な外出の道具を揃える |
| 8 | 初装着。静止して身体を低く歩く。室内で外す | 装着時の反応を調査 |
| 9 | 室内で再試行し数歩進む。適応完了とはしない | 商店街までの経路を調査、Short Trip発見／解禁 |

DAY3の状態はLivingTogether＋Exploring＋Medium＋Welcoming。最大の適応・無警戒・Committedへ自動的に上げない。

## 3. Knowledge一覧

| ID | 取得する情報 |
|---|---|
| KNOW_CAT_WANTS_OUTSIDE | 寝床等を利用しても外へ行きたがる |
| KNOW_INDOOR_VERTICAL_OPPORTUNITY | 室内の高低差・活動機会が少ない |
| KNOW_CAT_FAMILIAR_SHOPPING_STREETS | 以前、商店街の軒下や店の間を繰り返し通っていた |
| KNOW_HARNESS_ADAPTATION_UNCERTAIN | 強い逃避はないが拘束の違和感があり、適応可能性は未確定 |
| KNOW_ROUTE_WALKABLE | 商店街は約600mの徒歩圏 |
| KNOW_ROUTE_HAS_BUSY_ROAD | 途中に大通りがある |
| KNOW_SHOPPING_STREET_HAS_CAT_ONLY_PATHS | 商店街に猫だけが通れる細道がある |

最初の調査では活動不足・探索不足・以前の生活圏への関心を候補として残す。商店街への愛着を原因確定フラグとして開示しない。

## 4. WorldFlags一覧

| ID | 付与する工作 |
|---|---|
| CAT_TOWER_INSTALLED | タワーと室内巡回路 |
| INDOOR_VERTICAL_ROUTE_AVAILABLE | 同上 |
| HARNESS_OWNED | 安全な外出の道具 |
| CARRIER_OWNED | 同上 |
| SHOPPING_STREET_SHORT_TRIP_PREPARED | 商店街ショートトリップ |
| INDOOR_PLAY_PREPARED | 室内で遊ぶ準備 |
| SAFE_WINDOW_PERCH_AVAILABLE | 安全な窓辺の居場所 |

いずれも工作選択時は予約のみ、翌朝に反映。OutdoorRequest=falseやCAT_HATES_HARNESSは作らない。

## 5. Investigation一覧

| ID | 発見／実行の条件 | 結果 |
|---|---|---|
| INVESTIGATE_INDOOR_ACTIVITY | 外出要求の観察後 | 外出要求・室内高低差の2情報 |
| INVESTIGATE_FORMER_LIVING_AREA | 外出要求を観察済み、DAY6以降 | 商店街での過去の行動 |
| INVESTIGATE_HARNESS_RESPONSE | 初回装着の観察後 | 嫌悪／適応の結論を保留する情報 |
| INVESTIGATE_SHOPPING_STREET_ROUTE | 商店街の過去の行動を調査済み、DAY9以降 | 徒歩圏・大通り・猫専用の細道の3情報 |

経路結果には「住宅街→生活道路→大通り→商店街」「危険区間はキャリー」「全行程ハーネス徒歩は安定しない」を記載。

## 6. Operation一覧と解禁条件

| ID | 発見条件 | 追加の実行条件 |
|---|---|---|
| OP_INSTALL_INDOOR_VERTICAL_ROUTE | 室内高低差の情報 | 外出要求の情報も必要 |
| OP_PREPARE_INDOOR_PLAY | 室内高低差の情報 | 同情報 |
| OP_PREPARE_SAFE_WINDOW_PERCH | 室内高低差の情報 | 同情報 |
| OP_PREPARE_SAFE_OUTDOOR_GEAR | 商店街の過去の行動の情報 | 外出要求の情報＋佐藤が安全な方法を調べる観察 |
| OP_SHOPPING_STREET_SHORT_TRIP | 徒歩圏＋大通りの情報 | 猫専用細道＋外出要求＋ハーネス適応未確定の情報、およびCARRIER_OWNED＋HARNESS_OWNED |

Short Tripはキャリーで危険区間を通過し、商店街でハーネスを使う計画。工作は準備フラグのみを作り、旅行結果のイベントを発生させない。
必要な情報が同じでもキャリーを持たないケースはVisibleLockedとなり、調査結果のNewlyUnlockedOperationIdsにも含めない。

## 7. CAT REPORT一覧

その日の発生イベントに対応する一件を優先度で選ぶ。

| 対応する観察 | 一言 |
|---|---|
| 初接触 | そばまで行った。 |
| 初入室 | 中、見てきた。 |
| 同居開始 | ここで寝る。 |
| 外出要求の初観察 | 外、行きたい。 |
| 室内の遊び | 今日は遊んだ。 |
| 改善後のタワー利用 | 高いところ、いい。 |
| 通常のタワー利用 | 上で休んだ。 |
| 続く外出要求 | 外、まだ行きたい。 |
| 初ハーネス | あれ、歩きにくい。 |
| ハーネスで再び数歩 | 今日は、あれでも歩けた。 |
| 該当なし（既存の共通fallback） | 今日はここで休んだ。 |

ACTIONの枠は消費せず、再取得しても同じオブジェクトを返す。質問、分析的な答え、未知の人名は使わない。

## 8. 汎用基盤への追加

- NNNActionDefinition.Conditions / DiscoveryConditionsに既存のイベント条件を再利用。HasWorldFlag、EventOccurredなどを工作／調査双方で使う。
- DayAtLeastを追加。DAY9付近の調査提示をルートのデータで指定できる。
- 調査による解禁通知はKnowledgeだけでなくConditionsも評価する。日次枠を消費した直後でも、前提条件が揃ったことを通知する。
- ActionLastOfferedDayで提示履歴を保持し、Available候補の中では長く提示していないものを先に出す。同条件のときだけ工作を優先。調査専用枠／工作専用枠は設けない。
- ACTION選択開始時にその日の候補IDを確定。UIの再描画で並び替えや提示履歴更新を起こさない。
- DaySimulationResultにMajorEventRole / MajorEventCategoryを保存。過密警告は任意Relationship／Problemの連続を対象とし、Core／Milestoneを除く。

イベントに新しい効果APIを増やす必要はなかった。状態モデルの再抽象化、DispatchSituationDefinitionの導入、シミュレータの猫別分岐は行っていない。

## 9. ハチ専用ロジックの有無

Simulator、Director、Evaluatorにはcat.IdやハチのIDによる分岐なし。
ハチ固有のプロフィール、時期、台詞、条件、効果はSatoHachiObservationFactoryに集約。検証コードとメニューのハチ入口のみがこのFactoryを参照する。
汎用動詞を中心にしたActionIdを使用。特殊姿勢はHARNESS_FREEZE / HARNESS_LOW_WALKのみ。

## 10. 32 Seed検証結果

Unity 6000.2.2f1でコンパイルおよびSatoHachiVerification.RunBatchがPASS。
Seed 0～31それぞれで基準選択・全日SKIP・提示された選択肢からの別選択を実行（864日）。基準選択は再実行して288日分の再現性も確認。
さらにShort TripのDAY10選択→DAY11朝反映は境界APIのみ検証し、DAY10以降の物語イベントは作成していない。

検証項目:

- 全選択パターンでDAY2 Visiting、DAY3 LivingTogether、その後の同居維持。
- DAY3の受容／適応／警戒の独立性。
- タワー効果の翌日反映、室内利用候補増加と外出要求候補の存続。
- 初回装着と不確定な調査結果、嫌悪確定フラグなし。
- 経路調査の3情報とShort Tripの発見・解禁。
- キャリーのWorld Stateだけを欠くケースの拒否と、誤った解禁通知がないこと。
- CAT REPORTの一回性、日次ACTION制限、ACTION後の通常イベントなし。
- ログ時刻順、通常行動1～3件、同じSeedでの結果再現。
- タワー設置を一日遅らせたケースでも、道具の準備と初回装着へ進めること。

## 11. ACTION候補の偏り

ハチ32 Seed×3方針で、利用可能な調査が提示から外れる期間は最大1日。
別途、未使用の工作4件と調査3件が競合し続けるケースで、全7件が3日以内に提示されることを検証。
一日に見える候補は最大3件＋SKIP。完了済み・未発見は省く。基準選択が毎日実際の表示候補に含まれることも検証。

## 12. SatoSuzu回帰

既存のVerifyMultipleSeeds（5 Seed、30日、同居InvariantとNNN ACTION検証を含む）はPASS。
NNNObservationBatchRunner.RunStressTestは1000 Seed・30,000日でPassed 1000 / Warnings 0 / Failed 0、ログ順序違反0。
スズのFactory、候補猫評価、候補3匹選出、CaseGenerator、プロフィール生成には変更なし。

ログ: `Logs/SatoHachiVerification.log`, `Logs/SatoHachiSuzuStress.log`。

## 13. 表現しにくかった仕様

- 旧ACTION条件はKnowledgeだけだったため、キャリー所持や初回装着の観察を実行・発見条件にできなかった。既存条件の再利用で対応。
- 工作優先の固定ソートでは、未選択工作が調査を隠し続ける。種類別固定枠ではなく提示履歴で対応。
- 単発Coreの短い期限と一日一件のMajor枠は、プレイヤーの遅い工作で競合する。安全な方法の検索を期間内の優先Milestoneにして、タワー利用に枠を取られて道具の準備が永久にロックされる経路を防いだ。
- ハーネスへの慣れを連続的に評価するモデルはない。今回は初回・再試行という別イベントと不確定Knowledgeで表現。
- イベントをまとめて一日3～5シーンへ見せる製品UIは未実装。検証画面では通常1～3＋Major1をイベント単位の場面として示し、CAT REPORTを加えて基準進行を3～5場面にする。

## 14. DAY10以降へ進む前の課題

- プレイヤーが遅く調査・工作したときのCore期限を、絶対日付だけでなく前提成立からの猶予で扱うか検討する。今回、遅い選択ではDAY9までに基準進行を完了しない場合があり、SKIPを強制的に巻き戻してはいない。
- Short Tripの準備フラグから、移動・現地の反応・帰宅までの複数結果を設計する。準備＝成功にはしない。
- ハーネスへの適応は一回の低姿勢や一歩で確定しない。試行履歴・状態の必要量を次の具体的な出来事で検証する。
- 世界条件を追加／撤去したあとの工作再提示、発見と条件不成立の表示を長期ルートで評価する。
- ACTION候補の提示履歴はRuntimeのみ。セーブ完成時にはこの履歴と保留工作・DAYフェーズも保存対象にする。
- EndDayは既存バッチ互換のため未選択ならSKIPまで進める。今回の画面は明示フェーズで操作するが、製品UI導入時に誤った呼出を防ぐ境界を確認する。
- DispatchSituationDefinitionはDAY1～9に不要だったため未追加。今後、派遣前にRouteの導入イベントを選ぶ構成で検討可能。
