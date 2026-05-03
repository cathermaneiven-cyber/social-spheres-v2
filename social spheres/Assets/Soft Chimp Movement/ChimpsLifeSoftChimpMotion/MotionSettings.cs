using UnityEngine;

namespace SoftChimpMotion
{
    public class MotionSettings : MonoBehaviour
    {
        [Header("Player Stuff")]
        [Tooltip("Player Origin")]
        public Rigidbody rb;
        [Tooltip("Player Camera")]
        public SphereCollider headCollider;
        [Tooltip("Player Controller Left")]
        public GameObject leftController;
        [Tooltip("Player Controller Right")]
        public GameObject rightController;

        [Header("Settings for Movement")]
        [Tooltip("Sets the Movement to Snappy, Making the Movement More Like Gorilla tags")]
        public bool IsSnappy;
        [Tooltip("Sets the Movement to Between, Making the Movement More Like A Mix of Gorilla tag and Capuchins")]
        public bool IsBetween;
        [Tooltip("Sets the Movement to Snappy, Making the Movement More Like Capuchins")]
        public bool IsBouncy;

        [Header("Movement Booliens ( Changing might Affect Gameplay stuff )")]
        public int velocityHistorySize;
        [Tooltip("Checks if the Chimp is In A No Gravity Or Swim Area")]
        public bool inWaterOrSpace;

        private Vector3 lastPosition;
        private Vector3[] velocityHistory;
        private int velocityIndex;
        private Vector3 currentVelocity;
        private Vector3 denormalizedVelocityAverage;

        private MovementCollision leftHand;
        private MovementCollision rightHand;

        private void Start()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (leftController != null)
            {
                leftHand = leftController.GetComponent<MovementCollision>();
            }
            if (rightController != null)
            {
                rightHand = rightController.GetComponent<MovementCollision>();
            }

            InitializeVelocities();
            SetDefaultMovementSettings();
        }

        private void Update()
        {
            if (inWaterOrSpace)
            {
                rb.useGravity = false;
            }
            else
            {
                rb.useGravity = true;
            }

            StoreVelocities();
            ApplyMovementSettings();
        }

        private void InitializeVelocities()
        {
            velocityHistory = new Vector3[velocityHistorySize];
            lastPosition = transform.position;
        }

        private void StoreVelocities()
        {
            velocityIndex = (velocityIndex + 1) % velocityHistorySize;
            Vector3 oldestVelocity = velocityHistory[velocityIndex];
            currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
            denormalizedVelocityAverage += (currentVelocity - oldestVelocity) / (float)velocityHistorySize;
            velocityHistory[velocityIndex] = currentVelocity;
            lastPosition = transform.position;
        }

        public void TurnPlayer(float degrees)
        {
            transform.RotateAround(headCollider.transform.position, transform.up, degrees);
            denormalizedVelocityAverage = Quaternion.Euler(0, degrees, 0) * denormalizedVelocityAverage;
            for (int i = 0; i < velocityHistory.Length; i++)
            {
                velocityHistory[i] = Quaternion.Euler(0, degrees, 0) * velocityHistory[i];
            }
        }

        private void ApplyMovementSettings()
        {
            if (IsSnappy)
            {
                SetSnappyMovement();
            }
            else if (IsBetween)
            {
                SetDefaultMovementSettings();
            }
            else if (IsBouncy)
            {
                SetBouncyMovement();
            }
        }

        private void SetSnappyMovement()
        {
            if (leftHand != null && rightHand != null)
            {
                leftHand.frequency = 250f;
                leftHand.damping = 0f;
                leftHand.climbDivider = 3;
                leftHand.rotfrequency = 200f;
                leftHand.rotDamping = 0f;
                leftHand.UnstickDistance = 0.05f;
                leftHand.StickDistance = 0.01f;

                rightHand.frequency = 250f;
                rightHand.damping = 0;
                rightHand.climbDivider = 3;
                rightHand.rotfrequency = 200f;
                rightHand.rotDamping = 0f;
                rightHand.UnstickDistance = 0.05f;
                rightHand.StickDistance = 0.01f;
            }
        }

        private void SetBouncyMovement()
        {
            if (leftHand != null && rightHand != null)
            {
                leftHand.frequency = 15f;
                leftHand.damping = 3f;
                leftHand.rotfrequency = 50f;
                leftHand.rotDamping = 1.5f;
                leftHand.UnstickDistance = 0.3f;
                leftHand.StickDistance = 0.15f;

                rightHand.frequency = 15f;
                rightHand.damping = 3f;
                rightHand.rotfrequency = 50f;
                rightHand.rotDamping = 1.5f;
                rightHand.UnstickDistance = 0.3f;
                rightHand.StickDistance = 0.15f;
            }
        }

        private void SetDefaultMovementSettings()
        {
            if (leftHand != null && rightHand != null)
            {
                leftHand.frequency = 50f;
                leftHand.damping = 1f;
                leftHand.rotfrequency = 100f;
                leftHand.rotDamping = 0.9f;
                leftHand.UnstickDistance = 0.2f;
                leftHand.StickDistance = 0.1f;

                rightHand.frequency = 50f;
                rightHand.damping = 1f;
                rightHand.rotfrequency = 100f;
                rightHand.rotDamping = 0.9f;
                rightHand.UnstickDistance = 0.2f;
                rightHand.StickDistance = 0.1f;
            }
        }
    }
}
