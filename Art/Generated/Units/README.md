# 유닛 스프라이트 규격

모든 캐릭터·적의 논리 픽셀은 같은 월드 크기(1/32)를 사용한다. 게임용 PNG는 논리 원본을 Nearest Neighbor로 정수 4배 확대하고 Unity의 Pixels Per Unit을 128로 설정한다. Transform 기본 배율은 1이다. 이미지 해상도 때문에 유닛 체격이 커지지 않는다.

| 대상 | 논리 원본 | 게임용 PNG | PPU |
|---|---|---|---|
| PlayerSwordswoman | 64×96 | 256×384 | 128 |
| 일반 적 12종 | 64×64 | 256×256 | 128 |
| 보스 3종 | 96×96 | 384×384 | 128 |

- Unity 파일: `Assets/02.Sprites/Units/*.png`. Point 필터, 압축 없음, Mip Map 없음, Single Sprite, 알파는 0/255.
- 편집 원본: 이 폴더의 `.aseprite`와 `*Logical.png`. 정수 확대 파일을 다시 축소해 편집하지 않는다.
- 플레이어는 사용자가 선택한 갈색 단발·벽돌색 외투 디자인을 유지한다.
- 적과 보스는 각각 별도 ImageGen 원본을 사용한다. 원본·프롬프트는 `Art/Generated/Enemies`에 보관한다.
- 원본 캔버스는 트리밍하지 않는다. 크기와 위치를 맞춰 논리 캔버스에 배치하고 제한 팔레트, 불투명 가장자리, 픽셀 외곽선을 적용한다. 종별 체격은 서로 다르지만 픽셀 단위는 같다.
- Pivot은 발 아래 첫 좌표이며, 표시 높이는 `Assets/GameAssets.asset`의 EnemyHeights/BossHeights와 연결한다. 보스 전용 Sprite 배열은 BossArt다.
- `Import.csv`는 확대 후 크기·PPU·발 아래 여백·불투명 높이를 기록한다.
- 내보내기는 기존 `PrepareUnits.lua`를 Aseprite CLI로 실행한다. 편집용 Aseprite 파일은 논리 크기를 유지하므로 반복 실행해도 계속 커지지 않는다. 별도 Unity 에디터 도구는 없다.

`Preview.png`는 플레이어·일반 적·보스의 공통 픽셀 배율 비교용 이미지다.
