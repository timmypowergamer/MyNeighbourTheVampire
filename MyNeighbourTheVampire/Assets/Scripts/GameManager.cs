﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
	[System.Serializable]
	public class GameCharacter
	{
		public string CharacterID;
		public bool CanBeVampire = true;
		public bool isDead;
		public bool isInvited;
		public bool isVampire;
	}

	[Header("Characters")]
	public List<GameCharacter> GameCharacters = new List<GameCharacter>();

	[SerializeField]
	private RectTransform BackgroundContainer;
	[SerializeField]
	private UnityEngine.UI.Image BackgroundPrefab;

	protected Image _lastBackground;
	protected Image _currentBackground;

	[Header("Game Setup")]
	public GameLevel[] Levels;

	public int NumVampiresMin;
	public int NumVampiresMax;
	private int _numVampires = -1;
	public int NumVampires
	{
		get
		{
			if (_numVampires == -1)
			{
				_numVampires = Random.Range(NumVampiresMin, NumVampiresMax + 1);
			}
			return _numVampires;
		}
	}

	private Dictionary<string, GameCharacter> CharacterDict = new Dictionary<string, GameCharacter>();

	public static GameManager Instance;

	private void Awake()
	{
		Instance = this;
		Levels = GetComponentsInChildren<GameLevel>();
	}

	private void Start()
	{
		List<GameCharacter> availableChars = new List<GameCharacter>(GameCharacters.ToArray());

		foreach(GameCharacter gc in availableChars)
		{
			CharacterDict.Add(gc.CharacterID, gc);
		}

		if (availableChars != null && availableChars.Count > 0)
		{
			int i = 0;
			while (i < NumVampires)
			{
				if (availableChars.Count == 0) break;
				int rand = Random.Range(0, availableChars.Count);

				if (!availableChars[rand].CanBeVampire)
				{
					availableChars.Remove(availableChars[rand]);
					continue;
				}

				CharacterDict[availableChars[rand].CharacterID].isVampire = true;
				availableChars.Remove(availableChars[rand]);
				i++;
			}
		}
		StartCoroutine(StartGame());
	
	}

	public IEnumerator StartGame()
	{
		foreach (GameLevel level in Levels)
		{
			yield return RunLevel(level);
		}
		int vampiresKilled = 0;
		int guestsAlive = 0;
		foreach (GameCharacter gc in GameCharacters)
		{
			if (gc.isVampire && gc.isDead) vampiresKilled++;
			if (!gc.isVampire && !gc.isDead && gc.isInvited) guestsAlive++;
		}
		Debug.LogWarning($"KilledVampires = {vampiresKilled}, Guests Alive = {guestsAlive}");
	}

	public IEnumerator RunLevel(GameLevel level)
	{
		GameEvent nextEvent = level.GetNextEvent();
		while (nextEvent != null)
		{
			yield return nextEvent.Run();
			nextEvent = level.GetNextEvent();
		}
	}

	public bool IsVampire(string characterID)
	{
		if(CharacterDict.ContainsKey(characterID))
		{
			return CharacterDict[characterID].isVampire;
		}
		return false;
	}

	public bool IsDead(string characterID)
	{
		if (CharacterDict.ContainsKey(characterID))
		{
			return CharacterDict[characterID].isDead;
		}
		return false;
	}

	public bool IsInvited(string characterID)
	{
		if (CharacterDict.ContainsKey(characterID))
		{
			return CharacterDict[characterID].isInvited;
		}
		return false;
	}

	public void SetDead(string characterID, bool value)
	{
		if (CharacterDict.ContainsKey(characterID))
		{
			CharacterDict[characterID].isDead = value;
		}
	}

	public void SetInvited(string characterID, bool value)
	{
		if (CharacterDict.ContainsKey(characterID))
		{
			CharacterDict[characterID].isInvited = value;
		}
	}

	public static bool CheckCondition(string condition)
	{
		string[] conds = condition.Split(new string[] { "==" }, System.StringSplitOptions.None);
		if (Fungus.ConversationManager.Instance.Variables.ContainsKey(conds[0]))
		{
			if (Fungus.ConversationManager.Instance.Variables[conds[0]] == conds[1])
			{
				return true;
			}
		}
		return false;
	}

	public void SetBackground(Sprite newBackground)
	{
		if (_currentBackground == null || _currentBackground.sprite != newBackground)
		{
			if (_currentBackground != null)
			{
				_lastBackground = _currentBackground;
				_lastBackground.GetComponent<Animator>().SetTrigger("hide");
			}
			_currentBackground = Instantiate(BackgroundPrefab, BackgroundContainer.transform, false);
			_currentBackground.sprite = newBackground;
			_currentBackground.GetComponent<Animator>().SetTrigger("show");
		}
	}

}
