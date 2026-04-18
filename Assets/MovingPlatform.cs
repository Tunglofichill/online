using UnityEngine;
using Fusion;
using System.Collections.Generic;

public class MovingPlatform : NetworkBehaviour
{
    public Vector3 posA = new Vector3(0, 0.5f, 5f);
    public Vector3 posB = new Vector3(10, 0.5f, 5f);
    public float speed = 1f;

    [Networked] private float CurrentTime { get; set; }

    // list giu cac player dang dung tren platform
    private List<PlayerController> _passengers = new List<PlayerController>();

    public override void Spawned()
    {
        // tao trigger cho platform plane
        var trig = gameObject.AddComponent<MeshCollider>();
        trig.sharedMesh = GetComponent<MeshFilter>()?.sharedMesh;
        trig.convex = true; 
        trig.isTrigger = true;
    }

    public override void FixedUpdateNetwork()
    {
        // update thoi gian tu host
        if (HasStateAuthority)
        {
            CurrentTime += Runner.DeltaTime * speed;
        }

        // tinh toan vitri
        float pingPong = Mathf.PingPong(CurrentTime, 1f);
        Vector3 oldPos = transform.position;
        Vector3 newPos = Vector3.Lerp(posA, posB, pingPong);
        
        Vector3 delta = newPos - oldPos;
        transform.position = newPos;

        // move cac player di theo platform de tranh bi loi physics cua setparent
        foreach (var p in _passengers)
        {
            if (p != null)
            {
                p.transform.position += delta; 
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (!_passengers.Contains(player)) 
            {
                _passengers.Add(player);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            if (_passengers.Contains(player))
            {
                _passengers.Remove(player);
            }
        }
    }
}
