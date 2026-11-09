using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class MoveToTargetAgent : Agent
{
    [Header("References")]
    [SerializeField] private Transform target;
    [SerializeField] private RaceManager raceManager;
    private MoveToTargetAgent[] allAgents;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;

    [Header("Dash")]
    [SerializeField] private float dashForce = 8f;
    [SerializeField] private float dashCooldown = 8f;
    private bool isDashing;
    private float dashEndTime;

    [Header("Colours")]
    [SerializeField] private Renderer floor;
    [SerializeField] private Color winColour = Color.green;
    [SerializeField] private Color loseColour = Color.red;
    [SerializeField] private Color idleColour = Color.grey;

    private Rigidbody rb;
    private float nextDashTime;

    public override void Initialize()
    {
        rb = GetComponent<Rigidbody>();

        allAgents = FindObjectsOfType<MoveToTargetAgent>();

        if (floor != null)
            floor.material.color = idleColour;
    }

    private void FixedUpdate()
    {
        if (StepCount % 500 == 0)
        {
            Debug.Log($"StepCount: {StepCount}");
        }
    }

    public override void OnEpisodeBegin()
    {
        Debug.Log("RESET");

        // Reset velocity
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        isDashing = false;
        dashEndTime = 0f;
        nextDashTime = 0f;

        // Random spawn
        transform.localPosition = new Vector3(
            Random.Range(-12f, 12f),
            0.5f,
            Random.Range(-12f, 12f));

        if (raceManager != null)
        {
            raceManager.ResetTarget();
        }

        nextDashTime = 0f;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        // Direction to target
        sensor.AddObservation(target.position - transform.position);

        // Current velocity
        sensor.AddObservation(rb.linearVelocity);

        // Dash cooldown
        float cooldownRemaining =
            Mathf.Max(0, nextDashTime - Time.time);

        sensor.AddObservation(
            cooldownRemaining / dashCooldown);

        // Observe competitors
        foreach (MoveToTargetAgent agent in allAgents)
        {
            if (agent == this)
                continue;

            sensor.AddObservation(
                agent.transform.position -
                transform.position);
        }
    }

    public override void OnActionReceived(ActionBuffers actions)
    {
        float moveX = actions.ContinuousActions[0];
        float moveZ = actions.ContinuousActions[1];

        int dashAction = actions.DiscreteActions[0];

        Vector3 move =
            new Vector3(moveX, 0f, moveZ);

        move = Vector3.ClampMagnitude(move, 1f);

        float currentSpeed = moveSpeed;

        if (isDashing)
        {
            currentSpeed *= 4f;

            if (Time.time >= dashEndTime)
            {
                isDashing = false;
            }
        }

        transform.position += move * currentSpeed * Time.deltaTime;

        // Small penalty each step
        if (MaxStep > 0)
            AddReward(-1f / MaxStep);

        // Dash
        if (dashAction == 1 &&
            Time.time >= nextDashTime &&
            move.magnitude > 0.1f)
        {
            isDashing = true;

            dashEndTime = Time.time + 0.25f;
            nextDashTime = Time.time + dashCooldown;
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        MoveToTargetAgent other =
            collision.gameObject.GetComponent<MoveToTargetAgent>();

        if (other == null)
            return;

        // Reward successful bumps slightly
        float impact = collision.relativeVelocity.magnitude;

        if (impact > 5f)
        {
            AddReward(0.02f);
            other.AddReward(-0.01f);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Target"))
        {
            Debug.Log("TARGET");
            raceManager.AgentReachedTarget(this);
        }
        else if (other.CompareTag("Wall"))
        {
            Debug.Log("WALL");
            raceManager.AgentHitWall(this);
        }
    }

    public void Win()
    {
        AddReward(1f);

        if (floor != null)
            floor.material.color = winColour;

        EndEpisode();
    }

    public void Lose()
    {
        AddReward(-0.5f);

        if (floor != null)
            floor.material.color = loseColour;

        EndEpisode();
    }

    public void WallHit()
    {
        AddReward(-1f);

        if (floor != null)
            floor.material.color = loseColour;
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        var continuous = actionsOut.ContinuousActions;
        var discrete = actionsOut.DiscreteActions;

        var kb = Keyboard.current;

        continuous[0] =
            (kb.dKey.isPressed ? 1f : 0f)
            - (kb.aKey.isPressed ? 1f : 0f);

        continuous[1] =
            (kb.wKey.isPressed ? 1f : 0f)
            - (kb.sKey.isPressed ? 1f : 0f);

        // Space = Dash
        discrete[0] =
            kb.spaceKey.isPressed ? 1 : 0;
    }
}