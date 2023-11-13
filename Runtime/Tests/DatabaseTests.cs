using System.Data;
using Omni.Core;
using UnityEngine;

namespace Omni.Tests
{
    public class DatabaseTests : MonoBehaviour
    {
        private DBMSManager DBMSManager { get; set; }
        void Start()
        {
            DBMSManager = new((dbms) =>
            {
                dbms.Initialize("users", DBMSOp.SQLite);
                if (dbms.Connection.State == ConnectionState.Open)
                {
                    // Create table for sqlite, fields: id, name, age
                    int result = dbms.Factory.Statement("CREATE TABLE IF NOT EXISTS users (id INTEGER PRIMARY KEY AUTOINCREMENT, name TEXT, age INTEGER)");
                    Debug.Log($"Create table result: {result}");
                }
            });
        }

        // Update is called once per frame
        void Update()
        {

        }
    }
}
