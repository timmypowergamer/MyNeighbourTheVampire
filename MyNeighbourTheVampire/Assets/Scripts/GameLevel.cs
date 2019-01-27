using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameLevel : MonoBehaviour
{
	public string LevelID;
	public GameEvent[] EventOrder;

	private int _currentIndex = 0;

	private void Awake()
	{
		EventOrder = GetComponentsInChildren<GameEvent>();
	}

	public GameEvent GetNextEvent()
	{
		while (_currentIndex < EventOrder.Length)
		{
			GameEvent ge = EventOrder[_currentIndex];
			_currentIndex++;
			if (ge.CanRun()) return ge; 
		}
		return null;
	}
}
