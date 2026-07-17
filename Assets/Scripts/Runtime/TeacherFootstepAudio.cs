using UnityEngine;

namespace AdieLab.TeacherTraining
{
    [DisallowMultipleComponent]
    public sealed class TeacherFootstepAudio : MonoBehaviour
    {
        [SerializeField, Min(0.1f)] private float stepDistance = 0.72f;
        [SerializeField, Range(0f, 1f)] private float volume = 0.42f;

        private AudioSource source;
        private Vector3 previousPosition;
        private float accumulatedDistance;
        private bool useRightStep;

        public bool IsPlaying => source != null && source.isPlaying;
        public int PlayedStepCount { get; private set; }

        private void Awake()
        {
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            source.loop = false;
            source.volume = 1f;
            previousPosition = transform.position;
            accumulatedDistance = stepDistance * 0.45f;
        }

        private void Update()
        {
            Vector3 displacement = Vector3.ProjectOnPlane(transform.position - previousPosition, Vector3.up);
            previousPosition = transform.position;

            float inputMagnitude = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical")).magnitude;
            if (inputMagnitude < 0.1f)
            {
                return;
            }

            accumulatedDistance += displacement.magnitude;
            if (accumulatedDistance < stepDistance)
            {
                return;
            }

            accumulatedDistance %= stepDistance;
            PlayStep();
        }

        public void PlayStep()
        {
            if (source == null)
            {
                return;
            }

            AudioClip clip = useRightStep ? ProceduralAudioClips.SlipperRight : ProceduralAudioClips.SlipperLeft;
            source.pitch = useRightStep ? 1.025f : 0.985f;
            source.PlayOneShot(clip, volume);
            useRightStep = !useRightStep;
            PlayedStepCount++;
        }
    }
}
