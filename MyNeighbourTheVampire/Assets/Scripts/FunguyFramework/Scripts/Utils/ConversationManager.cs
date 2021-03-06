﻿// This code is part of the Fungus library (http://fungusgames.com) maintained by Chris Gregan (http://twitter.com/gofungus).
// It is released for free under the MIT open source license (https://github.com/snozbot/fungus/blob/master/LICENSE)

using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using System.Text;
using I2.Loc;

namespace Fungus
{
	/// <summary>
	/// Helper class to manage parsing and executing the conversation format.
	/// </summary>
	public class ConversationManager : MonoBehaviour
	{
		public static ConversationManager Instance { get; private set; }

		public struct ConversationItem
		{
			public string Text { get; set; }
			public Character Character { get; set; }
			public Character.PortraitData Portrait { get; set; }
			public RectTransform ToPosition { get; set; }
			public RectTransform FromPosition { get; set; }
			public bool Hide { get; set; }
			public FacingDirection FacingDirection { get; set; }
			public bool Flip { get; set; }
			public bool ClearPreviousLine { get; set; }
			public string[] ResponseLinks { get; set; }
			public string[] ResponseTexts { get; set; }
			public string Condition;
			public bool Kill;
			public bool Invite;
			public bool PlayerKill;
			public bool Guest;
		}

		protected Dictionary<string, Character> characters;

		protected bool exitSayWait;

		public Dictionary<string, string> Variables = new Dictionary<string, string>();

		protected virtual void Awake()
		{
			Instance = this;
		}

		/// <summary>
		/// Splits the string passed in by the delimiters passed in.
		/// Quoted sections are not split, and all tokens have whitespace
		/// trimmed from the start and end.
		protected static string[] Split(string stringToSplit)
		{
			var results = new List<string>();

			bool inQuote = false;
			var currentToken = new StringBuilder();
			for (int index = 0; index < stringToSplit.Length; ++index)
			{
				char currentCharacter = stringToSplit[index];
				if (currentCharacter == '"')
				{
					// When we see a ", we need to decide whether we are
					// at the start or send of a quoted section...
					inQuote = !inQuote;
				}
				else if (char.IsWhiteSpace(currentCharacter) && !inQuote)
				{
					// We've come to the end of a token, so we find the token,
					// trim it and add it to the collection of results...
					string result = currentToken.ToString().Trim(new[] { ' ', '\n', '\t', '\"' });
					if (result != "") results.Add(result);

					// We start a new token...
					currentToken = new StringBuilder();
				}
				else
				{
					// We've got a 'normal' character, so we add it to
					// the curent token...
					currentToken.Append(currentCharacter);
				}
			}

			// We've come to the end of the string, so we add the last token...
			string lastResult = currentToken.ToString().Trim();
			if (lastResult != "")
			{
				results.Add(lastResult);
			}

			return results.ToArray();
		}

		protected virtual SayDialog GetSayDialog()
		{
			SayDialog sayDialog = SayDialog.GetSayDialog();
			return sayDialog;
		}

		protected virtual List<ConversationItem> Parse(string conv)
		{
			//find SimpleScript say strings with portrait options
			//You can test regex matches here: http://regexstorm.net/tester
			var sayRegex = new Regex(@"(?<sayParams>[\W\w^\r]*?)`(?<text>[\W\w^\r]*?)`(?<links>[\W\w^\r]*?)\t");
			MatchCollection sayMatches = sayRegex.Matches(conv);

			var items = new List<ConversationItem>(sayMatches.Count);

			Character currentCharacter = null;
			for (int i = 0; i < sayMatches.Count; i++)
			{
				string text = sayMatches[i].Groups["text"].Value.Trim();
				string sayParams = sayMatches[i].Groups["sayParams"].Value;
				string links = sayMatches[i].Groups["links"].Value;


				// As text and SayParams are both optional, an empty string will match the regex.
				// We can ignore any matches where both are empty
				// or if they're Lua style comments
				if ((text.Length == 0 && sayParams.Length == 0) || text.StartsWith("--"))
				{
					continue;
				}

				string[] separateParams = null;

				if (!string.IsNullOrEmpty(sayParams))
				{
					separateParams = Split(sayParams);
				}

				string[] separateLinks = null;
				if(!string.IsNullOrEmpty(links))
				{
					separateLinks = links.Split('|');
				}

				var item = CreateConversationItem(separateParams, text, currentCharacter, separateLinks);

				// Previous speaking character is the default for next conversation item
				currentCharacter = item.Character;

				items.Add(item);
			}

			return items;
		}

