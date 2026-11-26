using System;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class Zombie : MonoBehaviour, IDamageable
{
    [Header("Vida")]
    [SerializeField] private float maxHealth = 100f;

    [Header("Ataque")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float attackRange = 1.8f;
    [SerializeField] private float attackCooldown = 1.2f;

    [Header("Persecución")]
    [SerializeField] private float detectionRange = 15f;
    [SerializeField] private Transform target; 

    
    public event Action<float, float> OnHealthChanged; 
    public event Action OnDied;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private NavMeshAgent agent;
    private Animator animator;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        CurrentHealth = maxHealth;
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
        }
    }

    private void Update()
    {
        if (IsDead || target == null) return;

        if (!agent.isOnNavMesh) return;

        // Alimenta el Blend Tree (Idle <-> Walk)
        animator.SetFloat("Speed", agent.velocity.magnitude);

        float distance = Vector3.Distance(transform.position, target.position);

        if (distance > detectionRange)
        {
            if (agent.hasPath) agent.ResetPath();
            return;
        }

        if (distance > attackRange)
        {
            agent.SetDestination(target.position);
        }
        else
        {
            agent.ResetPath();
            LookAtTarget();
            TryAttack();
        }
    }

    private void LookAtTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.001f) return;
        Quaternion look = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, look, 10f * Time.deltaTime);
    }

    private void TryAttack()
    {
        if (Time.time < nextAttackTime) return;
        nextAttackTime = Time.time + attackCooldown;

        animator.SetTrigger("attack");

        if (target.TryGetComponent(out IDamageable victim))
        {
            victim.TakeDamage(damage);
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsDead) return;

        CurrentHealth = Mathf.Max(CurrentHealth - amount, 0f);
        OnHealthChanged?.Invoke(CurrentHealth, maxHealth);

        if (CurrentHealth <= 0f) Die();
    }

    private void Die()
    {
        IsDead = true;
        animator.SetTrigger("die");
        agent.enabled = false;
        OnDied?.Invoke();

        Destroy(gameObject, 3f);
    }
}