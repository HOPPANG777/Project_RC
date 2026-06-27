# StockRhythm Scene Element List

작성일: 2026-06-27 KST

## 목적

이 문서는 StockRhythm MVP에서 다음으로 정해야 할 씬과, 각 씬에 들어갈 수 있는 UI/UX 요소 후보를 정리한다.
개발 구현 방식이나 Unity 씬 구조는 개발자가 결정한다.

## 우선 정해야 할 씬

현재 플레이 흐름 기준으로 아래 씬을 먼저 정한다.

```text
Title
-> Stage Brief
-> Tutorial Overlay
-> Gameplay -> Result -> Retry / Exit
```

Title은 채택 요소가 정리되어 있으므로, 다음 우선순위는 `Stage Brief`, `Tutorial Overlay`, `Gameplay`, `Result`, `Retry / Exit` 순서로 둔다.

## 1. Title

이미 채택한 요소:

| 요소 | 역할 |
| --- | --- |
| Game Title | 게임 이름 표시 |
| Tagline | 부제 |
| Start Button | 시작 진입 |
| Quit Button | 종료 |
| Settings Button | 일반 설정 진입 |
| Sync/Offset Entry | 리듬 보정 진입 |
| Safety Text | 실제 투자 오해 방지 |
| Version Text | 빌드 표기 |
| Background Visual | 분위기 형성 |
| Animated Chart Motif | 차트 리듬감 전달 |

## 2. Stage Brief

Stage Brief는 `SAMSU`를 시작하기 전, 플레이어가 이번 스테이지의 분위기와 규칙을 짧게 이해하는 화면이다.

전부 채택한다.

| 채택 요소 | 역할 |
| --- | --- |
| Stage Name | `SAMSU` 표시 |
| Stage Subtitle | 전자/제조풍 가상 종목 분위기 설명 |
| Mood Keywords | Drop / Rebound / Switch |
| Short Description | 급락과 반등이 섞인 가상 차트 설명 |
| Play Button | Gameplay로 진입 |
| Back Button | Title로 복귀 |
| Safety Text | 실제 투자/실제 종목 아님 고지 |
| Mini Chart Preview | 스테이지 분위기용 가상 차트 실루엣 |
| Difficulty Hint | 쉬움/보통 같은 간단한 체감 난이도 |
| Length Hint | 30초 내외 등 플레이 길이 안내 |

추후 정해야 할 세부 문구:

- Stage Subtitle 문구
- Mood Keywords를 영어로 유지할지 한글화할지
  - 현재 후보: Drop / Rebound / Switch

## 3. Tutorial Overlay

Tutorial Overlay는 별도 긴 튜토리얼이 아니라 플레이 직전 또는 초반에 규칙을 빠르게 전달하는 화면이다.

| 후보 요소 | 역할 |
| --- | --- |
| BUY Guide | 초록 캔들 = BUY |
| SELL Guide | 빨간 캔들 = SELL |
| Timing Guide | 판정선에 닿을 때 입력 |
| Input Guide | 왼쪽 Shift / 오른쪽 Shift 안내 |
| Profit Reminder | PROFIT은 게임 점수 |
| Start Prompt | 준비되면 시작 |
| Skip/Auto Close | 자동 닫힘 또는 건너뛰기 |
| Simple Example Note | 작은 예시 캔들 |

정해야 할 것:

- Tutorial Overlay를 플레이 전 화면으로 둘지, Gameplay 첫 5초에 띄울지
- 입력 키 안내를 표시할지
- 자동으로 닫힐지, 사용자가 닫을지

## 4. Gameplay

Gameplay는 MVP의 핵심 화면이다.
노트와 판정선 가독성이 가장 우선이다.

| 후보 요소 | 역할 |
| --- | --- |
| Chart Lane | 캔들/노트가 이동하는 중심 영역 |
| Judgement Line | 입력 타이밍 기준 |
| Candle Notes | BUY/SELL 판정 대상 |
| BUY Indicator | BUY 방향 표시 |
| SELL Indicator | SELL 방향 표시 |
| Profit Display | 현재 점수 |
| Combo Display | 현재 콤보 |
| Stage Name | 현재 스테이지 `SAMSU` 표시 |
| Judgement Feedback | PERFECT / GREAT / GOOD / MISS |
| Safety Text | 하단 안전 문구 |
| Background Trading Panel | 거래창 분위기용 장식 |
| NEWS Panel | 가상 뉴스 분위기 |
| Mini Chart Panel | 보조 차트 장식 |
| Progress Bar | 스테이지 진행도 |
| Pause Button | 일시정지 진입 |

