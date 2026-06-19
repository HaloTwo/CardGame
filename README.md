# Burger Monster Card Battle

## 사용한 Unity 버전

- Unity 6000.3.14f1

## 구현한 기능 목록

- 세로형 2D 턴제 카드 배틀 프로토타입
- 플레이어와 상대가 각각 6장의 카드로 전투 시작
- 각 진영의 카드 3장은 전장에 공개 배치, 남은 카드는 대기 카드로 관리
- 전장 카드가 제거되면 대기 카드에서 자동으로 신규 카드 배치
- 플레이어 턴 진행
  - 아군 카드 선택
  - 기본공격 또는 카드효과 선택
  - 대상 카드 선택
  - 피해/효과 적용
  - 턴 종료
- 상대 턴 자동 진행 및 단계별 안내 문구 출력
- HP 계산, 반격 피해, 카드 제거, 신규 카드 자동 배치
- 승리/패배 판정 및 결과 화면
- 카드 선택 UI, 현재 턴 표시 UI, 카드 HP 표시 UI, 대기 카드 UI
- 공격/피격 연출
  - 선택 카드 강조
  - 공격 카드 포커스
  - 피격 카드 흔들림
  - 화면 플래시
  - HIT 문구
  - 피해량 텍스트
- 로비 화면
  - 전투 시작
  - 덱 편집
  - 카드 도감
  - 전적/성장 확인
  - 저장 데이터 초기화
- 결과 저장
  - 승리 수
  - 패배 수
  - 보유 카드
  - 카드 레벨/경험치
  - 덱 편성 정보
- 추가 카드 타입 구현
  - 일반
  - 원거리
  - 무쌍
  - 힐러
  - 폭탄
  - 흡혈
  - 광전사
  - 수호자
  - 관통

## 주요 코드 구조 설명

### Assets/3.Script/CardBattle/Core

- `CardBattleManager.cs`
  - 전투 시작, 턴 전환, 플레이어 입력 흐름, 상대 AI 턴, 결과 처리, UI 이벤트 연결을 담당합니다.
- `BattleField.cs`
  - 전장 슬롯 3개와 대기 카드 목록을 관리합니다.
  - 카드 사망 처리 후 빈 슬롯에 대기 카드를 자동 배치합니다.
- `CardBattleRules.cs`
  - 기본공격, 카드효과, HP 계산, 반격, 힐러 턴 시작 효과 등 전투 규칙을 처리합니다.
- `EnemyBattleAI.cs`
  - 상대가 사용할 카드와 공격 대상을 고르는 간단한 AI 로직입니다.

### Assets/3.Script/CardBattle/Data

- `BattleCardDefinition.cs`
  - 카드 이름, 타입, HP, 이미지, 설명을 수정할 수 있는 ScriptableObject 데이터입니다.
- `BattleCardData.cs`
  - 전투 중 UI와 규칙에서 사용하는 카드 기본 데이터입니다.
- `BattleCardRuntime.cs`
  - 현재 HP, 소유자, 생존 여부처럼 전투 중 변하는 카드 상태를 관리합니다.
- `BattleDeckFactory.cs`
  - 기본 덱 또는 저장된 플레이어 덱을 전투용 런타임 카드 목록으로 생성합니다.
- `CardCatalog.cs`
  - 로비 도감과 덱 편집에서 사용하는 카드 목록입니다.
- `CardPlayerProfile.cs`
  - PlayerPrefs 기반으로 전적, 보유 카드, 카드 성장, 덱 편성 정보를 저장합니다.

### Assets/3.Script/UI

- `CardBattleView.cs`
  - 전투 화면 UI 참조와 텍스트/버튼/결과창 갱신을 담당합니다.
- `CardSlotView.cs`
  - 카드 슬롯 표시, 카드 선택 강조, 피격 흔들림, 피해량 텍스트 연출을 담당합니다.
- `CardBattleRuntimeViewBuilder.cs`
  - 전투 씬에 UI 참조가 없을 때 실행 가능한 기본 전투 UI를 런타임에 생성합니다.
- `StartSceneController.cs`
  - 로비 버튼, 도감, 덱 편집, 전적/성장 패널, 전투 씬 이동을 담당합니다.
- `StartSceneRuntimeViewBuilder.cs`
  - 로비 씬에 UI 참조가 없을 때 실행 가능한 기본 로비 UI를 런타임에 생성합니다.

### Assets/3.Script/Editor

- `CardBattleSceneBuilder.cs`
  - 전투 씬 오브젝트와 기본 카드 에셋 생성을 돕는 에디터용 빌더입니다.

## AI 도구, 외부 에셋, AI 생성 리소스 사용 범위와 출처

- OpenAI Codex
  - 코드 구조 정리, 기능 구현 보조, UI 수정, README 작성, 컴파일 검증에 사용했습니다.
- Unity MCP
  - Unity Editor 연동과 콘솔/컴파일 확인을 위한 자동화 도구로 사용했습니다.
- 외부 에셋
  - 별도 유료/무료 외부 에셋 패키지는 사용하지 않았습니다.
- 카드 일러스트
  - 현재 카드는 색상 사각형 기반의 임시 이미지입니다.
  - 이후 AI 생성 카드 이미지를 적용할 경우, 사용한 생성 도구 이름과 프롬프트/출처를 이 문서에 추가하면 됩니다.