		/// <summary>
		/// Using the string of say parameters before the ':',
		/// set the current character, position and portrait if provided.
		/// </summary>
		/// <returns>The conversation item.</returns>
		/// <param name="sayParams">The list of say parameters.</param>
		/// <param name="text">The text for the character to say.</param>
		/// <param name="currentCharacter">The currently speaking character.</param>
		protected virtual ConversationItem CreateConversationItem(string[] sayParams, string text, Character currentCharacter, string[] links)
		{
			var item = new ConversationItem();

			// Populate the story text to be written
			item.Text = text;
			item.ClearPreviousLine = true;

			if(links != null)
			{
				string[] linkSplit;
				List<string> responseList = new List<string>();
				List<string> linkList = new List<string>();
				for(int i = 0; i < links.Length; i++)
				{
					linkSplit = links[i].Split('=');
					if (!string.IsNullOrEmpty(linkSplit[0]) || !string.IsNullOrEmpty(linkSplit[1]))
					{
						linkList.Add(linkSplit[0]);
						responseList.Add(linkSplit[1]);
					}
				}
				if(linkList.Count > 0)
				{
					item.ResponseLinks = linkList.ToArray();
					item.ResponseTexts = responseList.ToArray();
				}
			}

			if (sayParams == null || sayParams.Length == 0)
			{
				// Text only, no params - early out.
				return item;
			}

			// try to find the character param first, since we need to get its portrait
			int characterIndex = -1;

			for (int i = 0; item.Character == null && i < sayParams.Length; i++)
			{
				Character c = GetCharacter(sayParams[i]);
				if(c != null)
				{
					characterIndex = i;
					item.Character = c;
					break;
				}
			}

			// Assume last used character if none is specified now
			if (item.Character == null)
			{
				item.Character = currentCharacter;
			}

			for(int i = 0; i < sayParams.Length; i++)
			{
				if(sayParams[i].Contains("="))
				{
					item.Condition = sayParams[i];
				}
				if (sayParams[i] == "kill") item.Kill = true;
				if (sayParams[i] == "invite") item.Invite = true;
				if (sayParams[i] == "playerkill") item.PlayerKill = true;
				if (sayParams[i] == "guest") item.Guest = true;
			}

			// Check if there's a Hide parameter
			int hideIndex = -1;
			if (item.Character != null)
			{
				for (int i = 0; i < sayParams.Length; i++)
				{
					if (i != characterIndex &&
						string.Compare(sayParams[i], "hide", true) == 0)
					{
						hideIndex = i;
						item.Hide = true;
						break;
					}
				}
			}

			int flipIndex = -1;
			if (item.Character != null)
			{
				for (int i = 0; i < sayParams.Length; i++)
				{
					if (i != characterIndex &&
						i != hideIndex &&
						(string.Compare(sayParams[i], ">>>", true) == 0
						 || string.Compare(sayParams[i], "<<<", true) == 0))
					{
						if (string.Compare(sayParams[i], ">>>", true) == 0) item.FacingDirection = FacingDirection.Right;
						if (string.Compare(sayParams[i], "<<<", true) == 0) item.FacingDirection = FacingDirection.Left;
						flipIndex = i;
						item.Flip = true;
						break;
					}
				}
			}

			// Next see if we can find a portrait for this character
			int portraitIndex = -1;
			if (item.Character != null)
			{
				for (int i = 0; i < sayParams.Length; i++)
				{
					if (item.Portrait == null &&
						item.Character != null &&
						i != characterIndex &&
						i != hideIndex &&
						i != flipIndex)
					{
						Character.PortraitData p = item.Character.GetPortrait(sayParams[i]);
						if (p != null)
						{
							portraitIndex = i;
							item.Portrait = p;
							break;
						}
					}
				}
			}

			// Next check if there's a position parameter
			int pos1Index = -1;
			int pos2Index = -1;
			Stage stage = Stage.GetActiveStage();
			if (stage != null)
			{
				for (int i = 0; i < sayParams.Length; i++)
				{
					if (i != characterIndex &&
						i != portraitIndex &&
						i != flipIndex &&
						i != hideIndex)
					{
						RectTransform r = stage.GetPosition(sayParams[i]);
						if (r != null)
						{
							if (pos1Index == -1)
							{
								pos1Index = i;
								item.ToPosition = r;
							}
							else
							{
								pos2Index = i;
								item.FromPosition = item.ToPosition;
								item.ToPosition = r;
								break;
							}
						}
					}
				}
			}

			//check if we should clear or not
			for (int i = 0; i < sayParams.Length; i++)
			{
				if (i != characterIndex &&
					i != portraitIndex &&
					i != hideIndex &&
					i != flipIndex &&
					i != pos1Index &&
					i != pos2Index)
				{
					if (string.Compare(sayParams[i], "noclear", true) == 0)
					{
						item.ClearPreviousLine = false;
					}
				}
			}

			return item;
		}

