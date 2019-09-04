using UnityEngine;

namespace OpenRelativity.GravityLenses
{
    public class GravityMirror : MonoBehaviour
    {
        public Camera playerCam;
        // Start is called before the first frame update
        void Start()
        {
            transform.position = -playerCam.transform.position;
            transform.forward = Vector3.Reflect(-playerCam.transform.forward, Vector3.up);
        }

        // Update is called once per frame
        public void ManualUpdate()
        {
            transform.position = -playerCam.transform.position;
            transform.forward = Vector3.Reflect(-playerCam.transform.forward, Vector3.up);
        }
    }
}
