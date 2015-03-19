using HatoLib;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HatoBMSLib
{
    internal class RandomBMSReader : IDisposable
    {
        StreamReader r;

        bool EditorMode;
        BMSExceptionHandler handler;

        //Random randomgen = new Random();
        //Stack<int> randomstack = new Stack<int>();
        //int skippinglevel = 0;
        bool skip = false;

        internal RandomBMSReader(Stream strm, Encoding enc, bool editorMode, BMSExceptionHandler handler)
        {
            r = new StreamReader(strm, enc);
            this.EditorMode = editorMode;
            this.handler = handler;
        }

        string nextline = null;

        internal string ReadLine()
        {
            if (nextline == null && !TryReadLine())
            {
                throw new EndOfStreamException("ファイルの終了に達しました。");
            }
            
            string ret = nextline;
            nextline = null;
            return ret;
        }

        /// <summary>
        /// ストリームから1行読み取れるかどうか調べます。
        /// 読み取れなかった場合はfalseを返し、読み取れた場合はnextlineに値をセットします。
        /// </summary>
        private bool TryReadLine()
        {
            if (nextline != null)
            {
                throw new InvalidOperationException("nextlineの値が消化されていません。");
            }

            while (!r.EndOfStream)
            {
                string line = r.ReadLine();
                // 遅そう・・・
                var matchRandom = new LazyMatch(line, @"^\s*#RANDOM\s+([0-9]+)\s*", RegexOptions.IgnoreCase);
                var matchIf = new LazyMatch(line, @"^\s*#IF\s+([0-9]+)\s*", RegexOptions.IgnoreCase);
                var matchEndif = new LazyMatch(line, @"^\s*#ENDIF\s*", RegexOptions.IgnoreCase);
                var matchEndrandom = new LazyMatch(line, @"^\s*#ENDRANDOM\s*", RegexOptions.IgnoreCase);
                Match match;

                if (matchRandom.Evaluate(out match))
                {
                    int range = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                }
                else if (matchIf.Evaluate(out match))
                {
                    int selected = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                    skip = selected != 1;
                }
                else if (matchEndif.Evaluate(out match))
                {
                    skip = false;
                }
                else
                {
                    if (!skip)
                    {
                        nextline = line;
                        return true;
                    }
                }
            }

            return false;
        }

        internal void Close()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal bool EndOfStream
        {
            get
            {
                return nextline == null && !TryReadLine();
            }
        }

        #region implementation of IDisposable
        // Flag: Has Dispose already been called?
        bool disposed = false;

        // Public implementation of Dispose pattern callable by consumers.
        void IDisposable.Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                // Free any other managed objects here.
                r.Dispose();
                r = null;
            }
            else
            {
                try
                {
                    throw new Exception("激おこ @ RandomBMSReader.Dispose(bool)");
                }
                catch
                {
                }
            }

            // Free any unmanaged objects here.
            disposed = true;
        }

        ~RandomBMSReader()
        {
            Dispose(false);
        }
        #endregion
    }
}
