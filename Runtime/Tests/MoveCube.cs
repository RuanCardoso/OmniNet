using MessagePack;
using Neutron.Core;
using UnityEngine;
using Logger = Neutron.Core.Logger;

public class MoveCube : NeutronObject
{
    [MessagePackObject]
    public struct NetMove
    {
        [Key(0)] public Vector3 Position;
        [Key(1)] public Vector3 Velocity;
        [Key(2)] public Vector3 AngularVelocity;
    }


    float force = 300;
    Rigidbody rb;
    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    protected override void Update()
    {
        base.Update();
        {
            if (IsMine)
            {
                float horizontal = Input.GetAxis("Horizontal");
                float vertical = Input.GetAxis("Vertical");
                rb.velocity = new Vector3(horizontal * force * Time.deltaTime, rb.velocity.y, vertical * force * Time.deltaTime);

                NetMove netMove = new()
                {
                    Position = transform.position,
                    Velocity = rb.velocity,
                    AngularVelocity = rb.angularVelocity,
                };

                var netStream = Get;
                netStream.Serialize(netMove);
                Remote(1, netStream, Channel.Unreliable, Target.Others);
            }
        }
    }


    [Remote(1)]
    public void SpawnPlayer(ByteStream parameters, ushort fromId, ushort toId, RemoteStats stats)
    {
        if (!IsMine)
        {
            NetMove netMove = parameters.Deserialize<NetMove>();
            transform.position = netMove.Position;
            rb.velocity = netMove.Velocity;
            rb.angularVelocity = netMove.AngularVelocity;
        }
    }
}
