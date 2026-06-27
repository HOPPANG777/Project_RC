# StockRhythm Project Update Log

시간대: Asia/Seoul (KST)

이 문서는 StockRhythm 관련 변경 사항을 한곳에서 관리하는 단일 업데이트 로그다.
혼선을 줄이기 위해 기존의 Directing 이관 로그와 MVP 결정 로그를 이 파일로 통합한다.

## 로그 관리 기준

- StockRhythm 관련 변경 기록은 이 파일을 기준으로 관리한다.
- `Directing_Files` 내부 기획/디렉팅 문서 변경만 기록한다.
- Unity 구현 파일, `Assets`, `Packages`, `ProjectSettings` 변경 기록은 현재 이 문서에서 다루지 않는다.
- 실제 개발 방식과 코드 구조는 개발자 결정 영역으로 두고, 여기에는 기획 결정과 문서 변경만 기록한다.

## 통합된 로그

| 날짜 | 구분 | 대상 | 내용 |
| --- | --- | --- | --- |
| 2026-06-26 23:26 | 원본 추가/수정 | `STOCK_RHYTHM_IDEA.md` | Stock Rhythm 초기 게임 아이디어 문서 작성 및 갱신. |
| 2026-06-26 23:29 | 원본 추가/수정 | `CONCEPT_IMAGES.md` | 콘셉트 이미지 목록과 이미지별 제작 프롬프트 정리. |
| 2026-06-26 23:29 | 원본 추가 | `concept-images/README.md` | 콘셉트 이미지 산출물 폴더 안내 파일 추가. |
| 2026-06-26 23:31 | 원본 추가 | `concept-images/01-key-visual.png` | 핵심 비주얼 이미지 추가. |
| 2026-06-26 23:31 | 원본 추가 | `concept-images/02-gameplay-mockup.png` | 게임 화면 모의 이미지 추가. |
| 2026-06-26 23:31 | 원본 추가 | `concept-images/03-play-flow.png` | 플레이 흐름 이미지 추가. |
| 2026-06-26 23:31 | 원본 추가 | `concept-images/04-input-judgement.png` | 조작/판정 방식 설명 이미지 추가. |
| 2026-06-26 23:32 | 원본 추가 | `concept-images/05-timeframe-difficulty.png` | 시간축별 난이도 구조 이미지 추가. |
| 2026-06-26 23:32 | 원본 추가 | `concept-images/06-risk-multiplier.png` | 리스크/배율 시스템 이미지 추가. |
| 2026-06-26 23:32 | 원본 추가 | `concept-images/07-backstage-events.png` | 시장 이벤트 배경 연출 이미지 추가. |
| 2026-06-26 23:32 | 원본 추가 | `concept-images/10-mvp-scope.png` | MVP 범위 요약 이미지 추가. |
| 2026-06-26 23:34 | 원본 추가 | `concept-images/08-seed-money-system.png` | 게임 재화 구조 이미지 추가. |
| 2026-06-26 23:34 | 원본 추가 | `concept-images/09-result-report.png` | 결과 화면 이미지 추가. |
| 2026-06-26 23:38 | 원본 추가 | `Stock Rhythm 컨셉 기획서.docx` | Word 형식 콘셉트 기획서 추가. |
| 2026-06-27 00:02 | 원본 추가 | `STOCK_RHYTHM_CONCEPT_UPDATE.md` | 밈 리듬게임 방향, BUY/SELL 2레인, 패러디 종목명, 변형 차트, AI 음악 방향 기록. |
| 2026-06-27 00:04 | 원본 추가 | `PROJECT_UPDATE_LOG.md` | 프로젝트 변경 로그 파일 추가. |
| 2026-06-27 00:11 | 원본 추가 | `concept-images/11-gameplay-scene.png` | BUY/SELL 2레인 플레이 장면 이미지 추가. |
| 2026-06-27 00:16 | 원본 추가 | `concept-images/12-gameplay-single-chart-path.png` | 단일 차트 경로 중심 플레이 장면 이미지 추가. |
| 2026-06-27 00:22 | 원본 추가 | `concept-images/13-gameplay-billboard-candle-notes.png` | 전광판, 뉴스 배경, 캔들형 BUY/SELL 노트 장면 이미지 추가. |
| 2026-06-27 00:23 | 원본 수정 | `STOCK_RHYTHM_CONCEPT_UPDATE.md` | 전광판, 주가, 뉴스 이미지, 캔들 클릭 힌트 구조를 게임 화면 업데이트 항목으로 기록. |
| 2026-06-27 00:28 | 원본 추가 | `concept-images/14-gameplay-trading-profit.png` | SCORE를 PROFIT으로 바꾸고 거래창형 UI에 가깝게 조정한 장면 이미지 추가. |
| 2026-06-27 00:28 | 원본 수정 | `STOCK_RHYTHM_CONCEPT_UPDATE.md` | SCORE 대신 PROFIT 사용, BID/ASK, NEWS, 보조 차트 등 거래창형 UI 방향 기록. |
| 2026-06-27 00:38 | 원본 수정 | `STOCK_RHYTHM_CONCEPT_UPDATE.md` | BUY/SELL 캔들 노트, 종가 기준 판정, PROFIT 점수화, 콤보 페널티, 스테이지 길이 규칙 기록. |
| 2026-06-27 00:50 | 원본 수정 | `STOCK_RHYTHM_CONCEPT_UPDATE.md` | 단방향 캔들 노트, 종가 범위 표시, 양쪽 Shift 입력, DJMAX 계열 판정/점수 참고, MVP 후보 `NVIVA`/`GAAGLE`/`SAMSU`, 실패 조건/안전 문구 기록. |
| 2026-06-27 | Directing 이관 | `Directing_Files` | Stock Rhythm 원본 산출물을 `Ideas`, `ProjectDirectAsset`, `UpdateLog` 구조에 맞춰 이관. |
| 2026-06-27 | Directing 이관 | `ProjectDirectAsset/StockRhythm` | Word 기획서, 콘셉트 이미지 문서, PNG 14개, 원본 메타 파일 보관. |
| 2026-06-27 | Directing 문서 작성 | `STOCKRHYTHM_FILE_INVENTORY.md` | 원본 파일별 배치 현황과 추가 관리 문서 목록 작성. |
| 2026-06-27 | 기획 확정 | `STOCK_RHYTHM_DIRECTION_BRIEF.md` | StockRhythm 핵심 방향, Unity 개발 확정, SAMSU 기준 MVP 방향 정리. 직접 구현 방식은 개발자 결정 영역으로 분리. |
| 2026-06-27 | MVP 확정 | `STOCK_RHYTHM_SAMSU_MVP_SPEC.md` | SAMSU 기준 MVP 규칙 정리. 판정폭은 PERFECT 40ms, GREAT 60ms, GOOD 100ms로 확정. |
| 2026-06-27 | MVP 확정 | `STOCK_RHYTHM_SAMSU_MVP_SPEC.md` | 노트 시간 단위는 0.1초 기준 1 캔들/노드로 확정. |
| 2026-06-27 | MVP 확정 | `STOCK_RHYTHM_SAMSU_MVP_SPEC.md` | 점수식은 `100 * (PERFECT: 1 / GREAT: 0.7 / GOOD: 0.5)`로 확정. |
| 2026-06-27 | MVP 확정 | `STOCK_RHYTHM_SAMSU_MVP_SPEC.md` | FEVER는 MVP 범위에서 제외. 결과 화면 우선 항목은 Accuracy, MaxCombo, Profit으로 확정. |
| 2026-06-27 | 작업표 작성 | `STOCK_RHYTHM_MVP_TASKS.md` | 기획자가 준비할 항목과 개발자 결정 영역 분리. 실제 패턴표 제작은 후순위로 이동. |
| 2026-06-27 | 추가 기획 정리 | `STOCK_RHYTHM_NEXT_PLANNING_ITEMS.md` | 넓은 기획안보다 플레이 플로우와 UI/UX 화면 구성을 우선하도록 정리. |
| 2026-06-27 | UI/UX 정리 | `STOCK_RHYTHM_PLAY_FLOW_UIUX.md` | 흐름을 `Title -> Stage Brief -> Tutorial Overlay -> Gameplay -> Result -> Retry / Exit`로 정리. |
| 2026-06-27 | UI/UX 정리 | `STOCK_RHYTHM_PLAY_FLOW_UIUX.md` | Title 채택 요소로 Game Title, Tagline, Start, Quit, Settings, Sync/Offset, Safety, Version, Background, Animated Chart 반영. |
| 2026-06-27 | 디자인 정리 | `STOCK_RHYTHM_DESIGN_GUIDE.md` | MVP 디자인 양식 추가. Dark Trading UI, Rhythm Clarity, Virtual Finance, Arcade Feedback, Minimal Noise 기준 정리. |
| 2026-06-27 | 씬 요소 정리 | `STOCK_RHYTHM_SCENE_ELEMENT_LIST.md` | Stage Brief, Tutorial Overlay, Gameplay, Result, Retry/Exit, Settings, Sync/Offset 후보 요소 리스트업. |
| 2026-06-27 | 로그 통합 | `PROJECT_UPDATE_LOG.md` | 기존 Directing 이관 로그와 MVP 결정 로그를 이 파일로 통합. 혼선 방지를 위해 별도 로그 파일 제거. |
| 2026-06-27 | 씬 요소 확정 | `STOCK_RHYTHM_SCENE_ELEMENT_LIST.md` | Stage Brief 후보 요소 전부 채택. |
| 2026-06-27 | UI/UX 정리 | `STOCK_RHYTHM_PLAY_FLOW_UIUX.md` | Stage Brief 채택 요소를 화면 흐름 문서에 반영. |
| 2026-06-27 | 디자인 정리 | `STOCK_RHYTHM_DESIGN_GUIDE.md` | Stage Brief 채택 요소를 디자인 기준에 반영. |

