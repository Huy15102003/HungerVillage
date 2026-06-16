using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;

namespace Game
{
    [RequireComponent(typeof(CharacterController))]
    public class FPController : MonoBehaviour
    {
        public static FPController Instance { get; private set; }
        [Header("Movement Parameters")]
        public float MaxSpeed => Sprinting ? SprintSpeed : WalkSpeed;
        public float Acceleration = 15f;

        [SerializeField] float WalkSpeed = 3.5f;
        [SerializeField] float SprintSpeed = 8f;

        public Vector3 CurrentVelocity { get; private set; }
        public float CurrentSpeed {get; private set; }

        public bool Sprinting
        {
            get
            {
                //return SprintInput && CurrentSpeed > 0.1f;
                return SprintInput && MoveInput.y > 0.1f;
            }
        }


        [Header("Look Parameters")]
        public float LookSensitivitySetting = 1f;
        public Vector2 LookSensitivity = Vector2.one;
        public float PitchLimit = 85f;
        [SerializeField] private float currentPitch = 0f;
        public float CurrentPitch
        {
            get => currentPitch;
            set
            {
                currentPitch = Mathf.Clamp(value, -PitchLimit, PitchLimit);
            }
        }
        
        [Header("Camera Parameters")]
        [SerializeField] float CameraNormalFOV = 60f;
        [SerializeField] float CameraSprintFOV = 80f;
        [SerializeField] float CameraFOVSmoothing = 1f;
        float TargetCameraFOV
        {
            get
            {
                return Sprinting ? CameraSprintFOV : CameraNormalFOV;
            }
        }

        [Header("Input")]
        public Vector2 MoveInput;
        public Vector2 LookInput;
        public bool SprintInput;

        [Header("Component")]
        [SerializeField] private CinemachineCamera Camera;
        [SerializeField] private CharacterController CharacterController;
        [Header("Pause")]
        public bool IsPaused { get; set; }
        public bool IsLookAtPaused => pausedLookTarget != null;
        Quaternion cachedPlayerRotation;
        float cachedPitch;
        bool hasCachedLook;
        Transform pausedLookTarget;
        Vector3 cachedCameraLocalPosition;
        bool hasCachedCameraLocalPosition;
        bool cameraYOffsetActive;
        float cameraLocalYOffset;
        Transform lockedPositionTarget;
        bool positionLockActive;

        [Header("Footstep")]
        [SerializeField] private float stepInterval = 0.5f; // thời gian giữa các bước
        [SerializeField] private float sprintStepMultiplier = 0.6f;

        private float stepTimer;

        #region Unity Methods
        void OnValidate()
        {
            if(CharacterController == null)
                CharacterController = GetComponent<CharacterController>();
        }
        #endregion

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            LookSensitivitySetting = PlayerPrefs.GetFloat("LookSensitivity", LookSensitivitySetting);
        }


        void Update()
        {
            if (IsPaused)
            {
                if (pausedLookTarget != null)
                {
                    LookAtTarget(pausedLookTarget.gameObject);
                }
                return;
            }
            MoveUpdate();
            LookUpdate();
            CameraUpdate();
            HandleFootsteps();
        }

        void LateUpdate()
        {
            if (positionLockActive && lockedPositionTarget != null)
            {
                transform.position = lockedPositionTarget.position;
            }

            if (cameraYOffsetActive && hasCachedCameraLocalPosition && Camera != null)
            {
                Vector3 targetPos = cachedCameraLocalPosition;
                targetPos.y += cameraLocalYOffset;
                Camera.transform.localPosition = targetPos;
            }
        }

