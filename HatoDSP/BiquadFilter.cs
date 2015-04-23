using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class BiquadFilter : Cell
    {
        FilterType type = FilterType.LowPass;

        readonly int ParamsRefreshRate = 1; //16;

        Cell waveCell = new NullCell();
        Cell cutoffCell;
        IIRFilter[] filt;

        int slope = 2;

        public BiquadFilter()
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
            get
            {
                if (waveCell == null) throw new Exception();  // ←例外発生することはない
                return waveCell.ChannelCount;
            }
        }

        float[][] input, cutoffsignal;
        float[] a0 = new float[256];
        float[] a1 = new float[256];
        float[] a2 = new float[256];
        float[] b0 = new float[256];
        float[] b1 = new float[256];
        float[] b2 = new float[256];

        public override void Take(int count, LocalEnvironment lenv)
        {
            float[][] retbuf = lenv.Buffer;

            if (input == null || input.Length < waveCell.ChannelCount || input[0].Length < count)
            {
                input = (new float[waveCell.ChannelCount][]).Select(x => new float[count]).ToArray();
            }
            for (int ch = 0; ch < waveCell.ChannelCount; ch++)
            {
                for (int i = 0; i < count; i++)
                {
                    input[ch][i] = 0;
                }
            }
            LocalEnvironment lenv2 = lenv.Clone();
            lenv2.Buffer = input;

            waveCell.Take(count, lenv2);  // バッファにデータを格納

            if (cutoffCell != null)
            {
                if (cutoffsignal == null || cutoffsignal.Length < cutoffCell.ChannelCount || cutoffsignal[0].Length < count)
                {
                    cutoffsignal = (new float[cutoffCell.ChannelCount][]).Select(x => new float[count]).ToArray();
                }
                for (int ch = 0; ch < cutoffCell.ChannelCount; ch++)
                {
                    for (int i = 0; i < count; i++)
                    {
                        cutoffsignal[ch][i] = 0;
                    }
                }
                LocalEnvironment lenv3 = lenv.Clone();
                lenv3.Buffer = cutoffsignal;  // 別に用意した空のバッファを与える
                cutoffCell.Take(count, lenv3);  // バッファにデータを格納
            }

            filt = filt ?? (new int[slope]).Select(x => new IIRFilter(waveCell.ChannelCount, 1, 0, 0, 0, 0, 0)).ToArray();

            //Signal[] input = waveCell.Take(count, lenv);
            //var cutoffsignal = cutoffCell == null ? new ConstantSignal(0, count) : cutoffCell.Take(count, lenv)[0];

            if (cutoffCell == null)
            {
                float cutoff = 0;

                double w0 = 2 * Math.PI * (800 + cutoff * 5000) / lenv.SamplingRate;
                float sin = (float)Math.Sin(w0);
                float cos = (float)Math.Cos(w0);
                float Q = 6.0f;
                float alp = sin / Q;

                float[] ab = null;

                switch (type)
                {
                    case FilterType.LowPass:
                        ab = new float[6] { 1 + alp, -2 * cos, 1 - alp, (1 - cos) * 0.5f, 1 - cos, (1 - cos) * 0.5f };
                        break;
                    default:
                        break;
                }

                for (int i = 0; i < filt.Length; i++)
                {
                    filt[i].UpdateParams(ab[0], ab[1], ab[2], ab[3], ab[4], ab[5]);
                }

                for (int i = 0; i < filt.Length; i++)
                {
                    filt[i].Take(count, new float[][][] { input });
                }
            }
            else
            {
                if (a0.Length < count)
                {
                    a0 = new float[count];
                    a1 = new float[count];
                    a2 = new float[count];
                    b0 = new float[count];
                    b1 = new float[count];
                    b2 = new float[count];
                }

                float[] cutoff = cutoffsignal[0];

                double w0 = 0;
                float sin = 0, cos = 0, alp = 0;

                float Q = 6.0f;

                for (int i = 0; i < count; i++)
                {
                    if (i % ParamsRefreshRate == 0)
                    {
                        w0 = 2 * Math.PI * (800 + cutoff[i] * 5000) / lenv.SamplingRate;
                        sin = (float)HatoDSPFast.FastMathWrap.Sin(w0);
                        cos = (float)Math.Cos(w0);
                        alp = sin / Q;
                    }

                    switch (type)
                    {
                        case FilterType.LowPass:
                            a0[i] = 1 + alp;
                            a1[i] = -2 * cos;
                            a2[i] = 1 - alp;
                            b0[i] = (1 - cos) * 0.5f;
                            b1[i] = 1 - cos;
                            b2[i] = (1 - cos) * 0.5f;
                            break;
                        default:
                            break;
                    }
                }

                for (int i = 0; i < filt.Length; i++)
                {
                    filt[i].Take(count, new float[][][] { 
                        input, 
                        new float[][] { 
                            a0,
                            a1,
                            a2,
                            b0,
                            b1,
                            b2,
                    }});
                }
            }

            for (int ch = 0; ch < waveCell.ChannelCount; ch++)
            {
                for (int i = 0; i < count; i++)
                {
                    retbuf[ch][i] += input[ch][i];
                }
            }
        }
    }
}
