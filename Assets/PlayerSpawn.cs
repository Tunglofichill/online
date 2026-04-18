using Fusion;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    public NetworkPrefabRef playerPrefab;

    public override void Spawned()
    {
        // chỉ master spawn
        if (Runner.IsSharedModeMasterClient)
        {
            Runner.Spawn(playerPrefab, new Vector3(0, 1, 0), Quaternion.identity);
            Debug.Log("SPAWN DONE");
        }
    }
}