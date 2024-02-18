using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Omni.Core
{
	public static class NetworkExtensions
	{
		/// <summary>
		/// Reads the value from the specified <see cref="DataIOHandler"/> and sets it to the <see cref="SyncRef{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="value">The <see cref="SyncRef{T}"/> to set the value to.</param>
		/// <param name="IOHandler">The <see cref="DataIOHandler"/> used to deserialize the value.</param>
		internal static void Read<T>(this SyncRef<T> value, IDataReader reader) where T : class
		{
			ISyncBaseValue<T> ISyncBaseValue = value;
			ISyncBaseValue.Intern_Set(reader.DeserializeWithMsgPack<T>(null));
		}

		/// <summary>
		/// Reads the serialized data for a <see cref="SyncRefCustom{T}"/> object and deserializes it using the provided <see cref="DataIOHandler"/>.
		/// </summary>
		/// <typeparam name="T">The type of the object that implements <see cref="ISyncCustom"/>.</typeparam>
		/// <param name="value">The <see cref="SyncRefCustom{T}"/> object to read and deserialize.</param>
		/// <param name="IOHandler">The <see cref="DataIOHandler"/> used for deserialization.</param>
		internal static void Read<T>(this SyncRefCustom<T> value, IDataReader reader) where T : class, ISyncCustom
		{
			if (value.Get() is ISyncCustom ISerialize)
			{
				ISerialize.Deserialize(reader);
			}
			else
			{
				OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
			}
		}

		/// <summary>
		/// Reads the value of a <see cref="SyncValueCustom{T}"/> object from a <see cref="DataIOHandler"/> using custom deserialization.
		/// </summary>
		/// <typeparam name="T">The type of the value being read.</typeparam>
		/// <param name="value">The <see cref="SyncValueCustom{T}"/> object to read the value from.</param>
		/// <param name="IOHandler">The <see cref="DataIOHandler"/> used for deserialization.</param>
		/// <remarks>
		/// This method reads the value of a <see cref="SyncValueCustom{T}"/> object from a <see cref="DataIOHandler"/> using custom deserialization.
		/// It first checks if the value implements the <see cref="ISyncCustom"/> interface. If it does, it calls the <see cref="ISyncCustom.Deserialize"/> method
		/// on the value to perform the deserialization. The deserialized value is then set on the <see cref="SyncValueCustom{T}"/> object using the
		/// <see cref="ISyncBaseValue{T}.Intern_Set"/> method. If the value does not implement the <see cref="ISyncCustom"/> interface, an error message
		/// is logged using the <see cref="OmniLogger.PrintError"/> method.
		/// </remarks>
		internal static void Read<T>(this SyncValueCustom<T> value, IDataReader reader) where T : unmanaged, ISyncCustom
		{
			T get_value = value.Get();
			if (get_value is ISyncCustom ISerialize)
			{
				ISerialize.Deserialize(reader);
				((ISyncBaseValue<T>)value).Intern_Set((T)ISerialize);
			}
			else
			{
				OmniLogger.PrintError("Error: Failed to deserialize SyncCustom object. Make sure it implements ISyncCustom.");
			}
		}

		/// <summary>
		/// Reads the serialized value from the specified <see cref="DataIOHandler"/> and sets it to the <see cref="ISyncBaseValue{T}"/>.
		/// </summary>
		/// <typeparam name="T">The type of the value.</typeparam>
		/// <param name="value">The <see cref="ISyncBaseValue{T}"/> to set the deserialized value to.</param>
		/// <param name="reader">The <see cref="DataIOHandler"/> used for reading the serialized value.</param>
		/// <remarks>
		/// This method is used for deserializing different types of values from the <see cref="DataIOHandler"/>.
		/// It supports deserialization of <see cref="int"/>, <see cref="bool"/>, <see cref="float"/>, and <see cref="byte"/> types.
		/// If the type is not supported, an error message will be logged.
		/// </remarks>
		internal static void Read<T>(this ISyncBaseValue<T> value, IDataReader reader) where T : unmanaged
		{
			var converter = SyncValue<T>.Converter; // for high performance, converter is used to avoid boxing!
			switch (value.TypeCode)
			{
				case TypeCode.Int32:
					{
						value.Intern_Set(converter.GetInt(reader.ReadInt()));
					}
					break;
				case TypeCode.Boolean:
					{
						value.Intern_Set(converter.GetBool(reader.ReadBool()));
					}
					break;
				case TypeCode.Single:
					{
						value.Intern_Set(converter.GetFloat(reader.ReadFloat()));
					}
					break;
				case TypeCode.Byte:
					{
						value.Intern_Set(converter.GetByte(reader.ReadByte()));
					}
					break;
				default:
					{
						OmniLogger.PrintError("Error: Unsupported TypeCode for deserialization -> ISyncBaseValue<T>");
					}
					break;
			}
		}

		public static NetworkIdentity Instantiate(this NetworkIdentity prefab, int ownerId, bool isServer, Vector3 position, Quaternion rotation, Action<NetworkIdentity> OnBeforeInstantiating = null)
		{
			return Instantiate(prefab, -1, ownerId, isServer, position, rotation, (identity) =>
			{
				identity.Id = identity.GetInstanceID();
				OnBeforeInstantiating?.Invoke(identity);
			});
		}

		public static NetworkIdentity Instantiate(this NetworkIdentity prefab, int identityId, int ownerId, bool isServer, Vector3 position, Quaternion rotation, Action<NetworkIdentity> OnBeforeInstantiating = null)
		{
			// Assign exposed vars before instantiating prefab....
			prefab.gameObject.SetActive(false);
			if (prefab.SpawnMode == SpawnMode.Scene)
			{
				throw new Exception("Error: Instantiated NetworkIdentity should not have SpawnMode set to Scene. Use dynamic instead.");
			}

			NetworkIdentity identity = MonoBehaviour.Instantiate(prefab, position, rotation);
			identity.Id = identityId;
			identity.OwnerId = ownerId;
			identity.IsDynamic = true;
			identity.IsServer = isServer;
			OnBeforeInstantiating?.Invoke(identity);
			identity.gameObject.SetActive(true);
#if !UNITY_SERVER || UNITY_EDITOR
			if (isServer && OmniNetwork.Main.HasServer)
			{
				// Ignore physics between client object and server object
				SceneManager.MoveGameObjectToScene(identity.gameObject, OmniNetwork.Main.ServerScene.GetValueOrDefault());
			}
#endif
			// Unassign exposed vars after instantiating prefab....
			prefab.gameObject.SetActive(true);
			return identity;
		}

		public static T ReadCustomMessage<T>(this int value) where T : unmanaged, IComparable, IConvertible, IFormattable
		{
			return Unsafe.As<int, T>(ref value);
		}
	}
}