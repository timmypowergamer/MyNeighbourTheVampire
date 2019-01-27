using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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

	[System.Serializable]
	public class CharacterEntry
	{
		public string CharacterID;
		public bool MustBeInvited;
		public string skipCondition;
		public string entryPoint;
	}

	[System.Serializable]
	public class GameLevel
	{
		public string LevelID;
		public List<CharacterEntry> CharacterOrder = new List<CharacterEntry>();

		private int _currentIndex = 0;

		public CharacterEntry GetNextCharacter()
		{
			while (_currentIndex < CharacterOrder.Count)
			{
				CharacterEntry ce = CharacterOrder[_currentIndex];
				_currentIndex++;
				if (Instance.IsDead(ce.CharacterID))
				{
					continue;
				}
				if(ce.MustBeInvited && !Instance.IsInvited(ce.CharacterID))
				{
					continue;
				}
				if(!string.IsNullOrEmpty(ce.skipCondition))
				{
					if(CheckCondition(ce.skipCondition))
					{
						continue;
					}
				}
				return ce;
			}
			return null;
		}

		public bool CheckCondition(string condition)
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
	}

	[Header("Characters")]
	public List<GameCharacter> GameCharacters = new List<GameCharacter>();

	[Header("Game Setup")]
	public List<GameLevel> Levels = new List<GameLevel>();

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
	}

	public IEnumerator RunLevel(GameLevel level)
	{
		UILetterboxedDialogue dialogue = CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue);

		string completedConvo = "";
		bool awaitingConversation = true;

		CharacterEntry nextCharacter = level.GetNextCharacter();
		while (nextCharacter != null)
		{
			awaitingConversation = true;
			dialogue.OnConversationComplete += (string convoID) => { completedConvo = convoID; awaitingConversation = false; };
			dialogue.PlayConversation(nextCharacter.CharacterID, nextCharacter.entryPoint);
			while (awaitingConversation)
			{
				yield return null;
			}
			nextCharacter = level.GetNextCharacter();
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




}
