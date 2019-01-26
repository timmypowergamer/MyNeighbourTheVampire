using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class AnimateTMProVertex : MonoBehaviour {
    public enum LetterPivot { center, baselineCenter, baselineLeft, baselineRight };

    public float duration = 3f; //duration is on a per character basis
    public float letterDelay = 0.1f; //delay between each letter starting the animation
    public LetterPivot pivot = LetterPivot.center;

    private TMP_Text m_TextComponent;
    private bool hasTextChanged;
    private bool animating = false;

    [SerializeField]
    bool debug = false;

    bool initialized = false; //safety net to make sure TMP finishes all its inits before we start the animation

    void Awake()
    {
        m_TextComponent = GetComponent<TMP_Text>();
    }

    // Use this for initialization
    void OnEnable()
    {
        StartCoroutine(AnimateVertices());
    }

    void Start()
    {
        initialized = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (debug && Input.GetKeyDown(KeyCode.Space) && !animating)
        {
            StartCoroutine(AnimateVertices());
        }
    }

    IEnumerator AnimateVertices()
    {
        //Debug.Log("start");
        animating = true;
        if (!initialized)
        {
            yield return new WaitForEndOfFrame();
        }
        // We force an update of the text object since it would only be updated at the end of the frame. Ie. before this code is executed on the first frame.
        // Alternatively, we could yield and wait until the end of the frame when the text object will be generated.
        m_TextComponent.ForceMeshUpdate();

        TMP_TextInfo textInfo = m_TextComponent.textInfo;

        Matrix4x4 matrix;

        // Cache the vertex data of the text object as the translate FX is applied to the original position of the characters.
        TMP_MeshInfo[] cachedMeshInfo = textInfo.CopyMeshInfoVertexData();
        Color32[] newVertexColors;
        Color32 c0 = m_TextComponent.color;

        //start time of this whole animation
        float animStartTime = Time.time;


        while (animating)
        {
            int characterCount = textInfo.characterCount;
            for (int i = 0; i < characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

                // Skip characters that are not visible and thus have no geometry to manipulate.
                if (!charInfo.isVisible)
                    continue;

                // Get the index of the material used by the current character.
                int materialIndex = textInfo.characterInfo[i].materialReferenceIndex;

                // Get the vertex colors of the mesh used by this text element (character or sprite).
                newVertexColors = textInfo.meshInfo[materialIndex].colors32;

                // Get the index of the first vertex used by this text element.
                int vertexIndex = textInfo.characterInfo[i].vertexIndex;

                // Get the cached vertices of the mesh used by this text element (character or sprite).
                Vector3[] sourceVertices = cachedMeshInfo[materialIndex].vertices;

                // calculate the pivot for each character. default to center
                Vector2 charPivot = Vector2.zero;
                switch (pivot)
                {
                    case LetterPivot.baselineCenter:
                        charPivot = new Vector2((sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x) / 2, charInfo.baseLine);
                        break;
                    case LetterPivot.baselineLeft:
                        charPivot = new Vector2(sourceVertices[vertexIndex + 0].x, charInfo.baseLine);
                        break;
                    case LetterPivot.baselineRight:
                        charPivot = new Vector2(sourceVertices[vertexIndex + 0].x + sourceVertices[vertexIndex + 2].x, charInfo.baseLine);
                        break;
                    default:
                        //center
                        charPivot = (sourceVertices[vertexIndex + 0] + sourceVertices[vertexIndex + 2]) / 2;
                        break;
                }

                // Need to translate all 4 vertices of each quad to aligned with middle of character / baseline.
                // This is needed so the matrix TRS is applied at the origin for each character.
                Vector3 offset = charPivot;

                Vector3[] destinationVertices = textInfo.meshInfo[materialIndex].vertices;

                destinationVertices[vertexIndex + 0] = sourceVertices[vertexIndex + 0] - offset;
                destinationVertices[vertexIndex + 1] = sourceVertices[vertexIndex + 1] - offset;
                destinationVertices[vertexIndex + 2] = sourceVertices[vertexIndex + 2] - offset;
                destinationVertices[vertexIndex + 3] = sourceVertices[vertexIndex + 3] - offset;

                //progress through the animation
                float p = Mathf.Clamp01((Time.time - (animStartTime + (i * letterDelay))) / duration);

                //ANIMATION MAGIC HAPPENS HERE
                Vector3 currentTranslate = LetterTranslateFunction(p, i, characterCount);
                Quaternion currentRotate = LetterRotateFunction(p, i, characterCount);
                Vector3 currentScale = LetterScaleFunction(p, i, characterCount);
                //ok magic's over pass in that magic
                matrix = Matrix4x4.TRS(currentTranslate, currentRotate, currentScale);

                destinationVertices[vertexIndex + 0] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 0]);
                destinationVertices[vertexIndex + 1] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 1]);
                destinationVertices[vertexIndex + 2] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 2]);
                destinationVertices[vertexIndex + 3] = matrix.MultiplyPoint3x4(destinationVertices[vertexIndex + 3]);

                destinationVertices[vertexIndex + 0] += offset;
                destinationVertices[vertexIndex + 1] += offset;
                destinationVertices[vertexIndex + 2] += offset;
                destinationVertices[vertexIndex + 3] += offset;

                //apply color animation
                // Only change the vertex color if the text element is visible.
                if (textInfo.characterInfo[i].isVisible)
                {
                    //color animation magic happens here!!!
                    c0 = LetterColorFunction(c0, p, i, characterCount);
                    //kk magic's over pass it all to the verticies
                    newVertexColors[vertexIndex + 0] = c0;
                    newVertexColors[vertexIndex + 1] = c0;
                    newVertexColors[vertexIndex + 2] = c0;
                    newVertexColors[vertexIndex + 3] = c0;

                    // New function which pushes (all) updated vertex data to the appropriate meshes when using either the Mesh Renderer or CanvasRenderer.
                    m_TextComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

                    // This last process could be done to only update the vertex data that has changed as opposed to all of the vertex data but it would require extra steps and knowing what type of renderer is used.
                    // These extra steps would be a performance optimization but it is unlikely that such optimization will be necessary.
                }

                //if the last character has finished its animation progression, set animating to false
                if (i == characterCount - 1 && p >= 1f)
                {
                    animating = false;
                    //Debug.Log("stopped");
                    OnAnimationComplete();
                }
            }

            // Push changes into meshes
            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                m_TextComponent.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }

            yield return new WaitForEndOfFrame();
        }
    }

    public virtual Vector3 LetterTranslateFunction(float p, int index, int numCharacters)
    {
        return Vector3.zero;
    }

    public virtual Quaternion LetterRotateFunction(float p, int index, int numCharacters)
    {
        return Quaternion.Euler(0, 0, 0);
    }

    public virtual Vector3 LetterScaleFunction(float p, int index, int numCharacters)
    {
        return Vector3.one;
    }

    public virtual Color32 LetterColorFunction(Color32 originalColor, float p, int index, int numCharacters){
        return originalColor;
    }

    public virtual void OnAnimationComplete(){
        //nothing!!!!
    }
}
