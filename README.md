# NNN（ニャンニャンネットワーク）候補抽出テスト

案件生成と猫候補抽出だけを検証する、ゲーム本体を含まない最小Unityプロジェクトです。

## 対応Unity

- Unity 2022.3 LTS（2022.3系の任意の新しいパッチを推奨）
- Built-in Render Pipeline。追加パッケージは不要です。
- シーン作成は不要です。Play開始時にデバッグUIが自動起動します。

## 開き方

1. Unity Hubで `NNN_CandidateTest_Unity` フォルダを「Add project from disk」から追加します。
2. Unity 2022.3 LTSで開き、スクリプトのインポート完了を待ちます。
3. 空のシーン、またはUnityが最初に開いた任意のシーンでPlayを押します。

`ProjectVersion.txt` は 2022.3.62f1 を指定していますが、手元の2022.3 LTSの別パッチで開いて構いません。初回にUnityがProjectSettingsを補完する場合があります。

## テストデータの生成

初回インポート後、Editorスクリプトが以下へScriptableObjectアセットを自動生成します。

```text
Assets/Resources/NNNTestData/
  Cats/       猫10匹
  Traits/     性格・生活タグ
  Case/       田中案件の人間、生活、住居、問題
  Benchmarks/ H01〜H10の人間、Lifestyle、住居、固定定義
  Tanaka_TestProfile.asset
```

自動生成されない場合は、Unityメニューの `NNN > Test Data > Generate If Missing` を選びます。初期値へ作り直す場合は `NNN > Test Data > Rebuild All` を選びます。

生成済みアセットがなくても、Play時には同じ内容をメモリ上に構築するフォールバックがあります。

## Human Benchmarkの使い方

画面上部の `H01 ... ▼` を押すとH01〜H10を直接選択できます。`< Prev` / `Next >` でも前後へ切り替えられます。選択した人間について10匹すべてを評価し、`CandidateSetBuilder`が「最も迷える3匹」をセットとして選びます。

- `Run 10 Benchmarks`: H01〜H10を同じSeedで一括評価し、画面とUnity ConsoleへCSV形式で表示
- `Export CSV`: 同じ結果をUTF-8 CSVとして `Application.persistentDataPath/NNN_HumanBenchmarks.csv` へ保存
- Seed入力欄 + `Use Seed`: 上位セットからの重み付き抽選を同じ条件で再現

ベンチマークは`CaseGenerationProfile.HumanBenchmarks`にのみ格納され、`HumanArchetypes` / `Lifestyles` / `Residences`のランダム案件候補には追加されません。各定義の`CreateCase`が固定条件を直接案件化します。

## 固定ベンチマーク10件

| ID | 人間 | 条件 | 住居 |
|---|---|---|---|
| H01 | 田中 健一 34 | 在宅・夜型・干渉少・改善余地・単調 | 2LDK / 55 / 65 / 25 |
| H02 | 佐藤 美咲 29 | 留守多・規則的・世話焼き | 1LDK / 45 / 45 / 20 |
| H03 | 山本 和夫 68 | 在宅・静か・規則的・干渉少 | 戸建 / 70 / 40 / 15 |
| H04 | 鈴木 彩 25 | 在宅・寂しがり・世話焼き・接触多 | 1K / 30 / 50 / 20 |
| H05 | 高橋 直樹 41 | 留守多・夜型・干渉少 | 2LDK / 60 / 75 / 25 |
| H06 | 伊藤 由美 38 | 神経質・静か・在宅 | 1K / 28 / 35 / 15 |
| H07 | 中村 浩二 32 | 引きこもり・単調・改善余地・寂しがり | 1LDK / 50 / 60 / 20 |
| H08 | 小林 麻衣 36 | 活動的・遊び好き・規則的・猫経験あり | 戸建 / 80 / 75 / 30 |
| H09 | 加藤 修 52 | 不規則・干渉少・猫経験あり・夜型 | 2DK / 48 / 70 / 20 |
| H10 | 吉田 恵 44 | 在宅・世話焼き・寂しがり・規則的 | 戸建 / 75 / 65 / 15 |

住居欄の数値は `Space / Vertical / EscapeRisk` です。