우선순위:

1. Chart Lane
2. Judgement Line
3. Candle Notes
4. BUY / SELL Indicator
5. Judgement Feedback
6. Profit / Combo
7. Stage Name
8. Safety Text
9. 장식 패널

정해야 할 것:

- Progress Bar를 넣을지
- Pause Button을 MVP에 넣을지
- NEWS/Mini Chart 같은 장식 패널을 얼마나 줄일지
- Judgement Feedback 위치

## 5. Result

Result는 플레이 성과를 요약하고 재도전을 유도하는 화면이다.

| 후보 요소 | 역할 |
| --- | --- |
| Result Title | 결과 화면 제목 |
| Accuracy | 전체 입력 정확도 |
| MaxCombo | 최고 연속 성공 수 |
| Profit | 최종 점수 |
| Judgement Breakdown | PERFECT/GREAT/GOOD/MISS 개수 |
| Retry Button | 같은 스테이지 재시작 |
| Exit Button | Title 또는 종료 흐름 |
| Stage Brief Button | Stage Brief로 복귀 |
| Safety Text | PROFIT은 실제 수익이 아닌 점수 |
| Short Result Message | 플레이 결과에 대한 짧은 문구 |

우선 채택 기준:

- Accuracy
- MaxCombo
- Profit
- Retry Button
- Safety Text

정해야 할 것:

- Judgement Breakdown을 MVP에 넣을지
- Stage Brief Button이 필요한지
- Result Message 문구 톤

## 6. Retry / Exit

Retry / Exit은 Result 이후 플레이어가 다음 행동을 고르는 구간이다.
별도 씬으로 둘지 Result 안의 버튼 영역으로 처리할지는 개발자가 결정한다.

| 후보 요소 | 역할 |
| --- | --- |
| Retry Button | Gameplay 재시작 |
| Exit Button | Title 또는 종료 |
| Confirm Quit | 실수 종료 방지 |
| Back to Title | Title로 돌아가기 |
| Continue Prompt | 다음 행동 유도 |

정해야 할 것:

- Retry / Exit을 별도 화면으로 둘지 Result 안에 포함할지
- Quit 확인창을 둘지
- Exit이 Title 복귀인지 앱 종료인지

## 7. Settings

Title에서 Settings Button을 채택했으므로, Settings 화면 또는 팝업도 후보로 둔다.

| 후보 요소 | 역할 |
| --- | --- |
| Master Volume | 전체 음량 |
| Music Volume | 음악 음량 |
| SFX Volume | 효과음 음량 |
| Display Mode | 화면 모드 |
| Input Guide | 입력 키 안내 |
| Back Button | Title로 복귀 |

정해야 할 것:

- Settings를 MVP에 실제로 열 수 있게 할지
- 음량만 먼저 둘지
- 화면/입력 설정은 후순위로 둘지

## 8. Sync/Offset

Title에서 Sync/Offset Entry를 채택했으므로, 보정 화면도 후보로 둔다.
구현 방식은 개발자 결정이며, 기획에서는 화면에 어떤 안내가 필요한지만 정한다.

| 후보 요소 | 역할 |
| --- | --- |
| Offset Title | 보정 화면 제목 |
| Short Explanation | 음악/입력 타이밍 보정 안내 |
| Test Beat Area | 테스트 박자 영역 |
| Current Offset Value | 현재 보정값 표시 |
| Apply Button | 보정값 적용 |
| Reset Button | 기본값 복귀 |
| Back Button | Title로 복귀 |

정해야 할 것:

- MVP에서 Sync/Offset 화면을 실제 제공할지
- 안내 문구를 얼마나 쉽게 쓸지
- 세부 조작 방식은 개발자 결정으로 둘 것

## 우선 결정 순서

1. Stage Brief 요소 확정
2. Tutorial Overlay 요소 확정
3. Gameplay 필수 UI 확정
4. Result 표시 항목 확정
5. Retry / Exit 처리 방식 확정
6. Settings와 Sync/Offset은 MVP 포함 범위 결정

## 후순위

- 실제 노트 패턴표
- 추가 스테이지 선택 화면
- 상세 옵션 화면
- 랭크/등급 화면
- 상점/해금/재화 화면
