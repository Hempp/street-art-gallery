using UnityEngine;
using UnityEngine.Events;
using System;
using System.Collections.Generic;

namespace GalleryVR.Multiplayer
{
    /// <summary>
    /// Core networking manager for the social VR gallery.
    /// Supports multiple backends: Photon, Normcore, Mirror, or custom WebSocket.
    /// </summary>
    public class NetworkManager : MonoBehaviour
    {
        public static NetworkManager Instance { get; private set; }

        [Header("Network Settings")]
        [SerializeField] private string gameVersion = "1.0";
        [SerializeField] private string roomName = "StreetArtGallery";
        [SerializeField] private int maxPlayersPerRoom = 50;
        [SerializeField] private bool autoConnect = true;

        [Header("Spawn Settings")]
        [SerializeField] private GameObject localPlayerPrefab;
        [SerializeField] private GameObject remotePlayerPrefab;
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private float spawnRadius = 2f;

        [Header("Player Data")]
        [SerializeField] private string defaultUsername = "Guest";
        [SerializeField] private int defaultAvatarId = 0;

        // Events
        public UnityEvent OnConnectedToServer;
        public UnityEvent OnJoinedRoom;
        public UnityEvent OnLeftRoom;
        public UnityEvent<NetworkPlayer> OnPlayerJoined;
        public UnityEvent<NetworkPlayer> OnPlayerLeft;
        public UnityEvent<string> OnConnectionError;

        // State
        public bool IsConnected { get; private set; }
        public bool IsInRoom { get; private set; }
        public string LocalPlayerId { get; private set; }
        public NetworkPlayer LocalPlayer { get; private set; }

        // Player registry
        private Dictionary<string, NetworkPlayer> players = new Dictionary<string, NetworkPlayer>();
        public IReadOnlyDictionary<string, NetworkPlayer> Players => players;

        // Local player data
        private PlayerData localPlayerData;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
                return;
            }

