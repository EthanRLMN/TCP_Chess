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
    
    
    [SerializeField] private GameObject ChatPanel;

    #endregion
    
    
    #region Unity Functions

    private void OnEnable()
    {
        ChatInputField.onSubmit.AddListener(AddToChatOutput);
    }


    private void OnDisable()
    {
        ChatInputField.onSubmit.RemoveListener(AddToChatOutput);
    }


    private void LateUpdate()
    {
        ToggleChat();
    }
    
    #endregion


    #region Custom Functions
    
    private void AddToChatOutput(string message)
    {
        // Check if message is empty
        if (string.IsNullOrWhiteSpace(message))
            return;
        
        ChatInputField.text = string.Empty;

        DateTime timeNow = DateTime.Now;

        string formattedInput = "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" + timeNow.Second.ToString("d2") + "</color>] " + message;

        if (ChatDisplayOutput != null)
        {
            if (ChatDisplayOutput.text == string.Empty)
                ChatDisplayOutput.text = formattedInput;
            else
                ChatDisplayOutput.text += "\n" + formattedInput;
        }
        
        ChatInputField.ActivateInputField();
        
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