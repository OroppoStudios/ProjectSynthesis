using UnityEngine;
using Mono.Data.Sqlite;
using System.Data;
using System.IO;

namespace Warner
	{
	public class PointsManager: MonoBehaviour
		{
		#region MEMBER FIELDS

		public static PointsManager instance;

		private string path;

		private const string dbName = "points.sqlite";

		#endregion


		
		#region INIT STUFF
		
		private void Awake()
			{
			instance = this;

			path = Application.persistentDataPath+"/"+dbName;

			if (!File.Exists(path))
				{
				SqliteConnection.CreateFile(path);
				query("CREATE TABLE 'users' ('name'	TEXT UNIQUE,'points' INTEGER);");
				}			
			}			

		#endregion



		#region EXECUTE STUFF

		private DataTable query(string queryString)
			{
			DataTable dataTable = new DataTable();
			using (IDbConnection connection = (IDbConnection) new SqliteConnection("URI=file:"+path+";PRAGMA journal_mode=WAL;"))
				{
				connection.Open();
				using (IDbCommand cmd = connection.CreateCommand())
					{
					cmd.CommandText = queryString;			
					using (IDataReader reader = cmd.ExecuteReader())
						{
						dataTable.Load(reader);
						}
					}
				connection.Close();
				return dataTable;
				}			
			}

		#endregion



		#region POINTS STUFF

		public int getPoints(string userName)
			{
			int points = 0;
			userName = userName.ToLower();

			DataTable data = query("SELECT points FROM users WHERE name='"+userName+"'");

			if (data.Rows.Count==1)
				int.TryParse(data.Rows[0]["points"].ToString(),out points);

			return points;
			}

		public void addPoints(string userName,int points)
			{
			userName = userName.ToLower();
			query("INSERT OR REPLACE INTO users (name,points) VALUES ('"+userName+"',COALESCE((SELECT points FROM users WHERE name='"+userName+"'),0)+"+points+");");
			}


		public void removePoints(string userName,int points)
			{
			userName = userName.ToLower();
			query("INSERT OR REPLACE INTO users (name,points) VALUES ('"+userName+"',COALESCE((SELECT points FROM users WHERE name='"+userName+"'),0)-"+points+");");
			}

		#endregion
		}
	}