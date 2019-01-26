using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueOption : MonoBehaviour
{
	private UIDialogueOptions _parent;
	[SerializeField]
	private TextMeshProUGUI _label;
	[SerializeField]
	private TextMeshProUGUI _optionLabel;

	private string[] options = new string[] { "(A)", "(B)", "(C)", "(D)" };

	private string _link;

	public void Set(int optionNum, string text, string link)
	{
		_parent = GetComponentInParent<UIDialogueOptions>();
		GetComponent<Button>().onClick.AddListener(Clicked);
		_link = link;

		_optionLabel.text = options[optionNum];
		_label.text = text;
	}

	public void Clicked()
	{
		_parent.OptionPicked(_link);
	}
}
