using System;
using System.Collections.Generic;
using System.Text;
using THOK.MCP;
using THOK.Util;
using THOK.AS.Stocking.StateManageProcess.Dao;

namespace THOK.AS.Stocking.StateManageProcess
{
    class ScannerStateProcess : AbstractProcess
    {
        /// <summary>
        /// 状态管理器列表
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
                        scannerStateManages[stateItemCode] = new ScannerStateManage(stateItemCode,this.Context.ProcessDispatcher);
                    }
                }
            }
            return scannerStateManages[stateItemCode];
        }

        protected override void StateChanged(StateItem stateItem, IProcessDispatcher dispatcher)
        {
            /*
             * stateItem.Name ： 消息来源 
             * stateItem.ItemName ： 
             *      对应 
             *           (0)Init                              ： 初始化（下载新数据时的初始化！）             * 
             *           (1)StateItemCode_ScannerMoveNext     ： 来自PLC数据单元，请求件烟通过，PLC将用当前经过件烟流水号，请求件烟通过！
             *           (2)StateItemCode_ScannerMoveTo       ： 来自PLC数据单元，请求较正数据，PLC将用当前经过件烟流水号，请求较正数据！
             *           (3)StateItemCode_ScannerShowData     ： 来自PLC数据单元，请求显示数据，PLC将用当前经过件烟流水号，请求显示数据！
             *           (4)StateItemCode(当stateItem.Name = “Scanner”) ： 来自扫码器的信息，数据为当前扫到的条码
             * stateItem.State 数据为当前扫到的条码
             *  
             */
            try
            {
                using (PersistentManager pm = new PersistentManager())
                {
                    string stateItemCode = "";
                    string action = "";

                    if (stateItem.ItemName == "Init")
                    {
                        ScannerStateManage scannerStateManage1 = new ScannerStateManage();
                        foreach (string stateCode in scannerStateManage1.GetStateItemCodeList())
                        {
                            GetStateManage(stateCode);
                        }

                        foreach (ScannerStateManage scannerStateManagesItem in scannerStateManages.Values)
                        {
                            scannerStateManagesItem.MoveTo(1);
                        }
                        return;
                    }

                    if (stateItem.ItemName == "Refresh")
                    {
                        foreach (ScannerStateManage scannerStateManagesItem in scannerStateManages.Values)
                        {
                            scannerStateManagesItem.ShowData();
                        }
                        return;
                    }

                    if (stateItem.Name == "Scanner")
                    {
                        stateItemCode = stateItem.ItemName;
                        action = "Scan";
                    }
                    else
                    {
                        stateItemCode = stateItem.ItemName.Split('_')[0];
                        action = stateItem.ItemName.Split('_')[1];
                    }

                    ScannerStateManage scannerStateManage = GetStateManage(stateItemCode);
                    int index = 0;
                    switch (action)
                    {
                        case "Scan":                                
                            if (stateItem.State is Dictionary<string, string> && ((Dictionary<string, string>)stateItem.State).ContainsKey("barcode"))
                            {
                                string barcode = ((Dictionary<string, string>)stateItem.State)["barcode"];
                                if (scannerStateManage.Check(barcode))
                                {
                                    if (scannerStateManage.MoveNext())
                                    {
                                        scannerStateManage.WriteToPlc(this.Context);
                                    }
                                    scannerStateManage.ShowData();
                                }
                            }
                            break;
                        case "ScannerMoveNext":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && scannerStateManage.Check(index))
                            {
                                if (scannerStateManage.MoveTo(index))
                                {
                                    if (scannerStateManage.MoveNext())
                                    {
                                        scannerStateManage.WriteToPlc(this.Context);
                                    }
                                }
                                scannerStateManage.ShowData();
                            }
                            break;
                        case "ScannerMoveTo":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0)
                            {
                                scannerStateManage.MoveTo(index);
                                Logger.Info(string.Format("{0} 号扫码器，校正完成,流水号：{1}", stateItemCode, index));
                                scannerStateManage.ShowData();
                            }
                            break;
                        case "ScannerShowData":
                            index = Convert.ToInt32(THOK.MCP.ObjectUtil.GetObject(stateItem.State));
                            if (index != 0 && scannerStateManage.Check(index))
                            {
                                scannerStateManage.ShowData(index-1);
                            }
                            break;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("ScannerStateProcess.StateChanged() 处理失败！原因：" + e.Message + e.StackTrace);
            }
        }
    }
}