		#region Public members

		/// <summary>
		/// Caches the character objects in the scene for fast lookup during conversations.
		/// </summary>
		public virtual void PopulateCharacterCache()
		{
			// cache characters for faster lookup
			characters = new Dictionary<string, Character>();
			Character[] chars = GetComponentsInChildren<Character>(true);
			for(int i = 0; i < chars.Length; i++)
			{
				characters[chars[i].name.ToLowerInvariant()] = chars[i];
			}
		}

		public Character GetCharacter(string characterID)
		{
			if (characters == null) PopulateCharacterCache();
			string charKey = characterID.ToLowerInvariant();
			if (!characters.ContainsKey(charKey)) return null;
			return characters[charKey];
		}

		public List<Character> GetAllCharacters ()
		{
			if (characters == null) PopulateCharacterCache();
			return new List<Character>(characters.Values);
		}

		/// <summary>
		/// Parse and execute a conversation string.
		/// </summary>
		public virtual IEnumerator DoConversation(string conv)
		{
			if (string.IsNullOrEmpty(conv))
			{
				yield break;
			}

			var conversationItems = Parse(conv);

			if (conversationItems.Count == 0)
			{
				yield break;
			}
			yield return DoConversation(conversationItems);
		}
		public virtual IEnumerator DoConversation(List<ConversationItem> conversationItems)
		{
			// Track the current and previous parameter values
			Character currentCharacter = null;
			Character.PortraitData currentPortrait = null;
			RectTransform currentPosition = null;
			RectTransform fromPosition = null;
			Character previousCharacter = null;

			// Play the conversation
			for (int i = 0; i < conversationItems.Count; ++i)
			{
				ConversationItem item = conversationItems[i];

				string alt = CheckConditions(item.Condition);
				if(!string.IsNullOrEmpty(alt))
				{
					CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue).NextConvoID = alt;
					yield break;
				}

				if (item.Guest)
				{
					GameManager.Instance.AddGuest();
				}

				if (item.Character != null)
				{
					currentCharacter = item.Character;
					if(item.Kill)
					{
						GameManager.Instance.SetDead(currentCharacter.gameObject.name, true);
					}
					if (item.Invite)
					{
						GameManager.Instance.SetInvited(currentCharacter.gameObject.name);
					}
				}
				if (item.PlayerKill)
				{
					GameManager.Instance.KillPlayer();
				}

				currentPortrait = item.Portrait;
				currentPosition = item.ToPosition;
				fromPosition = item.FromPosition;

				var sayDialog = GetSayDialog();

				if (sayDialog == null)
				{
					// Should never happen
					yield break;
				}

				sayDialog.SetActive(true);

				if (currentCharacter != null &&
					currentCharacter != previousCharacter)
				{
					sayDialog.SetCharacter(currentCharacter);
				}

				//Handle stage changes
				var stage = Stage.GetActiveStage();

				if (currentCharacter != null &&
					!currentCharacter.State.onScreen &&
					currentPortrait == null)
				{
					// No call to show portrait of hidden character
					// so keep hidden
					item.Hide = true;
				}

				if (stage != null && currentCharacter != null)
				{
					var portraitOptions = new PortraitOptions(true);
					portraitOptions.display = item.Hide ? DisplayType.Hide : DisplayType.Show;
					portraitOptions.character = currentCharacter;
					if (fromPosition == null)
					{
						portraitOptions.fromPosition = currentCharacter.State.position;
					}
					else
					{
						portraitOptions.fromPosition = fromPosition;
					}
					portraitOptions.toPosition = currentPosition;
					portraitOptions.portrait = currentPortrait;

					//Flip option - Flip the opposite direction the character is currently facing
					if (item.Flip) portraitOptions.facing = item.FacingDirection;

					// Do a move tween if the character is already on screen and not yet at the specified position
					if (currentPosition != currentCharacter.State.position)
					{
						portraitOptions.move = true;
					}

					if (item.Hide)
					{
						stage.Hide(portraitOptions);
					}
					else
					{
						stage.Show(portraitOptions);
					}
				}

				if (stage == null &&
					currentPortrait != null)
				{
					sayDialog.SetCharacterImage(currentPortrait);
				}

				previousCharacter = currentCharacter;

				bool hasResponse = (item.ResponseLinks != null && item.ResponseLinks.Length > 0) || (item.ResponseTexts != null && item.ResponseTexts.Length > 0);

				if (!string.IsNullOrEmpty(item.Text)) {
					exitSayWait = false;
					sayDialog.Say(ReplaceVariableTokens(item.Text), item.ClearPreviousLine, !hasResponse, true, true, false, null, () => {
						exitSayWait = true;
					});

					while (!exitSayWait)
					{
						yield return null;
					}
					exitSayWait = false;
				}



				if(hasResponse)
				{
					bool exitChoice = false;
					string choice = "";
					CanvasManager.instance.Get<UIDialogueOptions>(UIPanelID.DialogueOptions).Open(item.ResponseLinks, item.ResponseTexts, (string s) => { exitChoice = true; choice = s; });
					while (!exitChoice)
					{
						yield return null;
					}
					if (!string.IsNullOrEmpty(choice))
					{
						CanvasManager.instance.Get<UILetterboxedDialogue>(UIPanelID.Dialogue).NextConvoID = choice;
						yield break;
					}
				}
			}
		}

