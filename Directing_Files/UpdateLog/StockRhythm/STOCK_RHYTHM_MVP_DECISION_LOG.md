# StockRhythm MVP Decision Log

작성일: 2026-06-27 KST
최근 확정: 2026-06-27 KST

## 2026-06-27 결정 사항

| 항목 | 결정 |
| --- | --- |
| 개발 엔진 | Unity |
| 파일 관리 범위 | `Directing_Files` 내부만 수정 |
| 직접 구현 방식 | 개발자 결정 |
| 첫 MVP 스테이지 | `SAMSU` 기준으로 진행 |
| 판정 폭 | PERFECT 40ms / GREAT 60ms / GOOD 100ms |
| 노트 속도 | 0.1초 기준 1 캔들/노드 |
| 점수식 | `100 * (PERFECT: 1 / GREAT: 0.7 / GOOD: 0.5)` |
| FEVER | MVP에서 제거 |
| 패턴표 | 후순위. 개발자가 지정한 양식 확정 후 작성 |
| 결과 화면 | Accuracy, MaxCombo, Profit 우선 채택 |
| 추가 기획 범위 | 플레이 플로우와 UI/UX 화면 구성 중심 |
| 디자인 양식 | Dark Trading UI + Rhythm Clarity 기준 |

## 개발자 결정 영역

아래 항목은 기획 문서에서 확정하지 않고 개발자가 정한다.

- Unity 씬 구성
- 패턴 데이터 타입과 파일 형식
- 오디오 싱크 처리 방식
- 노트 이동 구현 방식
- 입력 처리 구조
- 보정 옵션
- 실제 코드 구조

## 반영 파일

| 파일 | 반영 내용 |
| --- | --- |
| `Ideas/StockRhythm/STOCK_RHYTHM_DIRECTION_BRIEF.md` | Unity 개발 확정은 유지하고 직접 구현 내용 제거 |
| `Ideas/StockRhythm/STOCK_RHYTHM_MVP_TASKS.md` | 개발자 결정 영역 분리 |
| `Ideas/StockRhythm/STOCK_RHYTHM_SAMSU_MVP_SPEC.md` | 플레이 규칙 중심으로 정리 |
| `Ideas/StockRhythm/STOCK_RHYTHM_NEXT_PLANNING_ITEMS.md` | 실제 패턴 제작 후순위 반영 |
| `Ideas/StockRhythm/STOCK_RHYTHM_PLAY_FLOW_UIUX.md` | 플레이 흐름과 화면 구성 신규 작성 |
| `Ideas/StockRhythm/STOCK_RHYTHM_DESIGN_GUIDE.md` | 디자인 양식 신규 작성 |
| `Ideas/StockRhythm/README.md` | 개발 사양 문서 색인 제거 |
