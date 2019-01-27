using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayCompleteEvent : GameEvent
{
	public override IEnumerator Run()
	{
		UIFader fader = CanvasManager.instance.Get<UIFader>(UIPanelID.Fader);
		yield return fader.EndDay();
	}
}
