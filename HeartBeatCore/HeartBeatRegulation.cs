using HatoBMSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartBeatCore
{
    class HeartBeatRegulation : GameRegulation
    {
        public HeartBeatRegulation()
        {
            JudgementWindowSize = 2.0;
        }

        public override Judgement SecondsToJudgement(double timedifference)
        {
            if (timedifference <= 0.02)
            {
                return Judgement.Perfect;
            }
            else if (timedifference <= 0.04)
            {
                return Judgement.Great;
            }
            else if (timedifference <= 0.10)
            {
                return Judgement.Good;
            }
            else if (timedifference <= 0.20)
            {
                return Judgement.Bad;  // 対象を取る（判定表示有り）
            }

            return Judgement.None;  // 対象を取らない（判定表示も無し）
        }
    }
}
