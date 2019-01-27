using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnterNameEvent : GameEvent
{
	public override IEnumerator Run()
	{
		UIEnterName nameEntry = CanvasManager.instance.Get<UIEnterName>(UIPanelID.EnterName);
		bool wait = true;
		nameEntry.OnClosed += () => { wait = false; };
		nameEntry.Open("bring_in_fast");
		while (wait)
		{
			yield return false;
		}
	}
}
