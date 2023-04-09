using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MasterNetworkAdapter : NetworkBehaviour
{
    public enum NetworkMode
    {
        Off,
        Client,
        Host,
        Server
    }
    public static NetworkMode mode = NetworkMode.Off;
    public static MasterNetworkAdapter instance;
    public static string address;
    public static string port;
    public static string playerName = "Test Name";
    public static string blueprint;
    public static string world;
    public static int POP_IN_DISTANCE = 500;

    public static bool lettingServerDecide;
    void Start()
    {
        Debug.Log("MNA starting...");

        HUDScript.ClearScore();
        if (EntityNetworkAdapter.playerFactions != null)
            EntityNetworkAdapter.playerFactions.Clear();
        instance = this;
        if (!NetworkManager.Singleton) return;
        
        if (NetworkManager.Singleton.IsServer)
        {
            if (!NetworkManager.Singleton.IsClient)
                PlayerCore.Instance.gameObject.SetActive(false);
            MasterNetworkAdapter.mode = MasterNetworkAdapter.NetworkMode.Server;
        }
        if (NetworkManager.Singleton.IsClient)
        {
            MasterNetworkAdapter.mode = MasterNetworkAdapter.NetworkMode.Client;
        }
        if (NetworkManager.Singleton.IsClient && NetworkManager.Singleton.IsServer)
        {
            MasterNetworkAdapter.mode = MasterNetworkAdapter.NetworkMode.Host;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Client)
        {
            MasterNetworkAdapter.mode = NetworkMode.Off;
            MasterNetworkAdapter.lettingServerDecide = false;
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("MainMenu");
            SceneManager.sceneLoaded += UnloadMessage;
        }
    }

    private void UnloadMessage(Scene s1, LoadSceneMode s2)
    {
        Debug.LogWarning("Server disconnected.");
        DevConsoleScript.Instance.SetActive();
        SceneManager.sceneLoaded -= UnloadMessage;
        return;
    }

    public static void StartClient()
    {
        ushort portVal = 0;
        if (!string.IsNullOrEmpty(port) && ushort.TryParse(port, out portVal))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Port = portVal;
        }
        if (!string.IsNullOrEmpty(address))
        {
            NetworkManager.Singleton.GetComponent<UnityTransport>().ConnectionData.Address = address;
        }

        NetworkManager.Singleton.StartClient();
        MasterNetworkAdapter.lettingServerDecide = true;
    }

    public static void StartServer()
    {
        Debug.Log("Starting server...");
        MasterNetworkAdapter.lettingServerDecide = false;
        NetworkManager.Singleton.StartServer();
    }

    public static void StartHost()
    {

        MasterNetworkAdapter.lettingServerDecide = false;
        NetworkManager.Singleton.StartHost();
    }

    [ClientRpc]
    public void GetWorldNameClientRpc(string worldName, int currentSector, ulong clientID)
    {
        if (NetworkManager.Singleton.IsHost || NetworkManager.LocalClientId != clientID) return;
        var path = System.IO.Path.Combine(Application.streamingAssetsPath, "Sectors", worldName);
        if (!System.IO.Directory.Exists(path)) return;
        SectorManager.currentSectorIndex = currentSector;
        WCWorldIO.LoadTestSave(path, true);
        SectorManager.instance.ReloadSector(SectorManager.currentSectorIndex);
    }


    [ClientRpc]
    public void ReloadSectorClientRpc(int sectorToChange, ClientRpcParams clientRpcParams = default)
    {
        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Host) return;
        SectorManager.instance.ReloadSector(sectorToChange);
    }

    [ClientRpc]
    public void NotifyInvalidBlueprintClientRpc(string reason, ClientRpcParams clientRpcParams = default)
    {
        if (clientRpcParams.Send.TargetClientIds != null &&
            !System.Linq.Enumerable.Contains<ulong>(clientRpcParams.Send.TargetClientIds, NetworkManager.Singleton.LocalClientId))
        {
            return;
        }
        Debug.LogWarning($"Your passed blueprint is invalid. Reason: {reason}\nUse command loadbp <blueprint JSON> to pass another one and join the game.");
        DevConsoleScript.Instance.SetActive();
    }

    [ClientRpc]
    public void BombExplosionClientRpc(Vector3 position)
    {
        if (MasterNetworkAdapter.mode == MasterNetworkAdapter.NetworkMode.Host) return;
        BombScript.ActivationCosmetic(position);
    }


    public GameObject networkObj;

    public void CreateNetworkObjectWrapper(string name, string blueprint, string idToGrab, bool isPlayer, int faction, Vector3 pos, ServerRpcParams serverRpcParams = default)
    {
        var obj = InternalEntitySpawnWrapper(blueprint, idToGrab, isPlayer, faction, pos, serverRpcParams);
        var networkAdapter = obj.GetComponent<EntityNetworkAdapter>();
        networkAdapter.blueprint = SectorManager.TryGettingEntityBlueprint(blueprint);
        if (isPlayer)
        {
            networkAdapter.blueprint.shellHealth = CoreUpgraderScript.defaultHealths;
            networkAdapter.blueprint.baseRegen = CoreUpgraderScript.GetRegens(networkAdapter.blueprint.coreShellSpriteID);
        }
        if (isPlayer) networkAdapter.playerName = name;
        else
        {
            networkAdapter.passedFaction = faction;
            networkAdapter.SetUpHuskEntity();
        }
    }

    public Dictionary<ulong, bool> playerSpawned = new Dictionary<ulong, bool>();

    [ClientRpc]
    public void SetScoreClientRpc(string player, int val, ClientRpcParams clientRpcParams = default)
    {
        if (MasterNetworkAdapter.mode == NetworkMode.Host) return;
        HUDScript.SetScore(player, val);
    }

    [ClientRpc]
    public void AddScoreClientRpc(string player, int val, ClientRpcParams clientRpcParams = default)
    {
        if (MasterNetworkAdapter.mode == NetworkMode.Host) return;
        HUDScript.AddScore(player, val);
    }

    [ClientRpc]
    public void RemoveScoreClientRpc(string player, ClientRpcParams clientRpcParams = default)
    {
        if (MasterNetworkAdapter.mode == NetworkMode.Host) return;
        HUDScript.RemoveScore(player);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RequestVoteServerRpc(int i, ServerRpcParams serverRpcParams = default)
    {
        if (!DialogueSystem.Instance.IsVoting()) return;
        ulong clientId = serverRpcParams.Receive.SenderClientId;
        int voteToAdd = -1;
        int voteToSub = -1;
        if (!DialogueSystem.Instance.votesById.ContainsKey(clientId))
        {
            DialogueSystem.Instance.votesById.Add(clientId, i);
        }
        else
        {
            voteToSub = DialogueSystem.Instance.votesById[clientId];
            DialogueSystem.Instance.voteNumbers[voteToSub]--;
            DialogueSystem.Instance.votesById[clientId] = i;
        }
        voteToAdd = i;
        DialogueSystem.Instance.voteNumbers[i]++;
        RequestRefreshVoteClientRpc(voteToAdd, voteToSub);
    }

    [ClientRpc]
    public void RequestRefreshVoteClientRpc(int newVoteToAdd, int newVoteToSub, ClientRpcParams clientRpcParams = default)
    {
        if (MasterNetworkAdapter.mode == NetworkMode.Host)
        {
            DialogueSystem.Instance.RefreshButtons();
            return;
        }
        if (newVoteToAdd >= 0 && DialogueSystem.Instance.voteNumbers.Count > newVoteToAdd)
            DialogueSystem.Instance.voteNumbers[newVoteToAdd]++;
        if (newVoteToSub >= 0 && DialogueSystem.Instance.voteNumbers.Count > newVoteToSub)
            DialogueSystem.Instance.voteNumbers[newVoteToSub]--;
        DialogueSystem.Instance.RefreshButtons();
    }


    [ServerRpc(RequireOwnership = false)]
    public void CreatePlayerServerRpc(string name, string blueprint, int faction, ServerRpcParams serverRpcParams = default)
    {
        if (!playerSpawned.ContainsKey(serverRpcParams.Receive.SenderClientId))
            playerSpawned.Add(serverRpcParams.Receive.SenderClientId, false);
        if (playerSpawned[serverRpcParams.Receive.SenderClientId]) return;
        string reason = "";
        if (!ValidateBluperintOnServer(blueprint, out reason))
        {
            NotifyInvalidBlueprintClientRpc(reason, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new ulong[] {serverRpcParams.Receive.SenderClientId}
                }
            });
            return;
        }
        CreateNetworkObjectWrapper(name, blueprint, "player-"+serverRpcParams.Receive.SenderClientId, true, faction, Vector3.zero, serverRpcParams);
        playerSpawned[serverRpcParams.Receive.SenderClientId] = true;
    }

    [ClientRpc]
    public void EnergySphereCollectClientRpc(ClientRpcParams clientRpcParams = default)
    {
        if (PlayerCore.Instance) 
            AudioManager.PlayClipByID("clip_powerup", PlayerCore.Instance.transform.position);
    }


    [ClientRpc]
    public void BulletMissClientRpc(Vector2 position, Vector2 rotation, ClientRpcParams clientRpcParams = default)
    {
        Instantiate(ResourceManager.GetAsset<GameObject>("bullet_miss_prefab"), position, Quaternion.Euler(0, 0, Mathf.Atan2(rotation.y, rotation.x) * Mathf.Rad2Deg));
    }

    [ClientRpc]
    public void BulletHitClientRpc(Vector2 position, ClientRpcParams clientRpcParams = default)
    {
        Instantiate(ResourceManager.GetAsset<GameObject>("bullet_hit_prefab"), position, Quaternion.identity);
    }

    private bool ValidateBluperintOnServer(string blueprint, out string reason)
    {
        if (blueprint.Length > 25000) // Blueprint too large. We can't have the server do too much work here or else it will chug everyone.
        {
            reason = "Too many characters in JSON.";
            return false;
        }
        var print = ScriptableObject.CreateInstance<EntityBlueprint>();
        try
        {
            print = SectorManager.TryGettingEntityBlueprint(blueprint);
        }
        catch // invalid blueprint
        {
            reason = "Blueprint did not parse.";
            return false;
        }
        if (print.intendedType != EntityBlueprint.IntendedType.ShellCore)
        {
            reason = "Blueprint not for ShellCores.";
            return false; // print is of incorrect type
        } 
        var abilityDict = new Dictionary<AbilityID, int>() {
            [AbilityID.MainBullet] = 0,
            [AbilityID.Harvester] = 0,
            [AbilityID.EnergyAura] = 0,
            [AbilityID.SpeedAura] = 0,
            [AbilityID.HealAura] = 0,
            [AbilityID.Rocket] = 0,
            [AbilityID.SpeederBullet] = 0,
            [AbilityID.SiegeBullet] = 0,
            [AbilityID.DamageBoost] = 1,
            [AbilityID.Stealth] = 1,
            [AbilityID.PinDown] = 1,
            [AbilityID.Disrupt] = 1,
            [AbilityID.Retreat] = 1,
            [AbilityID.Control] = 1,
            [AbilityID.Command] = 1,
        };
        foreach (var part in print.parts)
        {
            if (!ResourceManager.allPartNames.Contains(part.partID))
            {
                reason = $"Invalid part ID: {part.partID}";
                return false;
            }
            if (abilityDict.ContainsKey((AbilityID)part.abilityID))
            {
                abilityDict[(AbilityID)part.abilityID]--;
                if (abilityDict[(AbilityID)part.abilityID] < 0)
                {
                    reason = $"Too many of ability: {((AbilityID)part.abilityID).ToString()}";
                    return false;
                }
            }
            if ((part.abilityID != 10 && !string.IsNullOrEmpty(part.secondaryData)) || (part.abilityID == 10 && !DroneUtilities.DEFAULT_SECONDARY_DATA.Contains(part.secondaryData)))
            {
                reason = "Part has invalid secondary data.";
                return false;
            }

            if (part.tier < 0 || part.tier > 3)
            {
                reason = "A part has tier < 0 or > 3.";
                return false;
            } 
        }

        reason = "Blueprint is not according to Ship Builder rules.";
        return ShipBuilder.ValidateBlueprint(print, false, print.coreShellSpriteID, true, CoreUpgraderScript.GetTotalAbilities(print.coreShellSpriteID));
    }

    private NetworkObject InternalEntitySpawnWrapper(string blueprint, string idToGrab, bool isPlayer, int faction, Vector3 pos, ServerRpcParams serverRpcParams = default)
    {
        var clientId = serverRpcParams.Receive.SenderClientId;
        var obj = Instantiate(networkObj).GetComponent<NetworkObject>();
        obj.SpawnWithOwnership(clientId);
        obj.GetComponent<EntityNetworkAdapter>().blueprintString = blueprint;
        obj.GetComponent<EntityNetworkAdapter>().passedFaction = faction;
        obj.GetComponent<EntityNetworkAdapter>().isPlayer.Value = isPlayer;
        obj.GetComponent<EntityNetworkAdapter>().idToUse = idToGrab;
        if (pos != Vector3.zero)
            obj.GetComponent<EntityNetworkAdapter>().ChangePositionWrapper(pos);
        NetworkManager.Singleton.OnClientDisconnectCallback += (u) =>
        {
            if (u == clientId) obj.Despawn();
        };
        
        return obj;
    }
}
