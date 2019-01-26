using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using UnityEngine.UI;

public class PortraitGraphic_Image : PortraitGraphic
{
	[SerializeField] private Image image;
	public Image Image
	{
		get
		{
			if (image == null) image = GetComponentInChildren<Image>();
			return image;
		}
		set
		{
			image = value;
		}
	}

	public override void SetColor(Color c)
	{
		Image.color = c;
	}

	public override void SetPortrait(Character.PortraitData portrait)
	{
		if (portrait != null)
		{
			Image.sprite = portrait.Sprite;
		}
	}
}
