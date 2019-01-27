// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

﻿using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Reflection;
using System.Text;
using TMPro;

namespace Fungus
{
    /// <summary>
    /// Current state of the writing process.
    /// </summary>
    public enum WriterState
    {
        /// <summary> Invalid state. </summary>
        Invalid,
        /// <summary> Writer has started writing. </summary>
        Start,
        /// <summary> Writing has been paused. </summary>
        Pause,
        /// <summary> Writing has resumed after a pause. </summary>
        Resume,
        /// <summary> Writing has ended. </summary>
        End
    }

    /// <summary>
    /// Writes text using a typewriter effect to a UI text object.
    /// </summary>
    public class Writer : MonoBehaviour, IDialogInputListener
    {
        [Tooltip("Gameobject containing a Text, Inout Field or Text Mesh object to write to")]
        [SerializeField] protected GameObject targetTextObject;

        [Tooltip("Gameobject to punch when the punch tags are displayed. If none is set, the main camera will shake instead.")]
        [SerializeField] protected GameObject punchObject;

        [Tooltip("Writing characters per second")]
        [SerializeField] protected float writingSpeed = 60;

        [Tooltip("Pause duration for punctuation characters")]
        [SerializeField] protected float punctuationPause = 0.25f;

        [Tooltip("Color of text that has not been revealed yet")]
        [SerializeField] protected Color hiddenTextColor = new Color(1,1,1,0);

        [Tooltip("Write one word at a time rather one character at a time")]
        [SerializeField] protected bool writeWholeWords = false;

        [Tooltip("Force the target text object to use Rich Text mode so text color and alpha appears correctly")]
        [SerializeField] protected bool forceRichText = true;

        [Tooltip("Click while text is writing to finish writing immediately")]
        [SerializeField] protected bool instantComplete = true;

        // This property is true when the writer is waiting for user input to continue
        protected bool isWaitingForInput;

        // This property is true when the writer is writing text or waiting (i.e. still processing tokens)
        protected bool isWriting;

        protected float currentWritingSpeed;
        protected float currentPunctuationPause;
        protected TMP_Text textUI;
        protected TMP_InputField inputField;
        protected Component textComponent;
        protected PropertyInfo textProperty;

        protected bool boldActive = false;
        protected bool italicActive = false;
        protected bool colorActive = false;
        protected string colorText = "";
        protected bool sizeActive = false;
        protected string sizeValue = "48";
		protected bool styleActive = false;
		protected string currentStyle = "";

		protected bool inputFlag;
        protected bool exitFlag;

        protected List<IWriterListener> writerListeners = new List<IWriterListener>();

        protected StringBuilder openString = new StringBuilder(256);
        protected StringBuilder closeString = new StringBuilder(256);
        protected StringBuilder leftString = new StringBuilder(1024);
        protected StringBuilder rightString = new StringBuilder(1024);
        protected StringBuilder outputString = new StringBuilder(1024);
        protected StringBuilder readAheadString = new StringBuilder(1024);

        protected string hiddenColorOpen = "";
        protected string hiddenColorClose = "";

        protected int visibleCharacterCount = 0;
        public WriterAudio AttachedWriterAudio { get; set; }

        protected virtual void Awake()
        {
            GameObject go = targetTextObject;
            if (go == null)
            {
                go = gameObject;
            }

            textUI = go.GetComponent<TMP_Text>();
            inputField = go.GetComponent<TMP_InputField>();

            // Try to find any component with a text property
            if (textUI == null && inputField == null)
            {
                var allcomponents = go.GetComponents<Component>();
                for (int i = 0; i < allcomponents.Length; i++)
                {
                    var c = allcomponents[i];
                    textProperty = c.GetType().GetProperty("text");
                    if (textProperty != null)
                    {
                        textComponent = c;
                        break;
                    }
                }
            }

            // Cache the list of child writer listeners
            var allComponents = GetComponentsInChildren<Component>();
            for (int i = 0; i < allComponents.Length; i++)
            {
                var component = allComponents[i];
                IWriterListener writerListener = component as IWriterListener;
                if (writerListener != null)
                {
                    writerListeners.Add(writerListener);
                }
            }

            CacheHiddenColorStrings();
        }

