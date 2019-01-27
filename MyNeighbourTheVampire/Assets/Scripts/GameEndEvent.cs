using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEndEvent : GameEvent
{
	public override IEnumerator Run()
	{
		EndScreen fader = CanvasManager.instance.Get<EndScreen>(UIPanelID.EndScreen);
		fader.Open();
		yield return null;
	}
}
