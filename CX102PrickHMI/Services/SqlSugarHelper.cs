using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SqlSugar;

namespace CX102PrickHMI.Services
{
   public abstract class SqlSugarHelper
    {

        protected static string connectionString = ConfigurationManager.ConnectionStrings["sqlite"].ConnectionString;//连接数据库字符串

        public static ConnectionConfig connectionConfig = new ConnectionConfig()
        {
            ConnectionString = connectionString,
            IsAutoCloseConnection = true,
            DbType = SqlSugar.DbType.Sqlite,
            MoreSettings = new ConnMoreSettings()
            {
                IsWithNoLockQuery = true,
            }
        };
    }
}
