#pragma once

namespace HatoDSPFast
{
    public ref class FastOscillator
    {
        double phase = 0;  // 積分を行うような場合には精度を必要とするけど、普通に 2pi ずつ減算すればいいよねっていう

        //int i2 = 0;  // 通しでのサンプル数（使われていない）

    public:
        FastOscillator();

        void Take(int count, array<float>^ buf,
            float pshift, float amplify, int waveform, float op1,
            float samplingRate,
            bool constantPitch, float lenv_Pitch, array<float>^ pitch,
            bool hasPhaseShift, array<float>^ phaseShiftArr);
    };
}