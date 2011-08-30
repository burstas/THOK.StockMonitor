using System;
using System.Collections.Generic;
using System.Text;
using THOK.MCP;
using THOK.Util;

namespace THOK.AS.Stocking.StateManage
{
    class LedProcess : AbstractProcess
    {
        /// <summary>
        /// ״̬�������б�
        /// </summary>
        private IDictionary<string,LedStateManage> ledStateManages = new Dictionary<string,LedStateManage>();
        private LedStateManage GetStateManage(string stateItemCode)
        {
            if (!ledStateManages.ContainsKey(stateItemCode))
            {
                lock (ledStateManages)
                {
                    if (!ledStateManages.ContainsKey(stateItemCode))
                    {
                        ledStateManages[stateItemCode] = new LedStateManage(stateItemCode);
                    }
                }                
            }
            return ledStateManages[stateItemCode];
        }

        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            /*
             * stateItem.Name �� ��Ϣ��Դ ��
             * stateItem.ItemName �� 
             *      ��Ӧ (1).StateItemCode_MoveNext /? ����PLC���ݵ�Ԫ���������ͨ����PLC���õ�ǰ����������ˮ�ţ��������ͨ����
             *           (2).StateItemCode_MoveTo   /? ����PLC���ݵ�Ԫ������������ݣ�PLC���õ�ǰ����������ˮ�ţ�����������ݣ�
             *           
             * stateItem.State ������PLC���ݿ����ˮ�š�
             */
            try
            {
                if (stateItem.ItemName == "Init")
                {
                    foreach (LedStateManage ledStateManage in ledStateManages.Values)
                    {
                        ledStateManage.MoveTo(1);
                    }
                    return;
                }

                using (PersistentManager pm = new PersistentManager())
                {
                    string stateItemCode = stateItem.ItemName.Split('_')[0];
                    string action = stateItem.ItemName.Split('_')[1];
                    LedStateManage ledStateManage = GetStateManage(stateItemCode);
                    int index = 0;
                    switch (action)
                    {
                        case "LedMoveNext":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && ledStateManage.Check(index))
                            {
                                if (ledStateManage.MoveTo(index))
                                {
                                    if (ledStateManage.MoveNext())
                                    {
                                        ledStateManage.WriteToPlc(dispatcher);
                                    }
                                }
                            }
                            break;
                        case "LedMoveTo":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0)
                            {
                                ledStateManage.MoveTo(index);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("LedProcess.StateChanged() ����ʧ�ܣ�ԭ��" + e.Message);
            }         
        }
    }
}