## 현재 기준 결정 사항

| 항목 | 결정 |
| --- | --- |
| 개발 엔진 | Unity |
| 파일 관리 범위 | `Directing_Files` 내부 |
| 직접 구현 방식 | 개발자 결정 |
| 첫 MVP 스테이지 | `SAMSU` |
| 판정 폭 | PERFECT 40ms / GREAT 60ms / GOOD 100ms |
| 노트 속도 | 0.1초 기준 1 캔들/노드 |
| 점수식 | `100 * (PERFECT: 1 / GREAT: 0.7 / GOOD: 0.5)` |
| FEVER | MVP에서 제외 |
| 결과 화면 | Accuracy, MaxCombo, Profit |
| 실제 패턴표 제작 | 후순위 |
| 현재 기획 우선순위 | 플레이 플로우, UI/UX, 디자인 양식, 씬별 요소 |

## 개발자 결정 영역

아래 항목은 기획 문서에서 확정하지 않는다.

- Unity 씬 구성
- 패턴 데이터 타입과 파일 형식
- 오디오 싱크 처리 방식
- 노트 이동 구현 방식
- 입력 처리 구조
- 보정 옵션
- 실제 코드 구조

## 삭제/통합된 로그

| 파일 | 처리 |
| --- | --- |
| `STOCK_RHYTHM_DIRECTING_MIGRATION_LOG.md` | `PROJECT_UPDATE_LOG.md`로 통합 후 삭제 |
| `STOCK_RHYTHM_MVP_DECISION_LOG.md` | `PROJECT_UPDATE_LOG.md`로 통합 후 삭제 |

## 후속 작업

1. Stage Brief 요소를 확정한다.
2. Tutorial Overlay 요소를 확정한다.
3. Gameplay 필수 UI를 확정한다.
4. Result 표시 항목을 확정한다.
5. Retry / Exit 처리 방식을 확정한다.
6. Settings와 Sync/Offset의 MVP 포함 범위를 정한다.
