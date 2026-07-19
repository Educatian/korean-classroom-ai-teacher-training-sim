# 정면 대화 화면 개선 기록

## 2026-07-16

- 카메라의 좌우 오프셋을 제거해 학생을 화면 중앙에서 정면으로 마주보게 했습니다.
- 카메라 추적형 소프트 스포트라이트를 추가하고 중성에 가까운 웜 톤으로 조정해 얼굴과 눈 주변을 밝게 했습니다.
- 학생 답변을 얼굴 위치에 따라 움직이는 말풍선으로 표시했습니다.
- 말풍선에 청록색 외곽선, 아이보리 내부, 그림자, 꼬리, 0.18초 페이드·스케일 전환을 적용했습니다.
- UI Canvas 깊이를 카메라 가까이 조정해 정면 대화 중 교실 메시가 선택지 위를 가리는 현상을 제거했습니다.

## 비교 스크린샷

- 개선 전: `Assets/Reference/Unity_FaceToFace_Before.png`
- 개선 후: `Assets/Reference/Unity_FaceToFace_After.png`
- 좌우 비교: `Assets/Reference/Unity_FaceToFace_Comparison.png`
- 최신 자동 QA 캡처: `Assets/Reference/Unity_FaceToFace_Dialogue.png`
