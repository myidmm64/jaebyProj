# Impact order — 마무리 및 검증

## 적용

- 게임 제목과 Unity Product Name: Impact order.
- 설정에서 한국어 / English 선택. 선택을 저장하고 메뉴·HUD·카드·아이템·적 행동을 즉시 갱신한다.
- 카드 34종, 아이템 21종, 적 12종과 보스·행동 설명을 번역했다. 영어 숫자는 K/M/B로 표시한다.
- 디버그 카드 추가, F2 단축키, 클리어 미리보기 버튼과 관련 코드를 제거했다. 실제 오류를 기록하는 진단 로그는 유지한다.
- 텍스트 여백과 자동 크기 조정, 카드명 겹침, 봉인 문구 중복, 제목 화면 뒤의 전투 UI를 정리했다.
- 기존 Imporder·Zanshin 저장 파일은 읽기 전용 호환 경로로 유지한다. 새 저장은 Impact order 경로에 작성한다.

## 확인한 범위

- Unity 6000.6.4f1에서 변경 스크립트 컴파일 및 Proto 씬 플레이 진입.
- 실제 1920×1080 Game 뷰: 제목, 설정, 언어 전환·재시작 후 유지, 기존 저장 이어하기, 가방 관리, 카드 뒷면, 드래그 장착, Tab/Space, 시퀀스 실행, 적 행동 효과·툴팁, 카드 보상과 다음 웨이브.
- 언어 전환 후 기존 카드 이름·설명·버프·HUD가 갱신되는 것을 확인했다.
- `dotnet run --project Tests/CombatRules/CombatRules.csproj --no-restore`: 1,237개 검사 통과. 저장 데이터 왕복, 가방 순서/삽입, 시퀀스 우선 합성, 집중 초기화, 궁극기 만료, 마지막 턴 판정, 최종 클리어 무보상, 번역을 포함한다.
- 진행 구조 검사: 3개 시드 각각 30전투, 카드 보상 29회, 아이템 보상 11회, 상점 3회. 이 검사는 승리를 주입해 진행 구조를 확인한다.
- 별도 전투 시뮬레이션: 시드 1~3과 21~28, 총 11회 모두 30전투 클리어. 시드 21~28의 클리어 전투당 평균 3.33턴. 자동 편성 탐색기의 결과이며 실제 플레이어의 승률을 의미하지 않는다.

앞선 검증 이후 Windows x64·WebGL 출시 빌드를 추가로 제작했다. itch.io 게시·업로드는 수행하지 않았다. 모든 해상도·운영체제·카드 조합에서의 무결함을 보증하는 검증은 아니다. 기존 Unity AI 패키지의 NoSubscription 메시지와 DOTween DLL importer 구버전 경고는 게임 로직 검사와 별개다.

수치 시뮬레이션 기록: `Tests/Balance/Results/ReleaseSmoke.json`, `Tests/Balance/Results/ReleaseFinal.json`.

## 2026-11-08 배포 파일

- Windows x64: `Builds/Release/ImpactOrder-Windows.zip` (55,866,175 bytes). Unity 빌드 성공, 실행 파일 헤드리스 시작 로그 확인, 런타임·데이터 포함과 디버그 심볼 폴더 제외 확인.
- WebGL: `Builds/Release/ImpactOrder-Web.zip` (20,609,363 bytes). Unity 빌드 성공, 로컬 HTTP 브라우저에서 메인 화면 표시 및 JavaScript 오류 없음 확인. 이번 브라우저 확인 범위는 시작 화면이며 전체 게임 재검증은 아니다.
- WebGL gzip decompression fallback 적용, 1280×720 기준, 브라우저 프레임 크기 대응. 저장 파일의 IndexedDB 동기화를 추가했다.
- 웹 ZIP 최상위 index.html과 Build 내 loader/data/wasm 파일 존재 확인.
- 로컬 테스트 서버의 Content-Length 경고와 URP 선택적 FSR 셰이더 경고가 있었으나 시작 화면은 정상 표시됐다. itch.io 서버 환경 자체는 아직 확인하지 않았다.
- 출시 소개문·설치·업로드 안내: `Builds/Release/ItchUpload.md`.
- 빌드 로그: `Logs/ReleaseWindows.log`, `Logs/ReleaseWeb.log`. 임시 Editor 빌드 스크립트는 제거했다.
