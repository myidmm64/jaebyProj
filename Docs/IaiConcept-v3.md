# 거합·공허 탐험 기획 v3

이 문서는 폭렬·마나 충전 중심의 CardSystem-v2.md와 PrototypeCards.md를 대체하는 최신 콘셉트 기획이다. 카드 13종 이름·범위, 집중 스택, 코스튬 무기·궁극기는 프로토타입에 반영했다. 학교 홀과 초원 사이의 입장·귀환 및 몬스터 외형도 연결했다. 다계층 진행·보스는 추후 구현 범위다. 아래 수치·고유 명칭·몬스터 행동은 프로토타입 제안이며, 사용자 확정 세계관과 구분한다.

## 사용자 확정 세계관

- 주인공은 거합에 소질이 있다. 마법학교에 나타난 공허에 들어가 정복한다.
- 공허 내부는 여러 계층으로 구성되며 각 계층에는 보스가 존재한다.
- 런 종료 후 돌아오는 장소는 마법학교 중앙 홀이고, 홀 중앙에 공허 입구가 있다.
- 제1계층은 초원. 일반 몬스터는 슬라임과 조약돌 정령 두 종류다. 보스는 추후 기획한다.
- 기존 폭렬 계열을 거합 계열로 변경한다. 집중 스택을 쌓고 거합으로 한꺼번에 피해를 주는 전투다.

## 전투 흐름 및 유지 규칙

카드 세팅 → 슬롯 순서대로 이동·준비·공격 카드 실행 → 적 행동. 이동도 카드이며 2칸 이내 목적지를 노란 점에서 고른다. 공격은 오른쪽 고정이고 방향 선택은 없다. 카드 범위 그림은 현재 위치를 중앙에 표시한다.

공통·거합 두 계열을 사용한다. 시퀀스는 1턴 1칸에서 매 턴 1칸씩 개방하여 최대 6칸. 카드 2장 합성·★3 상한·남은 턴 초기화·자동 합성·Q/E 가방 페이지·정렬은 유지한다. 수급은 첫 턴 이후 마지막 카드 보상으로부터 2턴마다 3장 중 1장, 웨이브 클리어 때 추가 1장. 클리어 보상 직후 턴 카드 지급은 생략한다.

## 집중 스택 제안

- 기존 마나 충전 자원을 집중으로 대체한다. 상한 12, 같은 전투의 턴 사이에 유지, 전투 종료 시 초기화.
- 자세를 잡거나 칼집·짧은 베기로 집중을 얻는다. 명중 획득은 대상 수·타수와 무관하게 카드당 한 번.
- '거합' 소비 카드는 실행 직전 집중을 전부 소모한다. 일반 거합은 집중 1 이상과 유효 적이 모두 필요하며, 조건 미충족이면 자원·일회성 버프를 보존한다.
- 피해는 기본 피해 + 소비 집중 × 계수. 초반에는 기존 폭렬 수치를 출발점으로 삼아 테스트한다.
- 기존 공통 카드 '집중'은 '예리함'으로 바꾼다. 자원 집중과 다음 공격 고정 피해 버프를 구분한다.

## 카드 이름·범위 대응안

P=(0,0), 오른쪽=+x, 위=+y. '첫 적'은 x가 가까운 적부터 판정한다. 아래 범위는 캐릭터 이동을 동반하지 않는다. 주변 회전 공격을 전방 부채 베기로 바꿔 준비한 방향에서 검을 뽑는 느낌을 강화한다.

