using System.Collections;
using UnityEngine;

public class IsometricCamera : MonoBehaviour
{
	public Transform Target;
	public Vector3 CameraOffset;
	public float PositionDamping;

	private void Awake()
	{
		if (CameraOffset == Vector3.zero)
		{
			CameraOffset = transform.position;
		}
	}

	private void Update()
	{
		Vector3 _newPosition = Vector3.Scale(Target.position, new Vector3(1, 0, 1)) + CameraOffset;

		transform.position = Vector3.Lerp(transform.position, _newPosition, Time.deltaTime * PositionDamping);
	}
}