        protected virtual void CacheHiddenColorStrings()
        {
            // Cache the hidden color string
            Color32 c = hiddenTextColor;
            hiddenColorOpen = String.Format("<color=#{0:X2}{1:X2}{2:X2}{3:X2}>", c.r, c.g, c.b, c.a);
            hiddenColorClose = "</color>";
        }

        protected virtual void Start()
        {

        }

        protected virtual void UpdateOpenMarkup()
        {
            openString.Length = 0;

            if (SupportsRichText())
            {
                if (sizeActive)
                {
                    openString.Append("<size=");
                    openString.Append(sizeValue);
                    openString.Append(">");
                }
                if (colorActive)
                {
                    openString.Append("<color=");
                    openString.Append(colorText);
                    openString.Append(">");
                }
                if (boldActive)
                {
                    openString.Append("<b>");
                }
                if (italicActive)
                {
                    openString.Append("<i>");
                }
				if(styleActive)
				{
					openString.Append("<style=");
					openString.Append(currentStyle);
					openString.Append(">");
				}
            }
        }

        protected virtual void UpdateCloseMarkup()
        {
            closeString.Length = 0;

            if (SupportsRichText())
            {
                if (italicActive)
                {
                    closeString.Append("</i>");
                }
                if (boldActive)
                {
                    closeString.Append("</b>");
                }
                if (colorActive)
                {
                    closeString.Append("</color>");
                }
                if (sizeActive)
                {
                    closeString.Append("</size>");
                }
				if (styleActive)
				{
					closeString.Append("</style>");
				}
            }
        }

        protected virtual bool CheckParamCount(List<string> paramList, int count)
        {
            if (paramList == null)
            {
                Debug.LogError("paramList is null");
                return false;
            }
            if (paramList.Count != count)
            {
                Debug.LogError("There must be exactly " + paramList.Count + " parameters.");
                return false;
            }
            return true;
        }

        protected virtual bool TryGetSingleParam(List<string> paramList, int index, float defaultValue, out float value)
        {
            value = defaultValue;
            if (paramList.Count > index)
            {
                Single.TryParse(paramList[index], out value);
                return true;
            }
            return false;
        }

