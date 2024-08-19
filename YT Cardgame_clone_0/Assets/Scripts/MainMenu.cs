using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private string gameplaySceneName = "Gameplay";

    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);   // Hiermit wird beim Server und allen Clients die neue Scene "Gameplay" geladen
    }

    public void StartServer()
    {
        NetworkManager.Singleton.StartServer();
        NetworkManager.Singleton.SceneManager.LoadScene(gameplaySceneName, LoadSceneMode.Single);   // Hiermit wird beim Server und allen Clients die neue Scene "Gameplay" geladen
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
    }
}
