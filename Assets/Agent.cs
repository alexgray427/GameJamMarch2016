using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SphereCollider))]
public abstract class Agent : MonoBehaviour
{
	/// <summary>
	/// Health.
	/// </summary>
	public class Health
	{
		public delegate void HealthEventHandler();
		public event HealthEventHandler OnDie;

		private float _maxHealth;
		private float _curHealth;

		public float MaximumHealth
		{
			get { return _maxHealth; }
		}

		public float CurrentHealth
		{
			get { return _curHealth; }
		}

		public Health(float health)
		{
			_maxHealth = health;
			_curHealth = health;
		}

		public void AddHealth(float amount)
		{
			_curHealth = Mathf.Clamp (_curHealth + amount, 0.0f, _maxHealth);
		}

		public void RemoveHealth(float amount)
		{
			_curHealth = Mathf.Clamp (_curHealth - amount, 0.0f, _maxHealth);

			if (_curHealth == 0.0f && OnDie != null)
			{
				OnDie();
			}
		}
      }

	public enum FlagType { Knight, Orc, Ghost, Ghoul }

	public bool mIsAlive
	{
		get { return (gameObject.activeSelf && _health.CurrentHealth > 0.0f); }
	}

	public FlagType mFlag
	{
		get { return _flag; }
	}

	public float MaxHealth;

	protected Animator _anim;
	protected NavMeshAgent _navAgent;
	protected SphereCollider _collider;

	protected StackFSM _stateMachine;
	protected List<Agent> _nearbyTargets;
	protected Agent _target;
	protected Vector3 _lastKnownPosition;
	protected float _speed;
	protected Timer _deathTimer;

	protected Health _health;
	protected FlagType _flag;
	protected int _attackTypes;

	private void Awake()
	{
		_anim = GetComponent<Animator>();
		_navAgent = GetComponent<NavMeshAgent>();
		_collider = GetComponent<SphereCollider>();

		_stateMachine = new StackFSM();
		_nearbyTargets = new List<Agent>();
		_target = null;
		_lastKnownPosition = Vector3.zero;
		_speed = _navAgent.speed;
		_navAgent.speed = 0.0f;
		_health = new Health (MaxHealth);
	}

	protected abstract void Setup ();

	protected virtual void Start()
	{
		Setup ();
		_deathTimer = new Timer(5.0f);
		_deathTimer.OnTimeFinish += DeathTimer_OnTimeFinish;
		_health.OnDie += Die;
		_stateMachine.AddState(Init);
	}

	protected virtual void Update()
	{
		_stateMachine.Update();

		_anim.SetBool("Walking", _navAgent.speed > 0.0f ? true : false);

		if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
		{
			_navAgent.speed = 0.0f;
		}
		else
		{
			_navAgent.speed = _speed;
		}
	}

	protected virtual void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Agent")
		{
			Agent agent = other.gameObject.GetComponent<Agent>();
		}
	}

	protected virtual void OnTriggerExit(Collider other)
	{
		if (other.tag == "Agent")
		{
			Agent agent = other.gameObject.GetComponent<Agent>();

			if (_nearbyTargets.Contains(agent))
				_nearbyTargets.Remove(agent);
		}
	}

	protected bool TargetExist()
	{
		if (_target != null)
		{
			if (_target.mIsAlive)
			{
				return true;
			}
			else
			{
				if (_nearbyTargets.Contains(_target))
				{
					_nearbyTargets.Remove(_target);
				}

				_target = null;
			}
		}

		return false;
	}

	protected bool InRange(Transform target)
	{
		return (Vector3.Distance(transform.position, target.position) < _collider.radius);
	}

	protected bool InSight(Transform target)
	{
		return (Vector3.Angle(transform.forward, target.transform.position - transform.position) < 45.0f);
	}

	private void Die()
	{
		_anim.SetBool("IsDead", true);
		_navAgent.speed = 0.0f;
		_deathTimer.Start();
	}

	private void DeathTimer_OnTimeFinish (Timer sender)
	{
		if (sender.Equals(_deathTimer))
		{
			_deathTimer.Reset();
			//Match Manager will reuse this object <OBJECT POOLING>
		}
	}

	public virtual void DamageTarget(float damage)
	{
		if (TargetExist())
		{
			_target.OnDamageReceived(this, damage);
		}
	}

	public virtual void OnDamageReceived(Agent attacker, float amount)
	{
		_health.RemoveHealth(amount);

		if (!mIsAlive)
		{
			_stateMachine.AddState(Die);
		}

		if (attacker.mIsAlive)
		{
			if (_target != null)
			{
				float dist1 = Vector3.Distance(transform.position, _target.transform.position);
				float dist2 = Vector3.Distance(transform.position, attacker.transform.position);

				if (dist2 < dist1)
				{
					_target = attacker;
				}
			}
			else
			{
				_target = attacker;
			}
		}
	}

	protected abstract void Init ();
}