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
            new BlockPreset("Analog OSC",    "Sawtooth",       1, new float[] {0, 0.5f, (float)Waveform.Saw, 0}),
            new BlockPreset("Analog OSC",    "Square",         2, new float[] {0, 0.5f, (float)Waveform.Square, 0}),
            new BlockPreset("Analog OSC",    "Triangle",       3, new float[] {0, 0.5f, (float)Waveform.Tri, 0}),
            new BlockPreset("Rainbow",       "Echo",           4, new float[] {}),  // Not Implemented
            new BlockPreset("Rainbow",       "Rainbow",        5, new float[] {}),
            new BlockPreset("Rainbow",       "Uni",            6, new float[] {}),  // Not Implemented
            new BlockPreset("Analog Filter", "Lowpass Filter", 7, new float[] {}),
            new BlockPreset("ADSR",          "AD",             8, new float[] {0.01f, 0.5f, 0.0f, 0.5f}),  // Not Implemented
            new BlockPreset("ADSR",          "ASDR",           9, new float[] {0.01f, 0.1f, 0.5f, 0.01f}),
        };
    }
}
