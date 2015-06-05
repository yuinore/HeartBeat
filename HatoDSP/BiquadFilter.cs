﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class BiquadFilter : SingleInputCell
    {
        FilterType type = FilterType.LowPass;

        Cell waveCell
        {
            get { return base.InputCells[0]; }
        }

        Cell cutoffCell
        {
            get
            {
                if (base.InputCells.Length <= 1 || base.InputCells[1] is NullCell)
                {
                    return null;
                }
                return base.InputCells[0];
            }
        }

        IIRFilter[] filt;

        int slope = 2;

        public BiquadFilter()
        {
        }

        public override void AssignControllers(CellParameterValue[] ctrl)
        {
            if (ctrl.Length >= 1)
            {
                type = (FilterType)(ctrl[0].Value + 0.5);
            }
        }

        public override CellParameterInfo[] ParamsList
        {
            get
            {
                return new CellParameterInfo[]{
                    new CellParameterInfo(
                        "Filter Type", false, 0, (float)(FilterType.Count - 1), (float)FilterType.LowPass,
                        x => ((FilterType)(x + 0.5)).ToString())
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

        JovialBuffer jInput = new JovialBuffer();
        JovialBuffer jCutoffSignal = new JovialBuffer();
        float[] a0 = new float[256];
        float[] a1 = new float[256];
        float[] a2 = new float[256];
        float[] b0 = new float[256];
        float[] b1 = new float[256];
        float[] b2 = new float[256];

        public override void Take(int count, LocalEnvironment lenv)
        {
            float[][] retbuf = lenv.Buffer;

            LocalEnvironment lenv2 = lenv.Clone();
            float[][] input = jInput.GetReference(waveCell.ChannelCount, count);
            lenv2.Buffer = input;
            waveCell.Take(count, lenv2);  // バッファにデータを格納

            float[][] cutoffsignal = null;
            if (cutoffCell != null)
            {
                LocalEnvironment lenv3 = lenv.Clone();
                cutoffsignal = jCutoffSignal.GetReference(cutoffCell.ChannelCount, count);
                lenv3.Buffer = cutoffsignal;  // 別に用意した空のバッファを与える
                cutoffCell.Take(count, lenv3);  // バッファにデータを格納
            }

            filt = filt ?? (new int[slope]).Select(x => new IIRFilter(waveCell.ChannelCount, 1, 0, 0, 0, 0, 0)).ToArray();

            if (cutoffCell == null)
            {
                float cutoff = 0;

                float w0 = (float)(2 * Math.PI * (800 + cutoff * 5000) / lenv.SamplingRate);
                float Q = 6.0f;

                float[] ab = new float[6];

                FilterDesigner.Biquad(
                    type, w0, Q, 0.5f,
                    out ab[0], out ab[1], out ab[2], out ab[3], out ab[4], out ab[5]);

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

                float w0 = 0;

                float Q = 6.0f;

                for (int i = 0; i < count; i++)
                {
                    w0 = (float)(2 * Math.PI * (800 + cutoff[i] * 5000) / lenv.SamplingRate);

                    FilterDesigner.Biquad(
                        type, w0, Q, 0.5f,
                        out a0[i], out a1[i], out a2[i], out b0[i], out b1[i], out b2[i]);
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
