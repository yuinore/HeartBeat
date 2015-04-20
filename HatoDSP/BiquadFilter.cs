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

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] input = waveCell.Take(count, lenv);
            var cutoffsignal = cutoffCell == null ? new ConstantSignal(0, count) : cutoffCell.Take(count, lenv)[0];
            filt = filt ?? (new int[slope]).Select(x => new IIRFilter(input.Length, 1, 0, 0, 0, 0, 0)).ToArray();

            if (cutoffsignal is ConstantSignal)
            {
                if (((ConstantSignal)(cutoffsignal)).count != count) throw new Exception();
                float cutoff = ((ConstantSignal)(cutoffsignal)).val;

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
                    input = filt[i].Take(count, new Signal[][] { input });
                }
            }
            else
            {
                float[] a0 = new float[count];
                float[] a1 = new float[count];
                float[] a2 = new float[count];
                float[] b0 = new float[count];
                float[] b1 = new float[count];
                float[] b2 = new float[count];

                float[] cutoff = cutoffsignal.ToArray();

                double w0 = 0;
                float sin = 0, cos = 0, alp = 0;

                float Q = 6.0f;

                for (int i = 0; i < count; i++)
                {
                    // float[] cutoff = cutoffsignal.ToArray(); // 酷すぎる

                    if (i % ParamsRefreshRate == 0)
                    {
                        w0 = 2 * Math.PI * (800 + cutoff[i] * 5000) / lenv.SamplingRate;
                        sin = (float)HatoDSPFast.FastMath.Sin(w0);
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
                    input = filt[i].Take(count, new Signal[][] { 
                        input, 
                        new Signal[] { 
                            new ExactSignal(a0,1.0f, false),
                            new ExactSignal(a1,1.0f, false),
                            new ExactSignal(a2,1.0f, false),
                            new ExactSignal(b0,1.0f, false),
                            new ExactSignal(b1,1.0f, false),
                            new ExactSignal(b2,1.0f, false)
                    }});
                }
            }

            return input;
        }
    }
}
