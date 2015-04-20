#include "stdafx.h"
#include "FastOscillator.h"
#include "FastMath.h"

#define DSPLib HatoDSPFast

namespace HatoDSPFast {
    using namespace System;
    using namespace System::Collections::Generic;
    using namespace System::Linq;
    using namespace System::Text;
    using namespace System::Threading::Tasks;

    FastOscillator::FastOscillator()
    {
    }

    void FastOscillator::Take(int count, array<float>^ buf,
        float pshift, float amplify, int waveform, float op1,
        float samplingRate,
        bool constantPitch, float lenv_Pitch, array<float>^ pitch){

        double overtoneBias = 0.36;  // 調整値

        double _2pi_rate = 2.0 * Math::PI / samplingRate;
        double inv_2pi = 1.0 / (2.0 * Math::PI);
        double inv_pi = 1.0 / Math::PI;
        double _2_pi = 2.0 / Math::PI;
        double inv_12 = 1.0 / 12.0;

        // これらの中間変数自体に物理的な意味は恐らく無いと思います。441は真ん中のラの音(n=69)の周波数です。
        double temp = Math::Log((samplingRate * 0.5) / 441.0) / Math::Log(2);  // log_2((SamplingRate * 0.5) / 441)
        double temp2 = 1.0 / ((samplingRate * 0.5) / 441.0);

        double freqoctave = 0;
        double freqratio = 0;
        double freq = 0;
        double phasedelta = 0;
        double logovertonefloat = 0;
        int logovertone = 0;
        bool isNotTooLow = false, isTooHigh = false, isVeryHigh = false, isInRange = false;

        if (constantPitch)
        {
            float constpitch = lenv_Pitch;
            freqoctave = (constpitch + pshift - 69.0) * inv_12;    // 441HzのAの音からのオクターブ差[oct]
            freqratio = DSPLib::FastMath::Pow2(freqoctave);        // 441HzのAの音からの音声の周波数比
            freq = freqratio * 441;                                // 音声の周波数[Hz]
            phasedelta = freq * _2pi_rate;                         // 音声の角周波数；基音の位相の増分[rad]
            logovertonefloat = temp - freqoctave + overtoneBias;   // 倍音(基音を含む)の数の、底を2とする対数
            // (*注：正確には、「小数部分切り捨てると、基音を含む倍音の数になる数字」の、底を2とする対数)
            logovertone = (int)logovertonefloat;                   // 倍音(基音を含む)の数の、底を2とする対数を切り捨てた数
            isNotTooLow = logovertone < DSPLib::FastMath::Get_WT_N(); // 音が低すぎないかどうかを表すbool変数
            isTooHigh = phasedelta >= Math::PI;                    // 音が高すぎるかどうかを表すbool変数
            isVeryHigh = logovertone <= 0;                         // 音が高く、単一のsin波で信号を表せるかどうかを表す
            isInRange = isNotTooLow && !isVeryHigh;                // 上の3条件をまとめた一時変数
        }
        else
        {
            //pitch = lenv.Pitch.ToArray();
        }

        for (int i = 0; i < count; i++)
        {
            if (!constantPitch)
            {
                // TODO: Tri, Sinでは使用されない変数があるため最適化
                freqoctave = (pitch[i] + pshift - 69.0) * inv_12;      // 441HzのAの音からのオクターブ差[oct]
                freqratio = DSPLib::FastMath::Pow2(freqoctave);        // 441HzのAの音からの音声の周波数比
                freq = freqratio * 441;                                // 音声の周波数[Hz]
                phasedelta = freq * _2pi_rate;                         // 音声の角周波数；基音の位相の増分[rad]
                logovertonefloat = temp - freqoctave + overtoneBias;   // 倍音(基音を含む)の数の、底を2とする対数
                // (*注：正確には、「小数部分切り捨てると、基音を含む倍音の数になる数字」の、底を2とする対数)
                logovertone = (int)logovertonefloat;                   // 倍音(基音を含む)の数の、底を2とする対数を切り捨てた数
                isNotTooLow = logovertone < DSPLib::FastMath::Get_WT_N(); // 音が低すぎないかどうかを表すbool変数
                isTooHigh = phasedelta >= Math::PI;                    // 音が高すぎるかどうかを表すbool変数
                isVeryHigh = logovertone <= 0;                         // 音が高く、単一のsin波で信号を表せるかどうかを表す
                isInRange = isNotTooLow && !isVeryHigh;                // 上の3条件をまとめた一時変数
            }

            switch (waveform)
            {
            case 0:  //Waveform.Saw:
                if (isInRange) { buf[i] = (float)(DSPLib::FastMath::Saw(phase, logovertone)); }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh) { buf[i] = (float)(DSPLib::FastMath::Sin(phase) * _2_pi); }  // この周波数帯は、出力前LPF(名前忘れた)を掛けると消えてしまう。
                else
                {
                    double normphase = phase * inv_2pi;
                    int inorm = (int)normphase;
                    if (phase < 0) inorm -= 1;
                    double temp3 = normphase - inorm;  // 剰余演算。"temp3 = normphase % 1;" を表す。
                    buf[i] += (float)((0.5 - temp3) * 2);
                }
                break;

            case 1:  //Waveform.Square:
                if (isInRange) { buf[i] = (float)((DSPLib::FastMath::Saw(phase, logovertone) - DSPLib::FastMath::Saw(phase + Math::PI, logovertone))); }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh) { buf[i] = (float)(DSPLib::FastMath::Sin(phase) * _2_pi * 2); }
                else
                {
                    if (phase >= 0){
                        buf[i] += (float)(((Int64)(phase * inv_pi) & 1) * (-2) + 1);  // (phase * inv_pi) % 2 == 0 ? 1 : -1
                    }
                    else{
                        buf[i] += (float)(((Int64)(phase * inv_pi) & 1) * 2 - 1);
                    }
                }
                break;

            case 4:  //Waveform.Pulse:
                if (isInRange) { buf[i] = (float)((DSPLib::FastMath::Saw(phase, logovertone) - DSPLib::FastMath::Saw(phase + (1 - op1) * (2 * Math::PI), logovertone))); }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh) { buf[i] = (float)((DSPLib::FastMath::Sin(phase) - DSPLib::FastMath::Sin(phase + (1 - op1) * (2 * Math::PI))) * _2_pi); }
                else
                {
                    double x = phase * inv_pi * 0.5;  // 0～1で1周期
                    double decimalPart = x - (Int64)x;  // 小数部分
                    if (x < 0) decimalPart += 1;

                    buf[i] += (decimalPart < op1 ? 1 - op1 : -op1) * 2;
                }
                break;

