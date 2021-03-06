﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class CellTree  // HatoSynthでのモジュールの単位
    {
        // Cell と同様に、 Generate を呼んだ後に、 AssignChildren や AssignControllers を呼んではならない
        // GUI による CellParameterValue の変更があった場合は[お察し下さい]。

        Func<Cell> generator;
        public string Name;  // must be unique, but can be null??
        List<CellWire> children = new List<CellWire>();
        CellParameterValue[] ctrl;
        bool constructed = false;
        
        public CellTree(Func<Cell> generator)
        {
            Console.WriteLine("CellTree.ctor() " + Name);

            this.generator = generator;  // CellTree<T> where T:Cell にして new T() しても良かったかもしれないですね・・・。
        }

        public CellTree(string name, string module)
        {
            Console.WriteLine("CellTree.ctor() " + Name);

            Name = name;

            string moduleLower = module.ToLower();
            var first = ModuleList.Modules.FirstOrDefault(x => x.NameLowerCase == moduleLower);

            if (first == null)
            {
                throw new PatchFormatException("モジュール " + module + " は存在しません。");
            }

            generator = first.Generator;
        }

        public void AddChildren(CellWire[] children)
        {
            Console.WriteLine("CellTree.AddChildren() " + Name);

            if (constructed) throw new Exception("CellTreeの呼び出しに誤りがある可能性があります。");

            this.children.AddRange(children);
        }

        public void AddChildren(CellTree[] children)  // 互換性のために
        {
            if (constructed) throw new Exception("CellTreeの呼び出しに誤りがある可能性があります。");

            this.children.AddRange(
                Enumerable.Range(0, children.Length)
                .Select(i => new CellWire(children[i], i))
                .ToArray());
        }

        public void AssignControllers(CellParameterValue[] ctrl)
        {
            Console.WriteLine("CellTree.AssignControllers() " + Name);

            if (constructed) throw new Exception("CellTreeの呼び出しに誤りがある可能性があります。");

            this.ctrl = ctrl;  // カプセル化は？？
        }

        public void AssignControllers(float[] ctrl)  // 便宜的に
        {
            if (constructed) throw new Exception("CellTreeの呼び出しに誤りがある可能性があります。");

            this.ctrl = ctrl.Select(x => new CellParameterValue(x)).ToArray();
        }

        public Cell Generate()
        {
            Console.WriteLine("CellTree.Generate() " + Name);

            constructed = true;

            if (generator == null)
            {
                return null;
            }

            Cell cell = generator();
            if (children != null)
            {
                cell.AssignChildren(children.ToArray());
            }
            if (ctrl != null)
            {
                cell.AssignControllers(ctrl);
            }
            return cell;
        }
    }
}
