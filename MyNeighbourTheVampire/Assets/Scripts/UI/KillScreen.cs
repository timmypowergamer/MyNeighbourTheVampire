using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillScreen : UIPanel
{
	public bool canContinue = false;

	public override void Open(string transitionTrigger = "open")
	{
		base.Open(transitionTrigger);
	}

	protected override void onOpenComplete()
	{
		base.onOpenComplete();
		canContinue = true;
	}

	public void Continue()
	{
		if(canContinue)
		{
			Close("close");
		}
	}

	public override void Close()
	{
		base.Close();
		UnityEngine.SceneManagement.SceneManager.LoadScene(0);
	}

	public void Quit()
	{
		if(canContinue)
		{
			Application.Quit();
		}
	}
}
