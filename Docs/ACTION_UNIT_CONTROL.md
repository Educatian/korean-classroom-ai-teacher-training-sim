# 얼굴 Action Unit 및 제스처 제어

`NpcPerformance.SetActionUnit(unit, intensity)`로 얼굴 근육을 개별 제어합니다. intensity는 0~1이며 실제 Rocketbox 블렌드셰이프 가중치 0~100으로 변환됩니다. `SetAffectVector`는 정서 벡터와 제스처를 함께 바꾸며, 얼굴 값은 프레임마다 부드럽게 전이됩니다.

| FACS 단위 | 의미 | 프로젝트 사용 |
|---|---|---|
| AU1 | 안쪽 눈썹 올림 | 불안, 슬픔, 도움 요청 |
| AU2 | 바깥 눈썹 올림 | 놀람, 불확실성 |
| AU4 | 눈썹 내림 | 분노, 대립, 집중 |
| AU5 | 윗눈꺼풀 올림 | 높은 각성, 경계 |
| AU6 | 볼 올림 | 완화된 긍정 정서 |
| AU7 | 눈꺼풀 조임 | 긴장, 분노 |
| AU9 | 코 찡그림 | 강한 거부감 |
| AU12 | 입꼬리 당김 | 회복, 안도 |
| AU15 | 입꼬리 내림 | 슬픔, 무력감 |
| AU17 | 턱 올림 | 억눌린 울음, 긴장 |
| AU20 | 입술 늘임 | 두려움, 불안 |
| AU23 | 입술 조임 | 통제된 분노 |
| AU24 | 입술 누름 | 말 억제, 저항 |
| AU25 | 입술 벌림 | 호흡과 발화 준비 |
| AU26 | 턱 내림 | 놀람, 격한 발화 |
| AU45 | 눈 깜박임 | 자동 미세 움직임 |

## 문제행동 제스처 상태

- `AvoidGaze`: 시선 회피와 낮은 눈맞춤 가중치
- `Fidget`: 착석 초조 동작
- `Withdraw`: 상체를 접고 관계에서 물러나는 동작
- `Protest`: 손과 팔을 이용한 거부 표현
- `Defiant`: 높은 주도성의 격앙된 반응
- `Listen`: 교사의 말을 듣는 정면 시선
- `Recover`: 긴장이 낮아지는 착석 제스처

LLM은 위 상태명 중 하나를 반환합니다. 알 수 없는 값은 `Fidget`으로 안전하게 대체합니다. Animator는 모든 상태를 착석 동작으로 구성해 책상과의 충돌 및 서 있는 자세로의 급격한 전환을 줄였습니다.

## 코드 예시

```csharp
npc.SetActionUnit(FacialActionUnit.AU4BrowLowerer, 0.65f);
npc.SetActionUnit(FacialActionUnit.AU24LipPressor, 0.50f);
npc.SetAffectVector(
    new AffectVector(valence: -0.70f, arousal: 0.85f, dominance: 0.55f),
    BehaviorGesture.Defiant);
```
