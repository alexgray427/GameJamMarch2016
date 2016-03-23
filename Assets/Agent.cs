using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(SphereCollider))]
public class Agent : MonoBehaviour
{
	public const float SEARCH_DELAY = 3.0f;
	public const float ATTACK_DELAY = 2.0f;

	public enum FlagType { Knight, Orc, Ghost, Ghoul }

	public bool IsPlayer;
	public FlagType Flag;
	public int AttackTypes;

	public bool mIsAlive
	{
		get { return (gameObject.activeSelf && _curHealth > 0.0f); }
	}

	private Animator _anim;
	private NavMeshAgent _navAgent;
	private SphereCollider _collider;

	public StackFSM _stateMachine;
	public List<Agent> _nearbyTargets;
	public Agent _target;
	public Vector3 _lastKnownPosition;
	public float _speed;
	public float _searchTimer;
	public float _attackTimer;

	//Move Health Later
	public float _maxHealth;
	public float _curHealth;

//	private StackFSM _stateMachine;
//	private List<Agent> _nearbyTargets;
//	private Agent _target;
//	private Vector3 _lastKnownPosition;
//	private float _speed;
//	private float _wanderTimer;
//	private float _attackTimer;
//
//	//Move Health Later
//	private float _maxHealth;
//	private float _curHealth;

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
		_searchTimer = SEARCH_DELAY;
		_attackTimer = 0.0f;

