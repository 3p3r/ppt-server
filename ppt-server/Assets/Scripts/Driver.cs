using System.IO;
using UnityEngine;

public class Driver : MonoBehaviour
{
    PptView pptView;

	void Start ()
    {
        string path = Path.Combine(PptView.RootPath, "test.pptx");
        pptView = new PptView(path);
	}
	
	void Update ()
    {
	
	}

    void OnDestroy()
    {
        if (pptView != null)
            pptView.Dispose();
    }
}
