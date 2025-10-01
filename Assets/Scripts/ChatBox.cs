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
    
    #endregion


    #region Custom Functions
    
    private void AddToChatOutput(string newText)
    {
        ChatInputField.text = string.Empty;

        DateTime timeNow = DateTime.Now;

        string formattedInput = "[<#FFFF80>" + timeNow.Hour.ToString("d2") + ":" + timeNow.Minute.ToString("d2") + ":" + timeNow.Second.ToString("d2") + "</color>] " + newText;

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

    #endregion
}