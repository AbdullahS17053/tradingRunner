using System.Collections;
using UnityEngine;

namespace RunnerForex.Castle
{
    public class CastleComputerTrigger : MonoBehaviour
    {
        [Header("Chair & Desk Alignment")]
        public Transform chairSeatTransform;
        public Transform computerCameraPosition;
        public float cameraSwoopDuration = 1.2f;

        [Header("Audio")]
        public AudioClip typingSound;
        public AudioSource computerAudioSource;

        private bool hasTriggered = false;
        private Camera activeCamera;
        private Vector3 runnerCamOriginalPos;
        private Quaternion runnerCamOriginalRot;

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered) return;

            if (other.CompareTag("Player"))
            {
                hasTriggered = true;
                RunnerController runner = other.GetComponent<RunnerController>();
                StartCoroutine(SitDownSequenceRoutine(runner));
            }
        }

        private IEnumerator SitDownSequenceRoutine(RunnerController runner)
        {
            // 1. Freeze runner controls
            if (runner != null)
            {
                runner.enabled = false;
            }
            if (GameManager.Instance != null)
            {
                GameManager.Instance.isTradingChallengeActive = true;
            }

            // 2. Align player to chair seat
            if (chairSeatTransform != null && runner != null)
            {
                Vector3 startPos = runner.transform.position;
                Quaternion startRot = runner.transform.rotation;
                float alignElapsed = 0f;
                while (alignElapsed < 0.4f)
                {
                    alignElapsed += Time.deltaTime;
                    float t = alignElapsed / 0.4f;
                    runner.transform.position = Vector3.Lerp(startPos, chairSeatTransform.position, t);
                    runner.transform.rotation = Quaternion.Slerp(startRot, chairSeatTransform.rotation, t);
                    yield return null;
                }
                runner.transform.position = chairSeatTransform.position;
                runner.transform.rotation = chairSeatTransform.rotation;
            }

            // 3. Play Sitting Pose animation
            if (runner != null && runner.animator != null)
            {
                runner.animator.Play("SittingPose", 0, 0f);
            }

            // 4. Camera Swoop to Computer Screen
            activeCamera = Camera.main;
            if (activeCamera != null && computerCameraPosition != null)
            {
                runnerCamOriginalPos = activeCamera.transform.position;
                runnerCamOriginalRot = activeCamera.transform.rotation;

                float camElapsed = 0f;
                while (camElapsed < cameraSwoopDuration)
                {
                    camElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, camElapsed / cameraSwoopDuration);
                    activeCamera.transform.position = Vector3.Lerp(runnerCamOriginalPos, computerCameraPosition.position, t);
                    activeCamera.transform.rotation = Quaternion.Slerp(runnerCamOriginalRot, computerCameraPosition.rotation, t);
                    yield return null;
                }
                activeCamera.transform.position = computerCameraPosition.position;
                activeCamera.transform.rotation = computerCameraPosition.rotation;
            }

            // 5. Play typing sounds
            if (computerAudioSource != null)
            {
                if (typingSound != null) computerAudioSource.clip = typingSound;
                else if (SoundManager.Instance != null && SoundManager.Instance.collectTPSound != null) computerAudioSource.clip = SoundManager.Instance.collectTPSound;

                if (computerAudioSource.clip != null)
                {
                    computerAudioSource.loop = true;
                    computerAudioSource.Play();
                }
            }

            // 6. Launch Castle Challenge
            if (CastleChallengeController.Instance != null)
            {
                CastleChallengeController.Instance.computerTrigger = this;
                CastleChallengeController.Instance.StartCastleChallengeSession();
            }
        }

        public void PlayerStandUpAndUnlockPortal(GameObject exitPortal)
        {
            StartCoroutine(StandUpSequenceRoutine(exitPortal));
        }

        private IEnumerator StandUpSequenceRoutine(GameObject exitPortal)
        {
            if (computerAudioSource != null)
            {
                computerAudioSource.Stop();
            }

            RunnerController runner = RunnerController.Instance;

            // 1. Play Sit To Stand trigger
            if (runner != null && runner.animator != null)
            {
                runner.animator.ResetTrigger("SitToStand");
                runner.animator.SetTrigger("SitToStand");
            }

            // 2. Camera Swoop back to runner view
            if (activeCamera != null)
            {
                Vector3 currentCamPos = activeCamera.transform.position;
                Quaternion currentCamRot = activeCamera.transform.rotation;

                float camElapsed = 0f;
                while (camElapsed < cameraSwoopDuration)
                {
                    camElapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, camElapsed / cameraSwoopDuration);
                    activeCamera.transform.position = Vector3.Lerp(currentCamPos, runnerCamOriginalPos, t);
                    activeCamera.transform.rotation = Quaternion.Slerp(currentCamRot, runnerCamOriginalRot, t);
                    yield return null;
                }
                activeCamera.transform.position = runnerCamOriginalPos;
                activeCamera.transform.rotation = runnerCamOriginalRot;
            }

            // 3. Unlock exit portal with particle effect
            if (exitPortal != null)
            {
                exitPortal.SetActive(true);
            }

            // 4. Stand complete -> start running again
            if (runner != null)
            {
                if (runner.animator != null)
                {
                    runner.animator.SetBool("GameStarted", true);
                }
                runner.enabled = true;
            }

            if (GameManager.Instance != null)
            {
                GameManager.Instance.isTradingChallengeActive = false;
            }
        }
    }
}
