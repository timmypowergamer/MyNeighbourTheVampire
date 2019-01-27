using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneEvent : GameEvent
{
	public Sprite BackgroundSprite;

	public override bool CanRun()
	{
		return base.CanRun();
	}

	public override IEnumerator Run()
	{
		yield return new WaitForSeconds(startDelay);
		GameManager.Instance.SetBackground(BackgroundSprite);
		yield return new WaitForSeconds(endDelay);
	}
}
