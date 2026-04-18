using UnityEngine;
using Fusion;
using System.Collections;

public class InvisibilityCloak : NetworkBehaviour
{
    [Networked] public NetworkBool IsActive { get; set; }

    public override void Spawned()
    {
        IsActive = true;
    }

    public override void Render()
    {
        // tắt mesh khi bi nhat mat
        var renderer = GetComponent<MeshRenderer>();
        var col = GetComponent<Collider>();
        
        if (renderer != null) renderer.enabled = IsActive;
        if (col != null) col.enabled = IsActive;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsActive) return;

        var player = other.GetComponent<PlayerController>();
        
        // check quyen player
        if (player != null && player.HasStateAuthority)
        {
            player.RPC_ApplyInvisibility();
            RPC_HideItemAndStartCooldown();
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_HideItemAndStartCooldown()
    {
        if (!IsActive) return; 

        IsActive = false;
        StartCoroutine(RespawnRoutine());
    }

    private IEnumerator RespawnRoutine()
    {
        // doi 15s respawn item
        yield return new WaitForSeconds(15f);
        IsActive = true;
    }
}
