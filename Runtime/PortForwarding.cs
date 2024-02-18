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

using Omni.Core;
using Open.Nat;
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace Omni.Internal
{
	[DefaultExecutionOrder(-150)]
	public class PortForwarding : MonoBehaviour
	{
		public static PortForwarding Instance { get; private set; }

		[Serializable]
		private class PortMapping
		{
			[SerializeField]
			internal string m_Name = "Omni Server";
			[SerializeField]
			internal bool m_Enabled = false;
			[SerializeField]
			internal int m_Timeout = 5000;
			[SerializeField]
			internal PortMapper m_PortMapper = PortMapper.Upnp;
			[SerializeField]
			internal Protocol m_Protocol = Protocol.Tcp;
			[SerializeField] internal int m_ExternalPort;
			[SerializeField] internal int m_InternalPort;
			[SerializeField] internal int m_LifeTime = 0;

			internal PortMapping(string name)
			{
				m_Name = name;
			}
		}

		[InfoBox("This component enables your local computer to function as a public server, allowing external access to specified ports. Ensure UPnP or PMP is enabled in your router settings.")]
		[InfoBox("To stop an instance, set Enabled to false. To activate, set it to true.")]
		[SerializeField]
		private PortMapping[] m_Mapping = new PortMapping[] { new("Omni Server"), new("Ntp Server") };

		private void Awake()
		{
			Instance = this;
		}

		private void Start() { }

		private async void Init(bool open)
		{
			NatDiscoverer discoverer = new NatDiscoverer();
			for (int i = 0; i < m_Mapping.Length; i++)
			{
				PortMapping portMapping = m_Mapping[i];
				if (portMapping.m_InternalPort != 0 && portMapping.m_ExternalPort != 0)
				{
					try
					{
						CancellationTokenSource cts = new CancellationTokenSource(portMapping.m_Timeout);
						NatDevice device = await discoverer.DiscoverDeviceAsync(portMapping.m_PortMapper, cts);
						Mapping mapping = new Mapping(portMapping.m_Protocol, portMapping.m_InternalPort, portMapping.m_ExternalPort, portMapping.m_Name);
						if (open)
						{
							if (portMapping.m_Enabled)
							{
								await device.CreatePortMapAsync(mapping);
								OmniLogger.Print($"Port mapping created successfully -> Public: {portMapping.m_ExternalPort} -> Internal: {portMapping.m_InternalPort}"); // Example error message
							}
						}
						else
						{
							if (!portMapping.m_Enabled)
							{
								await device.DeletePortMapAsync(mapping);
								OmniLogger.Print($"Port mapping deleted successfully"); // Example error message
							}
						}
					}
					catch (MappingException ex)
					{
						OmniLogger.PrintError($"Error: Failed to create port mapping due to a mapping exception. Please make sure to use different ports. -> {ex.Message}");
					}
					catch (NatDeviceNotFoundException ex)
					{
						OmniLogger.PrintError($"Error: NAT device not found. Please make sure UPnP or PMP is enabled on your router. -> {ex.Message}");
					}
				}
			}
		}

		[Button("Start")]
		public void Open()
		{
			Init(true);
		}

		[Button("Stop")]
		public void Close()
		{
			Init(false);
		}

		[Button("Close All")]
		public void CloseAll()
		{
			Delete(PortMapper.Upnp);
			Delete(PortMapper.Pmp);
		}

		public async void Delete(PortMapper portMapper, int timeout = 10000)
		{
			try
			{
				NatDiscoverer discoverer = new NatDiscoverer();
				CancellationTokenSource cts = new CancellationTokenSource(timeout);
				NatDevice device = await discoverer.DiscoverDeviceAsync(portMapper, cts);
				IEnumerable<Mapping> mappings = await device.GetAllMappingsAsync();
				foreach (var mapping in mappings)
				{
					try
					{
						await device.DeletePortMapAsync(mapping);
						OmniLogger.Print("Success! deleted all ports.");
					}
					catch
					{
						// Ignore......
						continue;
					}
				}
			}
			catch
			{
				// Ignore......
			}
		}
	}
}