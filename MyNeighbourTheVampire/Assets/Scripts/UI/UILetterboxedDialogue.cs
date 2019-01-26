﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fungus;
using UnityEngine.UI;
using TMPro;

public class UILetterboxedDialogue : UIPanel {

	public System.Action<string> OnConversationComplete;
	public System.Action<string> OnConversationCancelled;
	private string _convoID;
	private bool _isPlaying = false;
	private bool _autoClose = true;
	[SerializeField] private float _defaultStartDelay = 1f;
	[SerializeField] private float _defaultEndDelay = 1f;
	[SerializeField] private SayDialog _dialog;

	/// <summary>
	/// Opens the dialogue window and plays a conversation that matches the conversationKey.
	/// </summary>
	/// <param name="conversationKey">Conversation Key to play. Should be the name of a key from the localization sheet, without a numbered suffix. (i.e. "NPC_Bo/bo_intro")</param>
	/// <param name="startDelay">Time to wait before starting the conversation. -1 for default</param>
	/// <param name="endDelay">Time to wait after finishing the conversation before the complete callback is called. -1 for default</param>
	/// <param name="closeOnFinished">Automatically close the window when the conversation is finished (and after endDelay has expired)</param>
	public void PlayConversation(string conversationKey, float startDelay = -1, float endDelay = -1, bool closeOnFinished = true)
	{
		if(_isPlaying)
		{
			Debug.LogError($"Please wait for the current conversation to finish (or cancel it) before starting a new one.");
			return;
		}
		if(string.IsNullOrEmpty(conversationKey))
		{
			Debug.LogError($"Please specify a conversation key to open Dialogue panel with");
			return;
		}

		_convoID = conversationKey;
		_autoClose = closeOnFinished;
		Open("bring_in");

		List<ConversationManager.ConversationItem> conversation = ConversationManager.GetConversationItems(conversationKey);
		if (conversation.Count == 0)
		{
			Debug.LogError($"Could not find conversation for key '{conversationKey}' in localization sheet");
			Close("close");
			OnConversationComplete?.Invoke(_convoID);
			OnConversationComplete = null;
			OnConversationCancelled = null;
			return;
		}

		CanvasManager.instance.StartCoroutine(playConversation(conversation, startDelay, endDelay));
	}

	private IEnumerator playConversation(List<ConversationManager.ConversationItem> conversationItems, float startDelay, float endDelay)
	{
		if (startDelay < 0) startDelay = _defaultStartDelay;
		if (endDelay < 0) endDelay = _defaultEndDelay;
		_isPlaying = true;
		_dialog.Clear();
		_dialog.SetCharacter(null);
		yield return new WaitForSeconds(startDelay);
		yield return ConversationManager.Instance.DoConversation(conversationItems);
		yield return new WaitForSeconds(endDelay);
		OnConversationComplete?.Invoke(_convoID);
		OnConversationComplete = null;
		OnConversationCancelled = null;
		if(_autoClose)
		{
			Close("close");
		}
		_convoID = "";
		_isPlaying = false;
	}

	public void CancelConversation()
	{
		StopCoroutine("playConversation");
		OnConversationCancelled?.Invoke(_convoID);
		OnConversationComplete = null;
		OnConversationCancelled = null;
		if (_autoClose)
		{
			Close("close");
		}
		_convoID = "";
		_isPlaying = false;
	}
}