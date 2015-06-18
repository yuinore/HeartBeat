using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    /// <summary>
    /// オシレータがそうであるように、入力をそのまま加算して出力に渡します。
    /// 派生クラスは、Takeの代わりにTakeInternalを実装します。
    /// </summary>
    abstract class InputThroughCell : SingleInputCell
    {
        public abstract void TakeInternal(int count, LocalEnvironment lenv);

        public abstract int ChannelCountInternal { get; }

        int outChCnt = 0;
        JovialBuffer jSubBuf;
        JovialBuffer jIntBuf;

        //Cell[] base.InputCells

        //public override void AssignControllers(CellParameterValue[] ctrl);
        //public override CellParameterInfo[] ParamsList{ get; };

        public sealed override void Take(int count, LocalEnvironment lenv)
        {
            if (outChCnt == 0)
            {
                outChCnt = Math.Max(base.InputCells[0].ChannelCount, ChannelCountInternal);  // TODO: サイドチェイン入力の扱い
            }

            if (base.InputCells.Length == 1 && base.InputCells[0] is NullCell)
            {
                TakeInternal(count, lenv);
            }
            else //if (base.InputCells.Length == 1)
            {
                //**** [1] 子(黒三角)から結果を取得 ****
                if (base.InputCells[0].ChannelCount == outChCnt)
                {
                    base.InputCells[0].Take(count, lenv);
                }
                else
                {
                    int myChCnt = base.InputCells[0].ChannelCount;
                    if (jSubBuf == null) { jSubBuf = new JovialBuffer(); }
                    float[][] buf = jSubBuf.GetReference(myChCnt, count);
                    LocalEnvironment lenv2 = lenv.Clone();
                    lenv2.Buffer = buf;
                    base.InputCells[0].Take(count, lenv2);

                    for (int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            lenv.Buffer[ch][i] = buf[ch % myChCnt][i];
                        }
                    }
                }

                //**** [2] 自分自身(子クラス)から結果を取得 ****
                if (base.InputCells[0].ChannelCount == outChCnt)
                {
                    TakeInternal(count, lenv);
                }
                else
                {
                    int myChCnt = ChannelCountInternal;
                    if (jIntBuf == null) { jIntBuf = new JovialBuffer(); }
                    float[][] buf = jIntBuf.GetReference(myChCnt, count);
                    LocalEnvironment lenv2 = lenv.Clone();
                    lenv2.Buffer = buf;
                    TakeInternal(count, lenv2);

                    for (int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++)
                        {
                            lenv.Buffer[ch][i] = buf[ch % myChCnt][i];
                        }
                    }
                }

                // TODO: サイドチェイン入力の扱い
            }
        }

        public sealed override int ChannelCount
        {
            get
            {
                if (outChCnt == 0)
                {
                    outChCnt = Math.Max(base.InputCells[0].ChannelCount, ChannelCountInternal);

                    // TODO: サイドチェイン入力の扱い
                }

                return outChCnt;
            }
        }
    }
}
