using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiKirari.DataFlow;
using MomobamiKirari.Models;

namespace IntegrationTest.Flow
{
    [TestClass]
    public class FlowBlock_Test
    {
        [TestMethod]
        [DataRow(2019, 4, 29, 2019, 4, 29)]
        public void TestFlowTimeSpanBlock(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            DateTime endDate = new DateTime(endYear, endMonth, endDay);
            int days = Convert.ToInt32((endDate - startDate).TotalDays + 1);

            LaneFlowTimeSpanBlock oneMinuteBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.Minute, TestInit.ServiceProvider);
            BufferBlock<LaneFlow> oneMinuteResult = new BufferBlock<LaneFlow>();
            oneMinuteBlock.LinkTo(oneMinuteResult);

            LaneFlowTimeSpanBlock fiveMinuteBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.FiveMinutes, TestInit.ServiceProvider);
            BufferBlock<LaneFlow> fiveMinuteResult = new BufferBlock<LaneFlow>();
            fiveMinuteBlock.LinkTo(fiveMinuteResult);

            LaneFlowTimeSpanBlock fifteenMinuteBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.FifteenMinutes, TestInit.ServiceProvider);
            BufferBlock<LaneFlow> fifteenMinuteResult = new BufferBlock<LaneFlow>();
            fifteenMinuteBlock.LinkTo(fifteenMinuteResult);

            LaneFlowTimeSpanBlock sixtyMinuteBlock = new LaneFlowTimeSpanBlock(DateTimeLevel.Hour, TestInit.ServiceProvider);
            BufferBlock<LaneFlow> sixtyMinuteResult = new BufferBlock<LaneFlow>();
            sixtyMinuteBlock.LinkTo(sixtyMinuteResult);
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                for (int i = 0; i < 24 * 60; ++i)
                {
                    LaneFlow laneFlow = new LaneFlow
                    {
                        DateTime = date.AddMinutes(i),
                        Cars = 1
                    };
                    oneMinuteBlock.InputBlock.Post(laneFlow);
                    fiveMinuteBlock.InputBlock.Post(laneFlow);
                    fifteenMinuteBlock.InputBlock.Post(laneFlow);
                    sixtyMinuteBlock.InputBlock.Post(laneFlow);
                }
            }

            oneMinuteBlock.InputBlock.Complete();
            oneMinuteBlock.WaitCompletion();
            fiveMinuteBlock.InputBlock.Complete();
            fiveMinuteBlock.WaitCompletion();
            fifteenMinuteBlock.InputBlock.Complete();
            fifteenMinuteBlock.WaitCompletion();
            sixtyMinuteBlock.InputBlock.Complete();
            sixtyMinuteBlock.WaitCompletion();

            oneMinuteResult.TryReceiveAll(out IList<LaneFlow> oneMinuteFlows);
            Assert.AreEqual(days * 24 * 60, oneMinuteFlows.Count);
            for (int i = 0; i < oneMinuteFlows.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i), oneMinuteFlows[i].DateTime);
                Assert.AreEqual(1, oneMinuteFlows[i].Cars);
            }

            fiveMinuteResult.TryReceiveAll(out IList<LaneFlow> fiveFlows);
            Assert.AreEqual(days * 24 * 60 / 5, fiveFlows.Count);
            for (int i = 0; i < fiveFlows.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i * 5), fiveFlows[i].DateTime);
                Assert.AreEqual(5, fiveFlows[i].Cars);
            }

            fifteenMinuteResult.TryReceiveAll(out IList<LaneFlow> fifteenFlows);
            Assert.AreEqual(days * 24 * 60 / 15, fifteenFlows.Count);
            for (int i = 0; i < fifteenFlows.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i * 15), fifteenFlows[i].DateTime);
                Assert.AreEqual(15, fifteenFlows[i].Cars);
            }

            sixtyMinuteResult.TryReceiveAll(out IList<LaneFlow> sixtyFlows);
            Assert.AreEqual(days * 24, sixtyFlows.Count);
            for (int i = 0; i < sixtyFlows.Count; ++i)
            {
                Assert.AreEqual(startDate.AddHours(i), sixtyFlows[i].DateTime);
                Assert.AreEqual(60, sixtyFlows[i].Cars);
            }
        }

    }
}
