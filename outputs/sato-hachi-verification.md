# SatoHachi DAY1–11 verification

## Guided seed 0

### DAY 1

Scene: NORMAL_WATCH_HUMAN

- CAT_LOOK: ハチが佐藤の手元を見る。

Scene: NORMAL_GROOM

- CAT_GROOM: ハチが前足を舐める。

Scene: NORMAL_HUMAN_PAUSE

- HUMAN_LOOK: 佐藤が手を止め、ハチのいる方を見る。

Scene: VISIT_FIRST_CONTACT

- CAT_APPROACH: ハチが佐藤へ近づき、差し出されていない手の匂いを嗅ぐ。
- HUMAN_CROUCH: 佐藤が足を止めてしゃがむ。ハチはその場に残る。


CAT REPORT: 「そばまで行った。」

ACTION: SKIP

### DAY 2

Scene: NORMAL_GROOM

- CAT_GROOM: ハチが前足を舐める。

Scene: REL_ENTER_HOME

- CAT_WALK: 開いた玄関からハチが入り、壁沿いを歩く。
- HUMAN_PLACE: 佐藤が水と食器、トイレ、寝床を離して置き、窓と危険な隙間を確かめる。


CAT REPORT: 「中、見てきた。」

ACTION: SKIP

### DAY 3

Scene: NORMAL_WATCH_HUMAN

- CAT_LOOK: ハチが佐藤の手元を見る。

Scene: NORMAL_HOME_USE

- CAT_EAT: 食事を終えたハチが水を飲む。トイレを使い、寝床へ戻る。

Scene: NORMAL_HUMAN_PAUSE

- HUMAN_LOOK: 佐藤が手を止め、ハチのいる方を見る。

Scene: REL_START_COHABITATION

- HUMAN_PLACE: 佐藤が翌朝の食事を用意し、ハチの寝床の脇を空ける。
- CAT_REST: ハチが寝床で丸くなる。玄関で音がすると顔を上げる。


CAT REPORT: 「ここで寝る。」

ACTION: SKIP

### DAY 4

Scene: NORMAL_HOME_REST

- CAT_REST: ハチが家の寝床へ戻り、横になる。

Scene: REL_OUTSIDE_REQUEST

- CAT_MEOW: 寝床から起きたハチが玄関へ行き、ドアを見て鳴く。
- HUMAN_DOOR: 佐藤がドアへ手をかけ、外の車を見て閉めたまま手を戻す。


CAT REPORT: 「外、行きたい。」

ACTION: INVESTIGATE_INDOOR_ACTIVITY

食事・水・寝床・トイレは利用している。休んだあとも玄関へ向かう。高低差を使える場所は少ない。活動や探索の不足、以前の生活圏への関心が考えられ、まだ理由は絞れない。

### DAY 5

Scene: NORMAL_OUTSIDE_REQUEST

- CAT_MEOW: ハチが玄関へ歩き、ドアを見て短く鳴く。

Scene: REL_INDOOR_PLAY

- HUMAN_HOLD: 佐藤が紐のおもちゃを小さく動かす。
- CAT_PAW: ハチがおもちゃを前足で追う。遊び終えると玄関の方を見る。


CAT REPORT: 「今日は遊んだ。」

ACTION: OP_INSTALL_INDOOR_VERTICAL_ROUTE

### DAY 6

Scene: NORMAL_USE_TOWER

- CAT_JUMP: ハチがタワーの上段へ登る。

Scene: REL_USE_TOWER_AND_RETURN_TO_DOOR

- CAT_JUMP: ハチがタワーへ登り、窓辺を見渡す。
- CAT_MEOW: しばらく休んだあと、ハチが玄関のドアを見て鳴く。


CAT REPORT: 「高いところ、いい。」

ACTION: INVESTIGATE_FORMER_LIVING_AREA

以前は商店街の軒下や店の間を繰り返し通っていた。室内の寝床にも戻って休む。家を避けたいだけとは言い切れず、慣れた場所への関心も残る。

### DAY 7

Scene: NORMAL_VERTICAL_PATROL

- CAT_WALK: ハチが棚から窓辺までの巡回路を歩く。

Scene: NORMAL_LOOK_WINDOW

- CAT_LOOK: 高い場所でハチが外を眺める。

Scene: REL_SEARCH_SAFE_OUTDOOR_METHOD

- HUMAN_PHONE: 佐藤のスマホに猫用ハーネスとキャリーの商品が並ぶ。サイズ表を開き、ハチの身体を見る。


CAT REPORT: 「今日はここで休んだ。」

