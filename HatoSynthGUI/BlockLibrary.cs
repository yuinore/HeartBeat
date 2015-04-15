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
            public Func<Cell> Generator;
            public int GraphicId;
            public string DefaultName;
            public float[] Ctrl;

            public BlockPreset(Func<Cell> generator, string defaultName, int graphicId, float[] ctrl)
            {
                Generator = generator;
                DefaultName = defaultName;
                GraphicId = graphicId;
                Ctrl = ctrl;
            }

            public CellTree Generate()
            {
                CellTree c = new CellTree(Generator);
                if (Ctrl != null)
                {
                    c.AssignControllers(Ctrl);
                }
                return c;
            }
        }

        List<BlockPreset> Presets = new List<BlockPreset>
        {
            new BlockPreset(() => new AnalogOscillator(), "Sawtooth", 1, new float[] {}),
            new BlockPreset(() => new AnalogOscillator(), "Square", 2, new float[] {}),
            new BlockPreset(() => new AnalogOscillator(), "Triangle", 3, new float[] {}),
            new BlockPreset(() => new Rainbow(), "Echo", 4, new float[] {}),  // Not Implemented
            new BlockPreset(() => new Rainbow(), "Rainbow", 5, new float[] {}),
            new BlockPreset(() => new Rainbow(), "Uni", 6, new float[] {}),  // Not Implemented
            new BlockPreset(() => new BiquadFilter(), "Lowpass Filter", 7, new float[] {}),
            new BlockPreset(() => new ADSR(), "AD", 8, new float[] {}),  // Not Implemented
            new BlockPreset(() => new ADSR(), "ASDR", 9, new float[] {}),
        };
    }
}
