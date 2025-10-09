using UnityEngine;


public class PlayerSystem : MonoBehaviour
{
    [SerializeField] private string m_playerName = "Player1";
    [SerializeField] private bool m_isOnline = false;
    [SerializeField] private bool m_isPlaying = true;
    
    private Client m_client;


    private void Awake()
    {
        m_client = FindFirstObjectByType<Client>();
    }
}
