# 게임 시작 구조

`GameManager.Start()` → `StartGame(stage)` → `Stage.Init()` → `Player.Init(stage, cell)` / `Enemy.Init(stage, cell)`.

`MonoSingleton<T>`는 `Awake`에서 전역 인스턴스를 등록한다. `GameManager.Instance`로 접근하며, 씬 전환 시 유지된다. 중복 오브젝트는 비활성화 후 제거하고, 원본이 파괴되면 인스턴스 참조를 해제한다. GameManager는 씬의 최상위 오브젝트에 둔다.

`TestStage.unity`의 기존 GameManager와 Stage, Player, Enemy 참조를 연결했다. 씬을 열고 재생하면 GameManager가 지정된 Stage를 초기화한다. 이후 다른 스테이지로 시작하려면 `GameManager.Instance.StartGame(nextStage)`를 호출한다.

Stage Inspector 설정:

- **Map Size**: 정사각형 논리 영역의 한 변 칸 수 n. 기본 8, 최소 2.
- **Cell Size**: 한 칸의 로컬 좌표 크기. 기본 1.
- **Player / Enemy**: 시작 시 배치할 씬 캐릭터.
- **맵 그리드**: 칸을 체크하면 사용 가능, 해제하면 사용 불가. 위쪽 행이 큰 y이며 왼쪽 아래가 `(0, 0)`이다. 전체 체크/해제와 Undo를 지원하며, 크기 변경 시 겹치는 좌표의 상태를 유지한다. 플레이 모드에서는 체크 편집을 잠근다.

좌표는 왼쪽 아래가 `(0, 0)`, 오른쪽 위가 `(n-1, n-1)`이며 전체 영역은 Stage의 Transform을 중심으로 잡는다. 플레이어는 체크된 칸 중 가장 왼쪽, 적은 가장 오른쪽의 서로 다른 칸에서 시작하며 중앙 행에 가까운 위치를 우선한다. 사용 가능한 칸은 최소 두 개가 필요하다. Stage 선택 시 Scene 뷰에 논리 그리드가 표시되고 사용 불가 칸은 빨간색으로 표시된다.

`Player`와 `Enemy`는 `GridObject`를 상속한다. 공통 `Stage`, `Cell`, `Init`, `SetCell`로 그리드 위치를 관리하며, `SetCell`은 체크된 칸인지 확인한 뒤 그리드 좌표와 Transform 위치를 함께 갱신한다. `MapBounds`, `ContainsCell`, `IsCellEnabled`, `GetCellCenterWorld`로 영역과 월드 좌표를 조회한다.

이 설정은 시작 배치와 논리 영역을 정의한다. 현재 그려진 지형 및 흰 테두리 타일맵을 다시 그리지는 않는다. 큰 n을 시험할 때는 타일맵도 해당 영역까지 넓혀 사용한다.

검증: Unity 6000.6.4f1 플레이 모드에서 자동 초기화, 씬 참조, n=2/5/8/12, 칸 크기 변경과 이동된 Stage의 좌표, 반복 초기화, 범위 밖 좌표 거부, 필수 캐릭터 누락 거부, 싱글톤 중복 제거·씬 전환 유지·파괴 후 참조 해제를 확인했다.