        protected virtual IEnumerator ProcessTokens(List<TextTagToken> tokens, bool stopAudio, Action onComplete)
        {
            // Reset control members
            boldActive = false;
            italicActive = false;
            colorActive = false;
            sizeActive = false;
			styleActive = false;
            colorText = "";
            sizeValue = "48";
            currentPunctuationPause = punctuationPause;
            currentWritingSpeed = writingSpeed;

            exitFlag = false;
            isWriting = true;

            TokenType previousTokenType = TokenType.Invalid;

            for (int i = 0; i < tokens.Count; ++i)
            {
                // Pause between tokens if Paused is set
                while (Paused)
                {
                    yield return null;
                }

                var token = tokens[i];

                // Update the read ahead string buffer. This contains the text for any
                // Word tags which are further ahead in the list.
                readAheadString.Length = 0;
                for (int j = i + 1; j < tokens.Count; ++j)
                {
                    var readAheadToken = tokens[j];

                    if ((readAheadToken.type == TokenType.Words || readAheadToken.type == TokenType.Other) &&
                        readAheadToken.paramList.Count == 1)
                    {
                        readAheadString.Append(readAheadToken.paramList[0]);
                    }
                    else if (readAheadToken.type == TokenType.WaitForInputAndClear)
                    {
                        break;
                    }
                }

                switch (token.type)
                {
					case TokenType.Words:
						yield return StartCoroutine(DoWords(token.paramList, previousTokenType));
						break;
					case TokenType.Other:
						bool oldIC = instantComplete;
						bool oldIF = inputFlag;
						instantComplete = true;
						inputFlag = true;
						yield return StartCoroutine(DoWords(token.paramList, previousTokenType));
						instantComplete = oldIC;
						inputFlag = oldIF;
						break;
					case TokenType.NewLine:
						break;
					case TokenType.BoldStart:
						boldActive = true;
						break;
					case TokenType.BoldEnd:
						boldActive = false;
						break;
					case TokenType.StyleStart:
						if (CheckParamCount(token.paramList, 1))
						{
							styleActive = true;
							currentStyle = token.paramList[0];
						}
						break;
					case TokenType.StyleEnd:
						styleActive = false;
						break;
					case TokenType.ItalicStart:
						italicActive = true;
						break;

					case TokenType.ItalicEnd:
						italicActive = false;
						break;

					case TokenType.ColorStart:
						if (CheckParamCount(token.paramList, 1))
						{
							colorActive = true;
							colorText = token.paramList[0];
						}
						break;

					case TokenType.ColorEnd:
						colorActive = false;
						break;

					case TokenType.SizeStart:
						if (CheckParamCount(token.paramList,1))
						{
							sizeValue = token.paramList[0];
							sizeActive = true;
						}
						break;

					case TokenType.SizeEnd:
						sizeActive = false;
						break;

					case TokenType.Wait:
						yield return StartCoroutine(DoWait(token.paramList));
						break;

					case TokenType.WaitForInputNoClear:
						yield return StartCoroutine(DoWaitForInput(false));
						break;

					case TokenType.WaitForInputAndClear:
						yield return StartCoroutine(DoWaitForInput(true));
						break;

					case TokenType.WaitForVoiceOver:
						yield return StartCoroutine(DoWaitVO());
						break;

						case TokenType.WaitOnPunctuationStart:
						TryGetSingleParam(token.paramList, 0, punctuationPause, out currentPunctuationPause);
						break;

					case TokenType.WaitOnPunctuationEnd:
						currentPunctuationPause = punctuationPause;
						break;

					case TokenType.Clear:
						Text = "";
						break;

					case TokenType.SpeedStart:
						TryGetSingleParam(token.paramList, 0, writingSpeed, out currentWritingSpeed);
						break;

					case TokenType.SpeedEnd:
						currentWritingSpeed = writingSpeed;
						break;

					case TokenType.Exit:
						exitFlag = true;
						break;

					case TokenType.Message:
						if (CheckParamCount(token.paramList, 1))
						{
							//TODO: broadcast a message
						}
						break;

					case TokenType.VerticalPunch:
						{
							float vintensity;
							float time;
							TryGetSingleParam(token.paramList, 0, 10.0f, out vintensity);
							TryGetSingleParam(token.paramList, 1, 0.5f, out time);
							Punch(new Vector3(0, 1, 0), vintensity, time);
						}
						break;

					case TokenType.HorizontalPunch:
						{
							float hintensity;
							float time;
							TryGetSingleParam(token.paramList, 0, 10.0f, out hintensity);
							TryGetSingleParam(token.paramList, 1, 0.5f, out time);
							Punch(new Vector3(1, 0, 0), hintensity, time);
						}
						break;

					case TokenType.Punch:
						{
							float intensity;
							float time;
							TryGetSingleParam(token.paramList, 0, 10.0f, out intensity);
							TryGetSingleParam(token.paramList, 1, 0.5f, out time);
							Punch(new Vector3(1, 1, 0), intensity, time);
						}
						break;

					case TokenType.Flash:
						float flashDuration;
						TryGetSingleParam(token.paramList, 0, 0.2f, out flashDuration);
						Flash(flashDuration);
						break;

					case TokenType.Audio:
						{
							AudioSource audioSource = null;
							if (CheckParamCount(token.paramList, 1))
							{
								audioSource = FindAudio(token.paramList[0]);
							}
							if (audioSource != null)
							{
								audioSource.PlayOneShot(audioSource.clip);
							}
						}
						break;

					case TokenType.AudioLoop:
						{
							AudioSource audioSource = null;
							if (CheckParamCount(token.paramList, 1))
							{
								audioSource = FindAudio(token.paramList[0]);
							}
							if (audioSource != null)
							{
								audioSource.Play();
								audioSource.loop = true;
							}
						}
						break;

					case TokenType.AudioPause:
						{
							AudioSource audioSource = null;
							if (CheckParamCount(token.paramList, 1))
							{
								audioSource = FindAudio(token.paramList[0]);
							}
							if (audioSource != null)
							{
								audioSource.Pause();
							}
						}
						break;

					case TokenType.AudioStop:
						{
							AudioSource audioSource = null;
							if (CheckParamCount(token.paramList, 1))
							{
								audioSource = FindAudio(token.paramList[0]);
							}
							if (audioSource != null)
							{
								audioSource.Stop();
							}
						}
						break;
                }


                previousTokenType = token.type;

                if (exitFlag)
                {
                    break;
                }
            }

            inputFlag = false;
            exitFlag = false;
            isWaitingForInput = false;
            isWriting = false;

            NotifyEnd(stopAudio);

            if (onComplete != null)
            {
                onComplete();
            }
        }

