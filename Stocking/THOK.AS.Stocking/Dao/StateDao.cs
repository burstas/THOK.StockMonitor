using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.Util;

namespace THOK.AS.Stocking.Dao
{
    public class StateDao:BaseDao
    {
        /// <summary>
        /// ��ѯ����ɨ��״̬������
        /// </summary>
        /// <returns></returns>
        public DataTable FindStateQueryTypeTable()
        {
            string sql = "SELECT STATECODE,STATECODE + '|' + REMARK AS STATENAME FROM AS_STATEMANAGER_SCANNER";
            return ExecuteQuery(sql).Tables[0];
        }

        /// <summary>
        /// ����״̬��������Ų�����Ӧ��INDEXNO
        /// </summary>
        /// <param name="stateCode">״̬���������</param>
        /// <returns></returns>
        public DataTable FindScannerIndexNoByStateCode(string stateCode)
        {
            string sql = string.Format("SELECT INDEXNO FROM dbo.AS_STATEMANAGER_SCANNER WHERE STATECODE='{0}'",stateCode);
            return ExecuteQuery(sql).Tables[0];
        }

        /// <summary>
        /// ����INDEXNO��ѯ��������Ϣ
        /// </summary>
        /// <param name="indexNo">��ˮ��</param>
        /// <returns></returns>
        public DataTable FindOrderStateByIndexNo(string indexNo)
        {
            string sql = string.Format(@"SELECT ROW_INDEX,LINECODE,CIGARETTECODE,CIGARETTENAME,CHANNELCODE,
                            CASE CHANNELTYPE 
                                WHEN '3' THEN 'ͨ����'
                                WHEN '2' THEN '��ʽ��'
                                END CHANNELTYPENAME,
                            CASE 
                                WHEN ROW_INDEX < {0} THEN '��ɨ��'
                                WHEN ROW_INDEX = {0} THEN '��ɨ��'
                                WHEN ROW_INDEX > {0} THEN 'δɨ��'
                                END SCANNERSTATE
                            FROM V_STATE_SCANNER02 ",indexNo);
            return ExecuteQuery(sql).Tables[0];
        }
    }
}
