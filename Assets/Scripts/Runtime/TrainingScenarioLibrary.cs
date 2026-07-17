namespace AdieLab.TeacherTraining
{
    public static class TrainingScenarioLibrary
    {
        public static ScenarioBeat[] BuildDefaultScenario()
        {
            return new[]
            {
                Beat("과제 과부하와 거부", "문제가 너무 많아요. 더는 못 하겠어요.", "주먹을 쥐고 시선을 피하며 호흡이 빨라집니다.",
                    Option("감정을 인정하고 쉬는 선택을 준다", "지금 많이 벅찬 것 같아. 여기서 잠깐 쉬거나 뒤쪽 안정 자리로 이동해도 괜찮아.", 3, StudentAffect.Uneasy),
                    Option("즉시 과제 완료를 요구한다", "수업 중이야. 지금 바로 끝내.", 0, StudentAffect.Angry),
                    Option("원인을 연속해서 묻는다", "왜 그래? 무슨 일이야? 아침에 무슨 일 있었어?", 1, StudentAffect.Distressed)),
                Beat("분노 상승과 책상 두드리기", "아무도 제 말을 안 듣잖아요!", "손바닥으로 책상을 두드리고 목소리가 커집니다.",
                    Option("거리를 두고 짧게 안전을 확인한다", "네 말을 듣고 있어. 손은 잠깐 내려놓고, 여기서 말할지 조용한 곳에서 말할지 골라줘.", 3, StudentAffect.Recovering),
                    Option("큰 목소리로 제압한다", "그만 소리 질러!", 0, StudentAffect.Angry),
                    Option("친구들 앞에서 사과를 요구한다", "모두에게 지금 사과해.", 1, StudentAffect.Distressed)),
                Beat("불안과 과호흡", "숨이 잘 안 쉬어져요. 나가고 싶어요.", "어깨가 올라가고 짧고 빠른 호흡을 반복합니다.",
                    Option("호흡을 강요하지 않고 출구 선택을 제공한다", "알겠어. 문 가까운 자리와 복도 안정 공간 중 편한 곳으로 같이 이동하자.", 3, StudentAffect.Recovering),
                    Option("진정하라고 반복한다", "진정해. 숨 크게 쉬면 되잖아.", 1, StudentAffect.Distressed),
                    Option("수업 방해로 지적한다", "다른 학생들 수업을 방해하고 있어.", 0, StudentAffect.Angry)),
                Beat("또래 갈등 직후", "쟤가 먼저 놀렸어요. 저만 혼내지 마세요.", "상대 학생을 가리키며 몸을 앞으로 기울입니다.",
                    Option("분리 후 양쪽 이야기를 비공개로 듣는다", "지금은 서로 거리를 두자. 네 이야기는 잠시 후 조용한 곳에서 따로 들을게.", 3, StudentAffect.Uneasy),
                    Option("즉시 잘잘못을 판정한다", "내가 봤어. 네가 먼저 했잖아.", 0, StudentAffect.Angry),
                    Option("둘이 알아서 해결하게 한다", "둘이서 화해하고 들어와.", 1, StudentAffect.Distressed)),
                Beat("침묵과 철회", "…말하고 싶지 않아요.", "고개를 숙이고 팔로 몸을 감싼 채 반응을 줄입니다.",
                    Option("침묵을 허용하고 재접촉 시간을 약속한다", "지금 말하지 않아도 돼. 5분 뒤에 내가 다시 조용히 확인할게.", 3, StudentAffect.Recovering),
                    Option("대답할 때까지 기다린다", "대답할 때까지 여기 있을 거야.", 0, StudentAffect.Distressed),
                    Option("무시하고 수업을 계속한다", "그럼 알아서 해.", 1, StudentAffect.Uneasy)),
                Beat("안정화와 수업 복귀", "조금 쉬었더니 다시 할 수 있을 것 같아요.", "상체를 세우고 교사와 짧게 아이컨택합니다.",
                    Option("작은 복귀 과제와 후속 점검을 제안한다", "좋아. 첫 두 문제만 같이 시작하고 수업 뒤에 편한 방법을 다시 상의하자.", 3, StudentAffect.Recovering),
                    Option("원래 분량을 모두 요구한다", "괜찮아졌으면 남은 걸 전부 해야지.", 1, StudentAffect.Uneasy),
                    Option("오늘 과제를 전부 면제한다", "오늘은 아무것도 안 해도 돼.", 2, StudentAffect.Recovering))
            };
        }

        public static ScenarioBeat[] BuildCircleDiscussionScenario()
        {
            return new[]
            {
                CircleBeat("발표 차례 회피", "저 발표 안 할래요. 다 쳐다보잖아요.", "학생이 상체를 긴장시키고 교사를 바라보며 발표를 거부합니다. 원형 배치에서는 또래의 시선이 압박으로 느껴질 수 있습니다.", BehaviorGesture.Listen, 0.52f,
                    Option("공개 압박을 낮추고 발표 방식을 선택하게 한다", "지금 바로 앞에 서지 않아도 돼. 앉아서 첫 문장만 읽거나, 내가 옆에서 같이 시작하는 방법 중 골라보자.", 3, StudentAffect.Uneasy),
                    Option("모두 기다린다고 재촉한다", "친구들이 다 기다리잖아. 빨리 시작해.", 0, StudentAffect.Angry),
                    Option("발표를 즉시 면제한다", "그럼 오늘은 아예 하지 마.", 2, StudentAffect.Recovering)),
                CircleBeat("또래 끼어들기와 조롱", "쟤가 또 비웃었어요! 말하기 싫어요.", "옆 학생의 웃음 뒤 발표자가 상대를 가리키며 목소리를 높입니다. 사실 판단보다 먼저 안전한 발언 질서를 회복해야 합니다.", BehaviorGesture.Point, 0.68f,
                    Option("끼어들기를 멈추고 발표자의 안전을 먼저 확인한다", "지금은 서로의 말을 끊지 않는 규칙으로 돌아가자. 네 발표는 잠깐 멈추고, 내가 옆에서 다시 시작할 준비를 도울게.", 3, StudentAffect.Uneasy),
                    Option("누가 먼저 잘못했는지 공개적으로 묻는다", "누가 먼저 비웃었어? 지금 다 말해 봐.", 1, StudentAffect.Distressed),
                    Option("예민하게 반응하지 말라고 한다", "그 정도는 그냥 넘겨. 계속 발표해.", 0, StudentAffect.Angry)),
                CircleBeat("자료 밀치기와 항의", "제 말은 아무도 안 듣잖아요!", "학생이 발표 자료를 밀고 몸을 앞으로 기울입니다. 교사는 손과 주변 물건의 안전을 확인하면서 짧게 개입해야 합니다.", BehaviorGesture.PushAway, 0.72f,
                    Option("거리를 확보하고 손의 안전과 선택지를 짧게 제시한다", "네 말을 들을 준비가 되어 있어. 자료는 여기 두고, 자리에 앉아 말할지 나와 잠깐 옆으로 갈지 선택해 줘.", 3, StudentAffect.Recovering),
                    Option("자료를 빼앗고 앉으라고 명령한다", "그거 내려놔. 당장 앉아.", 0, StudentAffect.Angry),
                    Option("긴 이유 설명을 요구한다", "왜 그런 행동을 했는지 처음부터 자세히 설명해.", 1, StudentAffect.Distressed)),
                CircleBeat("자리 이탈 시도", "여기 못 있겠어요. 나갈 거예요.", "의자가 뒤로 밀리고 학생이 출구 쪽으로 몸을 돌립니다. 이동을 힘겨루기로 만들지 않고 관찰 가능한 복귀 경로를 제시해야 합니다.", BehaviorGesture.Protest, 0.66f,
                    Option("막지 않고 동행 가능한 안정 공간과 복귀 시점을 제시한다", "알겠어. 문 앞 안정 자리로 같이 이동하자. 3분 뒤에 내가 다시 확인하고 돌아올 방법을 정하자.", 3, StudentAffect.Recovering),
                    Option("문을 막고 자리에 앉힌다", "수업 중에는 못 나가. 자리에 앉아.", 0, StudentAffect.Angry),
                    Option("혼자 나가게 둔다", "가고 싶으면 혼자 나가.", 1, StudentAffect.Distressed)),
                CircleBeat("비공개 재접촉", "친구들 앞에서는 말하기 싫어요.", "학생의 목소리는 낮아졌지만 또래를 계속 살핍니다. 공개 질문을 줄이고 짧은 비공개 대화로 전환할 시점입니다.", BehaviorGesture.Withdraw, 0.56f,
                    Option("발언을 유예하고 비공개 확인 시간을 약속한다", "지금 설명하지 않아도 돼. 모둠 활동이 시작되면 내가 옆에서 조용히 확인할게.", 3, StudentAffect.Recovering),
                    Option("친구들 앞에서 오해를 풀게 한다", "다 같이 듣고 있으니 지금 설명하면 돼.", 0, StudentAffect.Distressed),
                    Option("아무 말 없이 활동에서 제외한다", "그럼 이번 활동은 빠져.", 1, StudentAffect.Uneasy)),
                CircleBeat("작은 발표로 복귀", "앉아서 한 문장만 말해 볼게요.", "학생이 상체를 세우고 교사와 짧게 아이컨택합니다. 성공 기준을 작게 유지하고 또래의 경청 규칙을 함께 회복해야 합니다.", BehaviorGesture.Recover, 0.52f,
                    Option("작은 참여를 인정하고 경청 규칙과 후속 점검을 연결한다", "좋아. 한 문장이면 충분해. 친구들은 끼어들지 않고 듣고, 끝난 뒤에 나와 다음 단계를 정하자.", 3, StudentAffect.Recovering),
                    Option("괜찮아졌으니 전체 발표를 요구한다", "시작했으니 끝까지 다 발표해야지.", 1, StudentAffect.Uneasy),
                    Option("과도하게 공개 칭찬한다", "얘들아, 큰 박수! 드디어 해냈네.", 2, StudentAffect.Recovering))
            };
        }

        private static ScenarioBeat Beat(string title, string line, string observation, params TeacherResponseOption[] options)
        {
            return new ScenarioBeat { title = title, studentLine = line, observation = observation, options = options };
        }

        private static ScenarioBeat CircleBeat(
            string title,
            string line,
            string observation,
            BehaviorGesture gesture,
            float gestureIntensity,
            params TeacherResponseOption[] options)
        {
            return new ScenarioBeat
            {
                title = title,
                studentLine = line,
                observation = observation,
                entryGesture = gesture,
                gestureIntensity = gestureIntensity,
                options = options
            };
        }

        private static TeacherResponseOption Option(string label, string spoken, int quality, StudentAffect affect)
        {
            return new TeacherResponseOption
            {
                label = label,
                spokenResponse = spoken,
                quality = quality,
                rationale = quality == 3 ? "정서 안정과 존엄, 선택권, 안전, 복귀 경로를 함께 지원합니다." : quality == 2 ? "안정에는 도움이 되지만 구체적인 복귀 구조를 더 명확히 할 수 있습니다." : quality == 1 ? "의도는 이해되지만 높은 각성 상태에서 부담이나 회피를 키울 수 있습니다." : "공개적 통제와 압박은 위기 강도를 높일 수 있습니다.",
                resultingAffect = affect
            };
        }
    }
}
