using MessagePack;
using Omni.Core;
using System;
using UnityEngine;

[MessagePackObject]
public class Person : IMessage
{
    [Key(1)]
    public byte Id => 1;
    [Key(2)]
    public string Name;
    [Key(3)]
    public int Age;
}

namespace Omni.Tests
{
    public class GlobalMessageTests : MonoBehaviour
    {
        private MessageStream messageStream;

        private void Awake()
        {
            messageStream = new MessageStream();
        }

        private void Start()
        {
            OmniNetwork.AddHandler<Person>(OnPersonReceived);
        }

        private void OnPersonReceived(ReadOnlyMemory<byte> data, ushort fromId, bool isServer, RemoteStats arg4)
        {
            OmniLogger.Print("Pessoa Ruan");
        }

        // Update is called once per frame
        void Update()
        {
            if (Input.GetKey(KeyCode.M))
            {
                Person pessoa = new Person();
                pessoa.SendMessage(messageStream, false);
            }
        }
    }
}
