using Helios;
using System.IO;
using UnityEngine;

public class BasicStream : MonoBehaviour
{
    PptStreamer pptStreamer;

	void Start ()
    {
        pptStreamer = new PptStreamer(new PptStreamer.LaunchOptions
        {
            SlideShowPath = Path.Combine(PptView.RootPath, "test.pptx"),
            StartSlide = 1,
            StreamAddress = "127.0.0.1",
            StreamPort = 10000,
            StreamWidth = 3840,
            StreamHeight = 2160
        });
	}
	
	void OnDestroy ()
    {
        pptStreamer.Dispose();
	}
}
