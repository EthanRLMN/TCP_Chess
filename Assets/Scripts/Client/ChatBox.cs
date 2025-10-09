using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class ChatBox : MonoBehaviour
{
    #region Variables
    
    public TMP_InputField ChatInputField;
    public TMP_Text ChatDisplayOutput;
    public Scrollbar ChatScrollbar;
    
    public bool IsVisible = false;
    
    private Client m_client;
    
    
    [SerializeField] private GameObject ChatPanel;

    #endregion
    
    
    #region Unity Functions

    private void Awake()
    {
        m_client = FindFirstObjectByType<Client>();
        ChatInputField.onSubmit.AddListener(OnSubmitMessage);
    }


    private void OnDestroy()
    {
        ChatInputField.onSubmit.RemoveListener(OnSubmitMessage);
    }


    private void LateUpdate()
    {
        ToggleChat();
    }
    
    #endregion


    #region Custom Functions
    
    private void OnSubmitMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        Client client = FindFirstObjectByType<Client>();
        if (client == null)
            return;
        
        client.SendChatMessage(message);
        AddToChatOutput($"{client.Nickname}|{message}");
    }
    
    
    public void AddToChatOutput(string rawMessage)
    {
        if (string.IsNullOrWhiteSpace(rawMessage)) return;

        string pseudo = "Unknown";
        string message = rawMessage;

        if (rawMessage.Contains("|"))
        {
            string[] parts = rawMessage.Split(new char[] { '|' }, 2);
            pseudo = parts[0];
            message = parts[1];
        }

        string formattedMessage = $"<color=#00FF00>{pseudo}</color> : {message}";

        if (ChatDisplayOutput.text == string.Empty)
            ChatDisplayOutput.text = formattedMessage;
        else
            ChatDisplayOutput.text += "\n" + formattedMessage;

        ChatInputField.text = string.Empty;
        ChatInputField.ActivateInputField();

        if (ChatScrollbar != null)
            ChatScrollbar.value = 0;
    }


    public void ToggleChat()
    {
        if (!Input.GetKeyDown(KeyCode.T))
            return;

        if (ChatInputField.isFocused)
            return;
        
        IsVisible = !IsVisible; 
        ChatPanel.SetActive(IsVisible);
                
        if (IsVisible)
            ChatInputField.ActivateInputField();
    }

    #endregion
}