﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HatoDSP
{
    public class Rainbow : Cell
    {
        CellTree children;
        List<Cell> list;
        int rainbowN = 3;

        public Rainbow()
        {
            this.children = null;

            list = new List<Cell>();
        }

        public override void AssignChildren(CellTree children)
        {
            this.children = children;

            list = new List<Cell>();  // 既存の割り当ては削除

            for (int i = 0; i < rainbowN; i++)
            {
                list.Add(children.Generate());
            }
        }

        public override Signal[] Take(int count, LocalEnvironment lenv)
        {
            Signal[] sum = null;
            Signal originalPitch = lenv.Pitch;

            for (int j = 0; j < list.Count; j++)
            {
                var x = list[j];

                // lenvのピッチをここで加工する

                lenv.Pitch = Signal.Add(originalPitch, new ConstantSignal(0.02f * (j - 1), count));

                var sig = x.Take(count, lenv);
                if (sum == null)
                {
                    //sum = (new Signal[sig.Length]).Select(nil => new ConstantSignal(0, count)).ToArray();  // ArrayTypeMismatchException
                    sum = (new Signal[sig.Length]).Select(nil => (Signal)(new ConstantSignal(0, count))).ToArray();
                }
                for (int i = 0; i < sig.Length; i++)
                {
                    sum[i] = Signal.Add(sum[i], sig[i]);
                }
            }

            return sum;
        }
    }
}