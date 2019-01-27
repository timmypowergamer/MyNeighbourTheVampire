using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIDialogueOptions : UIPanel
{

	public System.Action<string> OnChoiceMade;

	[SerializeField]
	private RectTransform _content;
	[SerializeField]
	private DialogueOption _itemPrefab;

	private string _pickedOption;

	public void Open(string[] links, string[] text, System.Action<string> onComplete)
	{
		if(!string.IsNullOrEmpty(links[0]) && string.IsNullOrEmpty(text[0]))
		{
			onComplete.Invoke(links[0]);
			return;
		}

		Open("bring_in");
		TransformUtil.DestroyChildren(_content);

		for(int i = 0; i < links.Length; i++)
		{
			DialogueOption option = Instantiate(_itemPrefab, _content, false);
			option.Set(i, text[i], links[i]);
		}
		OnChoiceMade += onComplete;
	}

	public void OptionPicked(string option)
	{
		_pickedOption = option;
		Close("close");
	}
	public override void Close()
	{
		OnChoiceMade?.Invoke(_pickedOption);
		OnChoiceMade = null;
		base.Close();
	}

}
