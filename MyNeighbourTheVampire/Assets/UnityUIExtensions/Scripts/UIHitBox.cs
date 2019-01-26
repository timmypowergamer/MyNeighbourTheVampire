using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// An empty graphic that can be hit by raycasts, but does not add any draw calls.
/// </summary>
public class UIHitBox : Graphic
{
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
    }
}
