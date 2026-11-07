# 캐주얼 검사

사용자가 선택한 `exec-8abb3967-f8b3-4fc3-b7fd-bcd4e662d755.png`를 적용했다. 갈색 단발, 입 없는 얼굴, 벽돌색 외투, 크림색 후드, 짧은 스커트와 운동화의 선택 버전이다.

- 생성: 내장 image_gen 도구. 아래 프롬프트로 생성한 선택 원본을 사용했다.
- 보존 원본: Art/Generated/Characters/PlayerCasualOriginal.png (1024×1536, 투명 PNG).
- 게임 스프라이트: Assets/02.Sprites/Units/PlayerSwordswoman.png (256×384).
- 편집 원본: Art/Generated/Units/PlayerSwordswoman.aseprite.
- Aseprite로 트리밍 없이 Nearest Neighbor 축소, 19색 정리, 반투명 가장자리 제거. 원본 PNG는 변경하지 않았다.
- 논리 원본은 64×96이며 Nearest Neighbor로 정확히 4배 확대해 저장한다. 플레이어 디자인은 변경하지 않는다.
- 모든 유닛은 128 PPU, Point 필터, 압축 없음, 바닥에 닿는 픽셀 기준 pivot과 기본 배율 1. 이미지와 PPU를 함께 4배로 올려, 논리 픽셀 1개의 월드 크기는 1/32로 통일한다. 체격 차이는 그림의 픽셀 수로 표현한다.
- 배경은 원래 채도·대비를 유지하고 SubduedBackdrop 머티리얼의 밝기만 0.9로 적용한다. 색조 보정은 하지 않는다.

선택 이미지 생성 프롬프트:

```text
Edit this single transparent game sprite. Preserve the pose, right-facing direction, sword, cream hoodie, chunky square pixel style, black pixel outline, overall 3.5-head proportion, shoes, framing and transparency. Make only these character design changes: replace all hair with a neat chin-length BROWN BOB haircut, absolutely no ponytail or long rear hair; replace trousers with a SHORT CHARCOAL PLEATED SKIRT and bare legs with small dark ankle socks; remove the mouth completely, smooth blank skin below the nose, eyes stay; recolor the teal outer jacket to muted warm BRICK RED / burgundy with just one shadow shade. The young adult human swordswoman wears casual clothes. Keep the pixel clusters large and simple as a 64x96 logical sprite, no tiny detail or soft gradients. Actual transparent background, no shadow or halo. One complete character only, no text, no additional poses.
```
