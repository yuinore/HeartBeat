using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class ButterworthFilterCell : Cell
    {
        FilterType type = FilterType.LowPass;

        Cell waveCell = new NullCell();
        Cell cutoffCell;
        IIRFilter[] filt;

        public ButterworthFilterCell()
        {
        }

        public override void AssignChildren(CellWire[] children)
        {
            // FIXME: 複数指定

            foreach (var wire in children)
            {
                if (wire.Port == 0)
                {
                    waveCell = wire.Source.Generate();
                }
                else if (wire.Port == 1)
                {
                    cutoffCell = wire.Source.Generate();
                }
            }
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            // TODO:
        }

        public override CellParameter[] ParamsList
        {
            get
            {
                return new CellParameter[]{
                };
            }
        }

        public override int ChannelCount
        {
            get {
                return waveCell.ChannelCount;
            }
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            int degree = 20;

            Signal[] input = waveCell.Take(count, lenv);
            float[] cutoff;

            filt = filt ?? Enumerable.Range(0, (degree + 1) / 2).Select(x => new IIRFilter(input.Length, 1, 0, 0, 0, 0, 0)).ToArray();

            if (cutoffCell == null)
            {
                //cutoff = new ConstantSignal((float)Math.PI / 2, count).ToArray();
                cutoff = null;
                float c = (float)(Math.PI / 2);
                double w0 = 2 * Math.PI * (100 + c * 5000) / lenv.SamplingRate;
                var kab = FilterDesigner.Butterworth(degree, w0);
                for (int k = 0; k < filt.Length; k++)
                {
                    filt[k].UpdateParams(kab[k][0], kab[k][1], kab[k][2], kab[k][3], kab[k][4], kab[k][5]);
                }
                // TODO: delete these statements
            }
            else
            {
                cutoff = cutoffCell.Take(count, lenv)[0].ToArray();
            }

            // k : 0 ～ (degree+1)/2, 縦続フィルタのインデックス
            // ab : 0 ～ 5, 係数のインデックス、a0,a1,a2,b0,b1,b2の順
            // i : 0 ～ count, サンプル番号
            float[][][] kabi = null;
            if (cutoffCell != null)
            {
                kabi = new float[(degree + 1) / 2][][];
                for (int k = 0; k < kabi.Length; k++)
                {
                    kabi[k] = new float[6][];
                    for (int ab = 0; ab < kabi[k].Length; ab++)
                    {
                        kabi[k][ab] = new float[count];
                    }
                }

                for (int i = 0; i < count; i++)
                {
                    double w0 = 2 * Math.PI * (100 + cutoff[i] * 5000) / lenv.SamplingRate;

                    var kab = FilterDesigner.Butterworth(degree, w0);

                    // 配列の添字順序の交換(無駄なオーバーヘッド)
                    for (int k = 0; k < kabi.Length; k++)
                    {
                        for (int ab = 0; ab < kabi[k].Length; ab++)
                        {
                            kabi[k][ab][i] = kab[k][ab];
                        }
                    }
                }
            }

            for (int k = 0; k < filt.Length; k++)
            {
                if (cutoffCell == null)
                {
                    input = filt[k].Take(count, new Signal[][] { 
                        input, 
                    });
                }
                else
                {
                    input = filt[k].Take(count, new Signal[][] { 
                        input, 
                        kabi[k].Select(x => (Signal)(new ExactSignal(x, 1.0f, false))).ToArray()
                    });
                }
            }

            return input;
        }
    }
}
