using System.Threading.Tasks;
using Neutron.Core;
using Neutron.Database;
using UnityEngine;

public class DatabaseTests : MonoBehaviour
{
    private static string address = "ofgame.c0mgaov7q9jm.sa-east-1.rds.amazonaws.com";
    private static string database = "SevenDayOfSurvival";
    private static string username = "admin";
    private static string password = "ofGame945173!";
    private SGBDManager Manager = new((db) => db.Initialize("Users", SGDBType.MySQL, $"Server={address};Database={database};Uid={username};Pwd={password};"), 5);
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            for (int i = 0; i < 10; i++)
            {
                Task.Run(() =>
                {
                    var dB = Manager.Get();
                    dB.Db.Insert(new
                    {
                        Name = "Ruan",
                    });
                    Manager.Release(dB);
                });
            }
        }
    }
}