| ID | 이전 | 새 이름 | 계열/종류 | 범위 좌표 | 효과 제안 |
|---|---|---|---|---|---|
| 0 | 마력탄 | 견제 베기 | 공통/공격 | (1,0) | 앞 1칸 공격 |
| 1 | 집중 | 예리함 | 공통/서포트 | 자신 | 다음 공격 첫 타격 강화 |
| 2 | 관통탄 | 찌르기 | 공통/공격 | (1,0),(2,0) | 직선 2칸 모두 공격 |
| 3 | 속사 | 연속 베기 | 공통/공격 | (1,0) | 앞 1칸 2회 공격 |
| 4 | 불씨탄 | 칼집 치기 | 거합/공격 | (1,0) | 명중 시 집중 획득 |
| 5 | 응축 | 정심 | 거합/서포트 | 자신 | 적 없이 집중 획득 |
| 6 | 화염고리 | 반월 베기 | 거합/공격 | (1,-1),(1,0),(1,1) | 전방 3칸, 명중 시 집중 획득 |
| 7 | 잔불구슬 | 스쳐 베기 | 거합/공격 | (1,0),(2,0) | 첫 적 타격, 집중 획득 |
| 8 | 점화 | 검의 호흡 | 거합/서포트 | 자신 | 다음 집중 전량 소모 기술 강화 |
| 9 | 폭렬 | 거합·횡월 | 거합/공격 | (1,-1),(1,0),(1,1) | 집중 전량 소모, 가까운 적들을 넓게 벰 |
| 10 | 폭렬창 | 거합·일섬 | 거합/공격 | (1,0),(2,0),(3,0) | 집중 전량 소모, 직선 3칸 모두 공격 |
| 11 | 대폭렬 | 비기·단공 | 거합/궁극기 | (1,0), x=2에서 y=-1..1, x=3에서 y=-2..2 | 집중 전량 소모, 넓어지는 부채꼴 9칸 공격 |
| 12 | 이동 | 보법 | 공통/이동 | 맨해튼 거리 2 이내 | 유효 목적지로 이동 |

비기·단공은 기존 궁극기의 획득 턴 한정·1회용 규칙을 유지한다. 쓰지 않아도 턴 종료 시 사라지고, 집중 0에서도 기본 피해로 사용할 수 있다. 외형은 폭발 대신 한 번의 커다란 검격으로 표현한다.

## 첫 코스튬 제안

- 이름: 검술부 수련복. 외형: 마법학교 학생 제복과 허리의 도검, 모자·지팡이보다 검과 칼집을 실루엣의 중심으로 삼는다.
- 무기: 마력도. 턴당 첫 거합 계열 집중 획득에 코스튬 성급만큼 집중 추가.
- 궁극기: 비기·단공 카드 1장 획득. 코스튬 성급은 카드 성급에 반영.

## 계층·귀환 구조

중앙 홀 → 홀 중앙의 공허 입구 → 제1계층 초원 → 계층 보스 → 다음 계층. 런을 끝내면 중앙 홀로 돌아온다. 사망·중도 귀환·클리어의 보상 차이는 추후 결정한다. 홀의 상점·NPC·영구 성장 기능은 아직 확정하지 않는다.

중앙 홀은 단정한 석재 바닥, 학생용 문양과 기둥, 중앙의 어두운 보라색 원형 공허 입구로 대비를 만든다. 제1계층은 밝은 초원이지만 일부 땅과 몬스터의 작은 보라색 균열로 공허 내부임을 암시한다. 보스 외형·행동은 이번 범위에 추가하지 않는다.

| 몬스터 | 외형 | 행동 제안 | 전투 역할 |
|---|---|---|---|
| 슬라임 | 큰 초록 덩어리, 단순한 눈, 작은 보라색 핵 | 접근 후 다음 턴 인접 칸 공격 예고 | 집중을 모으면서 거리 조절 학습 |
| 조약돌 정령 | 회색 조약돌 몸통, 작은 돌 팔, 보라색 눈 | 제자리에서 직선 3칸 돌 던지기 예고 | 보법으로 사선을 벗어나 거합 위치 확보 |

두 몬스터 모두 추가 방어·분열·소환 규칙 없이 시작한다.

## PixelLab 공통 아트 지침

정지 이미지. 캐릭터·오브젝트는 32×32, 타일셋은 각 타일 32×32. 검정에 가까운 외곽선 1~2px, 재질마다 기본색과 그림자색의 2톤만 사용한다. 하이라이트·광채·안티앨리어싱·그라데이션·미세 질감은 제외한다. 캐릭터 기본 방향은 오른쪽. 학생은 남색·자홍, 슬라임은 초록, 돌 정령은 회색·보라로 구분한다. 바닥은 캐릭터보다 대비를 낮춘다. UI는 이전 참고 이미지의 어두운 자주색 바탕·주황 테두리·자홍 포인트를 유지한다.

### 공통 스타일 문장

