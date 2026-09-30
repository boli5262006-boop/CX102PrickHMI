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

        public List<Alarms> GetByTimeRange(DateTime startInclusive, DateTime endExclusive)
        {
            using (SqlSugarClient db = new SqlSugarClient(connectionConfig))
            {
                return db.Queryable<Alarms>()
                    .Where(a => a.InsertTime >= startInclusive && a.InsertTime < endExclusive)
                    .OrderBy(a => a.InsertTime)
                    .OrderBy(a => a.AlarmState)
                    .ToList();
            }
        }

    }
}
