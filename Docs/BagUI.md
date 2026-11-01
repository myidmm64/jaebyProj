# 시퀀스 UI / 획득 패시브 HUD

`TestStage` 재생 후 **Tab** 또는 왼쪽 위 **TAB 시퀀스** 버튼으로 큰 시퀀스 패널을 열고 닫는다. 아래에서 올라오는 기존 슬라이드 방식과 닫기 버튼을 유지했다.

시퀀스는 크림색 헤더, 라임색 강조선, 큰 정사각형 슬롯과 연결 화살표로 구성했다. 12개 슬롯과 상단→하단의 지그재그 번호는 UI 확인용 예시이며, 실제 가방 크기나 실행 경로를 게임 규칙으로 확정하지 않는다. 카드 배치와 마법 실행은 아직 연결하지 않았다.

**패시브 가방은 제거했다.** 대신 오른쪽에 획득 아이템만 추가되는 아이콘 HUD가 있다. 시작 시 비어 있고, 획득 순서대로 쌓이며 중복 획득도 각각 추가한다. 항목이 많으면 스크롤할 수 있다. 마우스를 올리면 이름과 설명을 보여 주며 시퀀스를 닫아도 목록은 유지된다.

아이템 획득 코드에서 Canvas의 `PassiveItemHUD` 참조를 통해 호출한다:

```csharp
passiveItemHUD.AddItem(itemSprite, "아이템 이름", "아이템 설명");
```

아이콘이 null이면 이름의 첫 글자를 대신 표시한다. 플레이 모드에서 `PassiveItemHUD` 컴포넌트의 컨텍스트 메뉴 **프로토타입 / 테스트 아이템 획득**으로 표시를 시험할 수 있다. 실제 필드 아이템 획득 시스템은 아직 없어 해당 시스템에서 이 API를 호출해야 한다.

uGUI 프리팹: `Assets/3.Prefabs/UI/BagCanvas.prefab`.

- `Sequence Drawer / Sequence Slots`: 큰 시퀀스 슬롯과 연결 표시.
- `Acquired Passives`: 오른쪽 획득 목록과 비활성 아이콘 템플릿.
- `Item Tooltip`: 획득 아이템 이름·설명.
- `BagPanelUI`: Tab, 버튼, 0.25초 슬라이드 제어.
- `PassiveItemHUD`, `PassiveItemIcon`: 획득 목록과 툴팁 제어.
- `Sequence Drawer / Owned Card Bag`: 시퀀스 하단의 보유 카드 가방. 카드가 많으면 가로 스크롤한다.
- `OwnedCardBagUI`, `CardUIView`, `CardUIData`: 보유 카드 데이터와 표시. 이름·등급·분류·이미지·효과·3×3 범위 미리보기·해당 시 남은 턴을 카드 한 장에 표시한다.

보유 카드 추가 예시:

```csharp
ownedCardBagUI.AddCard(new CardUIData {
    name = "카드 이름",
    description = "카드 효과",
    artwork = cardSprite,
    range = new[] { Vector2Int.up },
    remainingTurns = -1
});
```

기본 상태는 비어 있다. 플레이 모드의 `OwnedCardBagUI` 컨텍스트 메뉴 **프로토타입 / 테스트 카드 추가**로 확인한다. 카드 보관 표시까지 구현했으며 시퀀스로 드래그 배치하거나 효과를 실행하는 기능은 아직 연결하지 않았다.

기존 기본 폰트를 [Google Fonts의 나눔고딕](https://github.com/google/fonts/tree/main/ofl/nanumgothic)으로 교체했다. Regular/Bold 원본과 OFL 라이선스를 `Assets/4.UI/Fonts`에 포함하여 시스템 폰트 설치에 의존하지 않고 한글을 표시한다. 텍스트 상자와 글자 크기도 함께 조정했다.

`Docs/BagUI-open.png`는 초기 상태, `Docs/BagUI-passives.png`는 예시 아이템 5개를 획득한 프리뷰다. 예시 아이템은 저장된 씬에 포함하지 않는다. 생성 도구는 `Tools > Explosive Wizard > Add Prototype Bag UI`, 기존 UI 재생성은 `Upgrade Bag UI`이며 재생성은 UI 수동 편집을 대체한다.

`Docs/BagUI-cards.png`는 보유 카드 3장과 패시브 5개를 표시한 예시다. 카드 역시 저장된 씬에는 포함하지 않는다. Canvas는 1280×720 기준, 너비 기준 스케일, 픽셀 정렬을 사용한다.

검증: Tab 입력, 열기/닫기, 연타 시 방향 전환, 정지 시간 중 애니메이션, 모든 UI 텍스트의 한글 글리프, 빈 목록, 60회 연속 획득·중복·순서, 툴팁, 스크롤 및 시퀀스를 닫은 뒤 HUD 유지.
