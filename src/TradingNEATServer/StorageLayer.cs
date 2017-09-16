using System;
using System.Data.SQLite;
using System.Collections.Generic;
using System.IO;

namespace TradingNEATServer
{
    public class StorageLayer
    {
        public static readonly string FILE_IO_PATH = @"C:\Users\alexl\OneDrive\Career\TradingTools\neat-trader\";

        private static StorageLayer instance;
        private static readonly string DB_FILE_NAME = FILE_IO_PATH + "TradingNEAT.db";

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
            bool databaseAlreadyExists = File.Exists(DB_FILE_NAME);
            this.connection = new SQLiteConnection($"Data Source={DB_FILE_NAME};Version=3");
            this.connection.Open();
            if (!databaseAlreadyExists)
            {
                this.initializeDatabase();
            }
        }

        private void initializeDatabase()
        {
            Dictionary<string, string> columns;

            // fitness_functions
            columns = new Dictionary<string, string>();
            columns["name"] = "TEXT";
            this.createTable("fitness_functions", columns, new string[] { });

            // training_sessions
            columns = new Dictionary<string, string>();
            columns["paused_at"] = "INTEGER";
            columns["transaction_fee"] = "REAL";
            columns["population_size"] = "INTEGER";
            this.createTable("training_sessions", columns, new string[] { "fitness_functions" });

            // generations
            columns = new Dictionary<string, string>();
            columns["finished_at"] = "INTEGER";
            this.createTable("generations", columns, new string[] { "training_sessions" });

            // data_sets
            columns = new Dictionary<string, string>();
            columns["seconds_between_each_data_point"] = "INTEGER";
            columns["base_currency"] = "TEXT";
            columns["target_currency"] = "TEXT";
            this.createTable("data_sets", columns, new string[] { });

            // data_points
            columns = Indicators.Instance.DBColumns;
            columns["close_price_0deriv"] = "REAL";
            columns["close_price_1deriv"] = "REAL";
            columns["close_price_2deriv"] = "REAL";
            columns["data_point_number"] = "INTEGER";
            this.createTable("data_points", columns, new string[] { "data_sets" });

            // data_sub_sets
            columns = new Dictionary<string, string>();
            columns["duration"] = "INTEGER"; // The total number of data_points to be selected from the data set.
            columns["name"] = "TEXT";
            columns["saved_at"] = "INTEGER"; // If 0, then that means it is not saved.
            this.createTable("data_sub_sets", columns, new string[] { "generations", "data_sets", "data_points" }); // The data_point_id column / foreign key will be for the starting point of the selected subset.

            // genomes
            columns = new Dictionary<string, string>();
            columns["structure"] = "BLOB";
            columns["saved_at"] = "INTEGER";
            columns["name"] = "TEXT";
            this.createTable("genomes", columns, new string[] { "generations" });

            // genome_data_sub_sets
            columns = new Dictionary<string, string>();
            columns["fitness"] = "REAL";
            columns["gain_factor"] = "REAL";
            columns["trades"] = "BLOB"; // JSON array of the trades made by the genome on the given data_sub_set.
            columns["transaction_fee"] = "REAL";
            columns["finished_at"] = "INTEGER";
            this.createTable("genome_data_sub_sets", columns, new string[] { "genomes", "data_sub_sets", "fitness_functions" });
        }

