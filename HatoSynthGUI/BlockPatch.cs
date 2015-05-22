using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoSynthGUI
{
    class BlockPatch
    {
        public int GraphicId;
        public string ModuleName;
        public string Name;
        public float[] Ctrl;  // 後から変更可能
        // TODO: エクスプレッション

        // BlockPatchの外からアクセスできないようにしたい
        // と思ったけどコードが汚くなるのでボックス化はやめます
        public BlockPatch(BlockPresetLibrary.BlockPreset preset, string blockName)
        {
            GraphicId = preset.GraphicId;
            ModuleName = preset.ModuleName;
            Name = blockName;
            Ctrl = preset.Ctrl.ToArray();
        }

        public BlockPatch(int graphicId, string moduleName, string blockName, float[] ctrls)
        {
            GraphicId = graphicId;
            ModuleName = moduleName;
            Name = blockName;
            Ctrl = ctrls.ToArray();
        }

        public BlockPatch Clone()
        {
            return new BlockPatch(GraphicId, ModuleName, Name, Ctrl);
        }
    }
}
