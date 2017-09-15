using System.Data.SQLite;
using System.Collections.Generic;

namespace TradingNEATServer
{
    public class StorageLayer
    {
        private static StorageLayer instance;
        private static readonly string DB_FILE_NAME = "TradingNEAT.db";

        private SQLiteConnection connection;

        public static StorageLayer Instance
        {
            get
            {
                if (StorageLayer.instance == null) StorageLayer.instance = new StorageLayer();
                return StorageLayer.instance;
            }
        }

        private StorageLayer()
        {
            try
            {
                this.connection = new SQLiteConnection($"Data Source={DB_FILE_NAME};Version=3");
                this.connection.Open();
            } catch
            {
                this.initializeDatabase();
            }
        }

        private void initializeDatabase()
        {
            SQLiteConnection.CreateFile(DB_FILE_NAME);
            this.connection = new SQLiteConnection($"Data Source={DB_FILE_NAME};Version=3");
            this.connection.Open();

        }

        private void createTable(string tableName, Dictionary<string, string> columns)
        {
            string sql = $"CREATE TABLE {tableName} ({})";
        }

    }
}