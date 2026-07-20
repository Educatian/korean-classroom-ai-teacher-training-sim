using System;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Security;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace AdieLab.TeacherTraining
{
    [Serializable]
    public struct StudentSpeechProsody
    {
        [Range(0.65f, 1.35f)] public float rate;
        [Range(-6f, 6f)] public float pitchSemitones;
        [Range(0f, 1f)] public float volume;
        public int commaPauseMilliseconds;
        public int sentencePauseMilliseconds;
        public float estimatedDurationSeconds;
    }

    public static class StudentSpeechProsodyPlanner
    {
        public static StudentSpeechProsody Plan(string text, AffectVector affect)
        {
            int characterCount = CountSpeechCharacters(text);
            float rate = Mathf.Clamp(0.86f + affect.arousal * 0.24f + affect.dominance * 0.05f, 0.72f, 1.20f);
            float pitch = Mathf.Clamp(affect.valence * 1.8f + affect.arousal * 1.2f - 0.4f, -3.5f, 3.5f);
            float volume = Mathf.Clamp01(0.66f + affect.arousal * 0.28f);
            int commaPause = Mathf.RoundToInt(Mathf.Lerp(250f, 105f, affect.arousal));
            int sentencePause = Mathf.RoundToInt(Mathf.Lerp(520f, 240f, affect.arousal));
            float duration = Mathf.Clamp(
                characterCount * 0.105f / rate + CountPunctuation(text) * commaPause * 0.001f,
                1.1f,
                12f);
            return new StudentSpeechProsody
            {
                rate = rate,
                pitchSemitones = pitch,
                volume = volume,
                commaPauseMilliseconds = commaPause,
                sentencePauseMilliseconds = sentencePause,
                estimatedDurationSeconds = duration
            };
        }

        public static string BuildSsml(string text, StudentSpeechProsody prosody)
        {
            string escaped = SecurityElement.Escape(text ?? string.Empty) ?? string.Empty;
            escaped = escaped
                .Replace(",", $",<break time=\"{prosody.commaPauseMilliseconds}ms\"/>")
                .Replace("，", $"，<break time=\"{prosody.commaPauseMilliseconds}ms\"/>")
                .Replace(".", $".<break time=\"{prosody.sentencePauseMilliseconds}ms\"/>")
                .Replace("。", $"。<break time=\"{prosody.sentencePauseMilliseconds}ms\"/>")
                .Replace("?", $"?<break time=\"{prosody.sentencePauseMilliseconds}ms\"/>")
                .Replace("!", $"!<break time=\"{prosody.sentencePauseMilliseconds}ms\"/>");
            int ratePercent = Mathf.RoundToInt(prosody.rate * 100f);
            int pitch = Mathf.RoundToInt(prosody.pitchSemitones);
            int volume = Mathf.RoundToInt(prosody.volume * 100f);
            return $"<speak version=\"1.0\" xml:lang=\"ko-KR\"><prosody rate=\"{ratePercent}%\" pitch=\"{pitch:+0;-0;0}st\" volume=\"{volume}%\">{escaped}</prosody></speak>";
        }

        public static float ScheduledMouthEnvelope(float elapsed, string text, StudentSpeechProsody prosody)
        {
            if (elapsed < 0f || elapsed > prosody.estimatedDurationSeconds)
            {
                return 0f;
            }

            float cadence = Mathf.Lerp(8.4f, 13.2f, Mathf.InverseLerp(0.72f, 1.20f, prosody.rate));
            float articulation = Mathf.Abs(Mathf.Sin(elapsed * cadence));
            if (IsLikelyPause(elapsed, text, prosody))
            {
                articulation *= 0.08f;
            }

            return Mathf.SmoothStep(0.04f, 0.72f, articulation);
        }

        private static bool IsLikelyPause(float elapsed, string text, StudentSpeechProsody prosody)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return false;
            }

            float progress = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, prosody.estimatedDurationSeconds));
            int index = Mathf.Clamp(Mathf.FloorToInt(progress * text.Length), 0, text.Length - 1);
            char value = text[index];
            return value == ',' || value == '，' || value == '.' || value == '。' || value == '?' || value == '!';
        }

        private static int CountSpeechCharacters(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return 1;
            }

            int count = 0;
            for (int index = 0; index < text.Length; index++)
            {
                if (!char.IsWhiteSpace(text[index]) && !char.IsPunctuation(text[index]))
                {
                    count++;
                }
            }

            return Mathf.Max(1, count);
        }

        private static int CountPunctuation(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return 0;
            }

            int count = 0;
            for (int index = 0; index < text.Length; index++)
            {
                if (char.IsPunctuation(text[index]))
                {
                    count++;
                }
            }

            return count;
        }
    }

    [DisallowMultipleComponent]
    public sealed class StudentSpeechSynthesizer : MonoBehaviour
    {
        public const string VoiceDisclosure = "학생 음성은 합성 음성입니다.";
        private Coroutine synthesis;

        public string DisclosureLabel => VoiceDisclosure;
        public bool IsLocallyAvailable
        {
            get
            {
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                return true;
#else
                return false;
#endif
            }
        }

        public void Synthesize(
            string text,
            AffectVector affect,
            Action<AudioClip, StudentSpeechProsody> completed,
            Action<string> failed)
        {
            Cancel();
            StudentSpeechProsody prosody = StudentSpeechProsodyPlanner.Plan(text, affect);
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            string apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
            synthesis = !string.IsNullOrWhiteSpace(apiKey)
                ? StartCoroutine(SynthesizeOpenAi(text, prosody, apiKey, completed, failed))
                : StartCoroutine(SynthesizeWindows(text, prosody, completed, failed));
#else
            failed?.Invoke("이 플랫폼은 보안 TTS 프록시 연결이 필요합니다.");
#endif
        }

        public void Cancel()
        {
            if (synthesis != null)
            {
                StopCoroutine(synthesis);
                synthesis = null;
            }
        }

        private void OnDisable()
        {
            Cancel();
        }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        [Serializable]
        private sealed class OpenAiSpeechRequest
        {
            public string model;
            public string input;
            public string voice;
            public string instructions;
            public string response_format;
        }

        private IEnumerator SynthesizeOpenAi(
            string text,
            StudentSpeechProsody prosody,
            string apiKey,
            Action<AudioClip, StudentSpeechProsody> completed,
            Action<string> failed)
        {
            var payload = new OpenAiSpeechRequest
            {
                model = Environment.GetEnvironmentVariable("OPENAI_TTS_MODEL") ?? "gpt-4o-mini-tts",
                input = text,
                voice = Environment.GetEnvironmentVariable("OPENAI_TTS_VOICE") ?? "coral",
                response_format = "wav",
                instructions = $"한국어 초등학생 역할의 자연스러운 합성 음성. 감정가는 {prosody.pitchSemitones:+0.0;-0.0;0.0}, 속도는 {prosody.rate:0.00}, 문장 사이에는 {prosody.sentencePauseMilliseconds}밀리초 정도 쉬어 말한다. 과장하지 않고 교실 대화처럼 말한다."
            };
            byte[] body = Encoding.UTF8.GetBytes(JsonUtility.ToJson(payload));
            using UnityWebRequest request = new UnityWebRequest(
                "https://api.openai.com/v1/audio/speech",
                UnityWebRequest.kHttpVerbPOST);
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.timeout = 30;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                yield return SynthesizeWindows(text, prosody, completed, failed);
                yield break;
            }

            string outputPath = Path.Combine(
                Application.temporaryCachePath,
                $"student-speech-openai-{Guid.NewGuid():N}.wav");
            bool writeFailed = false;
            try
            {
                File.WriteAllBytes(outputPath, request.downloadHandler.data);
            }
            catch (IOException)
            {
                writeFailed = true;
            }

            if (writeFailed)
            {
                yield return SynthesizeWindows(text, prosody, completed, failed);
                yield break;
            }

            string uri = new Uri(outputPath).AbsoluteUri;
            using UnityWebRequest audioRequest = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
            yield return audioRequest.SendWebRequest();
            if (audioRequest.result != UnityWebRequest.Result.Success)
            {
                try { File.Delete(outputPath); } catch (IOException) { }
                yield return SynthesizeWindows(text, prosody, completed, failed);
                yield break;
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(audioRequest);
            clip.name = "StudentSpeechOpenAi";
            completed?.Invoke(clip, prosody);
            try { File.Delete(outputPath); } catch (IOException) { }
            synthesis = null;
        }

        private IEnumerator SynthesizeWindows(
            string text,
            StudentSpeechProsody prosody,
            Action<AudioClip, StudentSpeechProsody> completed,
            Action<string> failed)
        {
            string outputPath = Path.Combine(
                Application.temporaryCachePath,
                $"student-speech-{Guid.NewGuid():N}.wav");
            string ssml = StudentSpeechProsodyPlanner.BuildSsml(text, prosody);
            string ssmlBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(ssml));
            string escapedOutput = outputPath.Replace("'", "''");
            string script = string.Concat(
                "Add-Type -AssemblyName System.Speech;",
                "$s=New-Object System.Speech.Synthesis.SpeechSynthesizer;",
                "$v=$s.GetInstalledVoices()|ForEach-Object{$_.VoiceInfo}|Where-Object{$_.Culture.Name -eq 'ko-KR'}|Select-Object -First 1;",
                "if($v){$s.SelectVoice($v.Name)};",
                "$x=[Text.Encoding]::UTF8.GetString([Convert]::FromBase64String('", ssmlBase64, "'));",
                "$s.SetOutputToWaveFile('", escapedOutput, "');$s.SpeakSsml($x);$s.Dispose();");
            string encodedCommand = Convert.ToBase64String(Encoding.Unicode.GetBytes(script));
            var startInfo = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -NonInteractive -ExecutionPolicy Bypass -EncodedCommand " + encodedCommand,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardError = true
            };

            Process process = null;
            try
            {
                process = Process.Start(startInfo);
            }
            catch (Exception exception)
            {
                failed?.Invoke($"로컬 학생 음성 합성을 시작하지 못했습니다: {exception.GetType().Name}");
                synthesis = null;
                yield break;
            }

            float timeout = Time.realtimeSinceStartup + 15f;
            while (process != null && !process.HasExited && Time.realtimeSinceStartup < timeout)
            {
                yield return null;
            }

            if (process == null || !process.HasExited)
            {
                try { process?.Kill(); } catch (InvalidOperationException) { }
                process?.Dispose();
                failed?.Invoke("로컬 학생 음성 합성 시간이 초과되었습니다.");
                synthesis = null;
                yield break;
            }

            string error = process.StandardError.ReadToEnd();
            int exitCode = process.ExitCode;
            process.Dispose();
            if (exitCode != 0 || !File.Exists(outputPath))
            {
                failed?.Invoke(string.IsNullOrWhiteSpace(error)
                    ? "한국어 시스템 음성을 사용할 수 없습니다."
                    : "한국어 시스템 음성 합성에 실패했습니다.");
                synthesis = null;
                yield break;
            }

            string uri = new Uri(outputPath).AbsoluteUri;
            using UnityWebRequest request = UnityWebRequestMultimedia.GetAudioClip(uri, AudioType.WAV);
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
            {
                failed?.Invoke("합성된 학생 음성을 불러오지 못했습니다.");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                clip.name = "StudentSpeech";
                completed?.Invoke(clip, prosody);
            }

            try { File.Delete(outputPath); } catch (IOException) { }
            synthesis = null;
        }
#endif
    }
}
