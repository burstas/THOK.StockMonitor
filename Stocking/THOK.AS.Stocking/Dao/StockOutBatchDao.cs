using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    public class StockOutBatchDao: BaseDao
    {
        public DataTable FindAll()
        {
            //todo
            string sql = @"SELECT A.BATCHNO,CASE WHEN A.LINECODE='00' THEN '全部' ELSE A.LINECODE END LINECODE,
                            A.QUANTITY, A.OUTQUANTITY,B.ORDERDATE,B.BATCHNO SORTBATCHNO
                            FROM AS_STOCK_OUT_BATCH A
                            LEFT JOIN AS_SC_SUPPLY B ON A.SORTNO = B.SORTNO 
                            GROUP BY A.BATCHNO,A.LINECODE,A.QUANTITY, A.OUTQUANTITY,B.ORDERDATE,B.BATCHNO";
            return ExecuteQuery(sql).Tables[0];
        }

        //~ 查询当前最大补货批次；
        internal int FindMaxBatchNo()
        {
            return Convert.ToInt32(ExecuteScalar("SELECT ISNULL(MAX(BATCHNO),0) FROM AS_STOCK_OUT_BATCH "));
        }

        //~ 生成补货批次；
        internal void InsertBatch(int batchNo, string lineCode, string channelGroup, string channelType, int sortNo, int quantity)
        {
            SqlCreate sqlCreate = new SqlCreate("AS_STOCK_OUT_BATCH", SqlType.INSERT);
            sqlCreate.Append("BATCHNO", batchNo);
            sqlCreate.AppendQuote("LINECODE", lineCode);
            sqlCreate.Append("CHANNELGROUP", channelGroup);
            sqlCreate.Append("CHANNELTYPE", channelType);
            sqlCreate.Append("SORTNO", sortNo);            
            sqlCreate.Append("QUANTITY", quantity);
            sqlCreate.Append("OUTQUANTITY", 0);
            string sql = sqlCreate.GetSQL();
            ExecuteNonQuery(sql);
        }
    }
}
