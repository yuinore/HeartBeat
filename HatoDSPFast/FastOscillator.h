#pragma once

namespace HatoDSPFast
{
    public ref class FastOscillator
    {
        double phase = 0;  // �ϕ����s���悤�ȏꍇ�ɂ͐��x��K�v�Ƃ��邯�ǁA���ʂ� 2pi �����Z����΂�����˂��Ă���

        //int i2 = 0;  // �ʂ��ł̃T���v�����i�g���Ă��Ȃ��j

    public:
        FastOscillator();

        void Take(int count, array<float>^ buf,
            float pshift, float amplify, int waveform, float op1,
            float samplingRate,
            bool constantPitch, float lenv_Pitch, array<float>^ pitch,
            bool hasPhaseShift, array<float>^ phaseShiftArr);
    };
}