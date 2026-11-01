# PixelLab 거합 시안

PixelLab 무료 체험 계정의 웹 UI에서 생성한 정지 이미지다. 모든 최종 파일은 32×32 PNG이며 애니메이션은 없다. PixelLab Reduce colors에서 재질별 기본색·그림자색 팔레트로 정리했다. 가공 전 생성물은 Originals/에 보관했다.

| 파일 | 용도 | 최종 색 수(투명 제외) |
|---|---|---|
| Player-IaiStudent.png | 캐주얼 여성 거합 검사 | 11 |
| Enemy-Slime.png | 슬라임 | 5 |
| Enemy-PebbleSpirit.png | 조약돌 정령 | 5 |
| Tile-MeadowGrass.png | 초원 잔디 | 2 |
| Tile-MeadowDirt.png | 초원 흙 | 2 |
| Tile-AcademyHall.png | 학교 중앙 홀 바닥 | 2 |
| Object-VoidEntrance.png | 홀 중앙 공허 입구 | 5 |
| UI-IaiPanel.png | 공통 UI 패널 | 7 |

무료 계정의 Map tools는 사용할 수 없어 초원은 연결형 Wang/오토타일 세트 대신 잔디·흙 단일 타일을 각각 생성했다. 경계·모서리 타일은 별도 작업이 필요하다. 홀은 바닥 타일과 입구 오브젝트이며 완성된 홀 맵은 아니다. 캐릭터의 중앙 28×28 여백과 외곽선 두께는 생성 지시와 일부 차이가 있으므로 최종 아트 확정 전에 수작업 검수가 필요하다.

Unity 설정: Sprite, PPU 32, Point 필터, mipmap 비활성화, 압축 없음. UI 패널에는 6px 9-slice 경계를 지정했다. TestStage에 캐릭터·초상화·슬라임·조약돌 정령·바닥·공허 입구·카드/가방/버튼 패널을 실제 적용했다. 게임은 학교 중앙 홀에서 시작하며 공허 입장과 학교로 귀환 버튼으로 홀과 전장을 전환한다.

기획·카드 구현: Docs/IaiConcept-v3.md와 Docs/PlayablePrototype.md.
입력 프롬프트·팔레트: Docs/PixelLab-Iai-prompts.json.
픽셀 검사: Docs/PixelLab-Iai-assets.json.
생성 화면: Docs/PixelLab-Iai-gallery.png.
검증: Logs/IaiCombat-check.log, Logs/IaiFinal-check.log.
