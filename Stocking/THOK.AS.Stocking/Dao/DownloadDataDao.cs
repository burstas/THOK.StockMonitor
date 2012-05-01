using System;
using System.Collections.Generic;
using System.Text;
using THOK.Util;
using System.Data;

namespace THOK.AS.Stocking.Dao
{
    class DownloadDataDao : BaseDao
    {
        internal void Clear()
        {
            ExecuteNonQuery("TRUNCATE TABLE AS_SC_STOCKCHANNELUSED");
            ExecuteNonQuery("TRUNCATE TABLE AS_SC_STOCKMIXCHANNEL");
            ExecuteNonQuery("TRUNCATE TABLE AS_SC_CHANNELUSED");
            ExecuteNonQuery("TRUNCATE TABLE AS_SC_SUPPLY");

            ExecuteNonQuery("TRUNCATE TABLE AS_STOCK_IN_BATCH");
            ExecuteNonQuery("TRUNCATE TABLE AS_STOCK_IN");

            ExecuteNonQuery("TRUNCATE TABLE AS_STOCK_OUT_BATCH");
            ExecuteNonQuery("TRUNCATE TABLE AS_STOCK_OUT");

            ExecuteNonQuery("UPDATE AS_STATEMANAGER_ORDER SET ROW_INDEX = 0");
            ExecuteNonQuery("UPDATE AS_STATEMANAGER_LED SET ROW_INDEX = 0");
            ExecuteNonQuery("UPDATE AS_STATEMANAGER_SCANNER SET ROW_INDEX = 0");
        }

        internal void InsertStockChannel(DataTable channelTable)
        {
            BatchInsert(channelTable, "AS_SC_STOCKCHANNELUSED");
        }

        internal void InsertStockMixChannel(DataTable mixTable)
        {
            BatchInsert(mixTable, "AS_SC_STOCKMIXCHANNEL");
        }

        internal void InsertChannelUSED(DataTable channelTable)
        {
            BatchInsert(channelTable, "AS_SC_CHANNELUSED");
        }

        internal void InsertSupply(DataTable supplyTable)
        {
            BatchInsert(supplyTable, "AS_SC_SUPPLY");
        }
    }
}
