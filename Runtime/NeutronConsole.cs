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
using static Neutron.Core.Enums;

namespace Neutron.Core
{
    internal class NeutronConsole
    {
        internal static void Initialize(CancellationToken token, NeutronNetwork network)
        {
            new Thread(() =>
            {
                Dictionary<string, string> commands = new();
                Logger.Print("Press 'Enter' to write a command!");
                Logger.Print("Ex: Ban -user Ruan -days 300");
                Logger.Print("Press 'ESC' to exit!");
                while (!token.IsCancellationRequested)
                {
                    var key = Console.ReadKey(true).Key;
                    switch (key)
                    {
                        case ConsoleKey.Enter:
                            Logger.Print("Write the command:");
                            string command = Console.ReadLine();
                            commands.Clear();
                            switch (command)
                            {
                                case "Clear":
                                case "clear":
                                    Console.Clear();
                                    break;
                                case "GC Collect":
                                case "gc collect":
                                    GC.Collect();
                                    Logger.Print("Collected");
                                    break;
                                case "Memory":
                                case "memory":
                                    long totalBytesOfMemoryUsed = GC.GetTotalMemory(false);
                                    Logger.Print($"Allocated managed memory: {totalBytesOfMemoryUsed.ToSizeUnit(SizeUnits.MB)} MB | {totalBytesOfMemoryUsed.ToSizeUnit(SizeUnits.GB)} GB");
                                    break;
                                case "fps":
                                case "FPS":
                                    Logger.Print($"FPS: {NeutronNetwork.framerate}");
                                    break;
                                case "Time":
                                case "time":
                                    Logger.Print($"Time: {NeutronTime.LocalTime}");
                                    Logger.Print($"Date/Hour: {DateTime.UtcNow}");
                                    break;
                                default:
                                    {
                                        if (!string.IsNullOrEmpty(command))
                                        {
                                            int paramsCount = 0;
                                            string[][] parameters = command.Split('-').Select(x => x.Split()).ToArray();
                                            if (parameters.Length <= 1) Logger.Print("Continuous execution without parameters!");
                                            else
                                            {
                                                for (int i = 1; i < parameters.Length; i++)
                                                {
                                                    if (parameters[i].IsInBounds(0) && parameters[i].IsInBounds(1))
                                                    {
                                                        string parameter = parameters[i][0];
                                                        string value = parameters[i][1];
                                                        if (string.IsNullOrEmpty(parameter) || string.IsNullOrEmpty(value))
                                                            Logger.Print("Continuous execution without parameters!");
                                                        else
                                                        {
                                                            paramsCount++;
                                                            if (!commands.TryAdd(parameter, value))
                                                                commands[parameter] = value;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        Logger.PrintError("Invalid parameters!");
                                                        break;
                                                    }
                                                }
                                            }

                                            command = parameters[0][0];
                                            Logger.Print($"Command executed: '{command}' | parameter count: {paramsCount}");
                                        }
                                        else Logger.PrintError("There are no commands!");
                                    }
                                    break;
                            }
                            break;
                        case ConsoleKey.Escape:
                            Logger.Print("Exiting...");
                            network.OnApplicationQuit();
                            UnityEngine.Application.Quit(0);
                            break;
                        default:
                            Logger.Print($"There is no command for the '{key}' key");
                            break;
                    }
                }
            })
            {
                Name = "Neutron_Console_Th",
                IsBackground = true,
                Priority = ThreadPriority.Lowest,
            }.Start();
        }
    }
}