        protected virtual IEnumerator DoWords(List<string> paramList, TokenType previousTokenType)
        {
            if (!CheckParamCount(paramList, 1))
            {
                yield break;
            }

            string param = paramList[0].Replace("\\n", "\n");

            // Trim whitespace after a {wc} or {c} tag
            if (previousTokenType == TokenType.WaitForInputAndClear ||
                previousTokenType == TokenType.Clear)
            {
                param = param.TrimStart(' ', '\t', '\r', '\n');
            }

            // Start with the visible portion of any existing displayed text.
            string startText = "";
            if (visibleCharacterCount > 0 &&
                visibleCharacterCount <= Text.Length)
            {
                startText = Text.Substring(0, visibleCharacterCount);
            }

            UpdateOpenMarkup();
            UpdateCloseMarkup();

            float timeAccumulator = Time.deltaTime;

            for (int i = 0; i < param.Length + 1; ++i)
            {
                // Exit immediately if the exit flag has been set
                if (exitFlag)
                {
                    break;
                }

                // Pause mid sentence if Paused is set
                while (Paused)
                {
                    yield return null;
                }

                PartitionString(writeWholeWords, param, i);
                ConcatenateString(startText);
                Text = outputString.ToString();

                NotifyGlyph();

                // No delay if user has clicked and Instant Complete is enabled
                if (instantComplete && inputFlag)
                {
                    continue;
                }

                // Punctuation pause
                if (leftString.Length > 0 &&
                    rightString.Length > 0 &&
                    IsPunctuation(leftString.ToString(leftString.Length - 1, 1)[0]))
                {
                    yield return StartCoroutine(DoWait(currentPunctuationPause));
                }

                // Delay between characters
                if (currentWritingSpeed > 0f)
                {
                    if (timeAccumulator > 0f)
                    {
                        timeAccumulator -= 1f / currentWritingSpeed;
                    }
                    else
                    {
                        yield return new WaitForSeconds(1f / currentWritingSpeed);
                    }
                }
            }
        }


        protected virtual void PartitionString(bool wholeWords, string inputString, int i)
        {
            leftString.Length = 0;
            rightString.Length = 0;

            // Reached last character
            leftString.Append(inputString);
            if (i >= inputString.Length)
            {
                return;
            }

            rightString.Append(inputString);

            if (wholeWords)
            {
                // Look ahead to find next whitespace or end of string
                for (int j = i; j < inputString.Length + 1; ++j)
                {
                    if (j == inputString.Length || Char.IsWhiteSpace(inputString[j]))
                    {
                        leftString.Length = j;
                        rightString.Remove(0, j);
                        break;
                    }
                }
            }
            else
            {
                leftString.Remove(i, inputString.Length - i);
                rightString.Remove(0, i);
            }
        }

        protected virtual void ConcatenateString(string startText)
        {
            outputString.Length = 0;

            // string tempText = startText + openText + leftText + closeText;
            outputString.Append(startText);
            outputString.Append(openString);
            outputString.Append(leftString);

            outputString.Append(closeString);

            // Track how many visible characters are currently displayed so
            // we can easily extract the visible portion again later.
            visibleCharacterCount = outputString.Length;

            // Make right hand side text hidden
            if (SupportsRichText() &&
                rightString.Length + readAheadString.Length > 0)
            {
                // Ensure the hidden color strings are populated
                if (hiddenColorOpen.Length == 0)
                {
                    CacheHiddenColorStrings();
                }

                outputString.Append(hiddenColorOpen);
                outputString.Append(rightString);
                outputString.Append(readAheadString);
                outputString.Append(hiddenColorClose);
            }
        }

