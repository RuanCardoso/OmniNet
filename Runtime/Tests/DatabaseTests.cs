using System;
using System.Threading.Tasks;
using MySqlConnector;
using Neutron.Core;
using Neutron.Database;
using UnityEngine;

public class DatabaseTests : MonoBehaviour
{
    private static string address = "localhost";
    private static string database = "usersdb";
    private static string username = "root";
    private static string password = "";
    private SGBDManager Manager;

    private void Start()
    {
        Manager = new((db) => db.Initialize("Users", SGDBType.MariaDB, $"Server={address};Database={database};Uid={username};Pwd={password};timeout=30;"), 100);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < 10; i++)
            {
                Task.Run(() =>
                {
                    try
                    {
                        var dB = Manager.Get();
                        int affectedRows = dB.Db.Insert(new
                        {
                            Name = "Ruan",
                        }, null, 1);
                        Neutron.Core.Logger.Print("Flushed");
                        Manager.Release(dB);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                });
            }
        }
    }

    private void OnApplicationQuit()
    {
        Manager.Close();
    }
}