using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class Driver : MonoBehaviour
{
    PptView pptView;
    public Texture2D pptTexture;
    public RawImage pptTexHost;

	void Start ()
    {
        string path = Path.Combine(PptView.RootPath, "test.pptx");
        pptView = new PptView(path);

        pptTexture = new Texture2D(3840, 2160);
        pptTexHost.texture = pptTexture;
	}
	
	void Update ()
    {
        byte[] pixels = pptView.GetScreenshot();
        
        if (pixels.Length > 0)
            pptTexture.LoadImage(pixels);
    }

    void OnDestroy()
    {
        if (pptView != null)
            pptView.Dispose();

        if (pptTexture != null)
            Destroy(pptTexture);
    }
}
