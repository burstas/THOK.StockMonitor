using System;
using System.Collections.Generic;
using System.Text;

using THOK.MCP;
using THOK.Util;
using System.Data;
using THOK.AS.Stocking.Util;

namespace THOK.AS.Stocking.StateManage
{
    class ScannerStateManage:BaseDao
    {
        private string stateItemCode = "";
        private string fixtureCode = "";
        private string dataView = "";
        private int index = 0;
        private string plcServicesName="";
        private string releaseItemName="";

        internal class LedItem
        {
            public string Name;
            public int Count = 0;
            public override string ToString()
            {
                return string.Format("{0}-{1}", Count > 0 ? Count.ToString().PadLeft(2, ' ') : "", Name);
            }
        }

         public ScannerStateManage(string stateItemCode)
        {
            this.stateItemCode = stateItemCode;
            GetParameters();
        }

        public void GetParameters()
        {
            string sql = "SELECT * FROM AS_STATEMANAGER_SCANNER WHERE STATECODE = '{0}'";
            sql = string.Format(sql, stateItemCode);
            DataTable table = ExecuteQuery(sql).Tables[0];
            this.index = Convert.ToInt32(table.Rows[0]["INDEXNO"].ToString());
        }

        public bool Check(int index)
        {
            GetParameters();
            if (this.index + 1 != index)
            {
                string strErr = "{0}ɨ����ˮ�ż���������λ����ˮ��Ϊ��[{1}]��PLC��ˮ��Ϊ��[{2}]�����˹�ȷ�ϡ� ";
                Logger.Error(string.Format(strErr, fixtureCode, this.index + 1, index));

                Stack<LedItem> data = new Stack<LedItem>();

                LedItem item = new LedItem();
                item.Name = string.Format("{0}ɨ������ˮ�ż�������", fixtureCode);
                data.Push(item);

                item = new LedItem();
                item.Name = string.Format("��λ����ǰ��ˮ��Ϊ{0},", this.index + 1);
                data.Push(item);

                item = new LedItem();
                item.Name = string.Format("PLC��ǰ��ˮ��Ϊ{0};", index);
                data.Push(item);

                LedItem[] ledItems = data.ToArray();
                Array.Reverse(ledItems);

                Show(ledItems);
                return false;
            }
            else
            {
                string str = string.Format("ɨ��д��ɹ�����ˮ�ţ�[{0}]", index);
                Logger.Info(str);
                return true;
            }
        }

        public bool MoveNext()
        {
            bool result = false;

            index++;
            string sql = "UPDATE AS_STATEMANAGER_SCANNER SET INDEXNO = {0} WHERE STATECODE = '{1}'";
            sql = string.Format(sql, index, "01");
            ExecuteNonQuery(sql);

            result = true;
            return result;
        }

        public bool MoveTo(int index)
        {
            bool result = false;

            this.index = index - 1;
            string sql = "UPDATE AS_STATEMANAGER_SCANNER SET INDEXNO = {0} WHERE STATECODE = '{1}'";
            sql = string.Format(sql, index - 1, "01");
            ExecuteNonQuery(sql);
            Logger.Info("ɨ��У�����");

            result = true;
            return result;
        }

        public bool ShowData(int index)
        {
            if (Check(index))
            {
                Stack<LedItem> data = new Stack<LedItem>();

                LedItem item = new LedItem();
                item.Name = string.Format("{0}��ɨ������ǰ����Ϊ��",fixtureCode);
                data.Push(item);

                item = new LedItem();
                item.Name = string.Format("");
                data.Push(item);

                LedItem[] ledItems = data.ToArray();
                Array.Reverse(ledItems);

                Show(ledItems);
            }
            return true;
        }

        public bool WriteToPlc(IProcessDispatcher dispatcher)
        {
            bool result = false;

            if (dispatcher.WriteToService("StockPLC_02", "Release", 2))
            {
                result = true;
            }

            return result;
        }

        public void Show(LedItem[] ledItems)
        {
            //todo;led.show(fixtureCode,ledItems);
        } 
    }
}