ACTION: OP_PREPARE_SAFE_OUTDOOR_GEAR

### DAY 8

Scene: NORMAL_USE_TOWER

- CAT_JUMP: ハチがタワーの上段へ登る。

Scene: NORMAL_LOOK_WINDOW

- CAT_LOOK: 高い場所でハチが外を眺める。

Scene: REL_FIRST_HARNESS

- HUMAN_HOLD: 佐藤が室内でハーネスを装着し、リードを緩めて待つ。
- HARNESS_FREEZE: ハチが足を止める。少し待って、身体を低くして数歩進む。
- HARNESS_LOW_WALK: 腹を低くしたまま寝床へ向かう。佐藤がハーネスを外す。


CAT REPORT: 「あれ、歩きにくい。」

ACTION: INVESTIGATE_HARNESS_RESPONSE

強い逃避反応は見られない。身体を拘束される違和感が強そうだが、嫌悪や適応可能性はまだ判断できない。

### DAY 9

Scene: NORMAL_USE_TOWER

- CAT_JUMP: ハチがタワーの上段へ登る。

Scene: NORMAL_VERTICAL_PATROL

- CAT_WALK: ハチが棚から窓辺までの巡回路を歩く。

Scene: REL_HARNESS_FEW_STEPS

- HUMAN_CROUCH: 佐藤がハーネスをつけたハチの前でしゃがみ、リードを緩める。
- CAT_WALK: ハチが数歩進んで止まる。佐藤が待つと、さらに一歩進む。

Scene: NORMAL_OUTSIDE_REQUEST

- CAT_MEOW: ハチが玄関へ歩き、ドアを見て短く鳴く。


CAT REPORT: 「今日は、あれでも歩けた。」

ACTION: INVESTIGATE_SHOPPING_STREET_ROUTE

距離は約600mで徒歩圏。住宅街→生活道路→大通り→商店街と続く。大通りはキャリー移動が必要。店の間には猫しか通れない細道もあり、現地でも自由移動は難しい。全行程のハーネス徒歩は安定しない。

### DAY 10

Scene: NORMAL_MORNING_WALK

- CAT_WALK: ハチが棚の脇から窓辺へ歩く。

Scene: NORMAL_MORNING_LOOK

- CAT_LOOK: ハチが窓の外を見る。


CAT REPORT: 「外、行きたい。」

ACTION: OP_SHOPPING_STREET_SHORT_TRIP

### DAY 11

Scene: DEPARTURE

- CAT_WALK: ハチが佐藤宅でキャリーへ入る。佐藤が扉を閉めて確かめる。
- HUMAN_HOLD: 佐藤がキャリーを持ち、生活道路と大通りを渡る。ハチは中にいる。

Scene: ARRIVAL

- HUMAN_PLACE: 商店街の車が入らない場所で、佐藤がキャリーを置く。
- HUMAN_HOLD: 佐藤がキャリーの中でハーネスを装着し、リードをつないでからハチを外へ出す。
- CAT_WALK: ハチが店先の匂いを嗅ぎ、歩き始める。

Scene: MISMATCH

- CAT_WALK: ハチが迷わず店と店の間の細い隙間へ向かう。
- HUMAN_STOP: 佐藤の肩は隙間を通らない。佐藤がその手前で止まり、ハチを追って進めない。
- CAT_MEOW: リードの先でハチが止まる。隙間の奥を見て短く鳴く。

Scene: RETURN

- HUMAN_PLACE: 佐藤がキャリーを開けて待つ。ハチが戻って入り、佐藤が扉を閉める。
- HUMAN_HOLD: 佐藤がキャリーを持って道路区間を戻り、家の中で扉を開ける。
- CAT_REST: ハチが家の寝床で横になる。しばらくして玄関の方へ顔を向ける。


CAT REPORT: 「あそこ、行けなかった。」

ACTION: SKIP

Observed Knowledge: KNOW_SHORT_TRIP_PARTIALLY_WORKS, KNOW_SHOPPING_STREET_ROUTE_MISMATCH


32 seeds × guided / skip / alternative policies: PASS (1056 days, plus 352 repeat days).
Guided Short Trip on DAY11: 32/32. No same-day trip, four scenes, return home, two observed knowledge tags and unresolved outdoor request: PASS.
Maximum consecutive days available Investigation was omitted: 1
Synthetic contention (4 Operations + 3 Investigations, no fixed kind slots): all appear within 3 days; reads are stable: PASS.
World-only carrier gate, delayed Short Trip, multiple route knowledge, CAT REPORT budget and SatoSuzu regression: PASS.