            InitializeLocalData();
        }

        private void Start()
        {
            if (autoConnect)
            {
                Connect();
            }
        }

        private void InitializeLocalData()
        {
            // Load saved player data or create default
            localPlayerData = new PlayerData
            {
                userId = GenerateUserId(),
                username = PlayerPrefs.GetString("Username", defaultUsername),
                avatarId = PlayerPrefs.GetInt("AvatarId", defaultAvatarId),
                avatarColor = PlayerPrefs.GetString("AvatarColor", "#FFFFFF"),
                outfit = PlayerPrefs.GetString("Outfit", "casual")
            };

            LocalPlayerId = localPlayerData.userId;
        }

        private string GenerateUserId()
        {
            string saved = PlayerPrefs.GetString("UserId", "");
            if (string.IsNullOrEmpty(saved))
            {
                saved = Guid.NewGuid().ToString().Substring(0, 8);
                PlayerPrefs.SetString("UserId", saved);
                PlayerPrefs.Save();
            }
            return saved;
        }

        // Connection methods
        public void Connect()
        {
            Debug.Log("[Network] Connecting to server...");

            // Simulate connection for now - replace with actual networking
            StartCoroutine(SimulateConnect());
        }

        private System.Collections.IEnumerator SimulateConnect()
        {
            yield return new WaitForSeconds(0.5f);

            IsConnected = true;
            Debug.Log("[Network] Connected to server");
            OnConnectedToServer?.Invoke();

            // Auto-join room
            JoinRoom(roomName);
        }

        public void Disconnect()
        {
            if (IsInRoom)
            {
                LeaveRoom();
            }

            IsConnected = false;
            Debug.Log("[Network] Disconnected from server");
        }

        public void JoinRoom(string room)
        {
            if (!IsConnected)
            {
                OnConnectionError?.Invoke("Not connected to server");
                return;
            }

            Debug.Log($"[Network] Joining room: {room}");
            StartCoroutine(SimulateJoinRoom(room));
        }

        private System.Collections.IEnumerator SimulateJoinRoom(string room)
        {
            yield return new WaitForSeconds(0.3f);

            IsInRoom = true;
            roomName = room;

            // Spawn local player
            SpawnLocalPlayer();

            Debug.Log($"[Network] Joined room: {room}");
            OnJoinedRoom?.Invoke();

            // Simulate other players joining (for testing)
            if (Application.isEditor)
            {
                yield return new WaitForSeconds(1f);
                SimulateRemotePlayer("Sarah", new Vector3(-3, 0, 2));
                yield return new WaitForSeconds(0.5f);
                SimulateRemotePlayer("Jimmy", new Vector3(-1, 0, 3));
                yield return new WaitForSeconds(0.5f);
                SimulateRemotePlayer("JOSH", new Vector3(2, 0, 5));
            }
        }

        public void LeaveRoom()
        {
            if (!IsInRoom) return;

            // Destroy local player
            if (LocalPlayer != null)
            {
                Destroy(LocalPlayer.gameObject);
                LocalPlayer = null;
            }

            // Clear all remote players
            foreach (var player in players.Values)
            {
                if (player != null)
                {
                    Destroy(player.gameObject);
                }
            }
            players.Clear();

            IsInRoom = false;
            OnLeftRoom?.Invoke();
            Debug.Log("[Network] Left room");
        }

        // Player spawning
        private void SpawnLocalPlayer()
        {
            Vector3 spawnPos = GetSpawnPosition();
            Quaternion spawnRot = Quaternion.identity;

            GameObject playerObj;

            if (localPlayerPrefab != null)
            {
                playerObj = Instantiate(localPlayerPrefab, spawnPos, spawnRot);
            }
            else
            {
                playerObj = new GameObject("LocalPlayer");
                playerObj.transform.position = spawnPos;
            }

            LocalPlayer = playerObj.GetComponent<NetworkPlayer>();
            if (LocalPlayer == null)
            {
                LocalPlayer = playerObj.AddComponent<NetworkPlayer>();
            }

            LocalPlayer.Initialize(localPlayerData, true);
            players[LocalPlayerId] = LocalPlayer;

            Debug.Log($"[Network] Spawned local player: {localPlayerData.username}");
        }

        private void SimulateRemotePlayer(string username, Vector3 position)
        {
            var data = new PlayerData
            {
                userId = Guid.NewGuid().ToString().Substring(0, 8),
                username = username,
                avatarId = UnityEngine.Random.Range(0, 10),
                avatarColor = GetRandomColor(),
                outfit = GetRandomOutfit()
            };

            SpawnRemotePlayer(data, position);
        }

        public void SpawnRemotePlayer(PlayerData data, Vector3 position)
        {
            if (players.ContainsKey(data.userId))
            {
                Debug.LogWarning($"[Network] Player {data.username} already exists");
                return;
            }

            GameObject playerObj;

            if (remotePlayerPrefab != null)
            {
                playerObj = Instantiate(remotePlayerPrefab, position, Quaternion.identity);
            }
            else
            {
                playerObj = new GameObject($"RemotePlayer_{data.username}");
                playerObj.transform.position = position;
            }

            var player = playerObj.GetComponent<NetworkPlayer>();
            if (player == null)
            {
                player = playerObj.AddComponent<NetworkPlayer>();
            }

            player.Initialize(data, false);
            players[data.userId] = player;

            OnPlayerJoined?.Invoke(player);
            Debug.Log($"[Network] Remote player joined: {data.username}");
        }

        public void RemovePlayer(string playerId)
        {
            if (players.TryGetValue(playerId, out var player))
            {
                OnPlayerLeft?.Invoke(player);
                Destroy(player.gameObject);
                players.Remove(playerId);
                Debug.Log($"[Network] Player left: {playerId}");
            }
        }

        private Vector3 GetSpawnPosition()
        {
            if (spawnPoints != null && spawnPoints.Length > 0)
            {
                var spawn = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                return spawn.position + UnityEngine.Random.insideUnitSphere * spawnRadius;
            }

            return Vector3.zero + UnityEngine.Random.insideUnitSphere * spawnRadius;
        }

        // Player data updates
        public void UpdateLocalUsername(string username)
        {
            localPlayerData.username = username;
            PlayerPrefs.SetString("Username", username);
            PlayerPrefs.Save();

            LocalPlayer?.UpdateData(localPlayerData);
            BroadcastPlayerUpdate(localPlayerData);
        }

        public void UpdateLocalAvatar(int avatarId, string color = null, string outfit = null)
        {
            localPlayerData.avatarId = avatarId;
            if (color != null) localPlayerData.avatarColor = color;
            if (outfit != null) localPlayerData.outfit = outfit;

            PlayerPrefs.SetInt("AvatarId", avatarId);
            if (color != null) PlayerPrefs.SetString("AvatarColor", color);
            if (outfit != null) PlayerPrefs.SetString("Outfit", outfit);
            PlayerPrefs.Save();

            LocalPlayer?.UpdateData(localPlayerData);
            BroadcastPlayerUpdate(localPlayerData);
        }

        private void BroadcastPlayerUpdate(PlayerData data)
        {
            // Send to server - implement with actual networking
            Debug.Log($"[Network] Broadcasting player update: {data.username}");
        }

        // RPC-style calls
        public void SendEmote(string emoteId)
        {
            // Broadcast emote to all players
            LocalPlayer?.PlayEmote(emoteId);
            BroadcastEmote(LocalPlayerId, emoteId);
        }

        private void BroadcastEmote(string playerId, string emoteId)
        {
            Debug.Log($"[Network] Broadcasting emote: {emoteId} from {playerId}");
        }

        public void SendChatMessage(string message)
        {
            var chatMsg = new ChatMessage
            {
                senderId = LocalPlayerId,
                senderName = localPlayerData.username,
                message = message,
                timestamp = DateTime.Now
            };

            // Display locally
            LocalPlayer?.ShowChatBubble(message);

            // Broadcast to others
            BroadcastChatMessage(chatMsg);
        }

        private void BroadcastChatMessage(ChatMessage msg)
        {
            Debug.Log($"[Network] Chat: {msg.senderName}: {msg.message}");
        }

        // Utility
        private string GetRandomColor()
        {
            Color[] colors = { Color.red, Color.blue, Color.green, Color.yellow, Color.cyan, Color.magenta };
            return ColorUtility.ToHtmlStringRGB(colors[UnityEngine.Random.Range(0, colors.Length)]);
        }

        private string GetRandomOutfit()
        {
            string[] outfits = { "casual", "streetwear", "formal", "futuristic", "creative" };
            return outfits[UnityEngine.Random.Range(0, outfits.Length)];
        }

        // Getters
        public int GetPlayerCount() => players.Count;
        public PlayerData GetLocalPlayerData() => localPlayerData;
    }

    [Serializable]
    public class PlayerData
    {
        public string userId;
        public string username;
        public int avatarId;
        public string avatarColor;
        public string outfit;
    }

    [Serializable]
    public class ChatMessage
    {
        public string senderId;
        public string senderName;
        public string message;
        public DateTime timestamp;
    }
}
