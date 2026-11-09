using UnityEngine;

public class RaceManager : MonoBehaviour
{
    private bool targetResetThisFrame;
    private MoveToTargetAgent[] allAgents;

    [SerializeField] private Transform target;

    private bool winnerChosen;

    private void Awake()
    {
        allAgents = FindObjectsOfType<MoveToTargetAgent>();
    }

    public void AgentReachedTarget(MoveToTargetAgent winner)
    {
        if (winnerChosen)
            return;

        winnerChosen = true;

        foreach (MoveToTargetAgent agent in allAgents)
        {
            if (agent == winner)
                agent.Win();
            else
                agent.Lose();
        }

        foreach (MoveToTargetAgent agent in allAgents)
        {
            agent.EndEpisode();
        }

        winnerChosen = false;
    }

    private void LateUpdate()
    {
        targetResetThisFrame = false;
    }

    public void ResetTarget()
    {
        if (targetResetThisFrame)
            return;

        target.localPosition = new Vector3(
            Random.Range(-12f, 12f),
            0.5f,
            Random.Range(3f, 12f));

        targetResetThisFrame = true;
    }

    public void AgentHitWall(MoveToTargetAgent loser)
    {
        loser.WallHit();

        foreach (MoveToTargetAgent agent in allAgents)
        {
            agent.EndEpisode();
        }
    }
}