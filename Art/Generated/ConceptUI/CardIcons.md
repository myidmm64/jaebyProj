# Card Icons

Built-in ImageGen으로 생성한 30종 아이콘 시트. 왼쪽에서 오른쪽, 위에서 아래 순서로 CardCatalog ID 0–29에 대응한다.

- 원본: `Art/Generated/ConceptUI/CardIconsOriginal.png`
- 게임용: `Assets/02.Sprites/UI/CardIcons.png` (384×320, 셀 64×64, 6열×5행)
- Aseprite CLI에서 nearest-neighbor로 축소. 투명 배경, Point 필터, 압축·밉맵 없음. 수동 슬라이스 정보로 등록.
- `GameAssets.CardArt`와 `GameUI.Art`에서 가방·시퀀스·보상·상점·상세 설명에 공통 적용.
- 피해량 배지: `Assets/02.Sprites/UI/DamageBadgeFlat.png` (64×48). 검은 테두리 없는 단색 픽셀 UI 도형.

## Generation Prompt

Create ONE production game sprite atlas, transparent RGBA background, exactly 6 equal columns x 5 equal rows, no gutters, 1200x1000 canvas. Each cell contains ONE centered simple chunky pixel-art icon with 20% transparent padding, bold dark 1-2 logical pixel outlines, flat base + one shadow, hand-pixeled 64x64 visual density, no gradients/glow/texture/text/numbers/borders. Consistent cute eerie dungeon magical-academy katana theme, cream steel, dark plum, coral red, cyan, mint and violet accents. Keep silhouettes distinct and very simple. Row-major subjects in exact order: Row1: silver katana upward red slash; three parallel thrusting blades; wide horizontal crescent slash; small hammer striking an anvil; two crossed swords with red spark; swirling four red slashes. Row2: cyan orb shockwave; purple eye in a targeting ring; heavy red finishing blade; rapier tip and star; three footprints in a wind spiral; purple lightning storm orb. Row3: tied cream focus pouch with blue ember; sword scabbard with impact spark; open cyan paper fan; sheathed katana emitting white cutting flash; red clenched fist; short katana surrounded by mint breathing wind. Row4: half-drawn katana; blue rune ring with violet center; wooden sharpening stone; two echoing katana silhouettes; sword under horizontal crescent moon; three sweeping red sword arcs. Row5: small meditating violet spirit; compressed violet diamond; violet tidal wave; silver sword with pale blue moon arc; dark sword in a void halo; brilliant white katana with compact gold burst. All 30 cells aligned exactly to the regular grid, each icon fully contained within its own cell, no backdrop or checkerboard.
