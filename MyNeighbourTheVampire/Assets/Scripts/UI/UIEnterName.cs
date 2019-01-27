using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Fungus;

public class UIEnterName : UIPanel
{

	public System.Action OnClosed;
	public TMP_InputField _input;

	public void OnButtonPress()
	{
		ConversationManager.Instance.Variables["player"] = _input.text;
		Close("close");
	}

	public override void Close()
	{
		base.Close();
		OnClosed?.Invoke();
		OnClosed = null;
	}
}
