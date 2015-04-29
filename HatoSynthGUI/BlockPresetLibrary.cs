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
            public int GraphicId;
            public string ModuleName;
            public string DefaultName;
            public float[] Ctrl;  // TODO: エクスプレッション

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
            new BlockPreset("Analog OSC",    "Sawtooth",       1,  new float[] {0, 0.5f, (float)Waveform.Saw, 0}),
            new BlockPreset("Analog OSC",    "Square",         2,  new float[] {0, 0.5f, (float)Waveform.Square, 0}),
            new BlockPreset("Analog OSC",    "Triangle",       3,  new float[] {0, 0.5f, (float)Waveform.Tri, 0}),
            new BlockPreset("Null",          "Echo",           4,  new float[] {}),  // Not Implemented
            new BlockPreset("Rainbow",       "Rainbow",        5,  new float[] {7, 0.2f, 1.0f, 1.0f}),
            new BlockPreset("Rainbow",       "Uni",            6,  new float[] {7, 0.0f, 1.0f, 1.0f}),
            new BlockPreset("Analog Filter", "Lowpass",        7,  new float[] {(float)FilterType.LowPass}),
            new BlockPreset("ADSR",          "AD",             8,  new float[] {0.001f, 0.50f, 0.0f, 0.50f}),  // Not Precisely Implemented
            new BlockPreset("ADSR",          "ASDR",           9,  new float[] {0.001f, 0.50f, 0.5f, 0.01f}),
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
        };
    }
}
