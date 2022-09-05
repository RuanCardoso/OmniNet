using MessagePack;
using Neutron.Core;
using UnityEngine;
using Logger = Neutron.Core.Logger;

public class LoadResolver : MonoBehaviour
{
    [MessagePackObject]
    public struct NetPlayer : ISerializable
    {
        [IgnoreMember] public int Id => 1;
        [Key(0)] public string Name { get; set; }
        [Key(1)] public ushort Score { get; set; }
        [Key(2)] public byte Health { get; set; }
        [Key(3)] public byte Mana { get; set; }
        [Key(4)] public byte Level { get; set; }
    }

    private void Awake()
    {
        NeutronNetwork.AddHandler<NetPlayer>(OnNetPlayer);
    }

    private void OnNetPlayer(ByteStream netStream, bool isServer)
    {
        NetPlayer netPlayer = netStream.Unpack<NetPlayer>();
        if (isServer)
        {
#if UNITY_SERVER || UNITY_EDITOR
            Logger.Print($"Servidor! {netPlayer.Health}");
#endif
        }
        else
        {
#if !UNITY_SERVER || UNITY_EDITOR
            Logger.Print($"Client! {netPlayer.Health}");
#endif
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            new NetPlayer()
            {
                Name = "Ruan",
                Score = 100,
                Health = 100,
                Mana = 100,
                Level = 1,
            }.Pack().Send();
        }
    }
}