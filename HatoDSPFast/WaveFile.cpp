#include "stdafx.h"
#include "WaveFile.h"

namespace HatoDSPFast
{
    static WaveFile::WaveFile()
    {
    }

    void WaveFile::WriteAllSamples(char* filename, float** buf, int channels, int samplecount, int samplingrate, int bitdepth) {
        System::String^ nFilename = gcnew System::String(filename);

        if (nFilename->IndexOf(gcnew System::String(":")) == -1) {  // ���� gcnew System::String() ���ĕs�v�H�H
            nFilename = HatoLib::HatoPath::FromAppDir(nFilename);
        }

        array<array<float>^>^ nBuf = gcnew array<array<float>^>(channels);

        for (int ch = 0; ch < channels; ch++) {
            nBuf[ch] = gcnew array<float>(samplecount);

            for (int i = 0; i < samplecount; i++) {
                nBuf[ch][i] = buf[ch][i];
            }
        }

        HatoLib::WaveFileWriter::WriteAllSamples(
            gcnew System::IO::FileStream(nFilename, System::IO::FileMode::Create),
            nBuf, 1, 44100, 32);
    }
}