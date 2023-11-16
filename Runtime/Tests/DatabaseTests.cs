using System.Threading.Tasks;
using Newtonsoft.Json;
using Omni.Core;
using Omni.Core.Hashing;
using Omni.Execution;
using UnityEngine;

namespace Omni.Tests
{
    public class DatabaseTests : OmniDispatcher
    {
        private DBMSManager DBMSManager;
        /// <summary>
        /// Start the DBMSManager, and initialize the database connection.
        /// </summary>
        override protected async void Start()
        {
            base.Start();
            DBMSManager = new DBMSManager((_) => _.Initialize("users", DBMSOp.MariaDB, "Server=localhost;Database=omni;Uid=root;Pwd=;"), 4, true);
            Hash();
            //await CreateTable();
            // await Register();
            await GetUser();

            Dispatch(() =>
            {
                Debug.Log("Teste");
            });

            Debug.Log("Await finished!");
        }

        private void Update() {
            Process();
        }

        /// <summary>
        /// Create a table in the database with 4 columns (id, name, email, password).
        /// </summary>
        private Task CreateTable()
        {
            return RunAsync(() =>
            {
                var Db = DBMSManager.Get();
                Db.Run("CREATE TABLE IF NOT EXISTS users (id INT AUTO_INCREMENT PRIMARY KEY, name VARCHAR(255), email VARCHAR(255), password VARCHAR(255))");
                Db.Release();
            });
        }

        private Task Register()
        {
            return RunAsync(() =>
            {
                for (int i = 0; i < 10; i++)
                {
                    var Db = DBMSManager.Get();
                    Db.Db.Insert(new
                    {
                        name = "Ruan: " + i,
                        email = "teste@gmail.com",
                        password = "123456"
                    });
                    Db.Release();
                }
            });
        }

        private Task GetUser()
        {
            return RunAsync(() =>
            {
                var Db = DBMSManager.Get();
                var row = Db.Db.Select("id", "name", "email", "password").MapPageResultsTo<User>(2, 10);
                foreach (var item in row)
                {
                    Debug.Log(item.Id);
                }
                Db.Release();
            });
        }

        private void Hash()
        {
            string hash = HashingAlgorithm.Hash("ruan", SecurityAlgorithm.SHA512);
            Debug.Log(hash);
        }
    }

    [JsonObject(MemberSerialization.Fields)]
    class User
    {
        private int id;
        public int Id => id;

        private string name;
        public string Name => name;

        private string email;
        public string Email => email;
    }
}
