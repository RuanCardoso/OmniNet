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
using System.Linq;
using System.Threading;
using static Omni.Core.Enums;

namespace Omni.Core
{
    /// <summary>
    /// This class is responsible for managing the console.
    /// Enable console commands to be executed.
    /// </summary>
    internal class OmniConsole
    {
        internal static void Initialize(CancellationToken cancellationToken, OmniNetwork self)
        {
            new Thread(() =>
            {
                Dictionary<string, string> commands = new();
                OmniLogger.Print("Press 'Enter' to write a command!");
                OmniLogger.Print("Ex: Ban -user Ruan -days 300");
                OmniLogger.Print("Press 'ESC' to exit!");

                while (!cancellationToken.IsCancellationRequested)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Enter:
                            {
                                OmniLogger.Print("Write the command:");
                                string command = Console.ReadLine();
                                commands.Clear();
                                switch (command)
                                {
                                    case "Clear":
                                    case "clear":
                                        {
                                            Console.Clear();
                                        }
                                        break;
                                    case "GC Collect":
                                    case "gc collect":
                                        {
                                            GC.Collect();
                                            OmniLogger.Print("GC: Collected memory!");
                                        }
                                        break;
                                    case "Memory":
                                    case "memory":
                                        {
                                            ulong totalBytesOfMemoryUsed = (ulong)GC.GetTotalMemory(false);
                                            OmniLogger.Print($"Allocated managed memory: {totalBytesOfMemoryUsed.ToSizeUnit(SizeUnits.MB)} MB | {totalBytesOfMemoryUsed.ToSizeUnit(SizeUnits.GB)} GB");
                                        }
                                        break;
                                    case "fps":
                                    case "FPS":
                                        {
                                            OmniLogger.Print($"FPS: {OmniNetwork.Framerate}");
                                        }
                                        break;
                                    case "Time":
                                    case "time":
                                        {
                                            OmniLogger.Print($"Time: {OmniTime.LocalTime}");
                                            OmniLogger.Print($"Date/Hour: {DateTime.UtcNow}");
                                        }
                                        break;
                                    default:
                                        {
                                            if (!string.IsNullOrEmpty(command))
                                            {
                                                int paramsCount = 0;
                                                string[][] parameters = command.Split('-').Select(x => x.Split()).ToArray(); // Split the command by '-' and then by ' '.
                                                if (parameters.Length <= 1)
                                                {
                                                    OmniLogger.Print("Continuing execution without provided parameters.");
                                                }
                                                else
                                                {
                                                    for (int i = 1; i < parameters.Length; i++)
                                                    {
                                                        if (parameters[i].IsInBounds(0) && parameters[i].IsInBounds(1))
                                                        {
                                                            string parameter = parameters[i][0];
                                                            string value = parameters[i][1];
                                                            if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(value))
                                                            {
                                                                OmniLogger.Print("Continuing execution without provided parameters.");
                                                            }
                                                            else
                                                            {
                                                                paramsCount++;
                                                                // Add or update the parameter and value.
                                                                if (!commands.TryAdd(parameter, value))
                                                                {
                                                                    commands[parameter] = value;
                                                                }
                                                            }
                                                        }
                                                        else
                                                        {
                                                            OmniLogger.PrintError("Invalid parameters!");
                                                            break;
                                                        }
                                                    }

                                                    // eg: Ban -user Ruan -days 300
                                                }

                                                command = parameters[0][0];
                                                OmniLogger.Print($"Command executed: '{command}' | parameter count: {paramsCount}");
                                            }
                                            else
                                            {
                                                OmniLogger.PrintError("There are no commands!");
                                            }
                                        }
                                        break;
                                }
                            }
                            break;
                        case ConsoleKey.Escape:
                            {
                                OmniLogger.Print("Exiting...");
                                self.OnApplicationQuit();
                                UnityEngine.Application.Quit(0);
                            }
                            break;
                        default:
                            OmniLogger.Print($"There is no command for the '{key}' key");
                            break;
                    }
                }
            })
            {
                Name = "Omni_Console_Th",
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
            }.Start();
        }
    }
}