		_maxHealth = 100.0f;
		_curHealth = _maxHealth;
	}

	private void Start()
	{
		_stateMachine.AddState(Init);
	}

	private void Update()
	{
		_stateMachine.Update();
	}

	private void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Agent")
		{
			Agent agent = other.gameObject.GetComponent<Agent>();

			if (agent.mIsAlive)
			{
				switch (Flag)
				{
				//Knights don't like Ghost or Ghouls
					case FlagType.Knight:
						if (agent.Flag == FlagType.Ghost || agent.Flag == FlagType.Ghoul)
							_nearbyTargets.Add(agent);
						break;
				//Orcs don't like Ghost or Ghouls
					case FlagType.Orc:
						if (agent.Flag == FlagType.Ghost || agent.Flag == FlagType.Ghoul)
							_nearbyTargets.Add(agent);
						break;
				//Ghost don't like Knights or Orcs
					case FlagType.Ghost:
						if (agent.Flag == FlagType.Knight || agent.Flag == FlagType.Orc)
							_nearbyTargets.Add(agent);
						break;
				//Ghouls don't like Knights or Orcs
					case FlagType.Ghoul:
						if (agent.Flag == FlagType.Knight || agent.Flag == FlagType.Orc)
							_nearbyTargets.Add(agent);
						break;
				}
			}
		}
	}

	private void OnTriggerExit(Collider other)
	{
		if (other.tag == "Agent")
		{
			Agent agent = other.gameObject.GetComponent<Agent>();

			if (_nearbyTargets.Contains(agent))
				_nearbyTargets.Remove(agent);
		}
	}

	private void Init()
	{
		Move(false);

		if (IsPlayer)
			_stateMachine.AddState(CheckInput);
		else if (Flag == FlagType.Orc)
			_stateMachine.AddState(Follow);
		else
			_stateMachine.AddState(Wander);
	}

	private void CheckInput()
	{
		Move(false);

		if (!IsPlayer)
			_stateMachine.RemoveState();

		if (TargetExist())
		{
			_stateMachine.AddState(Chase);
		}
		else if (_nearbyTargets.Count > 0)
		{
			foreach (Agent target in _nearbyTargets)
			{
				if (InRange(target.transform) && InSight(target.transform))
					_target = target;
			}
		}
	}

	private void Follow()
	{
		if (TargetExist())
		{
			_stateMachine.AddState(Chase);
		}
		else
		{
			if (_nearbyTargets.Count > 0)
			{
				foreach (Agent target in _nearbyTargets)
				{
					if (InRange(target.transform) && InSight(target.transform))
					{
						_target = target;
					}
				}
			}

			_navAgent.SetDestination(MatchManager.Instance.Player.transform.position);

			if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
			{
				Move(false);
			}
			else
			{
				Move(true);
			}
		}
	}

	private void Wander()
	{
		if (IsPlayer)
			_stateMachine.RemoveState();

		if (TargetExist())
		{
			_stateMachine.AddState(Chase);
		}
		else if (_nearbyTargets.Count > 0)
		{
			foreach (Agent target in _nearbyTargets)
			{
				if (InRange(target.transform) && InSight(target.transform))
					_target = target;
			}

			if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
			{
				Move(false);
			
				if (_searchTimer > 0.0f)
				{
					_stateMachine.AddState(Search);
				}
				else
				{
					_searchTimer = SEARCH_DELAY;
					_navAgent.SetDestination(transform.position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10)));
				}
			}
			else
			{
				Move(true);
			}
		}
		else
		{
			_stateMachine.AddState(Sleep);
		}
	}

	private void Search()
	{
		Move(false);

		if (_anim.GetBool("Searching") != true)
			_anim.SetBool("Searching", true);

		_searchTimer -= Time.deltaTime;

		if (_nearbyTargets.Count < 0 || _searchTimer <= 0.0f)
		{
			_anim.SetBool("Searching", false);
			_stateMachine.RemoveState();
		}
	}

	private void Sleep()
	{
		Move(false);

		if (_anim.GetBool("Sleeping") != true)
			_anim.SetBool("Sleeping", true);

		if (_nearbyTargets.Count > 0)
		{
			_anim.SetBool("Sleeping", false);
			_stateMachine.RemoveState();
		}
	}

	private void Chase()
	{
		Move(true);

		if (TargetExist())
		{
			if (InRange(_target.transform) && _target.mIsAlive)
			{
				_lastKnownPosition = _target.transform.position;
			}
			else
			{
				_target = null;
			}
		}

		_navAgent.SetDestination(_lastKnownPosition);

		if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
		{
			if (TargetExist())
			{
				_stateMachine.AddState(Attack);
			}
			else
			{
				_stateMachine.RemoveState();
			}
		}
	}

	private void Attack()
	{
		Move(false);

		if (TargetExist())
		{
			transform.LookAt(_target.transform.position);

			if (Vector3.Distance(transform.position, _target.transform.position) <= _navAgent.stoppingDistance)
			{
				if (_attackTimer > 0)
				{
					_attackTimer -= Time.deltaTime;
				}
				else
				{
					_anim.SetInteger("AttackType", (int) Random.Range(0, AttackTypes));
					_anim.SetTrigger("Attack");
					_attackTimer = ATTACK_DELAY;
				}
			}
			else
			{
				_stateMachine.RemoveState();
			}
		}
		else
		{
			_stateMachine.RemoveState();
		}
	}

	private bool TargetExist()
	{
		if (_target != null)
		{
			if (_target.mIsAlive)
				return true;
			else
				_target = null;
		}

		return false;
	}

	private bool InRange(Transform target)
	{
		return (Vector3.Distance(transform.position, target.position) < _collider.radius);
	}

	private bool InSight(Transform target)
	{
		return (Vector3.Angle(transform.forward, target.transform.position - transform.position) < 45.0f);
	}

	private void Move(bool isTrue)
	{
		if (isTrue)
		{
			_anim.SetBool("Walking", true);
			_navAgent.speed = _speed;
		}
		else
		{
			_anim.SetBool("Walking", false);
			_navAgent.speed = 0;
		}
	}

	private void Die()
	{
		if (_anim.GetBool("IsDead") == false)
			_anim.SetBool("IsDead", true);
		
		Move(false);
	}

	public void DamageTarget(float damage)
	{
		if (TargetExist())
		{
			_target.OnDamageReceived(this, damage);
		}
	}

	public void OnDamageReceived(Agent attacker, float amount)
	{
		_curHealth -= amount;

		if (!mIsAlive)
		{
			_stateMachine.AddState(Die);
		}
		Debug.LogFormat("{0}: {1}/{2}", Flag.ToString(), _curHealth, _maxHealth);

		if (_target == null && attacker.mIsAlive)
			_target = attacker;
	}
}