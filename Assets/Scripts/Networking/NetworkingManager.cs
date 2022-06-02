using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

using ExitGames.Client.Photon;
using Photon.Realtime;
using Photon.Pun;

public class NetworkingManager : MonoBehaviourPunCallbacks, IOnEventCallback
{
    static public NetworkingManager Instance;

    [Tooltip("The prefab for representing the photon player")]
    [SerializeField]
    private GameObject networkedPlayerPrefab = null;

    [Header("Local turtle")]
    [Tooltip("The local turtle transform to follow with the local photon player")]
    [SerializeField]
    private Transform localTurtle = null;

    [Header("Fake turtles")]
    [Tooltip("Prefab for fake turtles to match remote photon players")]
    [SerializeField]
    private Transform fakeTurtlePrefab = null;
    [Tooltip("The list of fake turtles to update")]
    public static List<Transform> fakeTurtles = new List<Transform>();
    [SerializeField]
    private Transform contentParent = null;


    [SerializeField]
    private string lobbyName = "Lobby";

    [Header("Brush styles")]
    [SerializeField]
    private BrushStyles myBrushStyles = null;
    public const byte brushStylesChangedEventCode = 1;

    private const string settingMenuPath = "AirLine/Lobby On Start";
    private static bool lobbyOnStart;

    [MenuItem(settingMenuPath, priority = 1)]
    private static void Setting()
    {
        lobbyOnStart = !lobbyOnStart;
    }

    [MenuItem(settingMenuPath, true)]
    private static bool SettingValidate()
    {
        Menu.SetChecked(settingMenuPath, lobbyOnStart);
        return true;
    }

    private void Start()
    {
        if (!lobbyOnStart) return;

        Instance = this;

        // in case we started with the wrong scene active, load the lobby scene
        if (!PhotonNetwork.IsConnected)
        {
            SceneManager.LoadScene(lobbyName);
            return;
        }

        if (PhotonNetwork.IsMasterClient)
        {
            Debug.LogFormat("IsMasterClient {0}", PhotonNetwork.IsMasterClient);
        }

        if (networkedPlayerPrefab == null)
        {
            Debug.LogError("<Color=Red><b>Missing</b></Color> playerPrefab Reference. Please set it up in GameObject 'Networking Manager'", this);
        }
        else
        {
            if (PhotonPlayerManager.LocalPlayerInstance == null)
            {
                Debug.LogFormat("Instantiating LocalPlayer in {0}", SceneManagerHelper.ActiveSceneName);

                // Spawn the prefab for the local player. it gets synced by using PhotonNetwork.Instantiate.
                GameObject networkedPlayer = PhotonNetwork.Instantiate(this.networkedPlayerPrefab.name, new Vector3(0f, 0f, 0f), Quaternion.identity, 0);
                networkedPlayer.GetComponent<PhotonPlayerManager>().localTurtleTransform = localTurtle;
            }
            else
            {
                Debug.LogFormat("Ignoring scene load for {0}", SceneManagerHelper.ActiveSceneName);
            }
        }

    }

    public override void OnEnable()
    {
        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        base.OnDisable();
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public void LeaveRoom()
    {
        PhotonNetwork.LeaveRoom();
    }

    public void OnBrushStylesChanged(string state)
    {
        // BrushStyles handles it's own serialization
        string brushStylesMessage = myBrushStyles.SerializeBrushStyles();

        RaiseEventOptions raiseEventOptions = new RaiseEventOptions { Receivers = ReceiverGroup.Others };
        PhotonNetwork.RaiseEvent(brushStylesChangedEventCode, brushStylesMessage, raiseEventOptions, SendOptions.SendReliable);
    }

    public void OnEvent(EventData photonEvent)
    {

        byte eventCode = photonEvent.Code;
        if (eventCode == brushStylesChangedEventCode)
        {
            string message = (string)photonEvent.CustomData;
            myBrushStyles.DeSerializeBrushStyles(message);
            Debug.Log("Incoming brush styles: " + message);
        }
    }

    private void Update()
    {
        // Synchronize fake turtles transforms with photon remote players transforms
        for (int i = 0; i < PhotonPlayerManager.remoteNetworkedPlayers.Count; i++)
        {
            PhotonPlayerManager playerManager = PhotonPlayerManager.remoteNetworkedPlayers[i];
            if (fakeTurtles[i].name != playerManager.UserId) Debug.LogError("Fake turtle name does not match networked player UserId!");
            fakeTurtles[i].localPosition = playerManager.transform.localPosition;
            fakeTurtles[i].localRotation = playerManager.transform.localRotation;
        }
    }

    private void AddFakeTurtle(string id)
    {
        Transform fakeTurtle = Instantiate(fakeTurtlePrefab, new Vector3(0, 0, 0), Quaternion.identity);
        fakeTurtle.parent = contentParent;
        Transform movingTurtle = fakeTurtle.GetComponent<ExposeChildTransform>().childTransform;
        movingTurtle.name = id;
        fakeTurtles.Add(movingTurtle);
    }

    private void RemoveFakeTurtle(string id)
    {
        for (int i = 0; i < PhotonPlayerManager.remoteNetworkedPlayers.Count; i++)
        {
            PhotonPlayerManager playerManager = PhotonPlayerManager.remoteNetworkedPlayers[i];
            if (playerManager.UserId == id)
            {
                PhotonPlayerManager.remoteNetworkedPlayers.RemoveAt(i);
                if (fakeTurtles[i].name != id) Debug.LogError("Fake turtle name does not match networked player UserId!");
                fakeTurtles.RemoveAt(i);
            }
        }
    }


    #region Photon Callbacks

    public override void OnPlayerEnteredRoom(Player other)
    {
        Debug.Log("OnPlayerEnteredRoom() " + other.NickName); // not seen if you're the player connecting
        AddFakeTurtle(other.UserId);
    }

    public override void OnPlayerLeftRoom(Player other)
    {
        Debug.Log("OnPlayerLeftRoom() " + other.NickName); // seen when other disconnects
        RemoveFakeTurtle(other.UserId);
    }

    public override void OnLeftRoom()
    {
        SceneManager.LoadScene(lobbyName);
    }

    #endregion
}