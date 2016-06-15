using System.Diagnostics;
using UnityEngine;
using System.IO;
using System;

public class PptView : IDisposable
{
    /// <summary>
    /// Returns path to the root directory of pptview.exe
    /// </summary>
    public static string RootPath
    {
        get { return Path.Combine(Application.streamingAssetsPath, "pptview"); }
    }

    /// <summary>
    /// Returns path to the pptview.exe executable, located in RootPath
    /// </summary>
    public static string BinaryPath
    {
        get { return Path.Combine(RootPath, "PPTVIEW.EXE"); }
    }

    /// <summary>
    /// Path to currently open presentation
    /// </summary>
    public readonly string PresentationPath;

    /// <summary>
    /// pptview.exe's process which is rendering the current presentation
    /// </summary>
    public readonly Process RendererProcess;

    public PptView(string presentation_path, uint start_slide = 1)
    {
        if (!File.Exists(presentation_path))
            throw new ArgumentException("presentation file does not exist.");

        if (!Directory.Exists(RootPath))
            throw new InvalidOperationException("root path does not exist.");

        if (!File.Exists(BinaryPath))
            throw new InvalidOperationException("binary path does not exist.");

        PresentationPath = presentation_path;

        RendererProcess = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                UseShellExecute = false,
                Arguments = string.Format("/FSN{0} \"{1}\"", start_slide, presentation_path),
                FileName = BinaryPath
            },
            EnableRaisingEvents = true
        };

        RendererProcess.Start();

        Disposed = false;
    }

    #region IDisposable Support
    public bool Disposed { get; private set; }

    protected virtual void Dispose(bool disposing)
    {
        if (!Disposed)
        {
            if (disposing)
            {
                if (RendererProcess != null)
                {
                    RendererProcess.CloseMainWindow();
                    RendererProcess.Dispose();
                }
            }

            Disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
    }
    #endregion
}
