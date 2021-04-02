using Npgsql;
using System;
using System.Collections.Generic;
using System.Text;

namespace IRM.SignalRTZ.DataAccess.Repositories
{
    //interface IBaseRepository
    //{
    //    NpgsqlConnection GetConnection(string connStr);
    //}

    public class BaseRepository
    {
        public NpgsqlConnection GetConnection(string connStr)
        {
            return new NpgsqlConnection(connStr);
        }
    }
}
