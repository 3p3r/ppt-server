using Helios;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class BasicRender : MonoBehaviour
{
    Helios.PptView      pptView;
    public Texture2D    pptTexture;
    public RawImage     pptTexHost;

	void Start ()
    {
        string path = Path.Combine(PptView.RootPath, "test.pptx");
        pptView = new PptView(path);

        pptTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.ARGB32, false);
        pptTexHost.texture = pptTexture;

        if (!Application.runInBackground)
            Application.runInBackground = true;
    }
	
	void Update ()
    {
        pptView.Render(ref pptTexture);

        if (Input.GetMouseButton(0))
            Debug.LogFormat("Slide #: {0}", pptView.SlideNumber);

        if (Input.GetKeyDown(KeyCode.RightArrow))
            pptView.NextStep();
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
            pptView.PreviousStep();
    }

    void OnDestroy()
    {
        if (pptView != null)
            pptView.Dispose();

        if (pptTexture != null)
            Destroy(pptTexture);
    }
}
