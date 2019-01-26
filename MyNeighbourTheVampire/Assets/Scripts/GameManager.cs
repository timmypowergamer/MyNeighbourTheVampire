using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
	private void Start()
	{
		UILetterboxedDialogue dialogue = CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue);
		dialogue.PlayConversation("GUEST_1","intro");
	}	
}
