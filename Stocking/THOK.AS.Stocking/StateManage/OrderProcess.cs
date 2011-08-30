using System;
using System.Collections.Generic;
using System.Text;

using THOK.MCP;
using THOK.Util;

namespace THOK.AS.Stocking.StateManage
{
    class OrderProcess:AbstractProcess
    {
        /// <summary>
        /// ״̬�������б�
        /// </summary>
        private IDictionary<string, OrderDataStateManage> orderDataStateManages = new Dictionary<string, OrderDataStateManage>();
        private OrderDataStateManage GetStateManage(string stateItemCode)
        {
            if (!orderDataStateManages.ContainsKey(stateItemCode))
            {
                lock (orderDataStateManages)
                {
                    if (!orderDataStateManages.ContainsKey(stateItemCode))
                    {
                        orderDataStateManages[stateItemCode] = new OrderDataStateManage(stateItemCode);
                    }
                }
            }
            return orderDataStateManages[stateItemCode];
        }

        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            /*
             * stateItem.Name �� ��Ϣ��Դ 
             * stateItem.ItemName �� 
             *      ��Ӧ (1).StateItemCode_MoveNext /? ����PLC���ݵ�Ԫ������д������PLC���õ�ǰ����������ˮ�ţ�����д�������ݣ�
             *           (2).StateItemCode_MoveTo   /? ����PLC���ݵ�Ԫ������������ݣ�PLC���õ�ǰ����������ˮ�ţ�����������ݣ�
             *           
             * stateItem.State  
             */
            try
            {
                if (stateItem.ItemName == "Init")
                {
                    foreach (OrderDataStateManage orderDataStateManage in orderDataStateManages.Values)
                    {
                        orderDataStateManage.MoveTo(1, dispatcher);
                    }
                    return;
                }

                using (PersistentManager pm = new PersistentManager())
                {
                    string stateItemCode = stateItem.ItemName.Split('_')[0];
                    string action = stateItem.ItemName.Split('_')[1];
                    OrderDataStateManage orderDataStateManage = GetStateManage(stateItemCode);
                    int index = 0;
                    switch (action)
                    {
                        case "OrderMoveNext":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && orderDataStateManage.Check(index))
                            {
                                if (orderDataStateManage.MoveNext())
                                {
                                    orderDataStateManage.WriteToPlc(dispatcher);
                                }
                            }
                            break;
                        case "OrderMoveTo":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0)
                            {
                                orderDataStateManage.MoveTo(index, dispatcher);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("OrderProcess.StateChanged() ����ʧ�ܣ�ԭ��" + e.Message);
            }
        }
    }
}
