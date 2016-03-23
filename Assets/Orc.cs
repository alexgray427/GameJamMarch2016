using UnityEngine;
using System.Collections;

public class Orc : Agent
{
	public const float ATTACK_DELAY = 2.0f;
	public const float SLEEP_DELAY = 15.0f;

	private Timer _attackTimer;
	private Timer _sleepTimer;
	private Knight _player;
	private bool _canSleep;

	protected override void Setup()
	{
		_flag = FlagType.Orc;
		_attackTypes = 3;

		_attackTimer = new Timer(ATTACK_DELAY);
		_sleepTimer = new Timer(SLEEP_DELAY);
		_player = Object.FindObjectOfType<Knight>();
		_canSleep = false;

		_attackTimer.OnTimeFinish += AttackTimer_OnTimeFinish;
		Knight.OnPlayerMove += OnPlayerMove;
		Knight.OnPlayerStop += OnPlayerStop;
		_sleepTimer.OnTimeFinish += SleepTimer_OnTimeFinish;
	}

	protected override void Update()
	{
		base.Update();

		if (!TargetExist() && _nearbyTargets.Count > 0)
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
		_stateMachine.AddState(Follow);
	}

	private void Follow()
	{
		if (TargetExist())
		{
			_stateMachine.AddState(Chase);
		}
		else
		{
			if (_canSleep)
			{
				_navAgent.SetDestination(transform.position);
				_anim.SetBool("Sleeping", true);
				_stateMachine.RemoveAllStates();
				_stateMachine.AddState(Sleep);
			}
			else
			{
				_navAgent.SetDestination(_player.transform.position);
			}
		}
	}

	private void Sleep()
	{
		if (!_canSleep || TargetExist())
		{
			_sleepTimer.Reset();
			_anim.SetBool("Sleeping", false);
			_stateMachine.RemoveState();
			_stateMachine.AddState(Init);
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

	private void OnPlayerMove ()
	{
		_sleepTimer.Reset();
		_canSleep = false;
	}

	private void OnPlayerStop ()
	{
		_sleepTimer.Start();
	}

	private void SleepTimer_OnTimeFinish (Timer sender)
	{
		if (sender.Equals(_sleepTimer) && !TargetExist())
		{
			_canSleep = true;
		}
	}
}