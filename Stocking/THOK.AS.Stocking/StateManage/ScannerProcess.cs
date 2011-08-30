using System;
using System.Collections.Generic;
using System.Text;
using THOK.MCP;
using THOK.Util;

namespace THOK.AS.Stocking.StateManage
{
    class ScannerProcess : AbstractProcess
    {
        /// <summary>
        /// ״̬�������б�
        /// </summary>
        private IDictionary<string, ScannerStateManage> scannerStateManages = new Dictionary<string, ScannerStateManage>();
        private ScannerStateManage GetStateManage(string stateItemCode)
        {
            if (!scannerStateManages.ContainsKey(stateItemCode))
            {
                lock (scannerStateManages)
                {
                    if (!scannerStateManages.ContainsKey(stateItemCode))
                    {
                        scannerStateManages[stateItemCode] = new ScannerStateManage(stateItemCode);
                    }
                }
            }
            return scannerStateManages[stateItemCode];
        }

        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            /*
             * stateItem.Name �� ��Ϣ��Դ 
             * stateItem.ItemName �� 
             *      ��Ӧ (1).StateItemCode_MoveNext /? ����PLC���ݵ�Ԫ���������ͨ����PLC���õ�ǰ����������ˮ�ţ��������ͨ����
             *           (2).StateItemCode_MoveTo   /? ����PLC���ݵ�Ԫ������������ݣ�PLC���õ�ǰ����������ˮ�ţ�����������ݣ�
             *           (3).StateItemCode_ShowData /? ����PLC���ݵ�Ԫ��������ʾ���ݣ�PLC���õ�ǰ����������ˮ�ţ�������ʾ���ݣ�
             *           
             * stateItem.State
             */
            try
            {
                using (PersistentManager pm = new PersistentManager())
                {
                    string stateItemCode = stateItem.ItemName.Split('_')[0];
                    string action = stateItem.ItemName.Split('_')[1];
                    ScannerStateManage scannerStateManage = GetStateManage(stateItemCode);
                    int index = 0;
                    switch (action)
                    {
                        case "MoveNext":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && scannerStateManage.Check(index))
                            {
                                if (scannerStateManage.MoveNext())
                                {
                                    scannerStateManage.WriteToPlc(dispatcher);
                                }
                            }
                            break;
                        case "MoveTo":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0)
                            {
                                scannerStateManage.MoveTo(index);
                            }
                            break;
                        case "ShowData":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && scannerStateManage.Check(index))
                            {
                                scannerStateManage.ShowData(index);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("ScannerProcess.StateChanged() ����ʧ�ܣ�ԭ��" + e.Message);
            }
        }
    }
}
