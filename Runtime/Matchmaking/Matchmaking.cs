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
using System.Collections.Generic;
using UnityEngine;
using static Omni.Core.OmniNetwork;

namespace Omni.Core.IMatchmaking
{
	[DefaultExecutionOrder(-2500)]
	public class Matchmaking : MonoBehaviour
	{
		public Dictionary<int, Channel> Channels { get; } = new Dictionary<int, Channel>();

		enum MatchmakingOption
		{
			JoinChannel = 54,
			LeaveChannel = 55,
		}

		internal void ProcessEvents()
		{
			NetworkCallbacks.Internal_OnCustomMessageReceived += Internal_OnCustomMessageReceived;
		}

		private void Internal_OnCustomMessageReceived(bool isServer, IDataReader reader, NetworkPeer peer, DataDeliveryMode deliveryMode)
		{
			MatchmakingOption matchmakingOption = reader.ReadCustomMessage<MatchmakingOption>(out int lastPos);
			switch (matchmakingOption)
			{
				case MatchmakingOption.JoinChannel:
					{
						int channelId = reader.Read7BitEncodedInt();
						if (isServer)
						{
							if (peer.Channel == 0)
							{
								if (channelId < 0 || channelId == 0)
								{
									OmniLogger.PrintError("The channelId must be non-negative and non-zero.");
									return;
								}

								JoinChannel(isServer, peer, channelId);
							}
							else
							{
								OmniLogger.PrintError("Failed to add the peer to the channel: Peer is already in a channel.");
							}
						}
						else NetworkCallbacks.FireChannelPlayerJoined(isServer, peer, channelId);
					}
					break;
				case MatchmakingOption.LeaveChannel:
					{
						if (isServer)
						{
							if (peer.Channel == 0)
							{
								OmniLogger.PrintError("Peer is not assigned to any channel.");
								return;
							}

							LeaveChannel(isServer, peer, peer.Channel);
						}
						else NetworkCallbacks.FireChannelPlayerLeft(isServer, peer, peer.Channel);
					}
					break;
				default:
					reader.Position = lastPos;
					break;
			}
		}

		internal void JoinChannel(bool isServer, NetworkPeer peer, int channelId)
		{
			if (Channels.TryGetValue(channelId, out Channel channel))
			{
				channel.PeersById.Add(peer.Id, peer);
				peer.Channel = channelId;
				NetworkCallbacks.FireChannelPlayerJoined(isServer, peer, channelId);
			}
			else
			{
				Channel newChannel = new Channel();
				if (Channels.TryAdd(channelId, newChannel))
				{
					newChannel.PeersById.Add(peer.Id, peer);
					peer.Channel = channelId;
					NetworkCallbacks.FireChannelPlayerJoined(isServer, peer, channelId);
				}
			}
		}

		internal void LeaveChannel(bool isServer, NetworkPeer peer, int channelId)
		{
			if (Channels.TryGetValue(peer.Channel, out Channel channel))
			{
				if (channel.PeersById.Remove(peer.Id))
				{
					peer.Channel = 0;
					NetworkCallbacks.FireChannelPlayerLeft(isServer, peer, channelId);
				}
			}
		}

		public void JoinChannel(int channelId, int peerId = 0, DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
		{
			if (channelId < 0)
			{
				throw new ArgumentException("The channelId must be non-negative.", nameof(channelId));
			}

			if (peerId != 0)
			{
				NetworkPeer peer = Communicator.GetPeerById(peerId);
				if (peer.Channel == 0)
				{
					JoinChannel(true, peer, channelId);
				}
				else return;
			}

			IDataWriter writer = Communicator.GetWriter();
			writer.Write7BitEncodedInt(channelId);
			Communicator.Internal_SendCustomMessage(MatchmakingOption.JoinChannel, writer, peerId, deliveryMode, sequenceChannel);
			Communicator.Release(writer);
		}

		public void LeaveChannel(int channelId, int peerId = 0, DataDeliveryMode deliveryMode = DataDeliveryMode.ReliableOrdered, byte sequenceChannel = 0)
		{
			if (channelId < 0)
			{
				throw new ArgumentException("The channelId must be non-negative.", nameof(channelId));
			}

			if (peerId != 0)
			{
				NetworkPeer peer = Communicator.GetPeerById(peerId);
				if (peer.Channel != 0)
				{
					LeaveChannel(true, peer, channelId);
				}
				else return;
			}

			IDataWriter writer = Communicator.GetWriter();
			Communicator.Internal_SendCustomMessage(MatchmakingOption.LeaveChannel, writer, peerId, deliveryMode, sequenceChannel);
			Communicator.Release(writer);
		}
	}
}