## 期待傾向

これは合否を猫名で固定するテストではなく、人間条件によって順位が変わるかを見るバランス検証です。

- H01: クロのSlowBuild、トラのTransformativeが強い基準案件
- H02 / H05: サバ・ゴマなど自立猫が上がる
- H03: 静かなシロがStable上位へ来る余地がある
- H06: トラ・ハチ・チャチャなど活発猫が狭さと神経質さで下がる
- H07: トラだけでなくハチ・モモ・ソラもTransformativeを競う
- H08: 活発猫が「変革」ではなく既存生活へのStable適応として評価される
- H09: 不規則生活にAdaptabilityが効き、SlowBuildがクロ固定にならない
- H10: ミケとモモがStableで競う

全案件でSlowBuildがクロ、Transformativeがトラのままなら、Selectorのランダム性ではなくEvaluatorの人間×猫の係数を見直す目安です。

## 実行とSeed操作

- `Generate`: 新しいSeedで再生成・再評価
- `Same Seed`: 現在と同じSeedで再現
- Seed入力欄 + `Use Seed`: 指定した整数Seedで再現

画面には次が表示されます。

- 生成された人間、年齢、住居、生活、問題
- Stable / SlowBuild / Transformative の役割別候補3匹（同じ猫の重複なし）
- Activity / Sociability / Independence / Adaptability とタグ
- Environment / Lifestyle / Human / Risk / Viability
- Stable / SlowBuild / Transformative の全Role Score
- 主な加点・減点理由
- 全10匹の比較用スコア一覧

Seedは設計スコアに小さな決定的揺らぎを加えるために使います。同じSeedなら候補結果も同じです。

## 固定の田中案件

- 田中 健一、34歳
- 在宅、夜型、干渉しすぎない
- 生活改善余地、単調な生活
- 2LDKマンション
- Space 55 / Vertical 65 / EscapeRisk 25

## 猫データと期待傾向

10匹は会話で決めた4能力値とタグをそのまま収録しています。評価式の狙いは次の候補帯です。

- Stable: ミケ / ソラ付近
- SlowBuild: クロ / ゴマ付近
- Transformative: トラ / ハチ付近

必ず同じ3匹に固定するテストではありません。Seedの小さな揺らぎ、Risk、役割間の重複排除によって近い候補へ変わることがあります。チャチャはTransformative要素が高い一方、`#脱走傾向` と住居のEscapeRiskによる減点を確認するための高リスク要員です。ソラは万能型が毎回選ばれすぎないかを見る比較対象です。

## コード構成

```text
Assets/Scripts/NNN/
  Data/        ScriptableObject定義、固定テストデータFactory
  Generation/  CaseGenerator
  Evaluation/  CatCandidateEvaluator、CatCandidateSelector
  UI/          自動起動するIMGUIデバッグ画面
  Editor/      ScriptableObjectアセット自動生成
```

評価係数は `CatCandidateEvaluator.cs`、三匹セットの構築とSeed抽選は `CandidateSetBuilder.cs` に分離しています。旧 `CatCandidateSelector.cs` はデバッグ比較と全猫評価APIのために残していますが、メイン候補には使用しません。

## CandidateSetBuilder（メイン候補選出）

`CatCandidateEvaluator`が出したEnvironment / Lifestyle / Human / Risk / Viability / Stable / SlowBuild / Transformativeを変更せず利用します。Viability 25以上の猫から三匹の全組み合わせ（10匹すべて通過時は120セット）を列挙し、次の式で評価します。

- Set Viability: 最低Viability 60% + 三匹の平均40%。一匹だけ成立性が低いセットを抑える
- Diversity: 三匹のStable / SlowBuild / Transformative三次元ベクトル間距離の平均
- Distinctiveness: 各猫の最高Role Scoreと2位との差の平均
- Tension: 三匹のViability最大差が小さいほど高得点
- Role Coverage: 各猫の最高Roleが3種類なら+15、2種類なら+7、1種類なら0
- Total: `Viability 35% + Diversity 30% + Distinctiveness 20% + Tension 15% + RoleCoverage bonus`

