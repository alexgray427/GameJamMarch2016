using System.Collections.Generic;

public class StackFSM
{
	public delegate void StateHandler();

	public StateHandler CurrentState
	{
		get { return _stack.Count > 0 ? _stack[_stack.Count - 1] : null; }
	}

	private List<StateHandler> _stack;

	public StackFSM()
	{
		_stack = new List<StateHandler>();
	}

	public void Update()
	{
		if (CurrentState != null)
			CurrentState();
	}

	public void AddState(StateHandler state)
	{
		if (CurrentState != state)
		{
			_stack.Add(state);
		}
	}

	public void RemoveState()
	{
		_stack.RemoveAt(_stack.Count - 1);
	}
}