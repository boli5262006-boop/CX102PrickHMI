using CX102PrickHMI.Interfaces;
using CX102PrickHMI.Models;
using SqlSugar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CX102PrickHMI.Services
{
    public class AlarmService : SqlSugarHelper, IAlarmRepository
    {
        public int Insert(Alarms entity)
        {
            using (SqlSugarClient db = new SqlSugarClient(connectionConfig))
            {
                return  db.Insertable(entity).ExecuteCommand();
            }
        }

    }
}
