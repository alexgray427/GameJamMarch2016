using UnityEngine;
using System.Collections;

public class Ghoul : Agent
{
	public const float SEARCH_DELAY = 5.0f;
	public const float ATTACK_DELAY = 2.5f;

	private Timer _searchTimer;
	private Timer _attackTimer;

	protected override void Setup()
	{
		_flag = FlagType.Ghoul;
		_attackTypes = 3;

		_searchTimer = new Timer(SEARCH_DELAY);
		_attackTimer = new Timer(ATTACK_DELAY);

		_searchTimer.OnTimeStart += SearchTimer_OnTimeStart;
		_attackTimer.OnTimeFinish += AttackTimer_OnTimeFinish;
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

			if (agent.mIsAlive && (agent.mFlag == FlagType.Knight || agent.mFlag == FlagType.Orc))
			{
				_nearbyTargets.Add(agent);
			}
		}
	}

	protected override void Init()
	{
		_stateMachine.AddState(Wander);
	}

	private void Wander()
	{
		if (TargetExist())
		{
			_stateMachine.AddState(Chase);
		}

		if (_navAgent.remainingDistance <= _navAgent.stoppingDistance)
		{
			_searchTimer.Start();
		}
	}

	private void Search()
	{
		Debug.Log(_searchTimer.mSecondsLeft);

		if (_searchTimer.mIsFinished || TargetExist())
		{
			Debug.Log("Finished Searching");
			_searchTimer.Reset();
			_anim.SetBool("Searching", false);
			_stateMachine.RemoveState();
			_navAgent.SetDestination(transform.position + new Vector3(Random.Range(-10, 10), 0, Random.Range(-10, 10)));
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

	private void SearchTimer_OnTimeStart (Timer sender)
	{
		if (sender.Equals(_searchTimer))
		{
			Debug.Log("Started Searching");
			_anim.SetBool("Searching", true);
			_stateMachine.AddState(Search);
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