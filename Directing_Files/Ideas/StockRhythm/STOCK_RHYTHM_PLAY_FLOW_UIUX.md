# StockRhythm Play Flow UI/UX

작성일: 2026-06-27 KST

## 목적

이 문서는 StockRhythm MVP에서 필요한 플레이 흐름과 UI/UX 화면 구성을 정리한다.
현재 단계에서는 실제 패턴 제작, 개발 구현 방식, 데이터 구조, 상세 사운드 기획은 다루지 않는다.

## MVP 화면 흐름

```text
Title
-> Stage Brief
-> Tutorial Overlay
-> Gameplay -> Result -> Retry / Exit
```

## 1. Title

| 항목 | 내용 |
| --- | --- |
| 목적 | 게임 진입과 콘셉트 전달 |
| 핵심 정보 | StockRhythm, 가상 차트 리듬 게임 |
| 주요 버튼 | Start, Quit |
| 안전 문구 | PROFIT은 실제 수익이 아닌 게임 점수 |

### Title 채택 요소

Title 화면에는 아래 요소를 채택한다.

| 구분 | 채택 요소 | 설명 |
| --- | --- | --- |
| 브랜딩 | Game Title | `StockRhythm` |
| 브랜딩 | Tagline | 게임 부제. 가상 차트 리듬 게임임을 짧게 설명 |
| 진입 | Start Button | Stage Brief로 진입 |
| 진입 | Quit Button | 게임 종료 |
| 설정 | Settings Button | 일반 옵션 진입 |
| 설정 | Sync/Offset Entry | 리듬게임 보정 옵션 진입 |
| 안내 | Safety Text | 실제 투자/수익이 아님을 고지 |
| 안내 | Version Text | 내부 테스트 빌드 표기 |
| 배경 | Background Visual | 어두운 차트/캔들 분위기 |
| 배경 | Animated Chart Motif | Title 배경에 은은한 차트 움직임 |

Title에서 제외:

| 제외 요소 | 이유 |
| --- | --- |
| Stage Preview | Stage Brief 화면에서 다룸 |
| 실제 종목/가격 정보 | 실제 투자 오해 방지 |

표시 문구 후보:

```text
StockRhythm
Virtual Chart Rhythm Game
PROFIT is score, not real return
```

## 2. Stage Brief

| 항목 | 내용 |
| --- | --- |
| 목적 | 플레이할 스테이지 정보 전달 |
| 스테이지 | SAMSU |
| 핵심 정보 | 급락, 반등, 전환이 있는 가상 차트 |
| 주요 버튼 | Play, Back |

### Stage Brief 채택 요소

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

표시 문구 후보:

```text
SAMSU
Pattern Mood: Drop / Rebound / Switch
Use BUY and SELL at the judgement line
```

## 3. Tutorial Overlay

튜토리얼은 별도 긴 설명 화면보다, 플레이 직전 또는 플레이 초반에 짧게 보여준다.

| 항목 | 내용 |
| --- | --- |
| 목적 | BUY/SELL 규칙 이해 |
| 표시 방식 | 짧은 오버레이 또는 첫 5초 안내 |
| 핵심 문구 | 초록 = BUY, 빨강 = SELL |
| 닫기 방식 | 자동 사라짐 또는 Start 입력 |

표시 문구 후보:

```text
Green Candle = BUY
Red Candle = SELL
Hit when it reaches the line
```

## 4. Gameplay

| 항목 | 내용 |
| --- | --- |
| 목적 | 실제 플레이 |
| 필수 정보 | SAMSU, Profit, Combo, BUY/SELL, 판정선 |
| 선택 정보 | NEWS, BID/ASK풍 장식, 보조 차트 |
| 안전 문구 | 화면 하단에 작게 유지 |

필수 UI:

| UI | 역할 |
| --- | --- |
| Stage Name | 현재 스테이지 표시 |
| Chart Lane | 캔들/노트가 이동하는 중심 영역 |
| Judgement Line | 입력 타이밍 기준 |
| BUY / SELL | 입력 방향 표시 |
| Profit | 현재 점수 |
| Combo | 연속 성공 수 |

화면 원칙:

- 노트와 판정선이 가장 먼저 보여야 한다.
- 장식용 거래창 정보는 플레이 시야를 방해하지 않는다.
- 실제 가격, 실제 종목, 실제 투자 조언처럼 보이는 문구는 사용하지 않는다.

## 5. Result

| 항목 | 내용 |
| --- | --- |
| 목적 | 플레이 결과 요약 |
| 핵심 정보 | Accuracy, MaxCombo, Profit |
| 주요 버튼 | Retry, Stage Select, Exit |
| 안전 문구 | Profit은 게임 점수라고 다시 표시 |

결과 항목:

| 항목 | 의미 |
| --- | --- |
| Accuracy | 전체 입력 정확도 |
| MaxCombo | 최고 연속 성공 |
| Profit | 최종 점수 |

표시 문구 후보:

```text
Accuracy
Max Combo
Profit
PROFIT is game score only
```

## 6. Retry / Exit

| 항목 | 내용 |
| --- | --- |
| Retry | 같은 SAMSU 스테이지 재시작 |
| Stage Select | 이후 스테이지가 늘어날 경우 사용 |
| Exit | 타이틀 또는 종료 |

MVP에서는 `Retry`를 가장 강조한다.
`Retry`를 선택하면 다시 `Gameplay`로 돌아가고, `Exit`를 선택하면 Title 또는 종료 흐름으로 나간다.

## 최소 제작 범위

MVP에서 반드시 필요한 화면은 아래 3개다.

| 우선순위 | 화면 | 이유 |
| --- | --- | --- |
| 1 | Gameplay | 실제 플레이 핵심 |
| 2 | Result | Accuracy, MaxCombo, Profit 확인 |
| 3 | Tutorial Overlay | 규칙 이해 보조 |

Title과 Stage Brief는 단순 화면으로 시작해도 된다.

## 후순위

- 실제 0.1초 단위 패턴표 제작
- 상세 사운드 연출
- 스테이지 선택 화면 고도화
- 결과 등급 S/A/B/C
- 상세 옵션 화면
