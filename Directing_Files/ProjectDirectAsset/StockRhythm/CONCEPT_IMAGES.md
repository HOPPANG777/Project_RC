# Stock Rhythm Concept Images

이 문서는 `Stock Rhythm` 컨셉 기획서에 사용할 설명용 이미지 목록과 제작 프롬프트를 정리한다.  
이미지는 실제 게임 리소스가 아니라, 컨셉 발표/기획서/피치덱에서 게임 구조를 이해시키기 위한 시각 자료다.

## 공통 스타일 가이드

- 용도: 컨셉 기획서용 이미지
- 전체 분위기: 어두운 금융 대시보드 + 네온 리듬게임 UI + 초록/빨강 매수매도 포인트
- 화면비: 기본 16:9 가로형
- 텍스트: 꼭 필요한 짧은 라벨만 사용
- 피해야 할 것: 실제 회사 로고, 실제 증권사 로고, 워터마크, 너무 작은 글자, 과도한 장식
- 차트 표현: 실제 종목명을 특정하지 않는 가상의 캔들/라인 차트
- 핵심 UI: `BUY`, `SELL`, 판정선, 가로형 차트 레인, 시드머니, 수익/손실

## 저장 위치

최종 이미지는 다음 폴더에 저장한다.

```text
C:\Users\a\Documents\Stock Rhythm\concept-images
```

## 제작 이미지 목록

| 번호 | 파일명 | 이미지 |
| --- | --- | --- |
| 01 | `01-key-visual.png` | 대표 키 비주얼 |
| 02 | `02-gameplay-mockup.png` | 게임 화면 목업 |
| 03 | `03-play-flow.png` | 플레이 흐름도 |
| 04 | `04-input-judgement.png` | 조작/판정 방식 설명 |
| 05 | `05-timeframe-difficulty.png` | 장투-단투 난이도 구조 |
| 06 | `06-risk-multiplier.png` | 레버리지/인버스 리스크 배율 |
| 07 | `07-backstage-events.png` | 백스테이지 이미지 연출 예시 |
| 08 | `08-seed-money-system.png` | 시드머니 시스템 |
| 09 | `09-result-report.png` | 플레이 후 결과 리포트 |
| 10 | `10-mvp-scope.png` | MVP 범위 요약 |

## 01. 대표 키 비주얼

**목적**  
게임의 정체성인 `주식 차트 + AI 음악 + 매수/매도 리듬게임`을 한 장으로 보여준다.

**프롬프트**

```text
Use case: stylized-concept
Asset type: concept pitch key visual, 16:9 landscape
Primary request: a striking key visual for a rhythm game based on stock market charts, where horizontal stock chart lanes fall downward like music notes and the player hits BUY and SELL buttons at the bottom
Scene/backdrop: dark futuristic trading desk, abstract financial dashboard, subtle audio waveform patterns, no real company logos
Subject: horizontal candlestick chart lanes descending toward a bright judgement line, green BUY note markers and red SELL note markers, two large game buttons labeled "BUY" and "SELL" near the bottom
Style/medium: polished cinematic game concept art, UI-heavy but readable, premium pitch deck visual
Composition/framing: wide 16:9, strong central perspective, chart lanes falling from top to bottom, judgement line near lower third, buttons clearly visible
Lighting/mood: energetic, high contrast, neon green and red accents, dark finance-tech mood
Text (verbatim): "BUY", "SELL"
Constraints: make the concept understandable at a glance; no real stock tickers; no logos; no watermark; avoid tiny unreadable UI text
```

## 02. 게임 화면 목업

**목적**  
실제 플레이 화면 구조를 보여준다. 가로형 차트 레인이 위에서 아래로 내려오고, 하단 판정선에서 매수/매도를 입력하는 구조가 명확해야 한다.

**프롬프트**

