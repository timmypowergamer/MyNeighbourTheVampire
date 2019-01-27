// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;

namespace Fungus
{
    /// <summary>
    /// Display story text in a visual novel style dialog box.
    /// </summary>
    public class SayDialog : MonoBehaviour
    {
        //[Tooltip("Duration to fade dialogue in/out")]
        //[SerializeField] protected float fadeDuration = 0.25f;

        [Tooltip("The continue button UI object")]
        [SerializeField] protected Button continueButton;

        [Tooltip("The name text UI object")]
        [SerializeField] protected TextMeshProUGUI nameText;

		[SerializeField] protected GameObject nameBox;

		[Tooltip("Graphic that should be colored with character color")]
		[SerializeField] protected Graphic nameColorGraphic;

        [Tooltip("The story text UI object")]
        [SerializeField] protected TextMeshProUGUI storyText;
        public virtual TextMeshProUGUI StoryText { get { return storyText; } }

		[Tooltip("Say bubble arrow/tick thingy for showing who is speaking")]
		[SerializeField] protected RectTransform arrow;
		public virtual RectTransform Arrow { get { return arrow; } }

		[SerializeField] protected Image background;
		public virtual Image Background { get { return background; } }

		[SerializeField] protected Image backgroundPrefab;


        [Tooltip("Close any other open Say Dialogs when this one is active")]
        [SerializeField] protected bool closeOtherDialogs;

        protected float startStoryTextWidth;
        protected float startStoryTextInset;

        protected WriterAudio writerAudio;
        protected Writer writer;
        protected CanvasGroup canvasGroup;

        protected bool fadeWhenDone = true;
        protected float targetAlpha = 0f;
        protected float fadeCoolDownTimer = 0f;

		protected Image _lastBackground;
		protected Image _currentBackground;

        protected Sprite currentCharacterImage;

        // Most recent speaking character
        protected static Character speakingCharacter;

		// Cache active Say Dialogs to avoid expensive scene search
		protected static List<SayDialog> activeSayDialogs = new List<SayDialog>();

		protected virtual void Awake()
		{
			if (!activeSayDialogs.Contains(this))
			{
				activeSayDialogs.Add(this);
			}
		}

		protected virtual void OnDestroy()
		{
			activeSayDialogs.Remove(this);
		}

		protected virtual Writer GetWriter()
        {
            if (writer != null)
            {
                return writer;
            }

            writer = GetComponent<Writer>();
            if (writer == null)
            {
                writer = gameObject.AddComponent<Writer>();
            }

            return writer;
        }

        protected virtual WriterAudio GetWriterAudio()
        {
            if (writerAudio != null)
            {
                return writerAudio;
            }

            writerAudio = GetComponent<WriterAudio>();
            if (writerAudio == null)
            {
                writerAudio = gameObject.AddComponent<WriterAudio>();
            }

            return writerAudio;
        }

        protected virtual void Start()
        {
            // Add a raycaster if none already exists so we can handle dialog input
            GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                gameObject.AddComponent<GraphicRaycaster>();
            }

            // It's possible that SetCharacterImage() has already been called from the
            // Start method of another component, so check that no image has been set yet.
            // Same for nameText.

            //if (nameText != null && nameText.text == "")
            //{
            //    SetCharacterName("", Color.white);
            //}
        }

        protected virtual void LateUpdate()
        {
            UpdateAlpha();

            if (continueButton != null)
            {
                continueButton.gameObject.SetActive( GetWriter().IsWaitingForInput );
            }
		}

        protected virtual void UpdateAlpha()
        {
			if (GetWriter().IsWriting)
			{
				targetAlpha = 1f;
				fadeCoolDownTimer = 0.1f;
			}
			else if (fadeWhenDone && Mathf.Approximately(fadeCoolDownTimer, 0f))
			{
				targetAlpha = 0f;
			}
			else
			{
				// Add a short delay before we start fading in case there's another Say command in the next frame or two.
				// This avoids a noticeable flicker between consecutive Say commands.
				fadeCoolDownTimer = Mathf.Max(0f, fadeCoolDownTimer - Time.deltaTime);
			}
		}

        protected virtual void ClearStoryText()
        {
            if (storyText != null)
            {
                storyText.text = "";
            }
        }

        #region Public members

		public Character SpeakingCharacter { get { return speakingCharacter; } }

        /// <summary>
        /// Currently active Say Dialog used to display Say text
        /// </summary>
        public static SayDialog ActiveSayDialog { get; set; }

