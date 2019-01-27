using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEvent : MonoBehaviour
{
	public float startDelay = 1f;
	public float endDelay = 0f;

	public virtual bool CanRun() { return true; }
	public virtual IEnumerator Run() { yield return null; }
}
