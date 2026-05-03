using UnityEngine;

public class MovementCollision : MonoBehaviour
{
    public MovementCollision OtherHand;

    [Header("Movement Settings")]
    [Header("Movement Is Made By Anemunt and Terminal,")]
    [Header("Any use To The Script Is Completely Fine to The Developers")]

    [Header("Tracking Speed")]
    public float frequency = 50f;

    [Header("How much to Soften Movement")]
    public float damping = 1f;

    [Header("I Dont Remember these, Just Keep them the same lol.")]
    public float rotfrequency = 100f;
    public float rotDamping = 0.9f;

    [Header("Player Rigid Body")]
    [SerializeField] Rigidbody playerRigidbody;

    [Header("Will Change to Hand Or Controller")]
    [SerializeField] Transform target;
    [Space]
    [Header("Movement Force Required to Move")]
    public float climbForce = 1000f;
    public float climbDrag = 500f;

    [Header("Speed Multiplyer When Hitting With Both Hands")]
    public float climbDivider = 2f;

    [Header("Bools for When Hand Tracking Is Added")]
    public bool isHandEnabled;
    public bool isControllerEnabled;

    [Header("Objects To Track to")]
    public GameObject Hand;
    public GameObject Controller;
    Vector3 _previousPosition;
    Rigidbody _rigidbody;

    [Header("Dont Touch, as this Could Cause Physics Problems")]
    public bool _isColliding;
    public bool _wasIsColliding;

    // New variables for slippery effect
    private bool isSlippery;
    public float maxSlipDistance = 0.2f; // Maximum distance to keep slipping

    private Collider currentSlipCollider;
    private Vector3 slidingDirection;

    [Header("Stick and Unstick Distances")]
    public float StickDistance = 0.1f;
    public float UnstickDistance = 0.2f;

    void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _rigidbody.maxAngularVelocity = float.PositiveInfinity;
        _previousPosition = transform.position;
    }

    void Update()
    {
        // Check if the Controller or Hand objects are active in the scene
        if (Controller.activeInHierarchy)
        {
            isControllerEnabled = true;
            isHandEnabled = false;
        }
        else
        {
            isHandEnabled = true;
            isControllerEnabled = false;
        }

        // Check if hand is away from the slippery object
        if (isSlippery && currentSlipCollider != null)
        {
            if (Vector3.Distance(transform.position, currentSlipCollider.ClosestPoint(transform.position)) > maxSlipDistance)
            {
                isSlippery = false;
                currentSlipCollider = null;
            }
        }
    }

    void FixedUpdate()
    {
        if (isHandEnabled)
        {
            // Track to the hand
            target = Hand.transform;
        }
        else if (isControllerEnabled)
        {
            // Track to the controller
            target = Controller.transform;
        }

        PIDMovement();
        PIDRotation();
        if (_isColliding) HookesLaw();
    }

    void PIDMovement()
    {
        float kp = (6f * frequency) * (6f * frequency) * 0.25f;
        float kd = 4.5f * frequency * damping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Vector3 targetPosition = target.position;

        Vector3 force = (targetPosition - transform.position) * ksg + (playerRigidbody.velocity - _rigidbody.velocity) * kdg;
        _rigidbody.AddForce(force, ForceMode.Acceleration);

        if (isSlippery)
        {
            AdjustSlidingDirection();
            _rigidbody.AddForce(slidingDirection * (climbForce * 0.1f), ForceMode.Acceleration); // Adjust force multiplier for smoother sliding
        }
    }

    void PIDRotation()
    {
        float kp = (6f * rotfrequency) * (6f * rotfrequency) * 0.25f;
        float kd = 4.5f * rotfrequency * rotDamping;
        float g = 1 / (1 + kd * Time.fixedDeltaTime + kp * Time.fixedDeltaTime * Time.fixedDeltaTime);
        float ksg = kp * g;
        float kdg = (kd + kp * Time.fixedDeltaTime) * g;
        Quaternion q = target.rotation * Quaternion.Inverse(transform.rotation);
        if (q.w < 0)
        {
            q.x = -q.x;
            q.y = -q.y;
            q.z = -q.z;
            q.w = -q.w;
        }
        q.ToAngleAxis(out float angle, out Vector3 axis);
        axis.Normalize();
        axis *= Mathf.Deg2Rad;
        Vector3 torque = ksg * axis * angle + -_rigidbody.angularVelocity * kdg;
        _rigidbody.AddTorque(torque, ForceMode.Acceleration);
    }

    void HookesLaw()
    {
        Vector3 displacementFromResting = transform.position - target.position;
        Vector3 force = displacementFromResting * climbForce;

        float drag = GetDrag();

        if (_isColliding && !_wasIsColliding && !OtherHand._wasIsColliding)
        {
            playerRigidbody.velocity = playerRigidbody.velocity / climbDivider;
            _wasIsColliding = true;
        }

        if (OtherHand._isColliding)
        {
            playerRigidbody.AddForce(force / 2, ForceMode.Acceleration);
            playerRigidbody.AddForce((drag * -playerRigidbody.velocity * climbDrag) / 2, ForceMode.Acceleration);
        }
        else
        {
            playerRigidbody.AddForce(force, ForceMode.Acceleration);
            playerRigidbody.AddForce(drag * -playerRigidbody.velocity * climbDrag, ForceMode.Acceleration);
        }
    }

    float GetDrag()
    {
        Vector3 handVelocity = (target.localPosition - _previousPosition) / Time.fixedDeltaTime;
        float drag = 1 / handVelocity.magnitude + 0.01f;
        drag = Mathf.Clamp(drag, 0.03f, 1f);
        _previousPosition = transform.position;
        return drag;
    }

    void AdjustSlidingDirection()
    {
        // Calculate the hand's local rotation around the Z-axis
        float zRotation = transform.localEulerAngles.z;

        // Adjust sliding direction based on Z-axis rotation
        if (zRotation < 180)
        {
            slidingDirection = Quaternion.Euler(0, 0, zRotation) * slidingDirection;
        }
        else
        {
            slidingDirection = Quaternion.Euler(0, 0, zRotation - 360) * slidingDirection;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        _isColliding = true;
        if (collision.collider.CompareTag("Slip"))
        {
            isSlippery = true;
            currentSlipCollider = collision.collider;
            slidingDirection = Vector3.ProjectOnPlane(target.position - transform.position, collision.contacts[0].normal).normalized;
        }
    }

    void OnCollisionStay(Collision collision)
    {
        if (isSlippery && collision.collider == currentSlipCollider)
        {
            AdjustSlidingDirection();
            _rigidbody.AddForce(slidingDirection * (climbForce * 0.1f), ForceMode.Acceleration); // Adjust force multiplier for smoother sliding
        }
    }

    void OnCollisionExit(Collision other)
    {
        _isColliding = false;
        _wasIsColliding = false;
        if (other.collider == currentSlipCollider)
        {
            isSlippery = false;
            currentSlipCollider = null;
        }
    }
}
