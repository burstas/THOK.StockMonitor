using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    public class ServerDao: BaseDao
    {
        public DataTable FindBatch()
        {
            string sql = @"SELECT TOP 1 BATCHID,ORDERDATE,BATCHNO  
                            FROM AS_BI_BATCH
                            WHERE ISUPTONOONEPRO='1' AND ISDOWNLOAD = '0'
                            ORDER BY ORDERDATE,BATCHNO";
            return ExecuteQuery(sql).Tables[0];
        }

        public DataTable FindStockChannel(string orderDate,string batchNo)
        {
            string sql = @"SELECT *
                            FROM AS_SC_STOCKCHANNELUSED 
                            WHERE ORDERDATE = '{0}' AND BATCHNO={1}";
            return ExecuteQuery(string.Format(sql, orderDate, batchNo)).Tables[0];
        }

        public DataTable FindStockMixChannel(string orderDate, string batchNo)
        {
            string sql = @"SELECT * 
                            FROM AS_SC_STOCKMIXCHANNEL 
                            WHERE ORDERDATE = '{0}' AND BATCHNO={1}";
            return ExecuteQuery(string.Format(sql, orderDate, batchNo)).Tables[0];
        }

        public DataTable FindChannelUSED(string orderDate, string batchNo)
        {
            string sql = @"SELECT A.* 
                            FROM AS_SC_CHANNELUSED A  
                            WHERE ORDERDATE='{0}' AND BATCHNO={1} ";
            return ExecuteQuery(string.Format(sql, orderDate, batchNo)).Tables[0];
        }

        public DataTable FindSupply(string orderDate, string batchNo)
        {
            string sql = @"SELECT A.*,LTRIM(RTRIM(B.BARCODE)) BARCODE 
                            FROM AS_SC_SUPPLY A
                            LEFT JOIN AS_BI_CIGARETTE B ON A.CIGARETTECODE=B.CIGARETTECODE 
                            WHERE ORDERDATE='{0}' AND BATCHNO = {1} 
                            ORDER BY LINECODE,SERIALNO";
            return ExecuteQuery(string.Format(sql, orderDate, batchNo)).Tables[0];
        }

        //~ 更新服务器分拣批次的补货数据的下载状态；
        public void UpdateBatchStatus(string batchID)
        {
            string sql = @"UPDATE AS_BI_BATCH SET ISDOWNLOAD='1' WHERE BATCHID={0}";
            ExecuteNonQuery(string.Format(sql, batchID));
        }

        //~ 更新卷烟条码信息到服务器；
        public void UpdateCigaretteToServer(string barcode, string cigaretteCode)
        {
            string sql = @"UPDATE AS_BI_CIGARETTE SET BARCODE = '{0}' WHERE CIGARETTECODE = '{1}'";
            ExecuteNonQuery(string.Format(sql, barcode, cigaretteCode));
        }
    }
}