        /// <summary>
        /// Returns a SayDialog by searching for one in the scene or creating one if none exists.
        /// </summary>
        public static SayDialog GetSayDialog()
        {
            if (ActiveSayDialog == null)
            {
				SayDialog sd = null;

				// Use first active Say Dialog in the scene (if any)
				if (activeSayDialogs.Count > 0)
				{
					sd = activeSayDialogs[0];
				}

                if (sd != null)
                {
                    ActiveSayDialog = sd;
                }

                if (ActiveSayDialog == null)
                {
                    // Auto spawn a say dialog object from the prefab
                    GameObject prefab = Resources.Load<GameObject>("Prefabs/SayDialog");
                    if (prefab != null)
                    {
                        GameObject go = Instantiate(prefab) as GameObject;
                        go.SetActive(false);
                        go.name = "SayDialog";
                        ActiveSayDialog = go.GetComponent<SayDialog>();
                    }
                }
            }

            return ActiveSayDialog;
        }

        /// <summary>
        /// Stops all active portrait tweens.
        /// </summary>
        public static void StopPortraitTweens()
        {
            // Stop all tweening portraits
            var activeCharacters = Character.ActiveCharacters;
            for (int i = 0; i < activeCharacters.Count; i++)
            {
                var c = activeCharacters[i];
                if (c.State.portraitGraphic != null)
                {
                    if (LeanTween.isTweening(c.State.portraitGraphic.gameObject))
                    {
                        LeanTween.cancel(c.State.portraitGraphic.gameObject, true);
                        PortraitGraphic.SetRectTransform(c.State.portraitGraphic.rectTransform, c.State.position);
                        if (c.State.dimmed == true)
                        {
                            c.State.portraitGraphic.SetColor(new Color(0.5f, 0.5f, 0.5f, 1f));
                        }
                        else
                        {
                            c.State.portraitGraphic.SetColor(Color.white);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Sets the active state of the Say Dialog gameobject.
        /// </summary>
        public virtual void SetActive(bool state)
        {
            gameObject.SetActive(state);
        }

        /// <summary>
        /// Sets the active speaking character.
        /// </summary>
        /// <param name="character">The active speaking character.</param>
        public virtual void SetCharacter(Character character)
        {
            if (character == null)
            {
                if (nameText != null)
                {
                    nameText.text = "";
                }
                speakingCharacter = null;
            }
            else
            {
                var prevSpeakingCharacter = speakingCharacter;
                speakingCharacter = character;

                // Dim portraits of non-speaking characters
                var activeStages = Stage.ActiveStages;
                for (int i = 0; i < activeStages.Count; i++)
                {
                    var stage = activeStages[i];
                    if (stage.DimPortraits)
                    {
                        var charactersOnStage = stage.CharactersOnStage;
                        for (int j = 0; j < charactersOnStage.Count; j++)
                        {
                            var c = charactersOnStage[j];
                            if (prevSpeakingCharacter != speakingCharacter)
                            {
                                if (c != null && !c.Equals(speakingCharacter))
                                {
                                    stage.SetDimmed(c, true);
                                }
                                else
                                {
                                    stage.SetDimmed(c, false);
                                }
                            }
                        }
                    }
                }

                string characterName = character.NameText;

                if (characterName == "")
                {
                    // Use game object name as default
                    characterName = character.GetObjectName();
                }

				if(speakingCharacter.BackgroundImage != null)
				{
					if(_currentBackground == null || _currentBackground.sprite != speakingCharacter.BackgroundImage)
					{
						if (_currentBackground != null)
						{
							_lastBackground = _currentBackground;
							_lastBackground.GetComponent<Animator>().SetTrigger("hide");
						}
						_currentBackground = Instantiate(backgroundPrefab, background.transform, false);
						_currentBackground.sprite = speakingCharacter.BackgroundImage;
						_currentBackground.GetComponent<Animator>().SetTrigger("show");
					}
					//background.color = Color.white;
					//background.sprite = speakingCharacter.BackgroundImage;
				}
				else
				{
					//background.color = Color.clear;
					//background.sprite = null;
					//background.color = speakingCharacter.NameColor;
				}

                SetCharacterName(characterName, character.NameColor);
			}
        }

		public void ClearBG()
		{
			if (_currentBackground != null)
			{
				_lastBackground = _currentBackground;
				_lastBackground.GetComponent<Animator>().SetTrigger("hide");
			}
		}

        /// <summary>
        /// Sets the character image to display on the Say Dialog.
        /// </summary>
        public virtual void SetCharacterImage(Character.PortraitData portrait)
        {
			//this is handled by the Stage now primarily, but if we ever wanted to do something in the say dialog itself we could here, I guess.
        }

        /// <summary>
        /// Sets the character name to display on the Say Dialog.
        /// Supports variable substitution e.g. John {$surname}
        /// </summary>
        public virtual void SetCharacterName(string name, Color color)
        {
			if(string.IsNullOrEmpty(name))
			{
				nameBox.SetActive(false);
				return;
			}
			nameBox.SetActive(true);
			if (nameText != null)
            {
				var subbedName = LocUtil.TranslateWithDefault(name, name);

                nameText.text = subbedName;
            }
			if(nameColorGraphic != null)
			{
				nameColorGraphic.color = color;
			}
        }

        /// <summary>
        /// Write a line of story text to the Say Dialog. Starts coroutine automatically.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="clearPrevious">Clear any previous text in the Say Dialog.</param>
        /// <param name="waitForInput">Wait for player input before continuing once text is written.</param>
        /// <param name="fadeWhenDone">Fade out the Say Dialog when writing and player input has finished.</param>
        /// <param name="stopVoiceover">Stop any existing voiceover audio before writing starts.</param>
        /// <param name="voiceOverClip">Voice over audio clip to play.</param>
        /// <param name="onComplete">Callback to execute when writing and player input have finished.</param>
        public virtual void Say(string text, bool clearPrevious, bool waitForInput, bool fadeWhenDone, bool stopVoiceover, bool waitForVO, AudioClip voiceOverClip, Action onComplete)
        {
			if (arrow != null && SpeakingCharacter != null)
			{
				if (SpeakingCharacter.State.portraitGraphic != null && SpeakingCharacter.State.portraitGraphic.Mouth != null)
				{
					//Vector2 localPos = TransformUtil.GetLocalPoint(arrow, character.Mouth.position, CanvasManager.CanvasCamera, CanvasManager.CanvasCamera);
					Vector2 localPos = TransformUtil.GetLocalPoint(arrow, SpeakingCharacter.State.position.position, Camera.main, Camera.main);
					arrow.anchoredPosition = new Vector2(localPos.x, arrow.anchoredPosition.y);
				}
			}
			StartCoroutine(DoSay(text, clearPrevious, waitForInput, fadeWhenDone, stopVoiceover, waitForVO, voiceOverClip, onComplete));
        }

        /// <summary>
        /// Write a line of story text to the Say Dialog. Must be started as a coroutine.
        /// </summary>
        /// <param name="text">The text to display.</param>
        /// <param name="clearPrevious">Clear any previous text in the Say Dialog.</param>
        /// <param name="waitForInput">Wait for player input before continuing once text is written.</param>
        /// <param name="fadeWhenDone">Fade out the Say Dialog when writing and player input has finished.</param>
        /// <param name="stopVoiceover">Stop any existing voiceover audio before writing starts.</param>
        /// <param name="voiceOverClip">Voice over audio clip to play.</param>
        /// <param name="onComplete">Callback to execute when writing and player input have finished.</param>
        public virtual IEnumerator DoSay(string text, bool clearPrevious, bool waitForInput, bool fadeWhenDone, bool stopVoiceover, bool waitForVO, AudioClip voiceOverClip, Action onComplete)
        {
            var writer = GetWriter();

            if (writer.IsWriting || writer.IsWaitingForInput)
            {
                writer.Stop();
                while (writer.IsWriting || writer.IsWaitingForInput)
                {
                    yield return null;
                }
            }

            if (closeOtherDialogs)
            {
                for (int i = 0; i < activeSayDialogs.Count; i++)
                {
                    var sd = activeSayDialogs[i];
                    if (sd.gameObject != gameObject)
                    {
                        sd.SetActive(false);
                    }
                }
            }
            gameObject.SetActive(true);

            this.fadeWhenDone = fadeWhenDone;

            // Voice over clip takes precedence over a character sound effect if provided

            AudioClip soundEffectClip = null;
            if (voiceOverClip != null)
            {
                WriterAudio writerAudio = GetWriterAudio();
                writerAudio.OnVoiceover(voiceOverClip);
            }
            else if (speakingCharacter != null)
            {
                soundEffectClip = speakingCharacter.SoundEffect;
            }

            writer.AttachedWriterAudio = writerAudio;

            yield return StartCoroutine(writer.Write(text, clearPrevious, waitForInput, stopVoiceover, waitForVO, soundEffectClip, onComplete));
        }

        /// <summary>
        /// Tell the Say Dialog to fade out once writing and player input have finished.
        /// </summary>
        public virtual bool FadeWhenDone { get {return fadeWhenDone; } set { fadeWhenDone = value; } }

        /// <summary>
        /// Stop the Say Dialog while its writing text.
        /// </summary>
        public virtual void Stop()
        {
            fadeWhenDone = true;
            GetWriter().Stop();
        }

        /// <summary>
        /// Stops writing text and clears the Say Dialog.
        /// </summary>
        public virtual void Clear()
        {
            ClearStoryText();

            // Kill any active write coroutine
            StopAllCoroutines();
        }

        #endregion
    }
}
