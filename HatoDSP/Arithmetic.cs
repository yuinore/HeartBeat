using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    // メモ：このセルで、SingleInputCellを継承してはならない。
    // なぜなら、SingleInputCell自身の実装に、Arithmeticを使用しているからである。
    public class Arithmetic : Cell
    {
        public enum OperationType
        {
            // AddSubが最も計算量が少ない（はず）
            AddSub = 0,  // 加減算 x1 + x2 + x3 + ... - y1 - y2 - y3 - ...
            MulDiv,  // 乗除算 x1 * x2 * x3 * ... / y1 / y2 / y3 / ...
            Sidechain,  // (x1 + x2 + x3 ...) * y1 * y2 * y3 ...
            Count
        }

        // assignChildrenで初期化
        Cell[] childX = new Cell[] { };  // port == 0
        Cell[] childY = new Cell[] { };  // port == 1
        int childCount;

        // chCntXを最初に使うときに初期化
        int[] chCntX = null;
        int[] chCntY = null;

        // outChCntの初期化時に初期化
        bool allSameCh;  // 子供を持たないか、またはすべての子供のチャンネル数が等しい時true
        int outChCnt = 0;

        OperationType op = OperationType.AddSub;

        JovialBuffer jTempbuf = new JovialBuffer();
        JovialBuffer jBufX = new JovialBuffer();
        JovialBuffer jBufY = new JovialBuffer();

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[] {
                    new CellParameterInfo("Operation", false, 0, (float)OperationType.Count, (float)OperationType.AddSub, x => ((OperationType)(x + 0.5f)).ToString())
                };
            }
        }

        public override void AssignChildren(CellWire[] children)
        {
            childX = children.Where(x => x.Port == 0).Select(x => x.Source.Generate()).ToArray();
            childY = children.Where(x => x.Port != 0).Select(x => x.Source.Generate()).ToArray();

            childCount = childX.Length + childY.Length;

            // この時点では、child.ChannelCountを呼ぶことは可能か？？ (多分ダメ)
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1)
            {
                op = (OperationType)(ctrl[0].Value + 0.5f);
            }
        }


        public override int ChannelCount
        {
            get
            {
                return AssignAndGetChannelCount();
            }
        }

        private int AssignAndGetChannelCount()
        {
            if (outChCnt == 0)
            {
                if (chCntX == null)
                {
                    chCntX = childX.Select(x => x.ChannelCount).ToArray();
                    chCntY = childY.Select(x => x.ChannelCount).ToArray();
                }

                if (chCntX.Length == 0 && chCntY.Length == 0)
                {
                    outChCnt = 1;
                }
                else
                {
                    outChCnt = chCntX.Concat(chCntY).Max();
                }

                // 子供を持たないか、またはすべての子供のチャンネル数が等しい時true
                allSameCh = chCntX.Concat(chCntY).Select(x => x == outChCnt ? 0 : 1).Sum() == 0;
            }
            return outChCnt;
        }

        public override void Skip(int count, LocalEnvironment lenv)
        {
            Take(count, lenv, true);
        }

        public override void Take(int count, LocalEnvironment lenv)
        {
            Take(count, lenv, false);
        }

        private void Take(int count, LocalEnvironment lenv, bool isSkip)
        {
            if (outChCnt == 0)
            {
                AssignAndGetChannelCount();  // ここでchCntXも初期化される
            }

            Debug.Assert(chCntX != null);

            if (childCount == 0)
            {
                if (op != OperationType.MulDiv)
                {
                    return;  // 入力が無い場合は0になる。
                }
                else
                {
                    // MulDivに入力が無い場合は、1になる。
                    Debug.Assert(outChCnt == 1);

                    for (int ch = 0; ch < outChCnt; ch++)
                    {
                        for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += 1f; }
                    }
                    return;
                }
            }
            else if (childCount == 1)  // 子供が1個だった場合。
            {
                if (childY.Length == 0)  // その唯一の入力ポートが0だった場合
                {
                    Debug.Assert(chCntX[0] == outChCnt);

                    childX[0].Take(count, lenv);  // そのまま加算して返る
                    return;
                }
                else  // その唯一の入力ポートが1だった場合
                {
                    Debug.Assert(chCntY[0] == outChCnt);

                    switch (op)
                    {
                        case OperationType.Sidechain:
                            // do nothing 何もせずに返る
                            return;
                    }
                }
            }
            else  // 子供が2個以上だった場合
            {
                if (childY.Length == 0)  // すべて port 0 の場合
                {
                    if (allSameCh && op != OperationType.MulDiv)
                    {
                        // 2個以上の子供の、すべてのポートが0で、
                        // かつMulDivではなく、
                        // なおかつすべての子供のチャンネル数が一緒だった場合

                        for (int j = 0; j < childX.Length; j++)
                        {
                            childX[j].Take(count, lenv);
                        }
                        return;
                    }
                }
                else if (childY.Length == 0)  // すべて port 1 の場合
                {
                    if (op == OperationType.Sidechain)
                    {
                        return;  // サイドチェインモードなのに主入力信号がない場合
                    }
                }
            }
            // TODO: int lenv.FromEndOfStream の実装。-1で未了？

            LocalEnvironment lenv2 = lenv.Clone();

            float[][] bufx = null, bufy = null;

            if (op == OperationType.MulDiv)
            {
                bufx = jBufX.GetReference(outChCnt, count, 1.0f);
                bufy = (childY.Length == 0) ? null : jBufY.GetReference(outChCnt, count, 1.0f);
            }
            else if (op == OperationType.Sidechain)
            {
                bufx = jBufX.GetReference(outChCnt, count);
                bufy = (childY.Length == 0) ? null : jBufY.GetReference(outChCnt, count, 1.0f);
            }

            // TODO: ここから先、未最適化。

            Cell[] child = childX.Concat(childY).ToArray();
            int[] chCnts = chCntX.Concat(chCntY).ToArray();

            for (int celId = 0; celId < child.Length; celId++)
            {
                var cel = child[celId];
                int port = celId < childX.Length ? 0 : 1;

                if (op == OperationType.AddSub && chCnts[celId] == outChCnt && port == 0)
                {
                    // AddSubモードで、出力とチャンネル数が一致し、portが0（つまり加算）の場合
                    cel.Take(count, lenv);  // 直接 lenv.Buffer に送り込む
                    continue;
                }

                float[][] tempbuf = jTempbuf.GetReference(chCnts[celId], count);
                lenv2.Buffer = tempbuf;
                cel.Take(count, lenv2);

                for (int ch = 0; ch < outChCnt; ch++)
                {
                    int srcch = chCnts[celId] == 1 ? 0 : ch;  // 送り元チャンネル
                    if (srcch >= chCnts[celId]) continue;  // 循環はさせない

                    if (port == 0)
                    {
                        switch (op)
                        {
                            case OperationType.MulDiv:
                                for (int i = 0; i < count; i++) { bufx[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.Sidechain:
                                for (int i = 0; i < count; i++) { bufx[ch][i] += tempbuf[srcch][i]; }
                                break;
                            case OperationType.AddSub:
                            default:
                                for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += tempbuf[srcch][i]; }
                                break;
                        }
                    }
                    else
                    {
                        switch (op)
                        {
                            case OperationType.MulDiv:
                                for (int i = 0; i < count; i++) { bufy[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.Sidechain:
                                for (int i = 0; i < count; i++) { bufy[ch][i] *= tempbuf[srcch][i]; }
                                break;
                            case OperationType.AddSub:
                            default:
                                for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] -= tempbuf[srcch][i]; }
                                break;
                        }
                    }
                }
            }

            switch (op)
            {
                case OperationType.MulDiv:
                    if (childY.Length == 0)
                    {
                        for (int ch = 0; ch < outChCnt; ch++) { for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += bufx[ch][i]; } }
                    }
                    else
                    {
                        for (int ch = 0; ch < outChCnt; ch++)
                        {
                            for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += Math.Max(Math.Min(bufx[ch][i] / bufy[ch][i], 1.0f), -1.0f); }  // 出力を発散させないための苦肉の策
                        }
                    }
                    break;
                case OperationType.Sidechain:
                        if (childY.Length == 0)
                        {
                            for (int ch = 0; ch < outChCnt; ch++) { for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += bufx[ch][i]; } }
                        }
                        else
                        {
                            for (int ch = 0; ch < outChCnt; ch++) { for (int i = 0; i < count; i++) { lenv.Buffer[ch][i] += bufx[ch][i] * bufy[ch][i]; } }
                        }
                    break;
                case OperationType.AddSub:
                default:
                    break;
            }
        }
    }
}
