using Helios;
using UnityEngine;

public class BasicServer : MonoBehaviour
{
    PptServer server;

	void Start ()
    {
        server = new PptServer("test.mosquitto.org");
	}
	
	void OnDestroy ()
    {
        server.Dispose();
	}
}