        protected virtual IEnumerator DoWait(List<string> paramList)
        {
            var param = "";
            if (paramList.Count == 1)
            {
                param = paramList[0];
            }

            float duration = 1f;
            if (!Single.TryParse(param, out duration))
            {
                duration = 1f;
            }

            yield return StartCoroutine( DoWait(duration) );
        }

        protected virtual IEnumerator DoWaitVO()
        {
            float duration = 0f;

            if (AttachedWriterAudio != null)
            {
                duration = AttachedWriterAudio.GetSecondsRemaining();
            }

            yield return StartCoroutine(DoWait(duration));
        }

        protected virtual IEnumerator DoWait(float duration)
        {
            NotifyPause();

            float timeRemaining = duration;
            while (timeRemaining > 0f && !exitFlag)
            {
                if (instantComplete && inputFlag)
                {
                    break;
                }

                timeRemaining -= Time.deltaTime;
                yield return null;
            }

            NotifyResume();
        }

        protected virtual IEnumerator DoWaitForInput(bool clear)
        {
            NotifyPause();

            inputFlag = false;
            isWaitingForInput = true;

            while (!inputFlag && !exitFlag)
            {
                yield return null;
            }

            isWaitingForInput = false;
            inputFlag = false;

            if (clear)
            {
                textUI.text = "";
            }

            NotifyResume();
        }

        protected virtual bool IsPunctuation(char character)
        {
            return character == '.' ||
                character == '?' ||
                    character == '!' ||
                    character == ',' ||
                    character == ':' ||
                    character == ';' ||
                    character == ')';
        }

        protected virtual void Punch(Vector3 axis, float intensity, float time)
        {
            GameObject go = punchObject;
            if (go == null)
            {
                go = Camera.main.gameObject;
            }

            if (go != null)
            {
				float shakePeriodTime = 0.42f; // The period of each shake
				LTDescr shakeTween = LeanTween.moveLocal(gameObject, axis * intensity, shakePeriodTime)
				.setEase(LeanTweenType.easeShake) // this is a special ease that is good for shaking
				.setLoopClamp()
				.setRepeat(-1);

				// Slow the camera shake down to zero
				LeanTween.value(punchObject, intensity, 0f, time).setOnUpdate(
					(float val) => {
						shakeTween.setTo(axis * val);
					}
).setEase(LeanTweenType.easeInQuart);
			}
        }

        protected virtual void Flash(float duration)
        {
            //var cameraManager = FungusManager.Instance.CameraManager;

            //cameraManager.ScreenFadeTexture = CameraManager.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
            //cameraManager.Fade(1f, duration, delegate {
            //    cameraManager.ScreenFadeTexture = CameraManager.CreateColorTexture(new Color(1f,1f,1f,1f), 32, 32);
            //    cameraManager.Fade(0f, duration, null);
            //});
        }

        protected virtual AudioSource FindAudio(string audioObjectName)
        {
            GameObject go = GameObject.Find(audioObjectName);
            if (go == null)
            {
                return null;
            }

            return go.GetComponent<AudioSource>();
        }

