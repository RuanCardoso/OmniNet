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
using System.Threading.Tasks;
using Neutron.Database;
using UnityEngine;

namespace Neutron.Core.Tests
{
    public class DatabaseTests : MonoBehaviour
    {
        private static string address = "localhost";
        private static string database = "usersdb";
        private static string username = "root";
        private static string password = "";
        private SGBDManager Manager;

        private void Start()
        {
            Manager = new((db) => db.Initialize("Users", SGDBType.MariaDB, $"Server={address};Database={database};Uid={username};Pwd={password};"), 2);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
            {
                for (int i = 0; i < 100; i++)
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
}