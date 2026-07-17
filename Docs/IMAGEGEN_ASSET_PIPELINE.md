# ImageGen 에셋 제작 및 Unity 반영 루프

이 프로젝트는 스토리보드 이미지를 시각 기준으로 삼고, 반복 가능한 순서로 교실을 개선합니다.

## 현재 생성 에셋

| 역할 | 파일 | Unity 사용 위치 |
|---|---|---|
| 교실 스토리보드 | `Assets/Reference/Storyboard_KoreanClassroom.png` | 구도, 채광, 책상 배치, 한국 교실 단서의 기준 |
| 밝은 자작나무 책상 표면 | `Assets/Art/Textures/BirchDesk_Albedo.png` | 학생 책상과 교사용 책상 재질 |
| 회색·베이지 비닐 타일 바닥 | `Assets/Art/Textures/ClassroomFloor_Albedo.png` | 교실 바닥 재질 |
| 한국 학교 운동장과 교사동 | `Assets/Art/Textures/WindowBackdrop_KoreanSchool.png` | 오른쪽 창문 밖 배경 |

이미지는 Codex의 ImageGen2 계열 이미지 생성 도구로 제작한 뒤 Unity 작업 경로에 저장했습니다. 재질 이미지는 색 정보만 담은 무광 알베도 용도이며, 메시 형태와 조명은 Unity에서 제어합니다.

## 반복 루프

1. `Assets/Reference/Storyboard_KoreanClassroom.png`에서 바꾸려는 한 가지 시각 목표를 정합니다.
2. ImageGen에서 정사각형 또는 와이드 에셋을 생성합니다. 재질이면 `seamless, tileable, flat albedo, no perspective, no text, no objects` 조건을 포함합니다.
3. 결과를 `Assets/Art/Textures` 또는 `Assets/Reference`에 PNG로 저장합니다.
4. 동일한 목적의 기존 파일을 교체할 때는 파일명을 유지해 Unity 참조를 보존합니다.
5. Unity 메뉴 `Tools > Teacher Training > Build Korean Classroom`으로 씬을 다시 만듭니다.
6. `Tools > Teacher Training > Capture Classroom Preview`와 PlayMode QA 캡처를 생성합니다.
7. 스토리보드와 Unity 캡처를 1600×900으로 맞춰 구도, 가시성, 한국 교실 단서, 캐릭터 초점, UI 방해 정도를 비교합니다.
8. 한 번에 한 범주만 수정하고 3단계부터 반복합니다.

## 생성 프롬프트 기준

- 스토리보드: 사실적인 현대 한국 중학교 교실, 녹색 칠판, 오른쪽 큰 창, 뒤쪽 사물함, 밝은 목재 책상, 교사 시점, 4명의 착석 학생, 중앙의 정서적으로 고조된 학생, 자연스러운 낮빛, 교육 시뮬레이션용 와이드 구도.
- 책상: 밝은 자작나무 합판의 미세한 나뭇결, 무광, 균일 조명, 타일 가능, 오브젝트와 글자 없음.
- 바닥: 한국 학교에서 흔한 회색과 따뜻한 베이지 계열 비닐 타일, 낮은 대비, 무광, 타일 가능, 원근 없음.
- 창밖: 한국 학교 운동장과 저층 교사동, 맑은 낮, 창문 프레임 없음, 사람이 없는 자연스러운 원경.

## 품질 게이트

- 생성 이미지가 UV에서 반복될 때 눈에 띄는 경계가 없어야 합니다.
- 학생 얼굴과 손의 가독성을 텍스처보다 조명과 카메라가 우선 보장해야 합니다.
- 생성 이미지 안의 글자를 교실 표지판으로 사용하지 않습니다. 한글 표지는 Unity TextMeshPro/UGUI 또는 메시 오브젝트로 별도 구성합니다.
- 얼굴 정서 상태는 이미지로 굳히지 않고 Rocketbox 블렌드셰이프와 애니메이션으로 실시간 표현합니다.
- 수정 후 EditMode 테스트와 전체 PlayMode 훈련 흐름을 모두 통과해야 합니다.
