using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    public class SupplyDao : BaseDao
    {
        //~ 查询补货计划总数量；
        public int FindCount()
        {
            return Convert.ToInt32(ExecuteScalar("SELECT COUNT(*) FROM AS_SC_SUPPLY"));
        }

        string sql;
        //~ 用于交换烟道时，更新烟道信息；
        public void UpdateChannelUSED(string lineCode, string sourceChannel, string targetChannel,string targetChannelGroupNo)
        {
            sql = "UPDATE AS_SC_SUPPLY SET CHANNELCODE='{0}',GROUPNO = '{1}' WHERE CHANNELCODE='{2}' AND LINECODE = '{3}' ";
            ExecuteNonQuery(string.Format(sql, targetChannel, targetChannelGroupNo, sourceChannel, lineCode));

            sql = "UPDATE AS_STOCK_OUT SET CHANNELCODE='{0}' WHERE CHANNELCODE='{2}' AND LINECODE = '{3}' ";
            ExecuteNonQuery(string.Format(sql, targetChannel, targetChannelGroupNo, sourceChannel, lineCode));
        }

        //~ 查询卷烟信息；
        internal DataTable FindCigaretteAll()
        {
            string sql = @"SELECT CIGARETTECODE,CIGARETTENAME 
                            FROM AS_SC_SUPPLY
                            GROUP BY CIGARETTECODE, CIGARETTENAME";
            return ExecuteQuery(sql).Tables[0];
        }

        //~ 查询卷烟信息；
        internal DataTable FindCigaretteAll(string cigaretteCode)
        {
            string sql = "";
            if (cigaretteCode == string.Empty)
            {
                return FindCigaretteAll();
            }
            else
            {
                sql = @"SELECT CIGARETTECODE,CIGARETTENAME,BARCODE 
                        FROM AS_SC_SUPPLY 
                        WHERE CIGARETTECODE = '{0}'
                        GROUP BY CIGARETTECODE,CIGARETTENAME,BARCODE";
                return ExecuteQuery(string.Format(sql, cigaretteCode)).Tables[0];
            }            
        }

        //~ 查询并判断条码是否存在；
        internal bool Exist(string barcode)
        {
            string sql = @"SELECT CIGARETTECODE,CIGARETTENAME 
                            FROM AS_SC_SUPPLY  
                            WHERE BARCODE = '{0}' 
                            GROUP BY CIGARETTECODE, CIGARETTENAME";
            return ExecuteQuery(string.Format(sql, barcode)).Tables[0].Rows.Count > 0;
        }

        //~ 查询新的补货计划；
        internal DataTable FindNextSupply(string lineCode, string channelGroup, string channelType, int sortNo)
        {
            string sql = @"SELECT TOP 3 * FROM
                            (
                                SELECT ROW_NUMBER() OVER(ORDER BY A.ORDERDATE,A.BATCHNO,A.LINECODE,A.SORTNO) ROW_INDEX,A.* 
                                    FROM AS_SC_SUPPLY A
                                    LEFT JOIN AS_SC_CHANNELUSED B 
                                        ON A.ORDERDATE = B.ORDERDATE 
                                        AND A.BATCHNO = B.BATCHNO 
                                        AND A.LINECODE = B.LINECODE 
                                        AND A.CHANNELCODE = B.CHANNELCODE
                                WHERE A.LINECODE ='{0}' AND A.CHANNELGROUP = '{1}' AND B.CHANNELTYPE IN ('{2}','{3}')
                            ) C
                            WHERE ROW_INDEX <= {4} AND SERIALNO NOT IN 
                                (SELECT SERIALNO 
                                    FROM AS_STOCK_OUT D 
                                    WHERE C.ORDERDATE = D.ORDERDATE 
                                        AND C.BATCHNO = D.BATCHNO 
                                        AND C.LINECODE = D.LINECODE 
                                )
                            ORDER BY ROW_INDEX";
            return ExecuteQuery(string.Format(sql, lineCode, channelGroup, channelType, channelType == "3" ? 4 : 0, sortNo)).Tables[0];
        }
    }
}