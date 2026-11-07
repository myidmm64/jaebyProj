# 인게임 아트 방향

- 화면 기준: 1920×1080 Game 뷰. 전투 영역에 지면이 보이고 발밑 여백을 확보하도록 구성.
- 공통 팔레트: 짙은 자주색 외곽선, 회보라색 돌 프레임, 따뜻한 상아색 종이, 이끼색 실행 버튼. 계열·종류만 연한 보라·금색·산호·초록으로 구분.
- 폰트: Galmuri11 본문 / Galmuri11 Bold 제목·카드 이름·숫자. 본문 24px 중심, 작은 정보 18px, 제목 36px 이상. 얇은 1px 그림자와 본문 행간 적용.
- 폰트 출처: https://github.com/quiple/galmuri · SIL OFL 1.1 원문을 Assets/04.Fonts/Galmuri-LICENSE.md에 동봉.
- 지면: 플레이어와 슬라임은 지면에 고정하고 발밑 그림자를 배치. 돌 정령은 고정된 지면 그림자 위에서만 부유.
- 원래 BattleBackground.png는 보존. 인게임은 BattleBackdrop.png를 사용하며 BackgroundCover가 화면을 채움.
- 배경만 built-in ImageGen으로 1회 편집. UI 도형과 그림자는 Aseprite CLI로 직접 제작.
- 플레이 기능 검증과 빌드는 수행하지 않음.

## Background Edit Prompt

Edit this pixel-art game battle background for a magical-school katana deckbuilding game. Keep the ruined greenhouse courtyard, plum/violet void portal, warm windows, moss greenery and pink flowers, and the same general visual identity. Recompose it specifically for the TOP HALF of a 1920x1080 game screen: the bottom half will be covered by card UI. Therefore the distant architecture's ground line MUST be at 30 percent of the full image height from the TOP (not in the middle); the entire lower 65 percent is flat open mossy grass battle ground. The central grass area around 40 percent image height must be empty and unobstructed so characters can stand there visibly above the card UI. Put the violet portal near upper right at 12 percent height, distant greenhouse and arched walls in the upper 8-32 percent, small edge foliage only. Preserve believable perspective without squashing buildings. Maintain a wide 16:9 composition. Simplify textures to match chunky chibi pixel sprites: clean hard square pixel clusters, thick dark plum outlines on foreground forms, just base and shadow tones per material, muted mauve stone, parchment warm highlights, olive grass, restrained violet magic. No blur, no gradients, no photographic texture, no text, no UI, no units or monsters. A clean cohesive production game backdrop with large quiet areas, easy silhouette readability. The original composition must change to bring the ground substantially higher; do not simply reproduce the original horizon.

## 초기 인게임 화면 다듬기

- 실제 1층 1턴 편성 화면만 관찰. 전투 실행·카드 조작·게임 진행·빌드는 하지 않음.
- 적 이름의 검은 패널을 덜고 몸체 위에 한 줄로 정렬.
- 슬라임 몸체만 0.2만큼 내려 발밑 그림자와 접지. 체력 UI 위치는 유지.
- 집중·스테이지·코스트·궁극기는 굵은 픽셀 서체로 구분.
- 시퀀스 카드 설명 영역을 208×130에서 224×154로 확장하고 최소 본문 크기를 22px로 조정.