        void HandleFootsteps()
        {
            if (!CharacterController.isGrounded)
            {
                stepTimer = 0f;
                return;
            }

            bool isMoving = CharacterController.velocity.magnitude > 0.1f;

            if (!isMoving)
            {
                stepTimer = 0f;
                return;
            }

            float interval = stepInterval;

            // chạy thì bước nhanh hơn
            if (Sprinting)
                interval *= sprintStepMultiplier;

            stepTimer -= Time.deltaTime;

            if (stepTimer <= 0f)
            {
                PlayFootstep();
                stepTimer = interval;
            }
        }
        void PlayFootstep()
        {
            string[] steps = { "Footstep1", "Footstep2", "Footstep3" };
            string randomStep = steps[Random.Range(0, steps.Length)];

            //Sau thêm sound thì nhớ bỏ comment để có tiếng bước chân
            //SoundManager.instance?.PlaySFX(randomStep);
        }

        #region Controller Methods
        private void MoveUpdate()
        {
            Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x;
            motion.y = 0;
            motion.Normalize();

            if (motion.sqrMagnitude >= 0.01f)
            {
                CurrentVelocity = Vector3.MoveTowards(
                    CurrentVelocity,
                    motion * MaxSpeed,
                    Acceleration * Time.deltaTime
                );
            }
            else
            {
                CurrentVelocity = Vector3.MoveTowards(
                    CurrentVelocity,
                    Vector3.zero,
                    Acceleration * Time.deltaTime
                );
            }

            float verticalVelocity = Physics.gravity.y * 20 * Time.deltaTime;
            Vector3 fullVelocity = new Vector3(CurrentVelocity.x, verticalVelocity, CurrentVelocity.z);

            CharacterController.Move(fullVelocity * Time.deltaTime);

            CurrentSpeed = CurrentVelocity.magnitude;
        }
        // private float _verticalVelocity = 0f;

        // private void MoveUpdate()
        // {
        //     // Tính hướng di chuyển
        //     Vector3 motion = transform.forward * MoveInput.y + transform.right * MoveInput.x;
        //     motion.y = 0;
        //     motion.Normalize();

        //     // 👉 DI CHUYỂN TỨC THÌ – KHÔNG ACCELERATION
        //     CurrentVelocity = motion * MaxSpeed;

        //     // --- Xử lý gravity ---
        //     if (CharacterController.isGrounded)
        //     {
        //         if (_verticalVelocity < 0)
        //             _verticalVelocity = -2f;
        //     }
        //     else
        //     {
        //         _verticalVelocity += Physics.gravity.y * Time.deltaTime;
        //     }

        //     // Tổng velocity
        //     Vector3 velocity = new Vector3(
        //         CurrentVelocity.x,
        //         _verticalVelocity,
        //         CurrentVelocity.z
        //     );

        //     CharacterController.Move(velocity * Time.deltaTime);

        //     CurrentSpeed = new Vector3(CurrentVelocity.x, 0, CurrentVelocity.z).magnitude;
        // }

        private void LookUpdate()
        {
            float sensX = LookSensitivitySetting * LookSensitivity.x;
            float sensY = LookSensitivitySetting * LookSensitivity.y;
            Vector2 input = new Vector2(LookInput.x * sensX, LookInput.y * sensY);
            
            CurrentPitch -= input.y;
            Camera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0, 0);

