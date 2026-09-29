using System;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Purga
{
    /// <summary>
    /// File-based playtest logger. Each play session writes one file to
    /// PlaytestLogs/ at the project root (outside Assets, so Unity never
    /// imports it) with every game action plus every Unity console message
    /// (logs, warnings, errors and exceptions with stack traces).
    /// Lines are flushed to disk immediately so the file survives crashes
    /// and can be read while the game is still running.
    /// </summary>
    public static class PlaytestLog
    {
        static readonly object writeLock = new object();
        static string filePath;
        static bool hooked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void OnPlayStart()
        {
            string dir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "PlaytestLogs");
            Directory.CreateDirectory(dir);
            filePath = Path.Combine(dir, $"session_{DateTime.Now:yyyyMMdd_HHmmss}.log");

            if (!hooked)
            {
                hooked = true;
                Application.logMessageReceivedThreaded += OnUnityMessage;
#if UNITY_EDITOR
                // Application.quitting also fires on play-mode exit in the editor,
                // so hook only one of the two to avoid a duplicated end line.
                EditorApplication.playModeStateChanged += state =>
                {
                    if (state == PlayModeStateChange.ExitingPlayMode) OnSessionEnd();
                };
#else
                Application.quitting += OnSessionEnd;
#endif
            }

            Write("SISTEMA", $"—— Sesión iniciada {DateTime.Now:yyyy-MM-dd HH:mm:ss} | Unity {Application.unityVersion} ——");
        }

        /// <summary>Game actions: combat log lines, decisions, state changes.</summary>
        public static void Action(string message) => Write("ACCION", message);

        /// <summary>Free-form notes from systems (setup dumps, encounter composition...).</summary>
        public static void Info(string message) => Write("INFO", message);

        static void OnSessionEnd()
        {
            Write("SISTEMA", "—— Sesión finalizada ——");
        }

        static void OnUnityMessage(string condition, string stackTrace, LogType type)
        {
            switch (type)
            {
                case LogType.Exception:
                    Write("EXCEPCION", $"{condition}\n{stackTrace.TrimEnd()}");
                    break;
                case LogType.Error:
                case LogType.Assert:
                    Write("ERROR", $"{condition}\n{stackTrace.TrimEnd()}");
                    break;
                case LogType.Warning:
                    Write("AVISO", condition);
                    break;
                default:
                    Write("LOG", condition);
                    break;
            }
        }

        static void Write(string tag, string message)
        {
            if (filePath == null) return;
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}{Environment.NewLine}";
            lock (writeLock)
            {
                try { File.AppendAllText(filePath, line); }
                catch (IOException) { /* never let logging break the game */ }
            }
        }
    }
}