        private void createTable(string tableName, Dictionary<string, string> columns, string[] tableReferences)
        {
            columns["id"] = "INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT";
            columns["created_at"] = "INTEGER";
            columns["updated_at"] = "INTEGER";
            string[] tableProperties = new string[columns.Count + 2 * tableReferences.Length]; // For each table reference, there is a column for the reference column itself, and another table property where the foreign key is established on that column.
            int columnIndex = 0, foreignKeyIndex = tableProperties.Length - 1;
            foreach(KeyValuePair<string, string> pair in columns)
            {
                tableProperties[columnIndex++] = pair.Key + " " + pair.Value;
            }
            foreach(string tableReference in tableReferences)
            {
                if (tableReference.Length == 0) throw new Exception("All tableReferences should be a table name with a length of at least one character.");
                string singularTableName = tableReference.Substring(0, tableReference.Length - 1);
                tableProperties[columnIndex++] = $"{singularTableName}_id INTEGER";
                tableProperties[foreignKeyIndex--] = $"FOREIGN KEY ({singularTableName}_id) REFERENCES {tableReference}(id)";
            }
            if (columnIndex - 1 != foreignKeyIndex)
            {
                throw new Exception("Error in createTable method od StorageLayer");
            }
            string sql = $"CREATE TABLE IF NOT EXISTS {tableName} ({string.Join(", ", tableProperties)})";
            SQLiteCommand command = new SQLiteCommand(sql, this.connection);
            command.ExecuteNonQuery();
        }

        public void insertIntoTable(string table, Dictionary<string, DBValue> data)
        {
            string[] columns = new string[data.Count];
            string[] values = new string[data.Count];
            int i = 0;
            foreach(KeyValuePair<string, DBValue> pair in data)
            {
                columns[i] = pair.Key;
                values[i++] = pair.Value.ToString();
            }
            string sql = $"INSERT INTO {table} ({string.Join(", ", columns)}, created_at, updated_at) values ({string.Join(", ", values)}, strftime('%s', 'now'), strftime('%s', 'now'))";
            SQLiteCommand command = new SQLiteCommand(sql, this.connection);
            if (command.ExecuteNonQuery() != 1) throw new Exception("INSERT INTO command apparently failed.");
        }

        public void updateTableWhere(string table, List<WhereClause> wheres, Dictionary<string, DBValue> data)
        {
            // string 
        }

        public void deleteFromTableWhere(string table )
        {

        }

        public class WhereClause
        {
            public enum ComparisonSymbol { LT, LTE, GT, GTE, EQ };
            private string column;
            private ComparisonSymbol comparisonSymbol;
            private DBValue value;

            public WhereClause(string column, ComparisonSymbol comparisonSymbol, DBValue value)
            {
                this.column = column;
                this.comparisonSymbol = comparisonSymbol;
                this.value = value;
            }

            public override string ToString()
            {
                string comparisonSymbolString;
                switch(this.comparisonSymbol)
                {
                    case ComparisonSymbol.EQ:
                        comparisonSymbolString = "==";
                        break;
                    case ComparisonSymbol.GT:
                        comparisonSymbolString = ">";
                        break;
                    case ComparisonSymbol.GTE:
                        comparisonSymbolString = ">=";
                        break;
                    case ComparisonSymbol.LT:
                        comparisonSymbolString = "<";
                        break;
                    case ComparisonSymbol.LTE:
                        comparisonSymbolString = "<=";
                        break;
                    case default:
                        throw new Exception("this.comparisonSymbol took on an invalid value");
                }
                return $"WHERE {column} {comparisonSymbolString} {value.ToString()}";
            }
        }

        public class DBValue
        {
            private readonly string stringValue;
            private readonly long longValue;
            private readonly double doubleValue;
            private enum TYPE { STRING, LONG, DOUBLE };
            private readonly TYPE type;

            public DBValue(string val) { this.stringValue = val; type = TYPE.STRING; }
            public DBValue(long val) { this.longValue = val; type = TYPE.LONG; }
            public DBValue(double val) { this.doubleValue = val; type = TYPE.DOUBLE; }

            public override string ToString()
            {
                switch (this.type)
                {
                    case TYPE.STRING:
                        return "'" + this.stringValue + "'";
                    case TYPE.LONG:
                        return "" + this.longValue;
                    case TYPE.DOUBLE:
                        return "" + this.doubleValue;
                    default:
                        throw new Exception("this.type was not STRING, LONG, or DOUBLE, which should be the only possible types taken on by a DBValue.");
                }
            }
        }

    }
}