            transform.Rotate(Vector3.up * input.x);
        }

        void CameraUpdate()
        {
            float targetFOV = CameraNormalFOV;

            if(Sprinting)
            {
                float speedRatio = CurrentSpeed / SprintSpeed;

                targetFOV = Mathf.Lerp(CameraNormalFOV, CameraSprintFOV, speedRatio);
            }

            Camera.Lens.FieldOfView = Mathf.Lerp(
                Camera.Lens.FieldOfView,
                targetFOV,
                CameraFOVSmoothing * Time.deltaTime
            );
        }
        #endregion
        [ContextMenu("Pause Controller")]
        public void Pause()
        {
            IsPaused = true;
            CrosshairUI.Instance?.Show(false);
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            CurrentVelocity = Vector3.zero;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        public void PauseMenu()
        {
            CrosshairUI.Instance?.Show(false);
            MoveInput = Vector2.zero;
            LookInput = Vector2.zero;
            CurrentVelocity = Vector3.zero;
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        [ContextMenu("Resume Controller")]
        public void Resume()
        {
            //Debug.Log("Resuming Player Controller");
            CrosshairUI.Instance?.Show(true);
            pausedLookTarget = null;
            if (hasCachedLook)
            {
                transform.rotation = cachedPlayerRotation;
                CurrentPitch = cachedPitch;
                Camera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
                hasCachedLook = false;
            }

            IsPaused = false;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
        public void ResumeMenu()
        {
            if(IsPaused)
            {
                //Debug.Log("controller is already resumed");
                return;
            }else{
                //Debug.Log("resuming controller");
                Resume();
            }
        }

        public void PauseLookAt(GameObject target)
        {
            if (target == null) return;

            // ✅ Cache hướng nhìn hiện tại
            cachedPlayerRotation = transform.rotation;
            cachedPitch = CurrentPitch;
            hasCachedLook = true;
            pausedLookTarget = target.transform;

            // --- Look at NPC ---
            Vector3 dir = target.transform.position - Camera.transform.position;
            dir.Normalize();

            Quaternion lookRot = Quaternion.LookRotation(dir);
            Vector3 euler = lookRot.eulerAngles;

            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;

            CurrentPitch = pitch;
            Camera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);

            Pause();
        }

        public void UpdatePausedLookTarget(GameObject target)
        {
            if (target == null) return;

            pausedLookTarget = target.transform;
            if (IsPaused)
            {
                LookAtTarget(target);
            }
        }

        public void ApplyCameraLocalYOffset(float yOffset)
        {
            if (Camera == null)
            {
                return;
            }

            if (!hasCachedCameraLocalPosition)
            {
                cachedCameraLocalPosition = Camera.transform.localPosition;
                hasCachedCameraLocalPosition = true;
            }

            cameraLocalYOffset = yOffset;
            cameraYOffsetActive = true;
        }

        public void RestoreCameraLocalPosition()
        {
            if (Camera == null || !hasCachedCameraLocalPosition)
            {
                return;
            }

            Camera.transform.localPosition = cachedCameraLocalPosition;
            hasCachedCameraLocalPosition = false;
            cameraYOffsetActive = false;
        }

        public void LockPositionTo(Transform target)
        {
            if (target == null)
            {
                return;
            }

            lockedPositionTarget = target;
            positionLockActive = true;
            transform.position = target.position;
            CurrentVelocity = Vector3.zero;
        }

        public void UnlockPosition()
        {
            positionLockActive = false;
            lockedPositionTarget = null;
        }

        public void ClearPausedLookTarget()
        {
            pausedLookTarget = null;
        }

        void LookAtTarget(GameObject target)
        {
            if (target == null) return;

            Vector3 dir = target.transform.position - Camera.transform.position;
            dir.Normalize();

            Quaternion lookRot = Quaternion.LookRotation(dir);
            Vector3 euler = lookRot.eulerAngles;

            // Yaw cho player
            transform.rotation = Quaternion.Euler(0f, euler.y, 0f);

            // Pitch cho camera
            float pitch = euler.x;
            if (pitch > 180f) pitch -= 360f;

            CurrentPitch = pitch;
            Camera.transform.localRotation = Quaternion.Euler(CurrentPitch, 0f, 0f);
        }


        public void FocusOnTarget(GameObject target, float duration = 2.5f)
        {
            if (target == null) return;

            StartCoroutine(FocusRoutine(target, duration));
        }

        private IEnumerator FocusRoutine(GameObject target, float duration)
        {
            Pause(); // khóa input nhưng KHÔNG xoay cố định

            float timer = 0f;

            while (timer < duration)
            {
                LookAtTarget(target);   // 👈 FOLLOW mỗi frame
                timer += Time.deltaTime;
                yield return null;
            }
            Resume();

        }



    }
}
