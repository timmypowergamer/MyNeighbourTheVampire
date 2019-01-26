using UnityEngine;

public class VertexTranslateIn : AnimateTMProVertex {
    public Vector3 startOffset;
    public Vector3 endOffset;
    public AnimationCurve offsetCurve;
    public AnimationCurve alphaCurve;

    public override Vector3 LetterTranslateFunction(float p, int index, int numCharacters)
    {
        return Vector3.LerpUnclamped(startOffset, endOffset, offsetCurve.Evaluate(p));
    }

    public override Color32 LetterColorFunction(Color32 originalColor, float p, int index, int numCharacters)
    {
        return new Color32(originalColor.r, originalColor.g, originalColor.b, (byte)(alphaCurve.Evaluate(p) * 255f));
    }

    public override void OnAnimationComplete()
    {
        //nothing!!!!
    }
}
