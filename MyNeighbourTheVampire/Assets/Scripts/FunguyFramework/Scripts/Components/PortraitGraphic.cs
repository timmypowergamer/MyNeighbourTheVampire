using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Fungus
{
	public abstract class PortraitGraphic : MonoBehaviour {

		public RectTransform rectTransform
		{
			get
			{
				if (gameObject != null)
				{
					return gameObject.GetComponent<RectTransform>();
				}
				return null;
			}
		}

		[SerializeField] protected RectTransform mouth;
		public RectTransform Mouth { get { return mouth; } }

		public abstract void SetPortrait(Character.PortraitData portrait);

		public abstract void SetColor(Color c);

		/// <summary>
		/// Performs a deep copy of all values from one RectTransform to another.
		/// </summary>
		public static void SetRectTransform(RectTransform oldRectTransform, RectTransform newRectTransform)
		{
			oldRectTransform.eulerAngles = newRectTransform.eulerAngles;
			oldRectTransform.position = newRectTransform.position;
			oldRectTransform.rotation = newRectTransform.rotation;
			oldRectTransform.anchoredPosition = newRectTransform.anchoredPosition;
			oldRectTransform.sizeDelta = newRectTransform.sizeDelta;
			oldRectTransform.anchorMax = newRectTransform.anchorMax;
			oldRectTransform.anchorMin = newRectTransform.anchorMin;
			oldRectTransform.pivot = newRectTransform.pivot;
			oldRectTransform.localScale = newRectTransform.localScale;
		}
	}
}
