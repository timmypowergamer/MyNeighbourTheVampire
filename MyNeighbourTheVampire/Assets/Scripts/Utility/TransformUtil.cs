using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class TransformUtil
{
	public static void DestroyChildren( Transform trans, Predicate<Transform> predicate = null, bool detach = true, bool despawnIfPooled = true)
	{
		for (int i = trans.childCount - 1; i >= 0; i--)
		{
			Transform child = trans.GetChild(i);
			if (child == null) continue;
			if (predicate != null) if (!predicate(child)) continue;
			{
				UnityEngine.Object.Destroy(child.gameObject);
			}
		}

		if (detach) trans.DetachChildren();
	}

	public static void DestroyChildrenImmediate( Transform trans )
	{
		var children = trans.GetComponentsInChildren<Transform>( true ).Where( t => t != trans ).ToList();
		foreach (var child in children)
		{
			if (child == null) continue;
			UnityEngine.Object.DestroyImmediate(child.gameObject);
		}
	}

	private static void LogPathFail(Transform xf, string path)
	{
		if (xf != null)
			return;
		Debug.LogError("Child transform path not found: " + path);
	}

	public static void SetInactive(Transform xf, string path = null)
	{
		if (!string.IsNullOrEmpty(path))
			xf = xf.Find(path);
		if (xf == null) return;
		xf.gameObject.SetActive(false);
	}

	public static string GetPath(this Transform current)
	{
		if (current.parent == null)
			return "/" + current.name;
		return current.parent.GetPath() + "/" + current.name;
	}

	public static void SetLayerRecursive(GameObject parent, int layer)
	{
		parent.layer = layer;
		foreach (Transform child in parent.transform)
		{
			SetLayerRecursive(child.gameObject, layer);
		}
	}

	public static Vector2 GetLocalPoint(RectTransform myRect, Vector3 worldPos, Camera destCamera, Camera sourceCamera)
	{
		Vector3 screenPos = sourceCamera.WorldToScreenPoint(worldPos);
		return GetLocalPoint(myRect, new Vector2(screenPos.x, screenPos.y), destCamera);
	}

	public static Vector2 GetLocalPoint(RectTransform myRect, Vector2 screenPos, Camera camera)
	{
		RectTransform parentRect = myRect.parent.GetComponent<RectTransform>();
		if (parentRect != null)
		{
			Vector2 local;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, screenPos, camera, out local);
			return local;
		}
		return screenPos;
	}

	public static Rect RectTransformToScreenSpace(RectTransform transform, Camera cam)
	{
		Vector3[] corners = new Vector3[4];
		transform.GetWorldCorners(corners);
		//convert to screen space
		for(int i = 0; i < 4; i++)
		{
			corners[i] = cam.WorldToScreenPoint(corners[i]);
        }

		return new Rect(
			corners[0].x,
			corners[0].y,
			corners[3].x - corners[0].x,
			corners[1].y - corners[0].y);
    }

	public static T GetOrAddComponent<T>(Transform trans) where T:Component
	{
		T ret = trans.GetComponent<T>();
		if(ret == null)
		{
			ret = trans.gameObject.AddComponent<T>();
		}
		return ret;
	}
}
