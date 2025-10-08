using System;
using System.Net;


public class Message
{
    public MessageBuilder.MessageType Type { get; set; }
    public byte[] Content { get; set; }


    public Message(MessageBuilder.MessageType type, byte[] messageContent)
    {
        Type = type;
        Content = messageContent;
    }
}


public class MessageBuilder
{
    public enum MessageType
    {
        Chat = 1,
        GameState = 2,
        PlayerAction = 3
    }

    
    #region Message Builder
    
    /*
     * Header Builder: Refer to https://learn.microsoft.com/en-us/dotnet/api/system.bitconverter?view=net-9.0
     * for full documentation.
     */
    public static byte[] BuildMessage(MessageType type, byte[] messageContent)
    {
        // Construct the header with 8 bytes: 4 for the type, 4 for the content length
        byte[] header = new byte[8];
        int contentLength = messageContent.Length;
        
        // First, copy the message length information: 4 bytes
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(contentLength)), 0, header, 0, 4);
        
        // Then copy the message type information: 4 remaining bytes
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)type)), 0, header, 4, 4);

        byte[] finalMessage = new byte[header.Length + contentLength];
        Array.Copy(header, 0, finalMessage, 0, header.Length);
        Array.Copy(messageContent, 0, finalMessage, header.Length, contentLength);
        
        return finalMessage;
    }


    /*
     * Converts a string to a properly formatted message with a header
     */
    public static byte[] BuildMessage(string message)
    {
        byte[] msgContent = System.Text.Encoding.UTF8.GetBytes(message);
        
        return BuildMessage(MessageType.Chat, msgContent);
    }


    public static byte[] BuildStateMessage(byte[] gameStateData)
    {
        return BuildMessage(MessageType.GameState, gameStateData);
    }


    public static byte[] BuildActionMessage(byte[] playerActionData)
    {
        return BuildMessage(MessageType.PlayerAction, playerActionData);
    }
    
    #endregion
    
    
    
    #region Message Receiver

    public static Message ParseMessage(byte[] messageData)
    {
        if (messageData.Length < 8)
            throw new ArgumentException("Message received does not match the message format!");
        
        // Read header
        byte[] header = new byte[8];
        Array.Copy(messageData, 0, header, 0, 8);
        
        // Read message type and length
        int contentLength = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 0));
        MessageType type = (MessageType)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header, 4));
        
        // Extract the message content
        byte[] content = new byte[contentLength];
        Array.Copy(messageData, 8, content, 0, content.Length);
        
        return new Message(type, content);
    }
    
    #endregion
}
