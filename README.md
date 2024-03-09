[![pt-br](https://img.shields.io/badge/lang-pt--br-green.svg)](https://github.com/RuanCardoso/OmniNet/blob/main/README.pt-br.md)

OmniNet - Networking at the Subatomic Level in Unity.

<img src="omniman.png" alt="omniman" width="400" height="400">
_______________________________________________________________________________

# OmniNet - A Unity Framework for Multiplayer Game Development

OmniNet is a Unity framework created with a strong focus on achieving high-performance in the development of multiplayer games.

**Requirements**: OmniNet requires Unity version 2021.2 or later. This dependency arises from Unity's integration of .NET Standard 2.1, which enhances Network Socket capabilities and introduces valuable features such as Span, Memory, ArrayPool, and more. I decided to leverage these new features to create a high-performance networking framework.

## Prerequisites

Before using this project, make sure you have the following dependency installed:

- [com.unity.nuget.newtonsoft-json](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)

This package is required for handling JSON serialization and deserialization in the OmniNet project. However, it's important to note that OmniNet utilizes binary serialization to achieve high performance in its operations.

You can install it through the Unity Package Manager by following these steps:

1. Open the Unity Editor.
2. Go to Window > Package Manager.
3. Click on the + button in the top-left corner of the Package Manager window.
4. Select **"Add package by name"**
5. Enter `com.unity.nuget.newtonsoft-json` as the package name.
6. Click **Add**.

However, to optimize performance in OmniNet, binary serialization is employed for its core operations, ensuring efficient data handling and processing.

## Features

- Performance-Driven Focus: At Omni, we uphold a steadfast commitment to high performance, approaching every task with optimization in mind.
- Data Flexibility: Send any type of data to the server, whether it's a primitive, class, structure, list, or dictionary - versatility is key.
- Remote Procedure Call (RPC): We facilitate seamless communication between systems through remote procedure calls, simplifying interaction among distributed components.
- NetVar: A powerful tool for managing network variables, allowing precise and efficient control over data transmitted between devices.
- Serialization Control: Utilize OnSend and OnReceive events to control data serialization, providing a flexible and customizable approach similar to Photon Networking's OnSerializeView.
- Custom Message Transmission: Easily send custom messages and large data blocks using our integrated transport system or via built-in HTTP servers.
- Database ORM with SqlKata & Dapper: Simplify database access and manipulation with our integration of SqlKata & Dapper, supporting a wide range of database management systems including SqlServer, MariaDb, MySql, PostgreSql, Oracle, SQLite, and Firebird.
- Streamlined HTTP Handling: We provide an intuitive solution for managing HTTP interactions, offering simplified handling for production-grade deployments without sacrificing ease of use.
- Real Web Server: For cases where simulation falls short, we provide a full-fledged web server for hosting and running applications in real production environments.
- Automatic Port Forwarding: We streamline the network setup process to ensure seamless communication, making it easy to access devices and services on local networks and the Internet.
- and much more.

## Documentation

[Documentation](../../wiki) - Here is the complete project documentation.