Native 32x32 pixel art. Crisp square pixels and chunky readable shapes. Near-black outlines, 1 to 2 pixels thick on the native grid. Exactly two flat tones per material: one saturated base color and one dark shadow color. Upper-left lighting, no highlight colors. No anti-aliasing, gradients, blur, glow, dithering, fine texture, text, numbers, or animation frames.

### 플레이어

One young magic academy student with a talent for iaijutsu, single full-body idle sprite facing right, slightly viewed from above, non-isometric. Oversized head and short body. Dark navy school uniform, vivid magenta collar and waist sash, short black hair, one katana sheathed at the waist. One hand resting on the sword hilt, calm ready-to-draw stance. The sheathed sword is the main silhouette feature. Entire sprite including outline inside the central 28x28 pixels. Transparent background. Single character only. [Append common style.]

### 슬라임

One small meadow slime enemy, facing right, slightly viewed from above, non-isometric. Squat lime-green blob with a wide base, two large simple dark eyes and one small dark-purple core visible as a solid marking. Opaque body, cute but slightly eerie, bold simple silhouette, no particles or accessories. Entire sprite including outline inside the central 28x28 pixels. Transparent background. Single enemy only. [Append common style.]

### 조약돌 정령

One small pebble spirit enemy, facing right, slightly viewed from above, non-isometric. A squat round grey stone body, two chunky pebble arms and two tiny stone feet, simple violet eyes, one broad dark-purple crack. Friendly-looking but hostile, clearly smaller and simpler than a golem. No armor, weapon, moss or floating fragments. Entire sprite including outline inside the central 28x28 pixels. Transparent background. Single enemy only. [Append common style.]

### 초원 타일셋

Use Standard Top-Down tileset, 32x32 per tile. Lower terrain: muted green meadow grass, broad flat areas, sparse chunky grass marks, two flat tones only. Upper terrain: warm ochre dirt path, mostly clean flat ground, sparse large earthy shadow marks, two flat tones only. Boundary: simple stepped grass-to-dirt edge, thin dark contour only at the material boundary. Non-isometric, no cliff walls, no individual black frames on interior tiles, no objects or scenery, no highlights or gradients. Keep terrain contrast lower than character contrast.

### 중앙 홀 바닥 타일

One seamless 32x32 top-down square floor tile for the central hall of a magic academy. Large pale grey-violet stone slab shapes, restrained dark plum seams, simple geometric school architecture. Flat walkable floor only. Two flat tones per material, crisp hard pixels, low contrast, no perspective, no walls, furniture, portal, objects, text, highlights, gradients, glow or heavy texture. Seamless repeating edges, opaque background.

### 공허 입구

One circular void entrance embedded in the floor of a magic academy central hall, viewed directly from above. A chunky near-black stone ring surrounds a flat deep-purple opening. Three large angular violet fissures suggest impossible depth using solid flat shapes only. No actual light emission. Centered inside a 32x32 canvas with a 2-pixel transparent margin. Near-black 1 to 2 pixel outlines, two flat tones per material, hard square pixels. No stairs, doorway, room, characters, floating particles, letters, glow, gradients or semi-transparent effects. Transparent background outside the ring.

### UI 패널

One empty square pixel art UI panel for a magic academy iaijutsu roguelike. Flat front view, bold arcade poster styling. Near-black plum interior, vivid orange frame, one small hot-magenta angular sword-slash accent at a corner. Native 32x32 pixels, 2-pixel dark outer outline, stepped corners, one base and one shadow tone per material, no highlights. Nine-slice friendly: decorations confined to the outer 6 pixels, straight repeatable edges and a plain center for text added later. Transparent outside. No text, icons, logo, gradient, glow, texture or anti-aliasing.


## 최신 아트 적용 상태

기본 주인공 외형은 캐주얼한 크림색 후드·조거 팬츠·운동화 차림의 여성 거합 검사다. 검술부 수련복 코스튬의 별도 의상 이미지는 아직 없으며 이름·성급·무기·궁극기 규칙을 사용한다. 제공된 PixelLab 이미지들은 TestStage의 학교 홀·초원·유닛·초상화·UI에 연결했다. 연결 근거와 조작은 PlayablePrototype.md의 최신 절을 따른다.
