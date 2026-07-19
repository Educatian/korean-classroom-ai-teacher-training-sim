using System;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    internal enum LlmResponseContract
    {
        PlainText,
        StudentTurn,
        TeacherRubric
    }

    internal static class OpenRouterRequestFactory
    {
        [Serializable]
        private sealed class ChatMessage
        {
            public string role;
            public string content;
        }

        [Serializable]
        private sealed class ChatRequest
        {
            public string model;
            public ChatMessage[] messages;
            public int max_tokens;
            public float temperature;
            public RequestMetadata metadata;
        }

        [Serializable]
        private sealed class RequestMetadata
        {
            public int prompt_version;
        }

        private const string StudentSchema =
            "{\"type\":\"json_schema\",\"json_schema\":{\"name\":\"student_turn\",\"strict\":true,\"schema\":{" +
            "\"type\":\"object\",\"properties\":{" +
            "\"studentReply\":{\"type\":\"string\"}," +
            "\"valence\":{\"type\":\"number\",\"minimum\":-1,\"maximum\":1}," +
            "\"arousal\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}," +
            "\"dominance\":{\"type\":\"number\",\"minimum\":-1,\"maximum\":1}," +
            "\"gesture\":{\"type\":\"string\",\"enum\":[\"Neutral\",\"AvoidGaze\",\"Fidget\",\"Withdraw\",\"Protest\",\"Defiant\",\"DeskTap\",\"Shield\",\"Point\",\"PushAway\",\"Listen\",\"Recover\"]}," +
            "\"actionUnits\":{\"type\":\"object\",\"properties\":{" +
            "\"au1\":{\"type\":\"number\"},\"au2\":{\"type\":\"number\"},\"au4\":{\"type\":\"number\"},\"au5\":{\"type\":\"number\"},\"au6\":{\"type\":\"number\"},\"au7\":{\"type\":\"number\"},\"au9\":{\"type\":\"number\"},\"au12\":{\"type\":\"number\"},\"au15\":{\"type\":\"number\"},\"au17\":{\"type\":\"number\"},\"au20\":{\"type\":\"number\"},\"au23\":{\"type\":\"number\"},\"au24\":{\"type\":\"number\"},\"au25\":{\"type\":\"number\"},\"au26\":{\"type\":\"number\"}}," +
            "\"required\":[\"au1\",\"au2\",\"au4\",\"au5\",\"au6\",\"au7\",\"au9\",\"au12\",\"au15\",\"au17\",\"au20\",\"au23\",\"au24\",\"au25\",\"au26\"],\"additionalProperties\":false}," +
            "\"dialogueSignals\":{\"type\":\"object\",\"properties\":{" +
            "\"feltHeard\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},\"perceivedPressure\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},\"choiceOffered\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},\"safetyConcern\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1},\"readyForReentry\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}}," +
            "\"required\":[\"feltHeard\",\"perceivedPressure\",\"choiceOffered\",\"safetyConcern\",\"readyForReentry\"],\"additionalProperties\":false}}," +
            "\"required\":[\"studentReply\",\"valence\",\"arousal\",\"dominance\",\"gesture\",\"actionUnits\",\"dialogueSignals\"],\"additionalProperties\":false}}}";

        private const string RubricSchema =
            "{\"type\":\"json_schema\",\"json_schema\":{\"name\":\"teacher_rubric\",\"strict\":true,\"schema\":{" +
            "\"type\":\"object\",\"properties\":{" +
            "\"schemaVersion\":{\"type\":\"integer\",\"const\":1}," +
            "\"confidence\":{\"type\":\"number\",\"minimum\":0,\"maximum\":1}," +
            "\"dimensions\":{\"type\":\"array\",\"minItems\":6,\"maxItems\":6,\"items\":{\"type\":\"object\",\"properties\":{" +
            "\"dimension\":{\"type\":\"integer\",\"enum\":[0,1,2,3,4,5]},\"score\":{\"type\":\"number\",\"minimum\":0,\"maximum\":3},\"evidence\":{\"type\":\"string\"}}," +
            "\"required\":[\"dimension\",\"score\",\"evidence\"],\"additionalProperties\":false}}," +
            "\"improvementSuggestion\":{\"type\":\"string\"}}," +
            "\"required\":[\"schemaVersion\",\"confidence\",\"dimensions\",\"improvementSuggestion\"],\"additionalProperties\":false}}}";

        public static string Create(
            string system,
            string user,
            LlmResponseContract contract,
            string model,
            OpenRouterRuntimeConfiguration configuration)
        {
            var request = new ChatRequest
            {
                model = model,
                messages = new[]
                {
                    new ChatMessage { role = "system", content = system },
                    new ChatMessage { role = "user", content = user }
                },
                max_tokens = configuration.MaxTokens,
                temperature = configuration.Temperature,
                metadata = new RequestMetadata { prompt_version = configuration.PromptVersion }
            };
            string json = JsonUtility.ToJson(request);
            if (contract == LlmResponseContract.PlainText)
            {
                return json;
            }

            string schema = contract == LlmResponseContract.StudentTurn ? StudentSchema : RubricSchema;
            return json.Substring(0, json.Length - 1) +
                   ",\"response_format\":" + schema +
                   ",\"provider\":{\"require_parameters\":true,\"data_collection\":\"deny\"}}";
        }
    }
}
