#pragma once
namespace HatoDSPFast
{
    public ref class WaveFile abstract sealed  // public static class
    {
    public:
        static WaveFile();

        static void WriteAllSamples(char* filename, float** buf, int channels, int samplecount, int samplingrate, int bitdepth);
    };
}
