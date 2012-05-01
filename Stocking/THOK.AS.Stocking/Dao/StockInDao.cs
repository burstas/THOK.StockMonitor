using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    class StockInDao:BaseDao 
    {
        //~
        public int FindMaxInID()
        {
            return Convert.ToInt32(ExecuteScalar("SELECT ISNULL(MAX(STOCKINID),0) FROM AS_STOCK_IN"));
        }

        //~
        public void Insert(int stockInID, int batchNo, string channelCode, string cigaretteCode, string cigaretteName, string barode, string state)
        {
            SqlCreate sqlCreate = new SqlCreate("AS_STOCK_IN", SqlType.INSERT);
            sqlCreate.Append("STOCKINID", stockInID);
            sqlCreate.Append("BATCHNO", batchNo);
            sqlCreate.AppendQuote("CHANNELCODE", channelCode);
            sqlCreate.AppendQuote("CIGARETTECODE", cigaretteCode);
            sqlCreate.AppendQuote("CIGARETTENAME", cigaretteName);
            sqlCreate.AppendQuote("BARCODE", barode);
            sqlCreate.AppendQuote("STATE", state);
            ExecuteNonQuery(sqlCreate.GetSQL());
        }

        //~
        public DataTable FindAll()
        {
            string sql = @"SELECT A.*,B.CHANNELNAME ,CASE WHEN STATE='0' THEN '未入库' ELSE '已入库' END STRSTATE 
                            FROM AS_STOCK_IN A
                            LEFT JOIN AS_SC_STOCKCHANNELUSED B ON A.CHANNELCODE = B.CHANNELCODE
                            ORDER BY STOCKINID";
            return ExecuteQuery(sql).Tables[0];
        }

        //~ 查询全部未入库计划；
        public DataTable FindCigarette()
        {
            string sql = @"SELECT TOP 1 A.* ,B.QUANTITY - B.INQUANTITY QUANTITY 
                            FROM AS_STOCK_IN A 
                            LEFT JOIN AS_STOCK_IN_BATCH B ON A.BATCHNO = B.BATCHNO
                            WHERE A.STATE = '0' AND B.STATE = '0' 
                            ORDER BY BATCHNO,STOCKINID";
            return ExecuteQuery(sql).Tables[0];
        }

        //~ 查询当前条码卷烟未入库计划；
        public DataTable FindCigarette(string barcode)
        {
            string sql = @"SELECT TOP 1 A.* ,B.QUANTITY - B.INQUANTITY QUANTITY 
                            FROM AS_STOCK_IN A 
                            LEFT JOIN AS_STOCK_IN_BATCH B ON A.BATCHNO = B.BATCHNO 
                            WHERE A.STATE = '0' AND B.STATE = '0' AND A.BARCODE = '{0}'
                            ORDER BY BATCHNO,STOCKINID";
            return ExecuteQuery(string.Format(sql, barcode)).Tables[0];
        }

        //~ 更新为已入库；
        public void UpdateScanStatus(string stockInID)
        {
            ExecuteNonQuery(string.Format("UPDATE AS_STOCK_IN SET STATE = '1' WHERE STOCKINID={0}", stockInID));
        }

        //~  查询入库未出库卷烟；
        public DataTable FindStockInForIsInAndNotOut()
        {
            string sql = @"SELECT * FROM AS_STOCK_IN
                            WHERE STATE = '1' AND (STOCKOUTID IS NULL OR STOCKOUTID = 0) ";
            return ExecuteQuery(sql).Tables[0];
        }

        //~  更新库存卷烟出库ID；
        public void UpdateStockOutIdToStockIn(DataTable table)
        {
            DataRow[] stockInRows = table.Select(string.Format("STOCKOUTID IS NOT NULL AND STOCKOUTID <> 0 "), "STOCKINID");
            foreach (DataRow row in stockInRows)
            {
                SqlCreate sqlCreate = new SqlCreate("AS_STOCK_IN", SqlType.UPDATE);
                sqlCreate.AppendQuote("STOCKOUTID", row["STOCKOUTID"]);
                sqlCreate.AppendWhere("STOCKINID", row["STOCKINID"]);
                ExecuteNonQuery(sqlCreate.GetSQL());
            }
        }
    }
}
