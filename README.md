[![pt-br](https://img.shields.io/badge/lang-pt--br-green.svg)](https://github.com/RuanCardoso/OmniNet/blob/main/README.pt-br.md)

OmniNet - Networking at the Subatomic Level in Unity.

<img src="omniman.png" alt="omniman" width="400" height="300">
_______________________________________________________________________________

# OmniNet - A Unity Framework for Multiplayer Game Development

OmniNet is a Unity framework created with a strong focus on achieving high-performance in the development of multiplayer games.

**Requirements**: OmniNet is limited to Unity version 2021.2 or newer. This is because Unity introduced Support for .Net Standard 2.1 starting from version 2021.2, which brings improvements to the Network Socket and introduces new features like Span, Memory, ArrayPool, and more.

I decided to leverage these new features to create a high-performance networking framework.

## Prerequisites

Before using this project, make sure you have the following dependency installed:

- [com.unity.nuget.newtonsoft-json](https://github.com/jilleJr/Newtonsoft.Json-for-Unity)

This package is required for handling JSON serialization and deserialization in the OmniNet project. However, it's important to note that OmniNet utilizes binary serialization to achieve high performance in its operations.

You can install it through the Unity Package Manager by following these steps:

1. Open the Unity Editor.
2. Go to Window > Package Manager.
3. Click on the + button in the top-left corner of the Package Manager window.
4. Select **Add package by name....**
5. Enter `com.unity.nuget.newtonsoft-json` as the package name.
6. Click **Add**.

However, for optimized performance with OmniNet, it utilizes binary serialization instead of JSON for its core operations.

## Documentation

[Documentation](../../wiki) - Here is the complete project documentation.
