using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WvWOverlay
{
    public class LogWriter
    {
        #region Locks

        private object m_oLockFileWrite = new object();

        #endregion

        #region Settings

        private string m_cLogPath;
        private TimeSpan m_oTimespanAlteLoeschen;

        private bool m_bRun = true;

        public string LOG_PATH
        {
            get { return m_cLogPath; }
            set { m_cLogPath = value; }
        }

        public TimeSpan TIMESPAN_ALTE_LOESCHEN
        {
            get { return m_oTimespanAlteLoeschen; }
            set { m_oTimespanAlteLoeschen = value; }
        }

        public string FILE_NAME_FORMAT
        {
            get
            {
                return "{0:d}.log";
            }
        }

        public string CURRENT_LOG_FILE
        {
            get
            {
                return Path.Combine(LOG_PATH, string.Format(FILE_NAME_FORMAT, DateTime.Now));
            }
        }

        #endregion

        #region Threads

        private Thread m_oDeleteThread;

        #endregion

        #region Typen
        
        public enum MESSAGE_TYPE
        {
            Info,
            Error,
            Warning,
            Debug
        }

        #endregion


        #region Konstruktoren

        /// <summary>
        /// Konstruktor 
        ///  > Erstellt, falls nötig, auch den Pfad
        /// </summary>
        /// <param name="cLogPath"></param>
        /// <param name="oTimespanAlteLoeschen"></param>
        public LogWriter(string cLogPath, TimeSpan oTimespanAlteLoeschen)
        {
            TIMESPAN_ALTE_LOESCHEN = oTimespanAlteLoeschen;
            LOG_PATH = cLogPath;

            CheckLogPath();

            m_oDeleteThread = new Thread(new ThreadStart(DeleteFileWorker));
            m_oDeleteThread.Start();
        }

        #endregion

        #region private Methoden

        /// <summary>
        /// Erstellt gegebenenfalls den Logpfad
        /// </summary>
        private void CheckLogPath()
        {
            try
            {
                if (!Directory.Exists(LOG_PATH))
                {
                    Directory.CreateDirectory(LOG_PATH);
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
        }

        /// <summary>
        /// Führt 1x / Stunde den LogFile-Löschlauf aus
        /// </summary>
        private void DeleteFileWorker()
        {
            try
            {
                while (m_bRun)
                {
                    DeleteOldLogfiles();
                    Thread.Sleep(1000 * 60 * 60); //1 Std
                }
            }
            catch(ThreadAbortException)
            {

            }
            catch (Exception oEx)
            {
                throw oEx;
            }
        }

        /// <summary>
        /// Löscht alle alten Logfiles
        /// </summary>
        private void DeleteOldLogfiles()
        {
            string[] aFiles;
            FileInfo oFileInfo;
            try
            {
                aFiles = Directory.GetFiles(LOG_PATH);
                foreach (string cFile in aFiles)
                {
                    oFileInfo = new FileInfo(cFile);

                    //Prüfung, ob zu alt
                    if (oFileInfo.CreationTime < DateTime.Now.AddSeconds(TIMESPAN_ALTE_LOESCHEN.TotalSeconds * -1))
                    {
                        //Zu alt
                        File.Delete(cFile);
                    }
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
            finally
            {
                aFiles = null;
                oFileInfo = null;
            }
        }

        #endregion

        #region public Methoden

        /// <summary>
        /// Beendet den Löschthread
        /// </summary>
        public void RequestStop()
        {
            try
            {
                m_bRun = false;
                if (m_oDeleteThread != null)
                {
                    m_oDeleteThread.Abort();
                    m_oDeleteThread = null;
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
        }

        /// <summary>
        /// Schreibt die Nachricht ins Log
        /// </summary>
        public void WriteMessage(string cLogMessage, MESSAGE_TYPE eTyp)
        {
            FileStream oFileStream = null;
            StackFrame oStackFrame;
            try
            {
                lock (m_oLockFileWrite)
                {

                    oStackFrame = new StackTrace().GetFrame(1);
                    // Datei öffnen, gegebenenfalls erstellen
                    if(!File.Exists(CURRENT_LOG_FILE))
                    {
                        oFileStream = File.Create(CURRENT_LOG_FILE);
                        oFileStream.Close();
                        oFileStream.Dispose();
                    }

                    using(StreamWriter oLogWriter = new StreamWriter(CURRENT_LOG_FILE, true, Encoding.UTF8))
                    {
                        oLogWriter.WriteLine(string.Format(
                            "[{0:T}] => {1} <{2}>.<{3}>: {4}",
                            DateTime.Now, 
                            eTyp.ToString().ToUpper(),
                            oStackFrame.GetMethod().ReflectedType.Name,
                            oStackFrame.GetMethod().Name,
                            cLogMessage
                        ));

                        oLogWriter.Close();
                    }
                }
            }
            catch (Exception oEx)
            {
                throw oEx;
            }
            finally
            {
                if (oFileStream != null)
                {
                    oFileStream.Close();
                    oFileStream.Dispose();
                }
            }
        }

        #endregion


        #region Destruktor
        /// <summary>
        /// Destruktor
        ///  > Beendet den Thread des Löschlaufs
        /// </summary>
        ~LogWriter()
        {
            RequestStop();
        }

        #endregion
    }
}
