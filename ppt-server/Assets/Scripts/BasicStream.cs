using Helios;
using System.IO;
using System.Net;
using UnityEngine;

public class BasicStream : MonoBehaviour
{
    PptStreamer pptStreamer;

	void Start ()
    {
        pptStreamer = new PptStreamer(new PptStreamer.LaunchOptions
        {
            SlideShowPath = Path.Combine(PptView.RootPath, "test.pptx"),
            StreamAddress = IPAddress.Loopback.ToString(),
            StreamHeight = PptView.ScreenHeight,
            StreamWidth = PptView.ScreenWidth,
            StreamPort = 10000,
            StartSlide = 1
        });
	}
	
	void OnDestroy ()
    {
        pptStreamer.Dispose();
	}
}
