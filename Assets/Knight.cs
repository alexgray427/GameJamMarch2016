using UnityEngine;
using System.Collections;

public class Knight : Agent
{
	public delegate void PlayerEventHandler();
	public static event PlayerEventHandler OnPlayerMove;
	public static event PlayerEventHandler OnPlayerStop;

	public const float ATTACK_DELAY = 1.5f;

	private Timer _attackTimer;

	protected override void Setup()
	{
		_flag = FlagType.Knight;
		_attackTypes = 3;

		_attackTimer = new Timer(ATTACK_DELAY);
		_attackTimer.OnTimeFinish += AttackTimer_OnTimeFinish;
	}

	protected override void Update()
	{
		_stateMachine.Update();

		_anim.SetBool("Walking", _navAgent.speed > 0.0f ? true : false);

		CheckInput();

		if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
		{
			_navAgent.speed = 0.0f;

			if (OnPlayerStop != null)
			{
				OnPlayerStop();
			}
		}
		else
		{
			_navAgent.speed = _speed;

			if (OnPlayerMove != null)
			{
				OnPlayerMove();
			}
		}
	}

	protected override void OnTriggerEnter(Collider other)
	{
		if (other.tag == "Agent")
		{
			Agent agent = other.gameObject.GetComponent<Agent>();

			if (agent.mIsAlive && (agent.mFlag == FlagType.Ghost || agent.mFlag == FlagType.Ghoul))
			{
				_nearbyTargets.Add(agent);
			}
		}
	}

	protected override void Init()
	{
		_stateMachine.AddState(Idle);
	}

	private void CheckInput()
	{
		if (Input.GetMouseButton(0))
		{
			Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;

			if (Physics.Raycast(ray, out hit, 100.0f, LayerMask.GetMask("Ground")))
			{
				_navAgent.SetDestination(hit.point);
				_stateMachine.RemoveAllStates();
				_stateMachine.AddState(Init);
				_target = null;
			}
		}
	}

	private void Idle()
	{
		if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
		{
			if (TargetExist())
			{
				_stateMachine.AddState(Chase);
			}
			else if (_nearbyTargets.Count > 0)
			{
				foreach (Agent target in _nearbyTargets)
				{
					if (InRange(target.transform) && InSight(target.transform))
					{
						_target = target;
					}
				}
			}
		}
	}

	private void Chase()
	{
		if (TargetExist())
		{
			if (InRange(_target.transform))
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
		if (TargetExist())
		{
			transform.LookAt(_target.transform.position);

			if (Vector3.Distance(transform.position, _target.transform.position) <= _navAgent.stoppingDistance)
			{
				_attackTimer.Start();
			}
			else
			{
				_attackTimer.Reset();
				_stateMachine.RemoveState();
			}
		}
		else
		{
			_stateMachine.RemoveState();
		}
	}

	private void AttackTimer_OnTimeFinish (Timer sender)
	{
		if (sender.Equals(_attackTimer))
		{
			_anim.SetInteger("AttackType", (int) Random.Range(0, _attackTypes));
			_anim.SetTrigger("Attack");
			_attackTimer.Reset();
		}
	}
}