        protected virtual void NotifyInput()
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnInput();
            }
        }

        protected virtual void NotifyStart(AudioClip audioClip)
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnStart(audioClip);
            }
        }

        protected virtual void NotifyPause()
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnPause();
            }
        }

        protected virtual void NotifyResume()
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnResume();
            }
        }

        protected virtual void NotifyEnd(bool stopAudio)
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnEnd(stopAudio);
            }
        }

        protected virtual void NotifyGlyph()
        {
            for (int i = 0; i < writerListeners.Count; i++)
            {
                var writerListener = writerListeners[i];
                writerListener.OnGlyph(leftString.ToString());
            }
        }

        #region Public members

        /// <summary>
        /// Gets or sets the text property of the attached text object.
        /// </summary>
        public virtual string Text
        {
            get
            {
                if (textUI != null)
                {
                    return textUI.text;
                }
                else if (inputField != null)
                {
                    return inputField.text;
                }
                else if (textProperty != null)
                {
                    return textProperty.GetValue(textComponent, null) as string;
                }

                return "";
            }

            set
            {
                if (textUI != null)
                {
                    textUI.text = value;
                }
                else if (inputField != null)
                {
                    inputField.text = value;
                }
                else if (textProperty != null)
                {
                    textProperty.SetValue(textComponent, value, null);
                }
            }
        }

        /// <summary>
        /// This property is true when the writer is writing text or waiting (i.e. still processing tokens).
        /// </summary>
        public virtual bool IsWriting { get { return isWriting; } }

        /// <summary>
        /// This property is true when the writer is waiting for user input to continue.
        /// </summary>
        public virtual bool IsWaitingForInput { get { return isWaitingForInput; } }

        /// <summary>
        /// Pauses the writer.
        /// </summary>
        public virtual bool Paused { set; get; }

        /// <summary>
        /// Stop writing text.
        /// </summary>
        public virtual void Stop()
        {
            if (isWriting || isWaitingForInput)
            {
                exitFlag = true;
            }
        }

        /// <summary>
        /// Writes text using a typewriter effect to a UI text object.
        /// </summary>
        /// <param name="content">Text to be written</param>
        /// <param name="clear">If true clears the previous text.</param>
        /// <param name="waitForInput">Writes the text and then waits for player input before calling onComplete.</param>
        /// <param name="stopAudio">Stops any currently playing audioclip.</param>
        /// <param name="waitForVO">Wait for the Voice over to complete before proceeding</param>
        /// <param name="audioClip">Audio clip to play when text starts writing.</param>
        /// <param name="onComplete">Callback to call when writing is finished.</param>
        public virtual IEnumerator Write(string content, bool clear, bool waitForInput, bool stopAudio, bool waitForVO, AudioClip audioClip, Action onComplete)
        {
            if (clear)
            {
                this.Text = "";
                visibleCharacterCount = 0;
            }

            if (!HasTextObject())
            {
                yield break;
            }

            // If this clip is null then WriterAudio will play the default sound effect (if any)
            NotifyStart(audioClip);

            string tokenText = content;
            if (waitForInput)
            {
                tokenText += "<wi>";
            }

            if(waitForVO)
            {
                tokenText += "<wvo>";
            }


            List<TextTagToken> tokens = TextTagParser.Tokenize(tokenText);

            gameObject.SetActive(true);

            yield return StartCoroutine(ProcessTokens(tokens, stopAudio, onComplete));
        }

        /// <summary>
        /// Sets the color property of the text UI object.
        /// </summary>
        public virtual void SetTextColor(Color textColor)
        {
            if (textUI != null)
            {
                textUI.color = textColor;
            }
            else if (inputField != null)
            {
                if (inputField.textComponent != null)
                {
                    inputField.textComponent.color = textColor;
                }
            }
        }

        /// <summary>
        /// Sets the alpha component of the color property of the text UI object.
        /// </summary>
        public virtual void SetTextAlpha(float textAlpha)
        {
            if (textUI != null)
            {
                Color tempColor = textUI.color;
                tempColor.a = textAlpha;
                textUI.color = tempColor;
            }
            else if (inputField != null)
            {
                if (inputField.textComponent != null)
                {
                    Color tempColor = inputField.textComponent.color;
                    tempColor.a = textAlpha;
                    inputField.textComponent.color = tempColor;
                }
            }
        }

        /// <summary>
        /// Returns true if there is a supported text object attached to this writer.
        /// </summary>
        public virtual bool HasTextObject()
        {
            return (textUI != null || inputField != null || textComponent != null);
        }

        /// <summary>
        /// Returns true if the text object has rich text support.
        /// </summary>
        public virtual bool SupportsRichText()
        {
            if (textUI != null)
            {
				return true;
            }
            if (inputField != null)
            {
                return true;
            }
            return false;
        }

        #endregion

        #region IDialogInputListener implementation

        public virtual void OnNextLineEvent()
        {
            inputFlag = true;

            if (isWriting)
            {
                NotifyInput();
            }
        }

        #endregion
    }
}