```text
Use case: ui-mockup
Asset type: gameplay screen mockup, 16:9 landscape
Primary request: realistic game UI mockup for a two-button stock rhythm game
Scene/backdrop: dark trading platform interface with blurred market-event background
Subject: multiple horizontal chart lanes falling downward from top to bottom, candlesticks and line graph embedded inside each lane, green BUY notes and red SELL notes placed on the chart, a bright judgement line across the bottom area, two large buttons labeled "BUY" and "SELL"
Style/medium: shippable high-fidelity game UI mockup, clean and practical, not a marketing poster
Composition/framing: 16:9 screen capture style, gameplay area dominant, score/combo/position HUD compact at the top, judgement line and buttons at the bottom
Text (verbatim): "BUY", "SELL", "COMBO", "SCORE", "POSITION"
Constraints: the chart lanes must be horizontal while moving downward; the UI must be readable; no real tickers; no real logos; no watermark
```

## 03. 플레이 흐름도

**목적**  
`실제 차트 데이터 -> AI 음악/노트 생성 -> 리듬 플레이 -> 결과 리포트`의 전체 구조를 설명한다.

**프롬프트**

```text
Use case: infographic-diagram
Asset type: concept planning flow diagram, 16:9 landscape
Primary request: clean pitch-deck flow diagram explaining a stock rhythm game pipeline
Scene/backdrop: dark neutral slide background with subtle grid
Subject: five-step horizontal workflow: chart data input, AI music generation, note pattern generation, rhythm gameplay, result report with score and seed money
Style/medium: polished startup pitch deck infographic, crisp UI cards, arrows, readable labels
Composition/framing: left-to-right process flow, five evenly spaced modules with simple icons, generous spacing
Text (verbatim): "Chart Data", "AI Music", "Note Pattern", "Gameplay", "Result Report", "Seed Money"
Constraints: labels must be readable; no dense paragraphs; no logos; no watermark; avoid extra unrelated steps
```

## 04. 조작/판정 방식 설명

**목적**  
판정선에 가로형 차트/노트가 닿는 순간 `BUY` 또는 `SELL` 버튼을 누르는 조작을 설명한다.

**프롬프트**

```text
Use case: infographic-diagram
Asset type: gameplay mechanic explainer, 16:9 landscape
Primary request: clear diagram explaining the input judgement mechanic of a two-button stock rhythm game
Scene/backdrop: simplified dark gameplay screen
Subject: one horizontal chart lane descending toward a judgement line, green BUY note arriving at the judgement line, red SELL note behind it, two large bottom buttons labeled "BUY" and "SELL", timing windows shown as subtle rings or bars
Style/medium: clean game design explainer image, high readability, semi-diagram semi-UI mockup
Composition/framing: chart lane in the center, downward arrows showing motion, judgement line across lower third, buttons below
Text (verbatim): "BUY", "SELL", "Judgement Line", "Perfect", "Good", "Miss"
Constraints: make the mechanic obvious without long text; no real stock tickers; no logos; no watermark
```

## 05. 장투-단투 난이도 구조

**목적**  
장투에서 스캘핑으로 갈수록 시간축이 짧아지고 게임 속도/노트 밀도/판정 난이도가 올라가는 구조를 보여준다.

**프롬프트**

```text
Use case: infographic-diagram
Asset type: difficulty ladder image, 16:9 landscape
Primary request: visual difficulty ladder for a stock rhythm game based on trading timeframes
Scene/backdrop: dark finance-tech slide with subtle chart grid
Subject: four ascending difficulty stages: Long-term, Swing, Day Trade, Scalping; each stage shows faster falling chart lanes, denser notes, and stricter timing window
Style/medium: polished game design infographic, clean icons, readable typography
Composition/framing: left-to-right progression or staircase layout, calm to intense visual progression
Text (verbatim): "Long-term", "Swing", "Day Trade", "Scalping", "Speed", "Note Density", "Timing"
Constraints: show increasing difficulty clearly; avoid tiny text; no logos; no watermark
```

## 06. 레버리지/인버스 리스크 배율

**목적**  
현물, 2X, 3X, 인버스 선택이 보상과 위험을 동시에 키우는 구조를 보여준다.

**프롬프트**

```text
Use case: infographic-diagram
Asset type: risk multiplier system diagram, 16:9 landscape
Primary request: concept image explaining spot, leverage, and inverse modes as risk multipliers in a stock rhythm game
Scene/backdrop: dark trading UI background with risk meter
Subject: selectable mode chips for Spot, 2X, 3X, Inverse, Inverse 2X; reward meter and loss meter increasing together; liquidation risk gauge for high leverage
Style/medium: polished game economy infographic, modern UI, readable controls
Composition/framing: central risk/reward scale, mode options on one side, warning gauge on the other
Text (verbatim): "Spot", "2X", "3X", "Inverse", "Inverse 2X", "Reward", "Risk", "Liquidation Risk"
Constraints: communicate high-risk high-reward; no real financial advice language; no logos; no watermark
```

