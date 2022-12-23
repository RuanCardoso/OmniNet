/*===========================================================
    Author: Ruan Cardoso
    -
    Country: Brazil(Brasil)
    -
    Contact: cardoso.ruan050322@gmail.com
    -
    Support: neutron050322@gmail.com
    -
    Unity Minor Version: 2021.3 LTS
    -
    License: Open Source (MIT)
    ===========================================================*/

using System;
using UnityEngine;
using static Neutron.Core.Enums;

namespace Neutron.Core.Tests
{
    [AddComponentMenu("")]
    public class SyncValueTests : NeutronObject
    {
        [SerializeField] private SyncValue<byte> life; // Envia pra rede mudanças no valor.

        private void Start()
        {
            ByteStream a = ByteStream.Get();
            a.WritePaylod(Channel.Unreliable, Target.Server, SubTarget.None, CacheMode.Append);
            a.Position = 0;
            a.ReadPaylod(out var ch, out var tg, out var subTar, out var cacheMode);
            Logger.PrintError($"{ch} {tg} {subTar} {cacheMode}");
            life = new SyncValue<byte>(this, 100);
        }

        protected internal override void OnSerializeView(byte id, ByteStream parameters)
        {
            switch (id)
            {
                case 1:
                    life.Set(parameters.ReadByte()); // Ler o valor que foi para a rede e atribui a variável, por questão de desempenho isso é feito de forma manual porque descobrir o tipo e criar o tipo da variavel em runtime e serializar demandaria muita complexidade em código.
                    break;
            }
        }
    }
}