using System;
using System.Collections.Generic;
using System.Text;
using THOK.MCP;
using THOK.Util;
using System.Data;
using THOK.AS.Stocking.Util;

namespace THOK.AS.Stocking.StateManage
{
    class OrderDataStateManage: BaseDao
    {
        private string stateItemCode = "";
        private string dataView = "";
        private int index = 0;
        private string plcServicesName = "";
        private string orderItemName = "";
        private string checkItemName = "";

        public OrderDataStateManage(string stateItemCode)
        {
            this.stateItemCode = stateItemCode;
            GetParameters();
        }

        public void GetParameters()
        {
            string sql = "SELECT * FROM AS_STATEMANAGER_ORDER WHERE STATECODE = '{0}'";
            sql = string.Format(sql, stateItemCode);
            DataTable table = ExecuteQuery(sql).Tables[0];

            this.dataView = table.Rows[0]["VIEWNAME"].ToString();
            this.index =Convert.ToInt32(table.Rows[0]["INDEXNO"].ToString());
            this.plcServicesName = table.Rows[0]["PLCSERVICESNAME"].ToString();
            this.orderItemName = table.Rows[0]["ORDERITEMNAME"].ToString();
            this.checkItemName = table.Rows[0]["CHECKITEMNAME"].ToString();
        }
      
        public bool Check(int index)
        {
            if (this.index + 1 != index)
            {
                string str = "������ˮ��У��������λ����ˮ��Ϊ��[{0}]��PLC��ˮ��Ϊ��[{1}]�����˹�ȷ�ϡ�";
                Logger.Error(string.Format(str, this.index+1, index));
                return false;
            }
            else
            {
                string str = string.Format("����д��ɹ�����ˮ�ţ�[{0}]",index);
                Logger.Info(str);
                return true;
            }
        }

        public bool MoveNext()
        {
            bool result = false;

            index++;
            string sql = "UPDATE AS_STATEMANAGER_ORDER SET INDEXNO = {0} WHERE STATECODE = '{1}'";
            sql = string.Format(sql, index, "01");
            ExecuteNonQuery(sql);

            result = true;
            return result;
        }

        public bool MoveTo(int index,IProcessDispatcher dispatcher)
        {
            bool result = false;

            this.index = index - 1;
            string sql = "UPDATE AS_STATEMANAGER_ORDER SET INDEXNO = {0} WHERE STATECODE = '01'";
            sql = string.Format(sql, index - 1, 01);
            ExecuteNonQuery(sql);            
    
            //дУ����ɱ�־��PLC
            dispatcher.WriteToService(plcServicesName,checkItemName,1);
            Logger.Info("����У�����");

            result = true;
            return result;
        }

        public bool WriteToPlc(IProcessDispatcher dispatcher)
        {
            bool result = false;
            //��PLCд�������� 
            //TODO �������ݼӹ�����
            if (dispatcher.WriteToService(plcServicesName,orderItemName, 2))
            {
                result = true;
            }
            return result;
        }
    }
}
