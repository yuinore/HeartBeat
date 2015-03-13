using HatoBMSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartBeatCore
{
    public abstract class GameRegulation
    {
        public double JudgementWindowSize = 2.0;

        public abstract Judgement SecondsToJudgement(double difference);

        // 対象を取るかどうか
        // 破壊が発生するかどうか
        // とかそういう
    }
}
