using HatoDSP;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoSynthGUI
{
    /// <summary>
    /// HatoSynthで画面右に表示されるブロックのプリセット一覧を表します。
    /// </summary>
    class BlockPresetLibrary
    {
        /// <summary>
        /// HatoSynthで画面右に表示されるブロックのプリセットを表します。
        /// Cell型を型パラメータに持ち、CellParameterの初期値をフィールドに持ちます。
        /// 子要素をフィールドに持ちません。
        /// </summary>
        //public class BlockPreset<T> where T : Cell, new()
        public class BlockPreset
        {
            public readonly int GraphicId;
            public readonly string ModuleName;
            public readonly string DefaultName;
            public readonly float[] Ctrl;  // TODO: エクスプレッション

            public BlockPreset(string moduleName, string defaultName, int graphicId, float[] ctrl)
            {
                ModuleName = moduleName;
                DefaultName = defaultName;
                GraphicId = graphicId;
                Ctrl = ctrl;
            }
        }

        public List<BlockPreset> Presets = new List<BlockPreset>
        {
            // FIXME: OSC の amp をいじると、乗算器による Amp Modulation がうまく行かない
            // あと、バッファの初期化とか
            new BlockPreset("Analog OSC",    "Sawtooth",       1,  new float[] {0, 0.05f, (float)Waveform.Saw, 0}),
            new BlockPreset("Analog OSC",    "Square",         2,  new float[] {0, 0.05f, (float)Waveform.Square, 0}),
            new BlockPreset("Analog OSC",    "Triangle",       3,  new float[] {0, 0.05f, (float)Waveform.Tri, 0}),
            new BlockPreset("Null",          "?",              4,  new float[] {}),
            new BlockPreset("Rainbow",       "Rainbow",        5,  new float[] {7, 0.2f, 1.0f, 1.0f}),
            new BlockPreset("Rainbow",       "Uni",            6,  new float[] {7, 0.0f, 1.0f, 1.0f}),
            new BlockPreset("Analog Filter", "Lowpass",        7,  new float[] {(float)FilterType.LowPass}),
            new BlockPreset("ADSR",          "AD",             8,  new float[] {0.001f, 0.50f, 0.0f, 0.50f}),  // Not Precisely Implemented
            new BlockPreset("ADSR",          "ADSR",           9,  new float[] {0.001f, 0.50f, 0.5f, 0.02f}),
            new BlockPreset("Analog Filter", "Low Shelf",      10, new float[] {(float)FilterType.LowShelf}),
            new BlockPreset("Analog Filter", "High Shelf",     11, new float[] {(float)FilterType.HighShelf}),
            new BlockPreset("Analog Filter", "Peaking",        12, new float[] {(float)FilterType.Peaking}),
            new BlockPreset("Analog Filter", "Bandpass",       13, new float[] {(float)FilterType.BandPass}),
            new BlockPreset("Analog Filter", "Highpass",       14, new float[] {(float)FilterType.HighPass}),
            // Notch, AllPass is not implemented...??
            new BlockPreset("Null",          "Feedback",       15, new float[] {}),
            new BlockPreset("Null",          "Loop",           16, new float[] {}),
            new BlockPreset("Null",          "Noise",          17, new float[] {}),
            new BlockPreset("Null",          "Timewarp",       18, new float[] {}),
            new BlockPreset("ADSR",          "Pad Envelope",   19, new float[] {4.0f, 1.0f, 1.0f, 4.0f}),
            new BlockPreset("Arithmetic",    "Multiply",       20, new float[] {(float)Arithmetic.OperationType.MulDiv}),
            new BlockPreset("Arithmetic",    "Subtract",       21, new float[] {(float)Arithmetic.OperationType.AddSub}),
            new BlockPreset("Arithmetic",    "Add",            22, new float[] {(float)Arithmetic.OperationType.Sidechain}),
            
            new BlockPreset("Null",          "Differentiate",  23, new float[] {}),
            new BlockPreset("Null",          "Integrate",      24, new float[] {}),
            new BlockPreset("Null",          "Reverb",         25, new float[] {}),
            new BlockPreset("Null",          "Phaser",         26, new float[] {}),
            new BlockPreset("Chorus",        "Chorus/Flanger", 27, new float[] {1.0f, 1.0f, 0.0f, 20.0f, 0.1f, 12.0f}),
            new BlockPreset("Chorus",        "Delay",          28, new float[] {1.0f, 0.0f, 0.3f, 20.0f, 0.2f, 300.0f}),
            new BlockPreset("Null",          "?",              29, new float[] {}),
            new BlockPreset("Null",          "?",              30, new float[] {}),
            new BlockPreset("Null",          "Wrap",           31, new float[] {}),
            new BlockPreset("Dynamics",      "Shaper",         32, new float[] {100}),
            new BlockPreset("Analog OSC",    "Pulse",          33, new float[] {0, 0.05f, (float)Waveform.Pulse, 0.125f}),
            new BlockPreset("Analog OSC",    "Impulse",        34, new float[] {0, 0.05f, (float)Waveform.Impulse, 0}),
            new BlockPreset("Analog OSC",    "Sin",            35, new float[] {0, 0.05f, (float)Waveform.Sin, 0}),
            new BlockPreset("Analog OSC",    "LFO",            36, new float[] {-60, 0.05f, (float)Waveform.Sin, 0}),  // TODO: キーボードトラッキングの無効化
            new BlockPreset("Comb Filter",   "Comb Filter",    37, new float[] {0.0f}),
            new BlockPreset("Freq Mod",      "Frequency Mod",  38, new float[] {0.0f}),
            new BlockPreset("Phase Mod",     "Phase Mod",      39, new float[] {0.0f}),
            new BlockPreset("Const",         "Constant",       40, new float[] {1.0f}),
            new BlockPreset("Tiny Mixer",    "Tiny Mixer",     41, new float[] {0.0f, 0.0f, 0.5f})
        };
    }
}
