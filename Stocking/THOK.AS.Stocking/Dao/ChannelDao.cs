using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    public class ChannelDao: BaseDao
    {
        //~  查询补货烟道信息；
        public DataTable FindAll()
        {
            string sql = @"SELECT CHANNELCODE,CHANNELNAME,
                            CASE 
                                WHEN CHANNELTYPE='1' THEN '拆叠垛缓存烟道'
                                WHEN CHANNELTYPE='2' THEN '普通单一烟道（补通道机）' 
                                WHEN CHANNELTYPE='3' THEN '普通单一烟道（补立式机）' 
                                WHEN CHANNELTYPE='4' THEN '普通混合烟道' 
                                WHEN CHANNELTYPE='5' THEN '整件混合烟道' 
                                ELSE '无' 
                            END CHANNELTYPE,
                            CIGARETTECODE, CIGARETTENAME 
                            FROM AS_SC_STOCKCHANNELUSED";
            return ExecuteQuery(sql).Tables[0];
        }

        //~ 查询所有卷烟拆叠垛通道信息；
        public DataTable FindChannelForCigaretteCode(bool b1,bool b2)
        {
            string sql = @"SELECT A.CHANNELCODE,A.CIGARETTECODE,A.CIGARETTENAME,A.ISSTOCKIN,A.REMAINQUANTITY,B.BARCODE,
                            (
	                            SELECT COUNT(*) FROM AS_SC_SUPPLY C
	                            LEFT JOIN AS_SC_CHANNELUSED D
		                            ON C.ORDERDATE = D.ORDERDATE
		                            AND C.BATCHNO = D.BATCHNO
		                            AND C.LINECODE = D.LINECODE
		                            AND C.CHANNELCODE = D.CHANNELCODE
	                            WHERE D.CHANNELTYPE != '{0}' AND D.CHANNELTYPE != '{1}' AND C.CIGARETTECODE = A.CIGARETTECODE
                            ) -
                            (   
                                SELECT ISNULL(SUM(INQUANTITY),0) FROM AS_STOCK_IN_BATCH 
                                WHERE CIGARETTECODE = A.CIGARETTECODE
                            ) QUANTITY,
                            (
                                SELECT COUNT(*) FROM AS_STOCK_IN
                                WHERE (STOCKOUTID IS NULL OR STOCKOUTID =0) AND STATE =1 AND CHANNELCODE = A.CHANNELCODE
                            ) QUANTITY_1
                            FROM AS_SC_STOCKCHANNELUSED A 
                            LEFT JOIN AS_SC_SUPPLY B 
                                ON A.CIGARETTECODE = B.CIGARETTECODE
                            WHERE A.CHANNELTYPE = '1'
                            GROUP BY A.CHANNELCODE,A.CIGARETTECODE,A.CIGARETTENAME,A.ISSTOCKIN,A.REMAINQUANTITY,B.BARCODE";
            return ExecuteQuery(string.Format(sql, b1 ? 0 : 5, b2 ? 0 : 2)).Tables[0];
        }

        //~ 查询卷烟拆叠垛通道信息；
        public DataTable FindChannelForCigaretteCode(string cigaretteCode,bool b1,bool b2)
        {
            string sql = @"SELECT A.CHANNELCODE,A.CIGARETTECODE,A.CIGARETTENAME,A.ISSTOCKIN,A.REMAINQUANTITY,B.BARCODE,
                            (
	                            SELECT COUNT(*) FROM AS_SC_SUPPLY C
	                            LEFT JOIN AS_SC_CHANNELUSED D
		                            ON C.ORDERDATE = D.ORDERDATE
		                            AND C.BATCHNO = D.BATCHNO
		                            AND C.LINECODE = D.LINECODE
		                            AND C.CHANNELCODE = D.CHANNELCODE
	                            WHERE D.CHANNELTYPE != '{0}' AND D.CHANNELTYPE != '{1}' AND C.CIGARETTECODE = A.CIGARETTECODE
                            ) -
                            (   
                                SELECT ISNULL(SUM(INQUANTITY),0) FROM AS_STOCK_IN_BATCH 
                                WHERE CIGARETTECODE = A.CIGARETTECODE
                            ) QUANTITY,
                            (
                                SELECT COUNT(*) FROM AS_STOCK_IN
                                WHERE (STOCKOUTID IS NULL OR STOCKOUTID =0) AND STATE =1 AND CHANNELCODE = A.CHANNELCODE
                            ) QUANTITY_1
                            FROM AS_SC_STOCKCHANNELUSED A 
                            LEFT JOIN AS_SC_SUPPLY B 
                                ON A.CIGARETTECODE = B.CIGARETTECODE
                            WHERE A.CHANNELTYPE = '1' AND A.CIGARETTECODE = '{2}'
                            GROUP BY A.CHANNELCODE,A.CIGARETTECODE,A.CIGARETTENAME,A.ISSTOCKIN,A.REMAINQUANTITY,B.BARCODE";
            return ExecuteQuery(string.Format(sql, b1 ? 0 : 5, b2 ? 0 : 2, cigaretteCode)).Tables[0];
        }

        //~ 动态调整补货烟道；
        public void ReSetStockChannel(string channelCode, DataRow cigaretteRow)
        {
            SqlCreate sqlCreate = new SqlCreate("AS_SC_STOCKCHANNELUSED", SqlType.UPDATE);
            sqlCreate.AppendQuote("CIGARETTECODE", cigaretteRow["CIGARETTECODE"]);
            sqlCreate.AppendQuote("CIGARETTENAME", cigaretteRow["CIGARETTENAME"]);
            sqlCreate.AppendQuote("QUANTITY", cigaretteRow["QUANTITY"]);
            sqlCreate.AppendQuote("REMAINQUANTITY", cigaretteRow["REMAINQUANTITY"]);
            sqlCreate.AppendWhere("CHANNELCODE", channelCode);
            ExecuteNonQuery(sqlCreate.GetSQL());
        }

        #region 交换分拣烟道    
    
        public DataTable FindChannelUSED()
        {
            string sql = "SELECT CHANNELCODE, CHANNELNAME, " +
                            " CASE CHANNELTYPE WHEN '2' THEN '立式机' WHEN '5' THEN '混合烟道' ELSE '通道机' END CHANNELTYPE, " +
                            " LINECODE, CIGARETTECODE, CIGARETTENAME, QUANTITY " +
                            " FROM AS_SC_CHANNELUSED ORDER BY LINECODE, CHANNELNAME";
            return ExecuteQuery(sql).Tables[0];
        }
       
        public DataTable FindChannelUSED(string lineCode, string channelCode)
        {
            string sql = "SELECT * FROM AS_SC_CHANNELUSED WHERE CHANNELCODE='{0}' AND LINECODE = '{1}' ";
            return ExecuteQuery(string.Format(sql, channelCode, lineCode)).Tables[0];
        }

        internal DataTable FindEmptyChannel(string lineCode, string channelCode, object channelGroup, object channelType)
        {
            string sql = "SELECT CHANNELCODE, " +
                            " LINECODE + ' 线  ' + CHANNELNAME + ' ' + CASE CHANNELTYPE WHEN '2' THEN '立式机' WHEN '5' THEN '立式机'  ELSE '通道机' END CHANNELNAME " +
                            " FROM AS_SC_CHANNELUSED " +
                            " WHERE CHANNELTYPE IN ('{0}') AND CHANNELTYPE != '5' AND CHANNELGROUP = {1} AND CHANNELCODE != '{2}' AND LINECODE = '{3}' " +
                            " ORDER BY CHANNELNAME";
            return ExecuteQuery(string.Format(sql, channelType, channelGroup, channelCode, lineCode)).Tables[0];
        }

        public void UpdateChannelUSED(string lineCode, string channelCode, string cigaretteCode, string cigaretteName, int quantity, string sortNo)
        {
            string sql = "UPDATE AS_SC_CHANNELUSED SET CIGARETTECODE='{0}', CIGARETTENAME='{1}', QUANTITY={2}, SORTNO={3} "+
                            " WHERE CHANNELCODE='{4}' AND LINECODE = '{5}'";
            ExecuteNonQuery(string.Format(sql,cigaretteCode, cigaretteName, quantity, sortNo, channelCode, lineCode));
        }

        #endregion
    }
}
