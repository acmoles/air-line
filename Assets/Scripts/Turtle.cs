using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turtle : MonoBehaviour
{
    public bool logging = false;

    public TurtleMovementState movementState = TurtleMovementState.Play;
    private TurtleMovementState lastMovementState = TurtleMovementState.Play;

    [SerializeField]
    private Transform contentParent;

    [SerializeField]
    private WaypointManager waypoints;

    [SerializeField]
    private TurtleSettings settings;

    [SerializeField]
    private FollowMe followMe;

    [SerializeField]
    private BrushStyles brushStyles;

    private Quaternion lastRotation = Quaternion.identity;

    private bool isMovingScripted = false;
    private bool isMovingWaypoints = false;
    private bool isRotatingToRest = false;

    void Start()
    {
        Sequences.PopulateSequenceList();
        StartSequence("Initial");
    }

    [ContextMenu("Force restart")]
    public void ForceRestart()
    {
        StopAllCoroutines();
        DisableFollowMe();
        StartSequence("Initial");
    }

    public void StartSequence(string commandString)
    {
        DisableFollowMe();
        movementState = TurtleMovementState.Play;
        if (!isMovingWaypoints && !isMovingScripted)
        {
            StartCoroutine(DoSequence(commandString));
        }
    }

    //TODO bug waypoints placed before first rest do not get played
    private IEnumerator DoSequence(string commandString)
    {
        yield return null;

        if (commandString != "Initial")
        {
            // Do scripted sequence
            if (logging) Debug.Log("Sequence Started!");
            isMovingScripted = true;

            yield return Sequences.DoSequence(this, commandString);

            if (logging) Debug.Log("Sequence Done!");
            isMovingScripted = false;
        }

        // Do any waiting waypoints
        if (waypoints != null && waypoints.WaypointsToPlay())
        {
            yield return DoWaypoints();
        }
        // else
        // {
        //     yield return DoRotateToRest();
        // }

        yield return null;
    }

    public void OnTriggerWaypoints()
    {
        if (!isMovingWaypoints && !isMovingScripted && !isRotatingToRest)
        {
            StartCoroutine(DoWaypoints());
        }
    }

    public void OnMovementStateUpdated(string state)
    {
        if (logging) Debug.Log("TurtleMovementState: " + state);

        TurtleMovementState tState;
        if (Enum.TryParse<TurtleMovementState>(state, out tState))
        {
            switch (tState)
            {
                case TurtleMovementState.FollowMe:
                    if (logging) Debug.Log("Enable follow me");
                    lastMovementState = movementState;
                    movementState = TurtleMovementState.FollowMe;
                    EnableFollowMe();
                    break;
                case TurtleMovementState.ExitFollowMe:
                    if (logging) Debug.Log("Exit FollowMe");
                    movementState = lastMovementState;
                    DisableFollowMe();
                    //StartCoroutine(DoRotateToRest());
                    break;
                case TurtleMovementState.Play:
                    if (logging) Debug.Log("Play");
                    movementState = TurtleMovementState.Play;
                    DisableFollowMe();
                    break;
                case TurtleMovementState.Pause:
                    if (logging) Debug.Log("Pause");
                    movementState = TurtleMovementState.Pause;
                    DisableFollowMe();
                    break;
            }
        }
        else
        {
            Debug.LogWarning("Cannot parse movement state");
        }
    }

    private void EnableFollowMe()
    {
        isMovingWaypoints = false;
        isMovingScripted = false;
        isRotatingToRest = false;
        StopAllCoroutines();

        followMe.enabled = true;
    }

    private void DisableFollowMe()
    {
        followMe.enabled = false;
    }

    private IEnumerator DoRotateToRest()
    {
        if (logging) Debug.Log("Rotating to rest started!");
        isRotatingToRest = true;
        yield return null;
        yield return RotateToRest();
        yield return null;
        isRotatingToRest = false;

        // Do any waiting waypoints
        if (waypoints != null && waypoints.WaypointsToPlay())
        {
            yield return DoWaypoints();
        }
        if (logging) Debug.Log("Rotating to rest Done!");
    }

    private IEnumerator DoWaypoints()
    {
        if (logging) Debug.Log("Waypoints Started!");
        isMovingWaypoints = true;
        yield return null;
        yield return NextWaypoint();
        yield return null;
        // Rotate to rest after finishing waypoints
        //yield return DoRotateToRest();
        if (logging) Debug.Log("Waypoints Done!");
    }

    private IEnumerator NextWaypoint()
    {
        while (movementState == TurtleMovementState.Pause)
        {
            yield return new WaitForSeconds(0.2f);
        }
        yield return waypoints.NextWaypoint((finished) => {
            if (logging) Debug.Log("No more waypoints to play: " + finished);
            isMovingWaypoints = false;
        }, this); // we pass the turtle in this case
    }

    public IEnumerator GotoTarget(Vector3 target)
    {
        if (logging) Debug.Log("goto target, " + target);
        yield return PointAt(target);
        yield return MoveToTarget(target);
    }

    public IEnumerator GotoTargetTurnInstant(Vector3 target)
    {
        if (logging) Debug.Log("goto target, " + target);
        yield return PointAtInstant(target);
        yield return MoveToTarget(target);
    }

    public IEnumerator Td(float distance)
    {
        yield return Segment(distance, 0, 90f);
    }

    public IEnumerator Rd(float distance)
    {
        yield return Segment(distance, 90f, 90f);
    }

    public IEnumerator Ld(float distance)
    {
        yield return Segment(distance, -90f, 90f);
    }

    public IEnumerator Pd(float distance)
    {
        yield return Segment(distance, 1800f, 90f);
    }

    public IEnumerator Segment(float distance, float roll, float turn)
    {
        yield return Move(distance);
        yield return Roll(roll);
        yield return Turn(turn);
    }

    public IEnumerator Turn(float angle)
    {
        yield return Turn(gameObject, "y", angle, settings.rotateSpeed);
    }

    public IEnumerator Dive(float angle)
    {
        yield return Turn(gameObject, "x", angle, settings.rotateSpeed);
    }

    public IEnumerator Roll(float angle)
    {
        yield return Turn(gameObject, "z", angle, settings.rotateSpeed);
    }

    public IEnumerator PointAt(Vector3 target)
    {
        yield return Turn(gameObject, "target", 0, settings.rotateSpeed, target);
    }

    public IEnumerator RotateToRest()
    {
        yield return Turn(gameObject, "rest", 0, settings.restRotateSpeed);
    }

    public IEnumerator Turn(GameObject objectToMove, string axis, float angle, float speed, Vector3? target = null)
    {
        if (logging) Debug.Log("start turn, " + axis + ": " + objectToMove.transform.rotation.eulerAngles);
        // Quaternion start = objectToMove.transform.rotation;
        Quaternion end = Quaternion.identity;
        // TODO enable user code to use constant/ease
        string method = "constant";
        switch (axis)
        {
            case "x":
                end = objectToMove.transform.rotation * Quaternion.Euler(angle, 0f, 0f);
                break;
            case "y":
                end = objectToMove.transform.rotation * Quaternion.Euler(0f, angle, 0f);
                break;
            case "z":
                end = objectToMove.transform.rotation * Quaternion.Euler(0f, 0f, angle);
                break;
            case "target":
                if (target == null)
                {
                    Debug.Log("no target");
                    end = objectToMove.transform.rotation;
                    break;
                }
                Vector3 direction = target.Value - objectToMove.transform.position;
                if (direction.sqrMagnitude < 0.00001f) end = objectToMove.transform.rotation;
                else
                {
                    direction.Normalize();
                    end = Quaternion.LookRotation(direction, objectToMove.transform.up);
                }
                break;
            case "rest":
                //Vector3 restDirection = Camera.main.transform.position - objectToMove.transform.position;
                Vector3 restDirection = objectToMove.transform.forward;
                float d = Vector3.Dot(restDirection, Vector3.up);
                restDirection -= d * Vector3.up;
                if (restDirection.sqrMagnitude < 0.00001f) end = objectToMove.transform.rotation;
                else
                {
                    restDirection.Normalize();
                    end = Quaternion.LookRotation(restDirection, Vector3.up);
                }
                method = "ease";
                break;
            default:
                end = objectToMove.transform.rotation;
                break;
        }

        if (speed == TurtleSettings.instantRotateSpeed)
        {
            yield return RotateInstant(objectToMove, end);
        }
        else
        {
            yield return RotateWithSpeed(objectToMove, end, speed, method);
        }

        if (logging) Debug.Log("end turn, " + axis + ": " + objectToMove.transform.rotation.eulerAngles);
        yield return null;
    }

    public IEnumerator RotateWithSpeed(GameObject objectToMove, Quaternion end, float speed, string method = "constant")
    {
        while (objectToMove.transform.rotation != end)
        {
            lastRotation = objectToMove.transform.rotation;
            if (method == "ease") objectToMove.transform.rotation = Quaternion.Slerp(objectToMove.transform.rotation, end, speed * Time.deltaTime);
            else objectToMove.transform.rotation = Quaternion.RotateTowards(objectToMove.transform.rotation, end, speed * Time.deltaTime);

            if (objectToMove.transform.rotation == lastRotation)
            {
                if (logging) Debug.LogError("Rotation lock");
                break;
            }

            yield return new WaitForEndOfFrame();
        }
        yield return null;
    }

    public IEnumerator RotateInstant(GameObject objectToMove, Quaternion end)
    {
        objectToMove.transform.rotation = end;
        yield return new WaitForEndOfFrame();
    }

    public IEnumerator TurnInstant(float angle)
    {
        yield return Turn(gameObject, "y", angle, TurtleSettings.instantRotateSpeed);
    }

    public IEnumerator DiveInstant(float angle)
    {
        yield return Turn(gameObject, "x", angle, TurtleSettings.instantRotateSpeed);
    }

    public IEnumerator RollInstant(float angle)
    {
        yield return Turn(gameObject, "z", angle, TurtleSettings.instantRotateSpeed);
    }

    public IEnumerator PointAtInstant(Vector3 target)
    {
        yield return Turn(gameObject, "target", 0, TurtleSettings.instantRotateSpeed, target);
    }

    public IEnumerator Move(float distance)
    {
        yield return Move(gameObject, distance, settings.moveSpeed);
    }

    public IEnumerator MoveToTarget(Vector3 target)
    {
        float distance = (target - gameObject.transform.position).magnitude;
        yield return Move(gameObject, distance, settings.moveSpeed);
    }

    public IEnumerator Move(GameObject objectToMove, float distance, float speed)
    {
        if (contentParent != null) distance *= contentParent.localScale.x;
        if (logging) Debug.Log("start move");
        Vector3 start = objectToMove.transform.position;
        Vector3 end = objectToMove.transform.position + objectToMove.transform.forward * distance;
        Vector3 linearPosition = objectToMove.transform.position;
        float dist = (end - start).sqrMagnitude;

        while (objectToMove.transform.position != end)
        {
            while (movementState == TurtleMovementState.Pause)
            {
                yield return new WaitForSeconds(0.2f);
            }
            linearPosition = Vector3.MoveTowards(linearPosition, end, speed * Time.deltaTime);
            float p = 1 - (end - linearPosition).sqrMagnitude / dist;
            p = EasingFunction.EaseInOutQuart(0f, 1f, p);
            objectToMove.transform.position = Vector3.Lerp(start, end, p);
            yield return new WaitForEndOfFrame();
        }
        if (logging) Debug.Log("end move");

        yield return null;
    }

    public IEnumerator SetColor(BrushColor color)
    {
        brushStyles.BrushColor = color;
        yield return null;
    }

    public IEnumerator SetCustomColor(Color color)
    {
        brushStyles.CustomColor = color;
        yield return null;
    }

    public IEnumerator SetSize(BrushSize size)
    {
        brushStyles.BrushSize = size;
        yield return null;
    }

    public IEnumerator SetBrushUpDown(BrushUpDownState state)
    {
        brushStyles.BrushToggle = state;
        yield return null;
    }

    // Unused!
    public IEnumerator MoveOverSeconds(GameObject objectToMove, Vector3 end, float seconds)
    {
        if (logging) Debug.Log("start move");
        float elapsedTime = 0;
        Vector3 startingPos = objectToMove.transform.position;
        while (elapsedTime < seconds)
        {
            transform.position = Vector3.Lerp(startingPos, end, (elapsedTime / seconds));
            elapsedTime += Time.deltaTime;
            yield return new WaitForEndOfFrame();
        }
        transform.position = end;
        if (logging) Debug.Log("end move");
    }

    void Update()
    {
        Debug.DrawLine(gameObject.transform.position, gameObject.transform.position + (gameObject.transform.forward * 1f), Color.red);
    }

    public IEnumerator QuadraticArc(Vector3 controlPoint, Vector3 endPoint)
    {
        Vector3[] points = Arc.GetArc(transform.position, controlPoint, endPoint, settings.arcSegments);
        for (int i = 0; i < points.Length; i++)
        {
            if (logging) Debug.Log("Arc Point: " + points[i]);
            yield return GotoTargetTurnInstant(points[i]);
        }
        yield return null;
    }
}
