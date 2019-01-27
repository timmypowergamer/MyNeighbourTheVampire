using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDEvent : GameEvent
{
	public bool Show = false;

	public override IEnumerator Run()
	{
		if (Show)
		{
			CanvasManager.instance.Get<UIHUD>(UIPanelID.HUD).Show();
		}
		else
		{
			CanvasManager.instance.Get<UIHUD>(UIPanelID.HUD).Hide();
		}
		yield return null;
	}
}
