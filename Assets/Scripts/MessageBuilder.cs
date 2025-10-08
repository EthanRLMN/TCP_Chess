using System;
using System.Net;

public class MessageBuilder
{
    public enum MessageType
    {
        Chat = 1,
        GameState = 2,
        PlayerAction = 3
    }
    
    
    #region Variables
    #endregion

    
    #region Public Functions
    
    /*
     * Header Builder : Refer to https://learn.microsoft.com/en-us/dotnet/api/system.bitconverter?view=net-9.0
     * for full documentation.
     */
    public static byte[] BuildMessage(MessageType type, byte[] messageContent)
    {
        // Construct the header with 8 bytes : 4 for the type, 4 for the content length
        byte[] header = new byte[8];
        int contentLength = messageContent.Length;
        
        // First copy the message length information : 4 bytes
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder(contentLength)), 0, header, 0, 4);
        
        // Then copy the message type information : 4 remaining bytes
        Array.Copy(BitConverter.GetBytes(IPAddress.HostToNetworkOrder((int)type)), 0, header, 4, 4);

        byte[] finalMessage = new byte[contentLength + header.Length];
        
        
        return new byte[10];
    }
    
    #endregion
}
