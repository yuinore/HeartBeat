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

        Random randomgen = new Random();
        List<int> randomstack = new List<int>();
        List<int> skipstack = new List<int>();
        bool skip = false;
        bool SekaiNoHazama = false;

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
                var matchRandom = new LazyMatch(line, @"^\s*#RANDOM\s+([0-9]+)\s*$", RegexOptions.IgnoreCase);
                var matchIf = new LazyMatch(line, @"^\s*#IF\s+([0-9]+)\s*$", RegexOptions.IgnoreCase);
                var matchEndif = new LazyMatch(line, @"^\s*#ENDIF\s*$", RegexOptions.IgnoreCase);
                var matchEndrandom = new LazyMatch(line, @"^\s*#ENDRANDOM\s*$", RegexOptions.IgnoreCase);
                var matchComment = new LazyMatch(line, @"^\s*[^\s#].*$", RegexOptions.IgnoreCase);  // コメント行
                var matchEmpty = new LazyMatch(line, @"^\s*$", RegexOptions.IgnoreCase);  // 空行
                Match match;

                if (matchRandom.Evaluate(out match))
                {
                    if (SekaiNoHazama)
                    {
                        if (randomstack.Count == 0) { throw new Exception("不適切な#RANDOMです。バグな気がします。"); }
                        randomstack.RemoveAt(randomstack.Count - 1);
                        SekaiNoHazama = false;
                    }

                    int range = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                    int next = randomgen.Next(range) + 1;  // 1-origin

                    randomstack.Add(next);
                    SekaiNoHazama = true;
                }
                else if (matchIf.Evaluate(out match))
                {
                    if (!SekaiNoHazama) { handler.ThrowFormatError("不適切な#IFです。これを無視します。"); continue; }
                    int selected = Convert.ToInt32(match.Groups[1].Captures[0].Value);
                    skipstack.Add(selected != randomstack[randomstack.Count - 1] ? 1 : 0);
                    skip = skipstack.Sum() > 0 ? true : false;
                    SekaiNoHazama = false;
                }
                else if (matchEndif.Evaluate(out match))
                {
                    if (SekaiNoHazama)
                    {
                        if (randomstack.Count == 0) { throw new Exception("不適切な#ENDIFです。バグな気がします。"); }
                        randomstack.RemoveAt(randomstack.Count - 1);
                        SekaiNoHazama = false;
                    }
                    if (skipstack.Count == 0) { handler.ThrowFormatError("不適切な#ENDIFです。これを無視します。"); continue; }
                    skipstack.RemoveAt(skipstack.Count - 1);
                    skip = skipstack.Sum() > 0 ? true : false;
                    SekaiNoHazama = true;
                }
                else if (matchComment.Evaluate(out match) || matchEmpty.Evaluate(out match))
                {
                }
                else
                {
                    if (SekaiNoHazama)
                    {
                        if (randomstack.Count == 0) { throw new Exception("不適切な文です。バグな気がします。：" + line); }
                        randomstack.RemoveAt(randomstack.Count - 1);
                        SekaiNoHazama = false;
                    }

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
