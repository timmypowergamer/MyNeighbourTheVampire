using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	private List<string> _vampires = new List<string>();
	public int NumVampires = 3;

	public static GameManager Instance;

	private void Awake()
	{
		Instance = this;
	}

	private void Start()
	{
		List<Fungus.Character> characters = Fungus.ConversationManager.Instance.GetAllCharacters();
		if (characters != null && characters.Count > 0)
		{
			for (int i = 0; i < NumVampires; i++)
			{
				if (characters.Count == 0) break;
				int rand = Random.Range(0, characters.Count);
				_vampires.Add(characters[rand].gameObject.name);
				characters.Remove(characters[rand]);
			}

			UILetterboxedDialogue dialogue = CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue);
			dialogue.PlayConversation("GUEST_1", "intro");
		}
	}	

	public bool IsVampire(string characterID)
	{
		return _vampires.Contains(characterID);
	}


}
