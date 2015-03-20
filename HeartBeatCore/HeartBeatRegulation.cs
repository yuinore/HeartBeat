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
        public override double JudgementWindowSize
        {
            get { return 2.0; }
        }

        public override int MaxScorePerObject
        {
            get { return 2; }
        }

        public override Judgement SecondsToJudgement(double timedifference)
        {
            // http://vsrg.club/forum/archive/index.php?thread-86-2.html

            if (timedifference <= 0.021)
            {
                return Judgement.Perfect;
            }
            else if (timedifference <= 0.060)
            {
                return Judgement.Great;
            }
            else if (timedifference <= 0.120)
            {
                return Judgement.Good;
            }
            else if (timedifference <= 0.200)
            {
                return Judgement.Bad;  // 対象を取る（判定表示有り）
            }

            return Judgement.None;  // 対象を取らない（判定表示も無し）
        }

        public override int JudgementToScore(Judgement judge)
        {
            switch (judge)
            {
                case Judgement.Perfect: return 2;
                case Judgement.Great: return 1;
                default: return 0;
            }
        }
    }
}
