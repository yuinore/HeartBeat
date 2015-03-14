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
        /// <summary>
        /// 判定ウィンドウの片側サイズ。より一般的には、空POORではない判定が出る範囲。
        /// </summary>
        public abstract double JudgementWindowSize
        {
            get;
        }

        /// <summary>
        /// 最高ランクを取った場合のEXスコア
        /// </summary>
        public abstract int MaxScorePerObject
        {
            get;
        }

        /// <summary>
        /// 時間差から、得られる判定結果を求めます。
        /// </summary>
        public abstract Judgement SecondsToJudgement(double difference);

        /// <summary>
        /// 判定から、EXスコアを求めます。
        /// </summary>
        public abstract int JudgementToScore(Judgement judge);

        // 対象を取るかどうか
        // 破壊が発生するかどうか
        // とかそういう

    }
}
