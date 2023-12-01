using Omni.Core;
using Omni.Core.Hashing;
using Omni.Execution;
using UnityEngine;

public class DatabaseTests : MonoBehaviour
{
	private DBMSManager DBMSManager;

	// Start is called before the first frame update
	void Start()
	{
		DBMSManager = new((db) =>
		{
			db.Initialize("users", DBMSOp.MySQL, "server=localhost;database=sevenday;pwd=;uid=root;");
		});

		var db = DBMSManager.Get();
		db.Run("CREATE TABLE IF NOT EXISTS users (ID INT AUTO_INCREMENT PRIMARY KEY, Usuario VARCHAR(255) NOT NULL, Senha VARCHAR(255) NOT NULL);");
		db._.Insert(new
		{
			Usuario = "Bruno",
			Senha = HashingUtility.Hash("12345", SecurityAlgorithm.BCrypt),
		});
		db.Release();
	}

	// Update is called once per frame
	void Update()
	{

	}
}