            case 3:  //Waveform.Tri:
                if (isInRange) { buf[i] = (float)(DSPLib::FastMath::Tri(phase, logovertone)); }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh) { buf[i] = (float)(Math::Cos(phase) * (8 / (Math::PI * Math::PI))); }
                else
                {
                    // 不連続点を含まず収束が速いため、最適化を行わない（TODO:計算量的には最適化した方が良いかも（要検証））
                    buf[i] = (float)(DSPLib::FastMath::Tri(phase, DSPLib::FastMath::Get_WT_N() - 1));  // 第2引数はlogovertoneのままでもよい
                }
                break;

            case 5:  //Waveform.Impulse:
                if (isInRange)
                {
                    double invovertonecount = freqratio * temp2;  // 小数部分切り捨てると、基音を含む倍音の数になる数字の逆数
                    buf[i] = (float)(DSPLib::FastMath::Impulse(phase, logovertone) * invovertonecount * 2);  // 音量はLPFを通した後基準で
                }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh)
                {
                    double invovertonecount = freqratio * temp2;  // 小数部分切り捨てると、基音を含む倍音の数になる数字の逆数
                    buf[i] = (float)(Math::Cos(phase) * 2 * invovertonecount);
                }
                else
                {
                    double dphase1 = phase * inv_pi;
                    double dphase2 = (phase + phasedelta) * inv_pi;

                    int iphase1 = (int)dphase1;
                    int iphase2 = (int)dphase2;

                    if (dphase1 < 0){ iphase1 -= 1; }
                    if (dphase2 < 0){ iphase2 -= 1; }

                    int lastval = (iphase1 & 1);
                    int currval = (iphase2 & 1);

                    buf[i] += (float)((lastval & (1 ^ currval)) << 1);  // lastval == 1 && currentval == 0 ? 2 : 0
                }
                break;

            case 2:  //Waveform.Sin:
            default:
                if (isTooHigh)
                {
                    buf[i] = 0;
                }
                else
                {
                    buf[i] = (float)DSPLib::FastMath::Sin(phase);
                }
                break;
            }

            phase += phasedelta;

            while (phase >= 16 * Math::PI) phase -= 16 * Math::PI;  // SIMD化した際にfloatをintにするのでそのために

        }
    }
}

#undef DSPLib