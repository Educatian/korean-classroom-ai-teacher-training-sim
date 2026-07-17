# 한국 교실 씬 계약

## 계층 구조

- `00_ENVIRONMENT`: 교실 메시, 칠판, 창, 사물함, 책상, 조명, 포스터
- `10_STUDENTS`: Rocketbox 학생 NPC 4명과 좌석 배치
- `20_SYSTEMS`: 훈련 상태, 채점, 카메라, 선택형 생성 AI 코치
- `30_INTERFACE`: 관찰 정보, 학생 발화, 교사 선택지, 점수와 피드백

## 아바타 계약

- Rocketbox 자녀 아바타 중 얼굴 블렌드셰이프가 포함된 FBX를 Humanoid로 임포트합니다.
- 기준 아바타마다 175개 블렌드셰이프가 있습니다. `FacialActionUnitController`는 AU1, AU2, AU4, AU5, AU6, AU7, AU9, AU12, AU15, AU17, AU20, AU23, AU24, AU25, AU26, AU45를 각각 0~1로 미세 제어합니다.
- 정서 상태는 valence(-1~1), arousal(0~1), dominance(-1~1)의 연속 벡터이며, 얼굴 근육 가중치는 프레임마다 목표값으로 보간됩니다.
- 몸동작은 성별에 맞는 착석 neutral, fidget, avoid-gaze, withdraw, protest, defiant, listen, recover 상태를 사용합니다.
- 일회성 제스처는 Exit Time 이후 Idle로 복귀합니다.
- 학생 정서 전환과 Animator 상태 전환은 `NpcPerformance`가 단일 진입점으로 관리합니다.

## 카메라와 UI 계약

- 기본 화면은 교사 시점 1600×900입니다.
- 중앙의 고조 학생 얼굴과 상체를 UI가 가리지 않아야 합니다.
- 정보 패널은 좌우 상단에 분리하고, 가운데 시야를 비워 둡니다.
- 정답을 색으로 암시하지 않도록 모든 선택지는 동일한 기본색을 사용합니다.
- 키보드 이동은 WASD, 시점 회전은 마우스 오른쪽 버튼입니다.
- `F` 키 또는 직접 대화 입력은 학생 얼굴을 향한 정면 대화 카메라로 부드럽게 이동합니다.

## 시나리오 계약

- 세 개의 고정 비트가 순서대로 진행됩니다: 초기 고조, 안전과 선택권, 회복 대화.
- 각 선택은 `ResponseScorer`가 결정론적으로 채점합니다.
- 외부 AI 코치가 꺼져 있거나 실패해도 훈련은 완주 가능해야 합니다.
- LLM 학생 응답은 `studentReply`, `valence`, `arousal`, `dominance`, `gesture` 구조로 파싱하며 범위를 경계에서 제한합니다.
- API 키가 없거나 요청이 실패하면 로컬 학생 모델이 같은 구조를 반환합니다.
- 각 반응은 개인정보를 포함하지 않는 JSONL 기록으로 남깁니다.
