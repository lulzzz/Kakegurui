using System;
using System.Collections.Generic;
using System.Threading.Tasks.Dataflow;
using Kakegurui.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MomobamiRirika.DataFlow;
using MomobamiRirika.Models;

namespace IntegrationTest.Density
{
    [TestClass]
    public class DensityBlock_Test
    {

        [TestMethod]
        [DataRow(2019, 4, 29, 2019, 4, 29)]
        public void TestDensityTimeSpanBlock(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            DateTime endDate = new DateTime(endYear, endMonth, endDay);
            int days = Convert.ToInt32((endDate - startDate).TotalDays + 1);

            DensityTimeSpanBlock oneMinuteBlock = new DensityTimeSpanBlock(DateTimeLevel.Minute, TestInit.ServiceProvider);
            BufferBlock<TrafficDensity> oneMinuteResult = new BufferBlock<TrafficDensity>();
            oneMinuteBlock.LinkTo(oneMinuteResult);

            DensityTimeSpanBlock fiveMinuteBlock = new DensityTimeSpanBlock(DateTimeLevel.FiveMinutes, TestInit.ServiceProvider);
            BufferBlock<TrafficDensity> fiveMinuteResult = new BufferBlock<TrafficDensity>();
            fiveMinuteBlock.LinkTo(fiveMinuteResult);

            DensityTimeSpanBlock fifteenMinuteBlock = new DensityTimeSpanBlock(DateTimeLevel.FifteenMinutes, TestInit.ServiceProvider);
            BufferBlock<TrafficDensity> fifteenMinuteResult = new BufferBlock<TrafficDensity>();
            fifteenMinuteBlock.LinkTo(fifteenMinuteResult);

            DensityTimeSpanBlock sixtyMinuteBlock = new DensityTimeSpanBlock(DateTimeLevel.Hour, TestInit.ServiceProvider);
            BufferBlock<TrafficDensity> sixtyMinuteResult = new BufferBlock<TrafficDensity>();
            sixtyMinuteBlock.LinkTo(sixtyMinuteResult);
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                for (int i = 0; i < 24 * 60 * 60; ++i)
                {
                    TrafficDensity flow = new TrafficDensity
                    {
                        DateTime = date.AddSeconds(i),
                        Value = 3
                    };
                    oneMinuteBlock.InputBlock.Post(flow);
                    fiveMinuteBlock.InputBlock.Post(flow);
                    fifteenMinuteBlock.InputBlock.Post(flow);
                    sixtyMinuteBlock.InputBlock.Post(flow);
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

            oneMinuteResult.TryReceiveAll(out IList<TrafficDensity> oneMinuteDensities);
            Assert.AreEqual(days * 24 * 60, oneMinuteDensities.Count);
            for (int i = 0; i < oneMinuteDensities.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i), oneMinuteDensities[i].DateTime);
                Assert.AreEqual(3, oneMinuteDensities[i].Value);
            }

            fiveMinuteResult.TryReceiveAll(out IList<TrafficDensity> fiveDensities);
            Assert.AreEqual(days * 24 * 60 / 5, fiveDensities.Count);
            for (int i = 0; i < fiveDensities.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i * 5), fiveDensities[i].DateTime);
                Assert.AreEqual(3, fiveDensities[i].Value);
            }

            fifteenMinuteResult.TryReceiveAll(out IList<TrafficDensity> fifteenDensities);
            Assert.AreEqual(days * 24 * 60 / 15, fifteenDensities.Count);
            for (int i = 0; i < fifteenDensities.Count; ++i)
            {
                Assert.AreEqual(startDate.AddMinutes(i * 15), fifteenDensities[i].DateTime);
                Assert.AreEqual(3, fifteenDensities[i].Value);
            }

            sixtyMinuteResult.TryReceiveAll(out IList<TrafficDensity> sixtyDensities);
            Assert.AreEqual(days * 24, sixtyDensities.Count);
            for (int i = 0; i < sixtyDensities.Count; ++i)
            {
                Assert.AreEqual(startDate.AddHours(i), sixtyDensities[i].DateTime);
                Assert.AreEqual(3, sixtyDensities[i].Value);
            }
        }

        [TestMethod]
        [DataRow(2019, 4, 29, 2019, 4, 29)]
        public void TestEventActionBlock(int startYear, int startMonth, int startDay, int endYear, int endMonth, int endDay)
        {
            DateTime startDate = new DateTime(startYear, startMonth, startDay);
            DateTime endDate = new DateTime(endYear, endMonth, endDay);
            int days = Convert.ToInt32((endDate - startDate).TotalDays + 1);
            EventRegionBlock block = new EventRegionBlock(TestInit.ServiceProvider);
            BufferBlock<TrafficEvent> result1 = new BufferBlock<TrafficEvent>();
            BufferBlock<TrafficEvent> result2 = new BufferBlock<TrafficEvent>();
            block.LinkTo(result1,result2);
            for (DateTime date = startDate; date <= endDate; date = date.AddDays(1))
            {
                for (int h = 0; h < 24; ++h)
                {
                    for (int i = 0; i <= 10; ++i)
                    {
                        block.InputBlock.Post(new TrafficEvent
                        {
                            DateTime = date.AddHours(h).AddSeconds(i)
                        });
                    }
                }
            }

            block.InputBlock.Complete();
            block.WaitCompletion();

            result1.TryReceiveAll(out IList<TrafficEvent> trafficEvents1);
            result2.TryReceiveAll(out IList<TrafficEvent> trafficEvents2);
            Assert.AreEqual(days * 24, trafficEvents1.Count);
            Assert.AreEqual(days * 24, trafficEvents2.Count);
            for (int i = 0; i < trafficEvents2.Count; ++i)
            {
                Assert.AreEqual(startDate.AddHours(i), trafficEvents1[i].DateTime);
                Assert.AreEqual(startDate.AddHours(i), trafficEvents2[i].DateTime);
                Assert.AreEqual(startDate.AddHours(i).AddSeconds(10), trafficEvents2[i].EndTime);

            }
        }
    }
}
