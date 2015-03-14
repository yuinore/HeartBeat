using HatoBMSLib;
using HatoLib;
using HatoSound;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HatoPlayer
{
    public class HatoPlayerDevice
    {
        //************* デバイス *************
        BMSStruct b;
        HatoSoundDevice hsound;

        //************* データとか *************
        Dictionary<int, SecondaryBuffer> WavidToBuffer = new Dictionary<int, SecondaryBuffer>();

        public HatoPlayerDevice(Form form, BMSStruct b)
        {
            this.b = b;
            hsound = new HatoSoundDevice(form);
        }

        /// <summary>
        /// wavまたはoggファイルが存在した場合にファイルを同期的に読み込みます。
        /// キー音を読み込むには、PrepareSound()の方を使用して下さい。
        /// </summary>
        /// <param name="fullpath">wavまたはoggファイルの、拡張子付きのフルパス。</param>
        /// <returns></returns>
        public SecondaryBuffer LoadAudioFileOrGoEasy(string fullpath)
        {
            if (AudioFileReader.FileExists(fullpath))
            {
                return new SecondaryBuffer(hsound, fullpath);
            }
            else
            {
                return new SecondaryBuffer(hsound);
            }
        }

        /// <summary>
        /// キー音を　同　期　的　に　読み込みます。
        /// もしキー音がwav/oggであれば、それを読み込みます。
        /// </summary>
        /// <param name="wavid"></param>
        public void PrepareSound(int wavid)
        {
            // TODO: 【重要】WavidToBufferのスレッドセーフ化
            // 参考：https://social.msdn.microsoft.com/Forums/vstudio/ja-JP/1665b757-05ba-4739-9be2-de5ba34f4762/dictionary?forum=csharpgeneralja

            SecondaryBuffer sbuf = null;
            string fn;

            if (!b.WavDefinitionList.TryGetValue(wavid, out fn) ||
                !AudioFileReader.FileExists(b.ToFullPath(fn)))
            {
                // WAV定義がされていなかったか、されていてもファイルが存在しなかった場合（空文字で定義されていた場合を含む）
                lock (WavidToBuffer)
                {
                    WavidToBuffer[wavid] = null;
                    return;
                }
            }
            else
            {
                if (!WavidToBuffer.TryGetValue(wavid, out sbuf))
                {
                    // まだ読み込みを開始していなかった場合

                    lock (WavidToBuffer)
                    {
                        WavidToBuffer[wavid] = null;  // 同じ音の多重読み込みを防止
                    }

                    //await Task.Run(() =>
                    {
                        // TODO:ファイルが壊れていた場合になんとかする処理
                        try
                        {
                            sbuf = new SecondaryBuffer(hsound, b.ToFullPath(fn));

                            lock (WavidToBuffer)
                            {
                                WavidToBuffer[wavid] = sbuf;
                            }
                        }
                        catch
                        {
                            // TODO:例外処理の削除
                            //TraceWarning("  Exception: " + e.ToString());
                        }
                    }
                }

                // sbuf にデータが読み込まれた
            }
        }

        /// <summary>
        /// wavidに割り当てられた音を直ちに最初から再生します。
        /// </summary>
        /// <param name="wavid">BMSで定義されたwavid</param>
        /// <param name="isKeysound">ユーザーの操作により再生された音であればtrue</param>
        public void PlaySound(int wavid, bool isKeysound)
        {
            SecondaryBuffer sbuf = null;

            if (WavidToBuffer.TryGetValue(wavid, out sbuf) && sbuf != null)
            {
                sbuf.StopAndPlay(isKeysound ? -6.0 : -10.0);
            }
            else
            {
                //TraceWarning("  Warning : " + b.WavDefinitionList.GetValueOrDefault(x.Wavid) + " is not loaded yet...");
            }
        }
    }
}
