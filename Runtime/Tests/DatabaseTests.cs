using System.Threading.Tasks;
using Omni.Core;
using Omni.Execution;
using UnityEngine;

namespace Omni.Tests
{
    public class DatabaseTests : DBMSBehaviour
    {
        private DBMSManager DBMSManager { get; set; }
        override protected async void Start()
        {
            base.Start();
            DBMSManager = new((dbms) =>
            {
                // mariadb string connection
                string conn = "Server=localhost;Database=omni;Uid=root;Pwd=;";
                dbms.Initialize("users", DBMSOp.MariaDB, conn);
            }, 4, false)
            {
                enableTemporaryConnections = false
            };

            await Task.Run(() =>
            {
                DBMS dbms = DBMSManager.Get();
                // Create a table with 5 fields, id, name, age, email, and password
                // id auto increments and is the primary key
                dbms.Run("CREATE TABLE IF NOT EXISTS users (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(255), age INT, email VARCHAR(255), password VARCHAR(255))");
                dbms.Release();
            });

            for (int i = 0; i < 1000; i++)
            {
                RunSequentially(() =>
                {
                    try
                    {
                        var dbms = DBMSManager.Get();
                        // Insert a user into the table
                        dbms.Db.Insert(new
                        {
                            name = "John Doe",
                            age = 30,
                            email = "aaa@gmail",
                            password = "123456"
                        });
                        dbms.Release();
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogException(e);
                    }
                });
            }
        }
    }
}
