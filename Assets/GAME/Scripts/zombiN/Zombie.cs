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
    [SerializeField] private Transform target; // Si lo dejas vacío, busca el objeto con tag "Player"

    // Eventos para que la barra de vida (u otros scripts) se enteren
    public event Action<float, float> OnHealthChanged; // (vidaActual, vidaMaxima)
    public event Action OnDied;

    public float CurrentHealth { get; private set; }
    public float MaxHealth => maxHealth;
    public bool IsDead { get; private set; }

    private NavMeshAgent agent;
    private float nextAttackTime;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
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

        // Aquí puedes disparar la animación de ataque:
        // animator.SetTrigger("Attack");

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
        agent.enabled = false;
        OnDied?.Invoke();

        // animator.SetTrigger("Die");
        Destroy(gameObject, 3f);
    }
}
