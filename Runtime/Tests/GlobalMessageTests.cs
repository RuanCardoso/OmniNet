using MessagePack;
using Omni.Core;
using System;
using UnityEngine;

[MessagePackObject]
public class Person : IMessage
{
    [IgnoreMember]
    public byte Id => 1;
    [Key(0)]
    public string Name { get; set; }
    [Key(1)]
    public int Age { get; set; }

    public Person()
    {
    }

    public Person(string name, int age)
    {
        Name = name;
        Age = age;
    }
}

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
            Person pessoa = new Person("Ruan", 21);
            pessoa.SendMessage(messageStream, false);
        }
    }
}
