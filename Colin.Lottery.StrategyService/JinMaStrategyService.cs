﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

using Quartz;

using Colin.Lottery.Analyzers;
using Colin.Lottery.Models;
using Colin.Lottery.Utils;
using Colin.Lottery.Common;

namespace Colin.Lottery.StrategyService
{
    public class JinMaStrategyService : StrategyService<JinMaStrategyService>
    {
        readonly ITrigger _pk10Trigger;
        readonly ITrigger _sscTrigger;

        public JinMaStrategyService()
        {
            /* 
             * 北京赛车时间为每天9:00到23:50，每5分钟开一期，共179期
             * 
             * “0 1/5 9-23 * * ?”
             * 每天9点到23点，每5分钟的第1分钟第0秒。如 9:01:00,9:06:00 ... 9:56:00 ... 23:51:00,23:56:00
            */
            _pk10Trigger = QuartzUtil.CreateTrigger($"{LotteryType.PK10}", "JinMaTrigger", "0 1/5 9-23 * * ?");
            _sscTrigger = QuartzUtil.CreateTrigger($"{LotteryType.CQSSC}", "JinMaTrigger", "0/5 * * * * ? *");
        }

        public async override Task Start()
        {
            await StartPK10Champion();
        }

        public override void Start(Dictionary<LotteryType, List<int>> typeRules)
        {
            if (typeRules == null)
                return;

            foreach (var type in typeRules.Keys)
            {
                switch (type)
                {
                    case LotteryType.CQSSC:
                        typeRules[type].ForEach(r =>
                        {
                            switch ((CQSSCRule)r)
                            {
                                case CQSSCRule.OddOrEven:
                                    break;
                                case CQSSCRule.BigOrSmall:
                                    break;
                                case CQSSCRule.DragonOrTiger:
                                    break;
                                case CQSSCRule.Last2Group:
                                    break;
                                case CQSSCRule.Last3Group:
                                    break;
                                case CQSSCRule.OneOddOrEven:
                                    break;
                                case CQSSCRule.OneBigOrSmall:
                                    break;
                                case CQSSCRule.One:
                                    break;
                            }
                        });
                        break;
                    case LotteryType.PK10:
                        typeRules[type].ForEach(async r =>
                        {
                            switch ((PK10Rule)r)
                            {
                                case PK10Rule.Champion:
                                    await StartPK10Champion();
                                    break;
                                case PK10Rule.Second:
                                    break;
                                case PK10Rule.Third:
                                    break;
                                case PK10Rule.Fourth:
                                    break;
                                case PK10Rule.BigOrSmall:
                                    break;
                                case PK10Rule.OddOrEven:
                                    break;
                                case PK10Rule.DragonOrTiger:
                                    break;
                                case PK10Rule.Sum:
                                    break;
                            }
                        });
                        break;
                }
            }
        }

        async Task StartPK10Champion(bool startWhenBreakGua = false)
        {
            //大管家Job，负责创建每期的扫水Job
            var stewardJob = QuartzUtil.CreateSimpleJob($"Steward_{LotteryType.PK10}_Job", $"{LotteryType.PK10}_JobGroup", async () =>
             {
                 var timestamp = DateTime.Now;
                 var periodNo = PK10Scheduler.Instance.GetPeriodNo(timestamp);
                 var tempJob = $"{periodNo}_Scan_{LotteryType.PK10}";
                 var ng = tempJob.JobAndTriggerNames();

                 await QuartzUtil.ScheduleSimpleJob(tempJob, async () =>
                 {
                     //超时自毁
                     if ((DateTime.Now - timestamp).TotalMinutes > 5)
                         await QuartzUtil.DeleteJob(ng.JobName, ng.JobGroup);

                     //扫水
                     var plans = await JinMaAnalyzer.Instance.GetForcastData();
                     JinMaAnalyzer.Instance.CalcuteScore(ref plans, startWhenBreakGua);

                     if (plans.Plan1.LastDrawedPeriod + 1 >= periodNo)
                     {
                         //TODO:分发消息

                         await QuartzUtil.DeleteJob(ng.JobName, ng.JobGroup);
                     }
                 }, "0/5 * * * * ? *");
             });

            await QuartzUtil.GetScheduler().ScheduleJob(stewardJob, _pk10Trigger);
        }
    }
}
