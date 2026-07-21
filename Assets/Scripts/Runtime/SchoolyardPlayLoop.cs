using System.Collections.Generic;
using UnityEngine;

namespace AdieLab.TeacherTraining
{
    /// <summary>
    /// Recess-time ambience for the schoolyard: background classmates slowly
    /// drift between spots near their home position (subtle bob, no walk clip
    /// exists in the rig) while a trio passes a soccer ball in low arcs. Purely
    /// cosmetic - the focal student and the assessed flow are untouched.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class SchoolyardPlayLoop : MonoBehaviour
    {
        private const float WanderRadius = 2.6f;
        private const float WanderSpeed = 0.45f;
        private const float BallFlightSeconds = 1.15f;
        private const float BallRestSeconds = 0.9f;

        private readonly List<Transform> wanderers = new List<Transform>();
        private readonly List<Vector3> homes = new List<Vector3>();
        private readonly List<Vector3> targets = new List<Vector3>();
        private readonly List<float> waitTimers = new List<float>();
        private readonly List<float> baseHeights = new List<float>();

        private readonly List<NpcPerformance> ballPlayers = new List<NpcPerformance>();
        private Transform ball;
        private int ballHolder;
        private int ballReceiver;
        private float ballTimer;
        private bool ballInFlight;
        private Vector3 ballFrom;
        private Vector3 ballTo;

        public static SchoolyardPlayLoop Install(
            NpcPerformance focalStudent,
            NpcPerformance[] classmates)
        {
            if (classmates == null || classmates.Length == 0)
            {
                return null;
            }

            var host = new GameObject("SchoolyardPlayLoop");
            var loop = host.AddComponent<SchoolyardPlayLoop>();
            var active = new List<NpcPerformance>();
            foreach (NpcPerformance classmate in classmates)
            {
                if (classmate != null &&
                    classmate != focalStudent &&
                    classmate.gameObject.activeInHierarchy)
                {
                    active.Add(classmate);
                    // No desks outdoors: the desk-contact IK bends moving NPCs
                    // into a mid-air sit the moment they leave their anchor.
                    var chinRest = classmate.GetComponentInChildren<ChinRestDeskContactController>(true);
                    if (chinRest != null)
                    {
                        chinRest.enabled = false;
                    }
                }
            }

            // The three classmates nearest each other become the ball trio;
            // everyone else drifts.
            for (int index = 0; index < active.Count; index++)
            {
                if (index < 3)
                {
                    loop.ballPlayers.Add(active[index]);
                }
                else
                {
                    Transform root = active[index].transform;
                    loop.wanderers.Add(root);
                    loop.homes.Add(root.position);
                    loop.targets.Add(root.position);
                    loop.waitTimers.Add(1.5f + (index % 5) * 0.8f);
                    loop.baseHeights.Add(root.position.y);
                }
            }

            if (loop.ballPlayers.Count >= 2)
            {
                loop.BuildBall();
            }

            return loop;
        }

        private void BuildBall()
        {
            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.name = "PlayLoopSoccerBall";
            Object.Destroy(sphere.GetComponent<Collider>());
            sphere.transform.localScale = Vector3.one * 0.24f;
            var renderer = sphere.GetComponent<Renderer>();
            renderer.material.color = new Color(0.93f, 0.93f, 0.9f, 1f);
            ball = sphere.transform;
            ballHolder = 0;
            ball.position = FootOf(ballPlayers[0]);
            ballTimer = BallRestSeconds;
            ballInFlight = false;
        }

        private void Update()
        {
            UpdateWanderers();
            UpdateBall();
        }

        private void UpdateWanderers()
        {
            for (int index = 0; index < wanderers.Count; index++)
            {
                Transform wanderer = wanderers[index];
                if (wanderer == null)
                {
                    continue;
                }

                if (waitTimers[index] > 0f)
                {
                    waitTimers[index] -= Time.deltaTime;
                    if (waitTimers[index] <= 0f)
                    {
                        Vector2 offset = Random.insideUnitCircle * WanderRadius;
                        targets[index] = homes[index] + new Vector3(offset.x, 0f, offset.y);
                    }

                    continue;
                }

                Vector3 position = wanderer.position;
                Vector3 target = new Vector3(targets[index].x, baseHeights[index], targets[index].z);
                Vector3 delta = target - position;
                delta.y = 0f;
                if (delta.magnitude < 0.08f)
                {
                    waitTimers[index] = 2.2f + Random.value * 3.5f;
                    continue;
                }

                Vector3 step = delta.normalized * (WanderSpeed * Time.deltaTime);
                float bob = Mathf.Abs(Mathf.Sin(Time.time * 6.4f + index)) * 0.028f;
                wanderer.position = new Vector3(
                    position.x + step.x,
                    baseHeights[index] + bob,
                    position.z + step.z);
                Quaternion facing = Quaternion.LookRotation(delta.normalized, Vector3.up);
                wanderer.rotation = Quaternion.Slerp(wanderer.rotation, facing, Time.deltaTime * 2.4f);
            }
        }

        private void UpdateBall()
        {
            if (ball == null || ballPlayers.Count < 2)
            {
                return;
            }

            ballTimer -= Time.deltaTime;
            if (ballInFlight)
            {
                float progress = 1f - Mathf.Clamp01(ballTimer / BallFlightSeconds);
                Vector3 flat = Vector3.Lerp(ballFrom, ballTo, progress);
                float arc = Mathf.Sin(progress * Mathf.PI) * 0.9f;
                ball.position = flat + Vector3.up * arc;
                ball.Rotate(Vector3.right, 420f * Time.deltaTime, Space.World);
                if (ballTimer <= 0f)
                {
                    ballInFlight = false;
                    ballHolder = ballReceiver;
                    ballTimer = BallRestSeconds + Random.value * 1.4f;
                    NpcPerformance receiver = ballPlayers[ballHolder];
                    if (receiver != null)
                    {
                        receiver.SetAmbientGesture(BehaviorGesture.Recover, 0.34f, 2.2f);
                    }
                }

                return;
            }

            if (ballTimer <= 0f)
            {
                ballReceiver = (ballHolder + 1 + Random.Range(0, ballPlayers.Count - 1)) % ballPlayers.Count;
                if (ballReceiver == ballHolder)
                {
                    ballReceiver = (ballHolder + 1) % ballPlayers.Count;
                }

                ballFrom = FootOf(ballPlayers[ballHolder]);
                ballTo = FootOf(ballPlayers[ballReceiver]);
                ballInFlight = true;
                ballTimer = BallFlightSeconds;
                NpcPerformance kicker = ballPlayers[ballHolder];
                if (kicker != null)
                {
                    kicker.SetAmbientGesture(BehaviorGesture.Point, 0.4f, 1.6f);
                }
            }
        }

        private static Vector3 FootOf(NpcPerformance player)
        {
            if (player == null)
            {
                return Vector3.zero;
            }

            Vector3 position = player.transform.position;
            return position + player.transform.forward * 0.55f + Vector3.up * 0.12f;
        }
    }
}
