using UnityEngine;
using UnityEngine.XR;

public class ControllerMovementCheck : MonoBehaviour
{
    public Transform controllerToMove;
    public float stationaryThreshold = 0.01f;
    public Transform targetObject; // The object the controller should move to.
    private Vector3 previousControllerPosition;
    private float stationaryTime;
    public float stationaryTimeThreshold = 5.0f;

    void Start()
    {
        previousControllerPosition = controllerToMove.position;
    }

    void Update()
    {
        // Get the current controller position.
        Vector3 currentControllerPosition = controllerToMove.position;

        // Calculate the distance moved by the controller since the last frame.
        float distanceMoved = Vector3.Distance(currentControllerPosition, previousControllerPosition);

        if (distanceMoved < stationaryThreshold)
        {
            // The controller is stationary or moving very slowly.
            stationaryTime += Time.deltaTime;

            if (stationaryTime >= stationaryTimeThreshold)
            {
                // The controller has been stationary for the specified time.
                MoveControllerToTargetObject();
            }
        }
        else
        {
            // Reset the stationary time if the controller moves.
            stationaryTime = 0.0f;
        }

        // Update the previous position for the next frame.
        previousControllerPosition = currentControllerPosition;
    }

    void MoveControllerToTargetObject()
    {
        // Set the controller's position to match the target object's position.
        controllerToMove.position = targetObject.position;
    }
}
