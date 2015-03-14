using HatoBMSLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    public class PlayingState
    {
        public int Combo;
        public double Gauge;  // 0.0 ～ 1.0
        public Judgement LastJudgement;
        public BMTime Current;

        public int TotalExScore;
        public int CurrentMaximumExScore;

        public int TotalAcceptance;
        public int CurrentMaximumAcceptance;

        public KeyEvent LastKeyEvent;
    }
}
