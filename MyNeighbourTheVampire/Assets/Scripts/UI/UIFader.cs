using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIFader : UIPanel
{
	[SerializeField] private TextMeshProUGUI _timeLabel;
	[SerializeField] private TextMeshProUGUI _dayLabel;
	[SerializeField] private TextMeshProUGUI _titleLabel;

	private string[] months = new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };

	public IEnumerator StartDay(int dayNum, string DayName, string dayTitle)
	{
		_dayLabel.text = DayName;
		_titleLabel.text = dayTitle;
		System.DateTime date = System.DateTime.Now.AddDays(dayNum);
		_timeLabel.text = $"{months[date.Month - 1]} {date.Day}, {date.Year}";

		gameObject.SetActive(true);
		Animator.SetTrigger("start");
		yield return new WaitForSeconds(0.2f);
;
		yield return AnimationUtil.WaitForAnim(Animator, null, "");
		gameObject.SetActive(false);
	}

	public IEnumerator EndDay()
	{
		gameObject.SetActive(true);
		Animator.SetTrigger("end");
		yield return new WaitForSeconds(0.2f);
		yield return AnimationUtil.WaitForAnim(Animator, null, "");
		gameObject.SetActive(false);
	}

	public void ClearBG()
	{
		GameManager.Instance.SetBackground(null);
	}
}
