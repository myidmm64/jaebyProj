# Combat UI

이미지 생성 없이 Aseprite CLI에서 만든 금속 테두리·청회색 판넬·적색 실행 버튼과 32×32 에너지 번개 아이콘.

- `Assets/02.Sprites/UI/Combat`: 새 스프라이트. 프레임은 9-slice, 카드 그림은 기존 30종 유지.
- `Assets/Shaders/CombatSprite.shader`: URP SpriteRenderer 피격 섬광·집중 윤곽·픽셀 소멸 셰이더.
- `Assets/Materials/CombatSprite.mat`: GameAssets에서 참조하는 전투용 머티리얼.
- `Assets/01.Scripts/BattleVFX.cs`: 사각 파티클·참격 궤적·배경 부유 입자·짧은 카메라 흔들림.
- 카드 호버·실행 반응, 준비된 실행 버튼과 궁극기 맥동, 판매 시 골드 반응, 체력바 감소 보간.
- 패널 테두리 12–14px, 카드 테두리 12–16px로 강화. 패널 9-slice 경계는 20px.
- 가방 상단의 페이지·보유량·정리·합성·관리·속도는 배경 없이 텍스트로 표시.
- BackgroundCover가 스프라이트 피벗을 보정하고 화면 비율에 맞춰 6% 여유를 두고 배경을 채운다. 편집 화면에서도 적용.
- MaterialPropertyBlock은 스크립트 재로딩 후 null일 때 사용 직전에 다시 생성.

검증·플레이·빌드는 실행하지 않음.
