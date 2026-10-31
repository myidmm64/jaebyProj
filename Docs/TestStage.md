# 테스트 스테이지 / 맵 팔레트

기획 기준: [폭렬 마법사 — 턴제 가방 로그라이크 기획 · Codex 인계](https://app.notion.com/p/40aa1bc58c8e81919616c0c9b94029fa)

## 열기와 편집

1. `Assets/Scenes/TestStage.unity`를 연다.
2. `Window > 2D > Tile Palette`에서 원하는 팔레트를 선택한다.
3. Hierarchy의 `Test Stage - Forest Arena / Grid - 16px Art Tiles` 아래에서 편집할 타일맵을 선택하고 칠한다.

`Assets/6.TilePalettes`에 원본 시트별 팔레트 4개와 `Tiles` 폴더를 둔다. `SpringFarm`은 지형, `farm objects free`는 나무·울타리·건물, `farm bridges free`는 다리, `farm inside free`는 실내 타일이다. 원본 스프라이트의 좌표를 사용해 배치하므로 시트의 빈 공간과 오브젝트 조립 배치가 유지된다. 여러 칸짜리 오브젝트는 팔레트에서 필요한 영역을 함께 선택해 그린다.

원본 설정인 32 PPU와 16×16 슬라이스를 유지했다. 그림 타일 한 칸은 월드 0.5×0.5이며, 전투 논리 칸 크기는 아직 정하지 않았다.

## 맵 구성

- `01 Ground`: 잔디 바닥, 전체 40×28 그림 타일.
- `02 Arena and Entrance`: 중앙 약 24×16 그림 타일의 흙바닥 투기장과 남쪽 진입로.
- `03 Ground Details`: 외곽 풀잎과 작은 돌 장식.
- `04 Border Foliage`: 외곽 수풀과 나무 장식.
- `05 Dirt Grid Overlay`: `border.png`를 흙 위에 겹치는 독립 타일맵. `Grid - 32px Border Cells` 아래에 있으며, 한 칸은 월드 1×1(그림 타일 2×2)이다. 칸 전체가 흙인 곳에만 표시해 잔디로 선이 넘어가지 않는다. 맨 아래 진입로의 반 칸은 제외한다. Tilemap Renderer를 끄면 테두리만 숨길 수 있다.

카메라와 조명에 독립적인 픽셀용 Unlit 머티리얼을 포함한다. 레퍼런스 포인트의 Player Entry, Enemy A/B는 빈 오브젝트이며 캐릭터를 생성하지 않는다. 이동·충돌·턴·적 행동·카드·궁극기 규칙은 포함하지 않은 맵 전용 씬이다. 수풀과 나무에 이동 불가 판정은 없다.

기존 `Proto.unity`와 빌드 시작 씬 설정은 유지했다. 테스트 맵을 보려면 `TestStage.unity`를 직접 연다. 맵은 일반 Unity Tilemap으로 저장되어 플레이 모드 밖에서도 편집 가능하며, 재생할 때 재생성하지 않는다.

## 생성 및 검증 도구

`Assets/1.Scripts/Editor/TestStageMapBuilder.cs`는 에디터 전용 제작 도구다. `Tools > Explosive Wizard > Create Test Stage Map`은 결과물이 없는 경우에만 생성하며, 이미 있는 맵이나 팔레트는 덮어쓰지 않는다. 기존 결과물은 타일 팔레트와 씬에서 직접 수정한다.

`TestStageMapBuilder.Validate`는 모든 팔레트의 스프라이트 좌표, 씬의 레이어와 타일 참조를 확인하고 `Docs/TestStage-preview.png`를 렌더링한다. 배치 모드에서 검증할 수 있으며, 열린 작업 씬은 별도로 보존한 상태에서 실행한다. `AddDirtGrid`는 저장된 씬의 흙바닥을 기준으로 테두리 레이어만 갱신한다.
