using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterEvent : GameEvent
{
	public string CharacterID;
	public bool MustBeInvited;
	public string skipCondition;
	public string entryPoint;

	public override bool CanRun()
	{
		if (GameManager.Instance.IsDead(CharacterID))
		{
			return false;
		}
		if (MustBeInvited && !GameManager.Instance.IsInvited(CharacterID))
		{
			return false;
		}
		if (!string.IsNullOrEmpty(skipCondition))
		{
			if (GameManager.CheckCondition(skipCondition))
			{
				return false;
			}
		}
		return true;
	}

	public override IEnumerator Run()
	{
		UILetterboxedDialogue dialogue = CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue);

		string completedConvo = "";
		bool awaitingConversation = true;

		dialogue.OnConversationComplete += (string convoID) => { completedConvo = convoID; awaitingConversation = false; };
		dialogue.PlayConversation(CharacterID, entryPoint);
		while (awaitingConversation)
		{
			yield return null;
		}
	}
}