		public string ReplaceVariableTokens(string text)
		{
			foreach(KeyValuePair<string, string> kvp in Variables)
			{
				if(text.Contains( $"<${kvp.Key}>"))
				{
					text = text.Replace($"<${kvp.Key}>", kvp.Value);
				}
			}
			return text;
		}

		public string CheckConditions(string conditionString)
		{
			if (string.IsNullOrEmpty(conditionString)) return "";

			string[] conditions = conditionString.Split('|');
			for(int i = 0; i < conditions.Length; i++)
			{
				if(conditions[i].Contains("=="))
				{
					return evalConditionalString(conditions[i]);
				}
				else if(conditions[i].Contains("="))
				{
					//set var
					string[] setVar = conditions[i].Split('=');
					Variables[setVar[0]] = setVar[1];
				}
			}
			return null;
		}

		public string evalConditionalString(string conditionalString)
		{
			string[] conds = conditionalString.Split(new string[] { "==", ">" }, System.StringSplitOptions.None);
			if(Variables.ContainsKey(conds[0]))
			{
				if(Variables[conds[0]] == conds[1])
				{
					return conds[2];
				}
			}
			return null;
		}

		public static List<ConversationItem> GetConversationItems(string characterID, string conversationKey)
		{
			StringBuilder sb = new StringBuilder();

			string key = characterID + "/" + conversationKey;

			var translationList = LocalizationManager.GetTermsList(key);
			if (GameManager.Instance.IsVampire(characterID))
			{
				var altList = LocalizationManager.GetTermsList(key + "/v");
				if (altList != null && altList.Count > 0) translationList = altList;
			}
			
			if(translationList == null)
			{
				Debug.LogError($"Conversation '{key}' is missing!");
				return new List<ConversationItem>();
			}
			for(int i = 0; i < translationList.Count; i++)
			{
				sb.Append($"{LocUtil.TranslateWithDefault("", "params", false, translationList[i])}`{LocUtil.TranslateWithDefault("", "dialogue", false, translationList[i])}`");
				for (int j = 1; j < 4; j++)
				{
					if (j > 1) sb.Append("|");
					sb.Append($"{LocUtil.TranslateWithDefault("", "response_" + j + "_link", false, translationList[i])}={LocUtil.TranslateWithDefault("", "response_" + j, false, translationList[i])}");
				}
				sb.Append("\t");
			}
			if(sb.Length > 0)
			{
				return Instance.Parse(sb.ToString());
			}
			return new List<ConversationItem>();
		}

        #endregion
    }
}
