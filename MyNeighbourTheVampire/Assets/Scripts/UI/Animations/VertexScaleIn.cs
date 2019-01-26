using UnityEngine;

public class VertexScaleIn : AnimateTMProVertex {
    public Vector3 startScale;
    public Vector3 endScale;
    public AnimationCurve scaleCurve;

    public override Vector3 LetterScaleFunction(float p, int index, int numCharacters)
    {
        return Vector3.LerpUnclamped(startScale, endScale, scaleCurve.Evaluate(p));
    }
}
