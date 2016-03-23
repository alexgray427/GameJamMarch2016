using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MatchManager : MonoBehaviour
{
	public delegate void MatchEventHandler();
	public event MatchEventHandler OnUpdate;

	public enum MatchState
	{
		Start,
		CountDown,
		InWave,
		End
	}
		
	public const float TIME_BETWEEN_WAVES = 10.0f;

	public static MatchManager Instance { get; private set; }
	public Text WaveTimeText;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else
			Destroy(this);
	}

	private void Update()
	{
		if (OnUpdate != null)
			OnUpdate();
	}
}

public class Timer
{
	public delegate void TimeEventHandler(Timer sender);
	public event TimeEventHandler OnTimeStart;
	public event TimeEventHandler OnTimeFinish;

	public float mSeconds { get; private set; }

	public float mSecondsLeft
	{
		get { return _count; }
	}

	public bool mIsFinished
	{
		get { return !_active || _count <= 0.0f ? true : false; }
	}

	private float _count;
	private bool _active;

	public Timer(float seconds)
	{
		MatchManager.Instance.OnUpdate += Update;
		mSeconds = seconds;
		Reset();
	}

	public void Update()
	{
		if (_active)
		{
			if (_count > 0.0f)
			{
				_count -= Time.deltaTime;
			}
			else
			{
				Stop();

				if (OnTimeFinish != null)
					OnTimeFinish(this);
			}
		}
	}

	public void Start()
	{
		_active = true;

		if (OnTimeStart != null)
			OnTimeStart(this);
	}

	public void Stop()
	{
		_active = false;
	}

	public void Reset()
	{
		Stop();
		_count = mSeconds;
	}
}