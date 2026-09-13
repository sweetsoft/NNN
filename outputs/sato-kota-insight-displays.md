# SatoKota Guided 表示一覧

Seed 42。各日のAction前の観察とInsight。

| DAY | Event / Scenes | CAT REPORT | UPDATE | CHANGE | QUESTION | ACTION |
|---|---|---|---|---|---|---|
| 1 | KOTA_FIRST_CONTACT / 1 | 近く、平気。 | コタは自分から佐藤に近づき、脚に頬を寄せた<br>佐藤がしゃがんでも、そばに残った | NEW 佐藤との接触が始まった | コタはこの人と、どこまで近づける？ | SKIP |
| 2 | KOTA_ENTER_HOME / 2 | 中、面白い。 | コタが佐藤宅へ入り、家具の周りを探索した<br>佐藤は食事・水・トイレ・寝床を準備した | NEW 室内へ入った | コタはこの家をどう使う？ | SKIP |
| 3 | KOTA_START_COHABITATION / 2 | ここ、好き。 | 佐藤が翌朝の世話を準備した<br>コタは家の寝床で休んだ | ↑ 滞在から同居へ進んだ | 一緒に暮らすと、どんなことが起きる？ | SKIP |
| 4 | KOTA_DESK_INTERRUPTION / 2 | あそこ、面白い。 | コタは作業机へ何度も近づいた<br>机の小物を前足で落とした | NEW 同居後の生活トラブルを確認 | なぜコタは作業中の机へ何度も来る？ | INVESTIGATE_ACTIVITY_PATTERN |
| 5 | KOTA_EVENING_PLAY / 2 | もっと遊ぶ。 | コタは夜に走り、棚へ登った<br>佐藤がおもちゃを動かすと強く反応し、そのあと休んだ | NEW 夕方以降に活発な行動を確認 | 遊ぶ時間を増やすと、夜の行動は変わる？ | OP_CREATE_PLAY_ROUTINE |
| 6 | KOTA_PLAY_ROUTINE_RESPONSE / 3 | いっぱい走った。 | 佐藤と遊ぶ時間が増えた<br>遊んだあとの走り回りは減った<br>それでも机には登っている | ↓ 夜の走り回りが減った<br>→ 机や棚への侵入は残っている | コタが机や棚へ登る理由は、遊びだけ？ | INVESTIGATE_VERTICAL_EXPLORATION |
| 7 | KOTA_VERTICAL_EXPLORATION / 4 | 高いところ、好き。 | コタは棚に登り、部屋を見渡した<br>棚から降りると、佐藤の机へ向かった |  | 登ってよい場所を増やすと、机への接近は変わる？ | OP_CREATE_ALLOWED_VERTICAL_ROUTE |
| 8 | KOTA_ALLOWED_ROUTE_RESPONSE / 3 | ここもいい。 | コタはタワーと空けた棚、窓辺を使った<br>そのあと、佐藤のキーボードの横へ座った | ↑ 登ってよい場所を使うようになった<br>→ 佐藤の作業机にも来ている | 高い場所があっても、なぜ佐藤の作業机へ来る？ | SKIP |
| 9 | KOTA_FOLLOW_HUMAN / 2 | 近くにいたい。 | コタは作業中の佐藤の近くで休もうとした<br>佐藤が移動すると、机を降りてあとを追った |  | 机そのものと、佐藤がいる場所。どちらが気になる？ | INVESTIGATE_HUMAN_PROXIMITY |
| 10 | KOTA_HUMAN_ADAPT_ENVIRONMENT / 2 | ここ、いい。 | 佐藤が壊れやすい小物を収納し、机横を空けた<br>コタはそばで見たあと、おもちゃを転がした | NEW 佐藤も生活空間の使い方を変え始めた | 佐藤のそばに猫用の場所があると、過ごし方は変わる？ | OP_CREATE_DESK_SIDE_CAT_SPOT |
| 11 | KOTA_SHARED_DESK_SPACE / 3 | ここ、いい。 | コタは机横の猫用スペースで休み、佐藤は作業を続けた<br>そのあと、作業中のペンを一本だけ落とした | ↑ 作業を続けながら、近くで過ごす場面ができた<br>→ 前足で物に触れる行動は残っている | 活発なコタと、この暮らし方をどう続ける？ | SKIP |

PASS: 1089 DAY / 33 Seed × Guided, SKIP, Alternative / no spoiler / invariance / partial experiments / trait ablation.