## 07. 백스테이지 이미지 연출 예시

**목적**  
급락/급등의 원인을 긴 글 대신 배경 이미지로 전달하는 연출을 보여준다.

**프롬프트**

```text
Use case: stylized-concept
Asset type: concept art collage for market event backgrounds, 16:9 landscape
Primary request: showcase backstage background transitions for a stock rhythm game, where market-event imagery appears behind the falling chart lanes
Scene/backdrop: split-screen or three-panel cinematic background examples
Subject: three market-event backgrounds behind transparent gameplay UI: interest rate hike with central bank monitors, earnings miss with broken report and falling graph, panic selling with red trading screens and chaotic order flow
Style/medium: cinematic concept art with UI overlay, polished pitch deck visual
Composition/framing: three clear panels or layered transition view, falling chart lanes and BUY/SELL notes visible in foreground
Text (verbatim): "RATE HIKE", "EARNINGS MISS", "PANIC SELLING"
Constraints: background must support gameplay readability; no real logos; no real news brands; no watermark
```

## 08. 시드머니 시스템

**목적**  
게임 재화 `시드머니`를 걸고 플레이한 뒤 실력/판단/난이도에 따라 수익 또는 손실이 반영되는 구조를 보여준다.

**프롬프트**

```text
Use case: infographic-diagram
Asset type: game economy system diagram, 16:9 landscape
Primary request: clear game economy diagram for a stock rhythm game where players stake seed money before a stage and gain or lose currency after gameplay
Scene/backdrop: dark UI dashboard with finance game styling
Subject: seed money wallet, stake selection, stage play, result calculation, profit/loss change; include small icons for rhythm accuracy, trading judgement, difficulty multiplier
Style/medium: polished pitch deck infographic with game UI components
Composition/framing: circular or left-to-right loop, wallet at start and end, profit/loss result emphasized
Text (verbatim): "Seed Money", "Stake", "Play Stage", "Score", "Profit", "Loss"
Constraints: make clear this is game currency, not real cash; no logos; no watermark; avoid casino imagery
```

## 09. 플레이 후 결과 리포트

**목적**  
스테이지 종료 후 점수, 정확도, 수익률, 시드머니 증감, 매수/매도 타이밍 평가를 보여주는 리포트 화면을 표현한다.

**프롬프트**

```text
Use case: ui-mockup
Asset type: result report screen mockup, 16:9 landscape
Primary request: high-fidelity result report screen for a stock rhythm game
Scene/backdrop: dark analytics dashboard with subtle chart background
Subject: final score panel, rhythm accuracy, virtual return rate, seed money profit/loss, timeline showing BUY and SELL timing markers, market event summary chips
Style/medium: polished game UI mockup, practical dashboard, readable hierarchy
Composition/framing: large score on left, performance metrics on right, trade timeline across the bottom
Text (verbatim): "Final Score", "Accuracy", "Return", "Seed Money", "BUY", "SELL", "Market Events"
Constraints: readable UI; no real tickers; no real investment advice; no logos; no watermark
```

## 10. MVP 범위 요약

**목적**  
첫 버전에서 만들 핵심 범위를 한 장으로 요약한다.

**프롬프트**

```text
Use case: productivity-visual
Asset type: pitch deck MVP scope slide, 16:9 landscape
Primary request: clean MVP scope slide for a stock rhythm game concept
Scene/backdrop: modern dark pitch deck background with subtle chart grid
Subject: six MVP blocks: one stock chart, 30-60 second stage, BUY/SELL two-button input, rule-based note generation, event background image, seed money result calculation
Style/medium: polished startup pitch deck slide, clean sans-serif typography, simple icons
Composition/framing: 2x3 grid of feature blocks with concise labels
Text (verbatim): "MVP Scope", "1 Stock", "30-60s Stage", "BUY / SELL", "Rule-based Notes", "Event Background", "Seed Money Result"
Constraints: readable slide; no dense paragraphs; no logos; no watermark
```

