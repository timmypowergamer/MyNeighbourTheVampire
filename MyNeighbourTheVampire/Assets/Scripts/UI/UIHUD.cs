using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class UIHUD : UIPanel
{
	[SerializeField] private TextMeshProUGUI _invites;
	[SerializeField] private TextMeshProUGUI _guests;
	[SerializeField] private TextMeshProUGUI _killed;

	private void Update()
	{
		_invites.text = GameManager.Instance._numInvited.ToString();
		_guests.text = GameManager.Instance._numGuests.ToString();
		_killed.text = GameManager.Instance._numVampiresKilled.ToString();
	}

	public void Show()
	{
		Animator.SetBool("visible", false);
	}

	public void Hide()
	{
		Animator.SetBool("visible", false);
	}
}
