using Helios;
using System.IO;
using System.Net;
using UnityEngine;

public class BasicStream : MonoBehaviour
{
    PptStreamer pptStreamer;

	void Start ()
    {
        string root = Path.Combine(Application.streamingAssetsPath, "pptview");

        pptStreamer = new PptStreamer(new PptStreamer.LaunchOptions
        {
            SlideShowPath = Path.Combine(root, "test.pptx"),
            StreamAddress = IPAddress.Loopback.ToString(),
            StreamHeight = PptView.ScreenHeight,
            StreamWidth = PptView.ScreenWidth,
            StreamPort = 10000,
            RootPath = root,
            StartSlide = 1
        });

        if (!Application.runInBackground)
            Application.runInBackground = true;
    }
	
	void OnDestroy ()
    {
        pptStreamer.Dispose();
	}
}
