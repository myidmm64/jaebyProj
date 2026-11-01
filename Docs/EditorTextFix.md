# 편집기 텍스트 표시 점검 — 2026-11-01

실행 중인 Unity 6000.6.4f1의 실제 Preferences, Inspector, Game/Scene 뷰를 computer-use로 확인했다.

## 편집기 자체의 글자

변경 전 General 설정은 Editor Font = Inter(Default), Editor Default Text Rendering Mode = SDF, Editor Text Generator Type = Advanced였다. 표시 문제 우회를 위해 **Bitmap + Standard**로 변경하고 실제 Preferences에 적용된 상태 및 메뉴·Inspector 표시를 확인했다. Editor Font는 Inter를 유지했다.

Unity 공식 문서도 SDF가 작은 글자나 낮은 DPI에서 더 흐려질 수 있으며, Bitmap은 더 선명한 표시를 위한 선택이라고 설명한다. Standard는 기존 Text Core를 사용한다. 이 조치는 편집기 전역 Preferences 변경으로, 프로젝트의 한글 폰트 교체와는 별개다. 특정 GPU 드라이버 결함으로 단정하지 않았다.

공식 참고: [General preferences reference](https://docs.unity.com/en-us/engine/6000.5/manual/unity-editor/editor-settings-reference/preferences/general).

## 게임 UI의 축소

프리팹 기본값은 1280×720이었으나 TestStage의 Canvas 인스턴스에는 **1920×1080 / Match 0.5** 오버라이드가 남아 있었다. 실제 Inspector의 Canvas 배율은 약 **0.448**이었고, 이 상태에서는 14포인트 라벨이 약 6픽셀 높이로 축소된다. 한글 폰트가 있어도 읽기 어려운 별도 원인이었다.

UI를 갱신하면서 씬과 프리팹의 Canvas 설정을 **1280×720 / 너비 기준 스케일 / Pixel Perfect**로 통일했다. 나눔고딕 Regular/Bold를 계속 포함하며 한글 글리프도 검증했다. Game 뷰를 매우 작게 줄이면 화면 전체가 축소되므로 텍스트 판독은 충분한 뷰 크기에서 확인한다.

프로젝트 파일 변경과 별도로, 앞서 변경한 편집기 Preferences는 이 컴퓨터에 저장된다.
