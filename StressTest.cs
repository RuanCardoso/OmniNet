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

using System.Collections;
using System.Threading;
using UnityEngine;

namespace Neutron.Core.Tests
{
    [AddComponentMenu("")]
    public class StressTest : MonoBehaviour
    {
        [SerializeField] private int perFrame = 1;
        [SerializeField] private bool Unreliable = false;
        [SerializeField] private bool Reliable = false;
        [SerializeField] private bool ReliableOrdered = false;
        [SerializeField] private bool MultiThreaded = false;

        int index = 0;
        public void Send(Channel channel)
        {
            ByteStream stream = ByteStream.Get();
            stream.WritePacket(MessageType.StressTest);
            stream.Write(++index);
            stream.Send(channel, Target.All);
        }

        private void Update()
        {
            for (int i = 0; i < perFrame; i++)
            {
                // check if it throws an error in multithread.
                if (MultiThreaded)
                {
                    ThreadPool.QueueUserWorkItem((o) =>
                    {
                        if (Unreliable || Reliable || ReliableOrdered)
                        {
                            if (Unreliable)
                                Send(Channel.Unreliable);
                            if (Reliable)
                                Send(Channel.Reliable);
                            if (ReliableOrdered)
                                Send(Channel.ReliableAndOrderly);
                        }
                    });
                }
                else
                {
                    if (Unreliable || Reliable || ReliableOrdered)
                    {
                        if (Unreliable)
                            Send(Channel.Unreliable);
                        if (Reliable)
                            Send(Channel.Reliable);
                        if (ReliableOrdered)
                            Send(Channel.ReliableAndOrderly);
                    }
                }
            }
        }

        private float count;

        private IEnumerator Start()
        {
            GUI.depth = 2;
            while (true)
            {
                count = 1f / Time.unscaledDeltaTime;
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void OnGUI()
        {
            GUI.Label(new Rect(5, 180, 100, 25), "FPS: " + Mathf.Round(count));
            Unreliable = GUI.Toggle(new Rect(10, 10, 100, 20), Unreliable, "Unreliable");
            Reliable = GUI.Toggle(new Rect(10, 30, 100, 20), Reliable, "Reliable");
            ReliableOrdered = GUI.Toggle(new Rect(10, 50, 110, 20), ReliableOrdered, "ReliableOrdered");
        }
    }
}