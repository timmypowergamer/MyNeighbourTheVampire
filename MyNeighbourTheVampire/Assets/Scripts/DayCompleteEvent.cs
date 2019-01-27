using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;

public class DayCompleteEvent : GameEvent
{
	public override IEnumerator Run()
	{
		UIFader fader = CanvasManager.instance.Get<UIFader>(UIPanelID.Fader);
		yield return fader.EndDay();
		ConversationManager.Instance.Variables["guests"] = GameManager.Instance._numGuests.ToString();
		ConversationManager.Instance.Variables["party"] = (GameManager.Instance._numGuests >= 104 ? "success" : "fail");
	}
}
