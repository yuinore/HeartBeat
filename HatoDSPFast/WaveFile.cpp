#include "stdafx.h"
#include "WaveFile.h"

namespace HatoDSPFast
{
    static WaveFile::WaveFile()
    {
    }

    void WaveFile::WriteAllSamples(char* filename, float** buf, int channels, int samplecount, int samplingrate, int bitdepth) {
        System::String^ nFilename = gcnew System::String(filename);

        if (nFilename->IndexOf(gcnew System::String(":")) == -1) {  // この gcnew System::String() って不要？？
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

    void WaveFile::ReadAllSamples(char* filename, float** buf, int maxchannels, int maxsamplecount) {
        System::String^ nFilename = gcnew System::String(filename);

        if (nFilename->IndexOf(gcnew System::String(":")) == -1) {  // この gcnew System::String() って不要？？
            nFilename = HatoLib::HatoPath::FromAppDir(nFilename);
        }

        array<array<float>^>^ nBuf = HatoLib::WaveFileReader::ReadAllSamples(
            gcnew System::IO::FileStream(nFilename, System::IO::FileMode::Open));

        int channels = nBuf->Length;
        if (channels == 0) return;
        if (channels > maxchannels) channels = maxchannels;
        
        int samplecount = nBuf[0]->Length;
        if (samplecount > maxsamplecount) samplecount = maxsamplecount;

        for (int ch = 0; ch < channels; ch++) {
            for (int i = 0; i < samplecount; i++) {
                buf[ch][i] = nBuf[ch][i];
            }
        }
    }
}