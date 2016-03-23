using System;
using System.Collections;
using UnityEngine;

public class MatchManager : MonoBehaviour
{
	public enum MatchState
	{
		Start,
		CountDown,
		InWave,
		End
	}

	public class PoolManager
	{

	}

	[Serializable]
	public struct WaveData
	{
		public SpawnData[] Spawns;
	}

	[Serializable]
	public struct SpawnData
	{
		public string Name;
	}

	public const float TIME_BETWEEN_WAVES = 10.0f;

	public static MatchManager Instance { get; private set; }

	public WaveData[] Waves;

	public Agent Player;

	private MatchState _state;
	private float _waveCountDown;
	private int _currentWave;

	private void Awake()
	{
		if (Instance == null)
			Instance = this;
		else
			Destroy(this);

		_state = MatchState.Start;
		_waveCountDown = TIME_BETWEEN_WAVES;
		_currentWave = 0;
	}

	IEnumerator Start()
	{
		yield return StartCoroutine(CountDown());
	}

	IEnumerator CountDown()
	{
		yield return new WaitForSeconds(TIME_BETWEEN_WAVES);
		StartCoroutine(SpawnWave());
		yield return null;
	}

	IEnumerator SpawnWave()
	{
		for(int cnt = 0; cnt < Waves[_currentWave].Spawns.Length; cnt++)
		{
			Debug.Log("Spawned: " + Waves[_currentWave].Spawns[cnt].Name);
			yield return new WaitForSeconds(0.5f);
		}

		_currentWave++;
		_state = MatchState.InWave;
		_waveCountDown = TIME_BETWEEN_WAVES;
		yield return null;
	}
}