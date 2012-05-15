using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using THOK.MCP;
using THOK.MCP.View;
using THOK.Util;
using THOK.AS.Stocking.Dao;

namespace THOK.AS.Stocking.View
{
    public partial class ButtonArea : ProcessControl
    {
        public ButtonArea()
        {
            InitializeComponent();
        }

        private void btnExit_Click(object sender, EventArgs e)
        {
            if (btnStop.Enabled)
            {
                MessageBox.Show("先停止出库才能退出系统。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                return;
            }
            if (DialogResult.Yes == MessageBox.Show("您确定要退出备货监控系统吗？", "询问", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
            {
                Util.LogFile.DeleteFile();
                Application.Exit();
            }
        }

        private void btnOperate_Click(object sender, EventArgs e)
        {
            try
            {
                THOK.AF.Config config = new THOK.AF.Config();
                THOK.AF.MainFrame mainFrame = new THOK.AF.MainFrame(config);
                mainFrame.Context = Context;
                mainFrame.ShowInTaskbar = false;
                mainFrame.Icon = new Icon(@"./App.ico");
                mainFrame.ShowIcon = true;
                mainFrame.StartPosition = FormStartPosition.CenterScreen;
                mainFrame.WindowState = FormWindowState.Maximized;
                mainFrame.ShowDialog();
            }
            catch (Exception ee)
            {
                Logger.Error("操作作业处理失败，原因：" + ee.Message);
            }

        }

        private void btnDownload_Click(object sender, EventArgs e)
        {            
            try
            {
                DownloadData();
                Context.ProcessDispatcher.WriteToProcess("LEDProcess", "Refresh", null);
                Context.ProcessDispatcher.WriteToProcess("LedStateProcess", "Refresh", null);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
        }

        private void btnUpload_Click(object sender, EventArgs e)
        {
            string text = "手工更新卷烟条码信息！";
            string cigaretteCode = "";
            string barcode = "";

            Scan(text, cigaretteCode, barcode);
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            try
            {
                Context.ProcessDispatcher.WriteToProcess("OrderDataStateProcess", "Start", null);
                Context.ProcessDispatcher.WriteToProcess("LEDProcess", "Refresh", null);
                Context.ProcessDispatcher.WriteToProcess("LedStateProcess", "Refresh", null);
                SwitchStatus(true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex.Message);
            }
            
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Context.ProcessDispatcher.WriteToProcess("OrderDataStateProcess", "Stop", null);
            SwitchStatus(false);
        }

        private void btnHelp_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(this, "help.chm");
        }

        private void SwitchStatus(bool isStart)
        {
            btnDownload.Enabled = !isStart;
            btnUpload.Enabled = !isStart;
            btnStart.Enabled = !isStart;
            btnStop.Enabled = isStart;
            btnSimulate.Enabled = !isStart;
        }

        //不可用；
        private void btnSimulate_Click(object sender, EventArgs e)
        {
        }

        /// <summary>
        /// 下载数据 最后修改日期 2010-10-30
        /// </summary>
        private void DownloadData()
        {
            try
            {
                using (PersistentManager pm = new PersistentManager())
                {
                    DownloadDataDao downloadDataDao = new DownloadDataDao();
                    StockOutDao stockOutDao = new StockOutDao();
                    SupplyDao supplyDao = new SupplyDao();

                    if (supplyDao.FindCount() != stockOutDao.FindOutQuantity())
                        if (DialogResult.Cancel == MessageBox.Show("还有未处理的数据，您确定要重新下载数据吗？", "询问", MessageBoxButtons.OKCancel, MessageBoxIcon.Question))
                            return;

                    using (PersistentManager pmServer = new PersistentManager("ServerConnection"))
                    {
                        ServerDao serverDao = new ServerDao();
                        serverDao.SetPersistentManager(pmServer);

                        //ORDER BY ORDERDATE,BATCHNO  查找第一批次（符合已优化，并已上传一号工程，未下载的批次）
                        DataTable table = serverDao.FindBatch();
                        if (table.Rows.Count != 0)
                        {                           
                            string batchID = table.Rows[0]["BATCHID"].ToString();
                            string orderDate = table.Rows[0]["ORDERDATE"].ToString();
                            string batchNo = table.Rows[0]["BATCHNO"].ToString();
                            string totalQuantity = supplyDao.FindCount().ToString();

                            //Clear
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState("清空作业表", 5, 1));
                            downloadDataDao.Clear();
                            System.Threading.Thread.Sleep(100);

                            //AS_SC_STOCKCHANNEL
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState("下载补货烟道表", 5, 2));
                            downloadDataDao.InsertStockChannel(serverDao.FindStockChannel(orderDate, batchNo));
                            System.Threading.Thread.Sleep(100);

                            //AS_SC_STOCKMIXCHANNEL
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState("下载补货混合烟道表", 5, 3));
                            downloadDataDao.InsertStockMixChannel(serverDao.FindStockMixChannel(orderDate, batchNo));
                            System.Threading.Thread.Sleep(100);
                            //AS_SC_CHANNELUSED
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState("下载分拣烟道表", 5, 4));
                            downloadDataDao.InsertChannelUSED(serverDao.FindChannelUSED(orderDate, batchNo));
                            System.Threading.Thread.Sleep(100);
                            //AS_SC_SUPPLY
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState("下载补货计划表", 5, 5));
                            downloadDataDao.InsertSupply(serverDao.FindSupply(orderDate, batchNo));
                            System.Threading.Thread.Sleep(100);

                            serverDao.UpdateBatchStatus(batchID);
                            Context.ProcessDispatcher.WriteToProcess("monitorView", "ProgressState", new ProgressState());

                            string dataAndTip = "数据下载完成，订单日期：[{0}],批次号：[{1}],总数量：[{2}]";
                            Logger.Info(string.Format(dataAndTip,orderDate,batchNo,totalQuantity));

                            //初始化PLC数据（叠垛线PLC，补货线PLC）
                            Context.ProcessDispatcher.WriteToService("StockPLC_01", "RestartData", 3);
                            //初始化入库扫码器
                            Context.ProcessDispatcher.WriteToProcess("ScanProcess", "Init", null);
                            //初始化状态管理器
                            Context.ProcessDispatcher.WriteToProcess("LedStateProcess", "Init", null);
                            Context.ProcessDispatcher.WriteToProcess("OrderDataStateProcess", "Init", null);
                            Context.ProcessDispatcher.WriteToProcess("ScannerStateProcess", "Init", null);
                            //生成入库请求任务数据
                            Context.ProcessDispatcher.WriteToProcess("StockInRequestProcess", "FirstBatch", null);               
                        }
                        else
                            MessageBox.Show("没有补货计划数据！", "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.Error("数据下载处理失败，原因：" + e.Message);
            }
        }

        public delegate void ProcessStateInMainThread(StateItem stateItem);

        private void ProcessState(StateItem stateItem)
        {
            switch (stateItem.ItemName)
            {
                case "SimulateDialog":
                    string scannerCode = stateItem.State.ToString();
                    THOK.AS.Stocking.View.SimulateDialog simulateDialog = new THOK.AS.Stocking.View.SimulateDialog();
                    simulateDialog.Text = scannerCode + " 号扫码器手工扫码！";
                    if (simulateDialog.ShowDialog() == DialogResult.OK)
                    {
                        Dictionary<string, string>  parameters = new Dictionary<string, string>();
                        parameters.Add("barcode", simulateDialog.Barcode);                        
                        Context.ProcessDispatcher.WriteToProcess("ScanProcess", scannerCode, parameters);
                    }
                    Context.ProcessDispatcher.WriteToProcess("ScanProcess","ErrReset", "01");
                    break;
                case "ScanDialog":
                    Dictionary<string, string> scanParam = (Dictionary<string, string>)stateItem.State;
                    Scan(scanParam["text"], scanParam["cigaretteCode"], scanParam["barcode"]);
                    break;
                case "MessageBox":
                    Dictionary<string, object> msgParam = (Dictionary<string, object>)stateItem.State;
                    MessageBox.Show((string)msgParam["msg"], (string)msgParam["title"], (MessageBoxButtons)msgParam["messageBoxButtons"], (MessageBoxIcon)msgParam["messageBoxIcon"]);
                    break;
                default:
                    break;
            }
        }

        public void Scan(string text, string cigaretteCode, string barcode)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                StockOutDao outDao = new StockOutDao();
                SupplyDao supplyDao = new SupplyDao();

                if (barcode != string.Empty && supplyDao.Exist(barcode))
                    return;

                DataTable table = supplyDao.FindCigaretteAll(cigaretteCode);

                if (table.Rows.Count > 0)
                {
                    THOK.AS.Stocking.View.ScanDialog scanDialog = new THOK.AS.Stocking.View.ScanDialog(table);
                    scanDialog.setInformation(text, barcode);
                    if (scanDialog.ShowDialog() == DialogResult.OK)
                    {
                        if (scanDialog.IsPass && scanDialog.Barcode.Length == 6)
                        {
                            cigaretteCode = scanDialog.SelectedCigaretteCode;
                            barcode = scanDialog.Barcode;

                            using (PersistentManager pmServer = new PersistentManager("ServerConnection"))
                            {
                                ServerDao serverDao = new ServerDao();
                                serverDao.SetPersistentManager(pmServer);
                                serverDao.UpdateCigaretteToServer(barcode, cigaretteCode);
                            }
                            outDao.UpdateCigarette(barcode, cigaretteCode);
                        }
                        else
                        {
                            MessageBox.Show("验证码错误！", "消息", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        }
                    }
                }
            }
        }

        public override void Process(StateItem stateItem)
        {
            base.Process(stateItem);
            this.BeginInvoke(new ProcessStateInMainThread(ProcessState), stateItem);
        }

    }
}
