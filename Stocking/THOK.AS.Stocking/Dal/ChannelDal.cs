using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using THOK.AS.Stocking.Dao;
using THOK.Util;

namespace THOK.AS.Stocking.Dal
{
    public class ChannelDal
    {
        public DataTable GetAllChannel()
        {
            using (PersistentManager pm = new PersistentManager())
            {
                ChannelDao channelDao = new ChannelDao();
                return channelDao.FindAll();
            }
        }

        #region ½»»»·Ö¼ðÑÌµÀ      

        internal DataTable GetChannelUSED()
        {
            using (PersistentManager pm = new PersistentManager())
            {
                ChannelDao channelDao = new ChannelDao();
                return channelDao.FindChannelUSED();
            }
        }
        
        internal DataTable GetChannelUSED(string lineCode, string channelCode)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                ChannelDao channelDao = new ChannelDao();
                return channelDao.FindChannelUSED(lineCode, channelCode);
            }
        }

        internal DataTable GetEmptyChannelUSED(string lineCode, string channelCode, int channelGroup, string channelType)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                ChannelDao channelDao = new ChannelDao();
                return channelDao.FindEmptyChannel(lineCode,channelCode, channelGroup, channelType);
            }
        }

        internal void ExechangeChannelUSED(string lineCode, string sourceChannel, string targetChannel)
        {
            using (PersistentManager pm = new PersistentManager())
            {
                ChannelDao channelDao = new ChannelDao();
                SupplyDao supplyDao = new SupplyDao();
                try
                {
                    pm.BeginTransaction();

                    DataTable channelTableSource = channelDao.FindChannelUSED(lineCode, sourceChannel);
                    DataTable channelTableTarget = channelDao.FindChannelUSED(lineCode, targetChannel);

                    channelDao.UpdateChannelUSED(lineCode, targetChannel,
                        channelTableSource.Rows[0]["CIGARETTECODE"].ToString(),
                        channelTableSource.Rows[0]["CIGARETTENAME"].ToString(),
                        Convert.ToInt32(channelTableSource.Rows[0]["QUANTITY"]),
                        channelTableSource.Rows[0]["SORTNO"].ToString());

                    channelDao.UpdateChannelUSED(lineCode, sourceChannel,
                        channelTableTarget.Rows[0]["CIGARETTECODE"].ToString(),
                        channelTableTarget.Rows[0]["CIGARETTENAME"].ToString(),
                        Convert.ToInt32(channelTableTarget.Rows[0]["QUANTITY"]),
                        channelTableTarget.Rows[0]["SORTNO"].ToString());

                    supplyDao.UpdateChannelUSED(lineCode, sourceChannel, "0000", channelTableTarget.Rows[0]["GROUPNO"].ToString());
                    supplyDao.UpdateChannelUSED(lineCode, targetChannel, sourceChannel, channelTableSource.Rows[0]["GROUPNO"].ToString());
                    supplyDao.UpdateChannelUSED(lineCode, "0000", targetChannel, channelTableTarget.Rows[0]["GROUPNO"].ToString());

                    pm.Commit();
                }
                catch
                {
                    pm.Rollback();
                }
            }
        }        

        #endregion
    }
}
