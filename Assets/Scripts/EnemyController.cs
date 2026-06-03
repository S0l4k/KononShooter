using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour, IDamageable
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float waypointReachDistance = 1f;
    [SerializeField] private float waypointChangeInterval = 5f;

    [Header("Health")]
    [SerializeField] private int maxHealth = 50;
    [SerializeField] private AudioClip hitSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField, Range(0f, 1f)] private float hitVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float deathVolume = 1f;

    private NavMeshAgent _agent;
    private int _currentHealth;
    private bool _isDead = false;
    private Vector3 _currentWaypoint;
    private float _nextWaypointTime = 0f;

    private void Start()
    {
        _currentHealth = maxHealth;
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = moveSpeed;

        PickNewWaypoint();
    }

    private void Update()
    {
        if (_isDead) return;

        if (Time.time >= _nextWaypointTime)
        {
            PickNewWaypoint();
        }

        if (_agent.remainingDistance <= waypointReachDistance)
        {
            PickNewWaypoint();
        }
    }

    private void PickNewWaypoint()
    {

        Vector3 randomDirection = Random.insideUnitSphere * 15f;
        randomDirection += transform.position;

        NavMeshHit hit;
        if (NavMesh.SamplePosition(randomDirection, out hit, 15f, NavMesh.AllAreas))
        {
            _currentWaypoint = hit.position;
            _agent.SetDestination(_currentWaypoint);
            _nextWaypointTime = Time.time + waypointChangeInterval;
        }
    }

    public void TakeDamage(int damage)
    {
        if (_isDead) return;

        _currentHealth -= damage;

        if (_currentHealth <= 0)
        {
            Die();
        }
        else
        {
            if (hitSound != null)
                AudioSource.PlayClipAtPoint(hitSound, transform.position, hitVolume);
        }
    }

    private void Die()
    {
        if (_isDead) return;
        _isDead = true;

        if (GameManager.Instance != null)
        {
            GameManager.Instance.RegisterEnemyKill(this);
        }

        if (deathSound != null)
            AudioSource.PlayClipAtPoint(deathSound, transform.position, deathVolume);

        _agent.enabled = false;
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach (var col in colliders)
            col.enabled = false;

        Destroy(gameObject, 0.5f);
    }
}