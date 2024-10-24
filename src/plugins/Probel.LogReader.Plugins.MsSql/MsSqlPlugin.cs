﻿using Dapper;
using Probel.LogReader.Core.Configuration;
using Probel.LogReader.Core.Constants;
using Probel.LogReader.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;


namespace Probel.LogReader.Plugins.MsSql
{
    public class MsSqlPlugin : PluginBase
    {
        #region Methods

        public override IEnumerable<DateTime> GetDays(OrderBy orderby = OrderBy.Desc)
        {
            var result = Query<DateTime>(Settings.QueryDay);

            switch (orderby)
            {
                case OrderBy.Asc: return result.OrderBy(e => e);
                case OrderBy.Desc: return result.OrderByDescending(e => e);
                case OrderBy.None: return result;
                default: throw new NotSupportedException($"The clause 'OrderBy.{orderby}' is not supported!");
            }
        }

        public override IEnumerable<LogRow> GetLogs(DateTime day, OrderBy orderby = OrderBy.Desc)
        {
            var result = Query<LogRow>(Settings.QueryLog, day);

            switch (orderby)
            {
                case OrderBy.Asc: return result.OrderBy(e => e.Time);
                case OrderBy.Desc: return result.OrderByDescending(e => e.Time);
                case OrderBy.None: return result;
                default: throw new NotSupportedException($"The clause 'OrderBy.{orderby}' is not supported!");
            }
        }

        private IEnumerable<T> Query<T>(string sql, DateTime? day = null)
        {
            var cString = Settings.ConnectionString;
            using (var c = new SqlConnection(cString))
            {
                var result = day.HasValue
                    ? c.Query<T>(sql, new { day })
                    : c.Query<T>(sql);
                return result;
            }
        }

        #endregion Methods
    }
}
