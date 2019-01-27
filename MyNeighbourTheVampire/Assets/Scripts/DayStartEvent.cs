using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DayStartEvent : GameEvent
{
	public int dayNum;
	public string DayName;
	public string DayTitle;

	public override IEnumerator Run()
	{
		UIFader fader = CanvasManager.instance.Get<UIFader>(UIPanelID.Fader);
		yield return fader.StartDay(dayNum, DayName, DayTitle);
	}
}
