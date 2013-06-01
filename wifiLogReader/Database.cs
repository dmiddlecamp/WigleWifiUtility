using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.OleDb;
using System.Data.SQLite;
using System.Data;

namespace wifiLogReader
{
    public class Database
    {
        DbConnection conn = null;
        theDatabaseType myType = theDatabaseType.PSQL;

        public enum theDatabaseType { PSQL, MDB, MSSQL, SQLITE, NONE };

        public DbConnection openConnection(theDatabaseType dbType, string connString)
        {
            try
            {
                switch (dbType)
                {
                    case theDatabaseType.MSSQL:
                        return new SqlConnection(connString);
                    case theDatabaseType.MDB:
                        return new OleDbConnection(connString);

                    case theDatabaseType.SQLITE:
                        return new System.Data.SQLite.SQLiteConnection(connString);

                    case theDatabaseType.PSQL:
                    case theDatabaseType.NONE:
                    default:
                        return new Npgsql.NpgsqlConnection(connString);
                }
            }
            catch { }
            return null;
        }

        public DbDataAdapter openDBA(theDatabaseType dbType)
        {
            switch (dbType)
            {
                case theDatabaseType.MSSQL:
                    return new SqlDataAdapter();
                case theDatabaseType.MDB:
                    return new OleDbDataAdapter();

                case theDatabaseType.SQLITE:
                    return new SQLiteDataAdapter();

                case theDatabaseType.PSQL:
                case theDatabaseType.NONE:
                default:
                    return new Npgsql.NpgsqlDataAdapter();
            }
        }

        public DbCommandBuilder openBuilder(theDatabaseType dbType)
        {
            switch (dbType)
            {
                case theDatabaseType.MSSQL:
                    return new SqlCommandBuilder();
                case theDatabaseType.MDB:
                    return new OleDbCommandBuilder();

                case theDatabaseType.SQLITE:
                    return new SQLiteCommandBuilder();

                case theDatabaseType.PSQL:
                case theDatabaseType.NONE:
                default:
                    return new Npgsql.NpgsqlCommandBuilder();
            }
        }

        public DbDataAdapter makeDBA(theDatabaseType dbType, string selectQuery)
        {
            switch (dbType)
            {
                case theDatabaseType.MSSQL:
                case theDatabaseType.MDB:
                case theDatabaseType.PSQL:
                case theDatabaseType.SQLITE:
                case theDatabaseType.NONE:
                default:
                    DbCommand cmd = conn.CreateCommand();
                    cmd.CommandText = selectQuery;

                    DbDataAdapter dba = openDBA(dbType);
                    dba.SelectCommand = cmd;
                    return dba;
            }
        }


        public bool Connect()
        {
            if (conn == null)
            {
                return false;
            }
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    return (conn.State == ConnectionState.Open);
                }
                return true;
            }
            catch { }
            return false;
        }

        public DataTable RunQuery(string sql)
        {
            if (!Connect())
                return null;

            DataTable dt = new DataTable("results");

            try
            {
                //run query
                DbDataAdapter dba = makeDBA(myType, sql);

                dba.Fill(dt);

            }
            catch { }
            return dt;
        }



        public void Close()
        {
            if ((conn != null) && (conn.State == ConnectionState.Open))
                conn.Close();
        }

        public DataTable GetSchema()
        {
            DataTable dt = null;
            try
            {
                dt = conn.GetSchema("Tables");

                //DataView dv = new DataView(dt);
                //if (myType == theDatabaseType.PSQL)
                //{
                //    dv.RowFilter = "table_schema = 'public'";
                //}
            }
            catch { }
            return dt;
        }

        public void UpdateDatabase(DataTable dt, string selectSql)
        {
            DbDataAdapter dba = makeDBA(myType, selectSql);
            DbCommandBuilder builder = openBuilder(myType);
            builder.DataAdapter = dba;

            dba.InsertCommand = builder.GetInsertCommand();
            dba.UpdateCommand = builder.GetUpdateCommand();
            dba.DeleteCommand = builder.GetDeleteCommand();

            dba.UpdateBatchSize = 100;
            dba.Update(dt);
            dt.AcceptChanges();
        }





        public void Connect(theDatabaseType type, string connString)
        {
            myType = type;
            conn = openConnection(myType, connString);
            Connect();
        }

        //private void openMSAccessConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    myType = theDatabaseType.MDB;
        //    conn = openConnection(myType, txtConnString.Text);
        //    Connect();
        //}

        //private void openSQLServerConnectionToolStripMenuItem_Click(object sender, EventArgs e)
        //{
        //    myType = theDatabaseType.MSSQL;
        //    conn = openConnection(myType, txtConnString.Text);
        //    Connect();
        //}





    }
}
