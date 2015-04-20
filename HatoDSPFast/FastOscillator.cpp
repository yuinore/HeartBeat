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

        double overtoneBias = 0.36;  // �����l

        double _2pi_rate = 2.0 * Math::PI / samplingRate;
        double inv_2pi = 1.0 / (2.0 * Math::PI);
        double inv_pi = 1.0 / Math::PI;
        double _2_pi = 2.0 / Math::PI;
        double inv_12 = 1.0 / 12.0;

        // �����̒��ԕϐ����̂ɕ����I�ȈӖ��͋��炭�����Ǝv���܂��B441�͐^�񒆂̃��̉�(n=69)�̎��g���ł��B
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
            freqoctave = (constpitch + pshift - 69.0) * inv_12;    // 441Hz��A�̉�����̃I�N�^�[�u��[oct]
            freqratio = DSPLib::FastMath::Pow2(freqoctave);        // 441Hz��A�̉�����̉����̎��g����
            freq = freqratio * 441;                                // �����̎��g��[Hz]
            phasedelta = freq * _2pi_rate;                         // �����̊p���g���G��̈ʑ��̑���[rad]
            logovertonefloat = temp - freqoctave + overtoneBias;   // �{��(����܂�)�̐��́A���2�Ƃ���ΐ�
            // (*���F���m�ɂ́A�u���������؂�̂Ă�ƁA����܂ޔ{���̐��ɂȂ鐔���v�́A���2�Ƃ���ΐ�)
            logovertone = (int)logovertonefloat;                   // �{��(����܂�)�̐��́A���2�Ƃ���ΐ���؂�̂Ă���
            isNotTooLow = logovertone < DSPLib::FastMath::Get_WT_N(); // �����Ⴗ���Ȃ����ǂ�����\��bool�ϐ�
            isTooHigh = phasedelta >= Math::PI;                    // �����������邩�ǂ�����\��bool�ϐ�
            isVeryHigh = logovertone <= 0;                         // ���������A�P���sin�g�ŐM����\���邩�ǂ�����\��
            isInRange = isNotTooLow && !isVeryHigh;                // ���3�������܂Ƃ߂��ꎞ�ϐ�
        }
        else
        {
            //pitch = lenv.Pitch.ToArray();
        }

        for (int i = 0; i < count; i++)
        {
            if (!constantPitch)
            {
                // TODO: Tri, Sin�ł͎g�p����Ȃ��ϐ������邽�ߍœK��
                freqoctave = (pitch[i] + pshift - 69.0) * inv_12;      // 441Hz��A�̉�����̃I�N�^�[�u��[oct]
                freqratio = DSPLib::FastMath::Pow2(freqoctave);        // 441Hz��A�̉�����̉����̎��g����
                freq = freqratio * 441;                                // �����̎��g��[Hz]
                phasedelta = freq * _2pi_rate;                         // �����̊p���g���G��̈ʑ��̑���[rad]
                logovertonefloat = temp - freqoctave + overtoneBias;   // �{��(����܂�)�̐��́A���2�Ƃ���ΐ�
                // (*���F���m�ɂ́A�u���������؂�̂Ă�ƁA����܂ޔ{���̐��ɂȂ鐔���v�́A���2�Ƃ���ΐ�)
                logovertone = (int)logovertonefloat;                   // �{��(����܂�)�̐��́A���2�Ƃ���ΐ���؂�̂Ă���
                isNotTooLow = logovertone < DSPLib::FastMath::Get_WT_N(); // �����Ⴗ���Ȃ����ǂ�����\��bool�ϐ�
                isTooHigh = phasedelta >= Math::PI;                    // �����������邩�ǂ�����\��bool�ϐ�
                isVeryHigh = logovertone <= 0;                         // ���������A�P���sin�g�ŐM����\���邩�ǂ�����\��
                isInRange = isNotTooLow && !isVeryHigh;                // ���3�������܂Ƃ߂��ꎞ�ϐ�
            }

            switch (waveform)
            {
            case 0:  //Waveform.Saw:
                if (isInRange) { buf[i] = (float)(DSPLib::FastMath::Saw(phase, logovertone)); }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh) { buf[i] = (float)(DSPLib::FastMath::Sin(phase) * _2_pi); }  // ���̎��g���т́A�o�͑OLPF(���O�Y�ꂽ)���|����Ə����Ă��܂��B
                else
                {
                    double normphase = phase * inv_2pi;
                    int inorm = (int)normphase;
                    if (phase < 0) inorm -= 1;
                    double temp3 = normphase - inorm;  // ��]���Z�B"temp3 = normphase % 1;" ��\���B
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
                    double x = phase * inv_pi * 0.5;  // 0�`1��1����
                    double decimalPart = x - (Int64)x;  // ��������
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
                    // �s�A���_���܂܂��������������߁A�œK�����s��Ȃ��iTODO:�v�Z�ʓI�ɂ͍œK�����������ǂ������i�v���؁j�j
                    buf[i] = (float)(DSPLib::FastMath::Tri(phase, DSPLib::FastMath::Get_WT_N() - 1));  // ��2������logovertone�̂܂܂ł��悢
                }
                break;

            case 5:  //Waveform.Impulse:
                if (isInRange)
                {
                    double invovertonecount = freqratio * temp2;  // ���������؂�̂Ă�ƁA����܂ޔ{���̐��ɂȂ鐔���̋t��
                    buf[i] = (float)(DSPLib::FastMath::Impulse(phase, logovertone) * invovertonecount * 2);  // ���ʂ�LPF��ʂ�������
                }
                else if (isTooHigh) { buf[i] = 0; }
                else if (isVeryHigh)
                {
                    double invovertonecount = freqratio * temp2;  // ���������؂�̂Ă�ƁA����܂ޔ{���̐��ɂȂ鐔���̋t��
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

            while (phase >= 16 * Math::PI) phase -= 16 * Math::PI;  // SIMD�������ۂ�float��int�ɂ���̂ł��̂��߂�

        }
    }
}

#undef DSPLib