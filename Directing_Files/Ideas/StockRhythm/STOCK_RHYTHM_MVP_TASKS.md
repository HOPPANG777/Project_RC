# StockRhythm MVP Tasks

작성일: 2026-06-27 KST
최근 확정: 2026-06-27 KST

## 1차 목표

Project_RC에서 StockRhythm의 첫 플레이 가능한 `SAMSU` 리듬 프로토타입을 만들기 위한 작업 목록이다.

## 기획 정리

| 상태 | 작업 | 산출물 |
| --- | --- | --- |
| 완료 | 원본 Stock Rhythm 산출물 이관 | `Directing_Files` 내부 문서와 자산 |
| 완료 | 현재 방향성 요약 | `STOCK_RHYTHM_DIRECTION_BRIEF.md` |
| 완료 | 첫 MVP 스테이지 확정 | `SAMSU` |
| 완료 | 판정 폭 확정 | PERFECT 40ms / GREAT 60ms / GOOD 100ms |
| 완료 | 점수식 확정 | `100 * 판정 배율` |
| 완료 | FEVER 범위 결정 | MVP에서 제외 |
| 완료 | 결과 화면 우선 항목 확정 | Accuracy / MaxCombo / Profit |
| 대기 | 패턴표 데이터 타입 구조 결정 | 캔들/노드 데이터 스키마 |

## SAMSU 규칙

| 항목 | 기준 |
| --- | --- |
| 판정 등급 | PERFECT / GREAT / GOOD / MISS |
| PERFECT | 40ms 이내, 100점 |
| GREAT | 60ms 이내, 70점 |
| GOOD | 100ms 이내, 50점 |
| MISS | 100ms 초과 또는 미입력, 0점 |
| 노트 속도 | 0.1초 기준 1 캔들/노드 |
| 점수식 | `100 * (PERFECT: 1 / GREAT: 0.7 / GOOD: 0.5)` |
| 결과 화면 | Accuracy, MaxCombo, Profit |
| FEVER | 사용하지 않음 |

## 프로토타입 제작

| 상태 | 작업 | 기준 |
| --- | --- | --- |
| 대기 | 2D 플레이 씬 구성 | 중앙 차트 라인, 판정선, UI 패널 |
| 대기 | BUY / SELL 입력 연결 | 왼쪽 Shift, 오른쪽 Shift |
| 대기 | 캔들 노트 프리팹 제작 | 상승 캔들 = BUY, 하락 캔들 = SELL |
| 대기 | 노트 이동 로직 제작 | 0.1초 기준 1 캔들/노드 |
| 대기 | 판정 로직 제작 | 종가 지점 기준 40/60/100ms 판정 |
| 대기 | PROFIT / COMBO 표시 | 점수와 콤보 실시간 갱신 |
| 대기 | 결과 화면 제작 | Accuracy, MaxCombo, Profit 표시 |

## 데이터 구조 결정 필요

패턴표는 구현 데이터 타입 구조를 확인한 뒤 결정한다.
우선 필요한 후보 필드는 아래와 같다.

| 필드 후보 | 목적 |
| --- | --- |
| `time` | 노트 판정 기준 시간 |
| `nodeIndex` | 0.1초 단위 캔들/노드 순서 |
| `side` | `BUY` 또는 `SELL` |
| `candleType` | 상승, 하락, 전환, 횡보 등 |
| `value` | 가상 차트 높이 또는 가격 위치 |
| `label` | 연출용 구간 이름 |

## 자산 제작

| 상태 | 작업 | 기준 |
| --- | --- | --- |
| 완료 | 콘셉트 이미지 이관 | `ProjectDirectAsset/StockRhythm/concept-images` |
| 대기 | SAMSU 배경 선정 | 원본 콘셉트 이미지 참고 |
| 대기 | 캔들 노트 색상 확정 | BUY = 초록, SELL = 빨강 |
| 대기 | UI 폰트와 숫자 스타일 확정 | 거래창 느낌, 과도한 실제 금융감 제거 |
| 대기 | 테스트용 루프 음악 선정 또는 생성 | 30-60초 루프 |

## 안전 표현

| 상태 | 작업 | 기준 |
| --- | --- | --- |
| 대기 | 안전 문구 UI 위치 확정 | 하단 또는 결과 화면 |
| 대기 | 실제 종목 오해 방지 검토 | 이름, 로고, 가격, 뉴스 표현 점검 |
| 완료 | `PROFIT` 설명 기준 확정 | 실제 수익이 아닌 게임 점수 |

## 1차 완료 기준

- `SAMSU` 기준 30초 이상 플레이 가능한 스테이지가 있다.
- BUY / SELL 입력으로 노트를 판정할 수 있다.
- 입력 결과가 PERFECT / GREAT / GOOD / MISS로 나뉜다.
- 입력 결과가 PROFIT과 COMBO에 반영된다.
- 결과 화면에서 Accuracy, MaxCombo, Profit을 볼 수 있다.
- 실제 투자 게임으로 오해될 수 있는 요소가 제거되어 있다.