Stable / SlowBuild / Transformativeを一匹ずつ強制はしません。全セットをTotal降順で並べ、上位5セットからSeed付きの重み抽選を行います。重みは1位とのスコア差に対して指数的に下がるため、僅差なら顔ぶれが揺れ、大差なら上位が選ばれやすくなります。

デバッグUIには選ばれた三匹それぞれのS/L/T/Viability、セット評価5項目、Total、Evaluated Sets、Selected Rank、Seedを表示します。ベンチマークCSVは `Human, CatA, CatB, CatC, SetScore, Viability, Diversity, Distinctiveness, Tension, RoleCoverage, SelectedRank` を出力します。Trait Diversityなどの追加評価はまだ含めていません。

## 2026-08-25 組み合わせ評価の調整

猫自身の能力だけでRoleが固定されないよう、Roleの基礎式を圧縮し、猫・人間・住居の相互作用を主な加減点へ変更しました。SelectorのSeed揺らぎは従来どおり最大±3.0のままで、分散のための乱数強化はしていません。

- Stable基礎: `Viability×0.42 + Adaptability×0.14 + Sociability×0.07 - Risk×0.18`
- SlowBuild基礎: `Viability×0.34 + Independence×0.16 + (100-Sociability)×0.07 + #臆病10 - Risk×0.10`
- Transformative基礎: `Viability×0.30 - Risk×0.22 + Activity>=70なら10 + Sociability>=70なら5`

主な組み合わせ加減点は次のとおりです。

- Stable: `#静かな生活×#静か +25`、`#世話焼き×#甘えん坊 +20`、`#寂しがり×高Sociability +20`、`#留守多×高Independence +25`、`#留守多×高Sociability -20`
- SlowBuild: `#干渉しすぎない×#臆病 +25`、`#猫経験あり×#臆病 +15`、`#留守多×高Independence +20`、`#神経質×#臆病 -20`、`#接触多×#臆病 -25`
- Transformative: `#単調な生活×#活発 +25`、`#引きこもり気味×#遊び好き +25`、`#生活改善余地×高Activity +20`、`#活動的×#活発 -20`、`#神経質×高Activity -25`、`Space<40×高Activity -25`

さらに、既存の `#規則的`、`#不規則`、`#猫経験あり`、`#活動的`、`#遊び好き` を使い、規則的な生活と馴染みやすい遊び好き、不規則生活と自立性、活動的な人と臆病猫などの相性も評価します。各発火ルールはデバッグUIのReasonsへ数値付きで表示されます。

### 旧Role固定Selectorの比較結果（固定Seed 20260825）

| ID | Stable | SlowBuild | Transformative |
|---|---|---|---|
| H01 | ソラ 58.1 | クロ 83.7 | トラ 83.8 |
| H02 | ゴマ 81.7 | サバ 73.6 | ハチ 46.5 |
| H03 | シロ 89.0 | クロ 81.4 | ソラ 45.2 |
| H04 | ミケ 100.0 | ゴマ 58.0 | ソラ 44.1 |
| H05 | ゴマ 77.7 | クロ 100.0 | ハチ 49.4 |
| H06 | シロ 81.0 | ゴマ 51.2 | ソラ 32.2 |
| H07 | ソラ 78.1 | クロ 55.5 | トラ 100.0 |
| H08 | トラ 75.9 | サバ 71.0 | ソラ 56.6 |
| H09 | ゴマ 69.2 | サバ 81.2 | トラ 36.7 |
| H10 | ミケ 100.0 | サバ 55.2 | ソラ 57.2 |

この表は調整時の旧方式を比較用に残したものです。メイン候補は上記CandidateSetBuilder方式です。`NNN > Test Data > Export Benchmark CSV` は固定Seedでプロジェクト直下の新方式 `NNN_HumanBenchmarks.csv` と、全猫の比較用 `NNN_HumanBenchmarks_AllScores.csv` を再生成します。

## コードコメント

すべてのデータ型、生成・評価・選抜処理、UIとEditor処理に日本語のXMLドキュメントコメントを付けています。各メンバ変数にも、保持する値、値の向き（高いほど良い／低いほど良い）、利用箇所が分かるコメントがあります。評価式の直前には係数の目的も記載しているため、企画上の狙いを確認しながら調整できます。
