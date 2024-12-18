﻿using UnityEngine;

namespace OpenRelativity.GravityLenses
{
    public class GravityMirror : MonoBehaviour
    {
        public Camera playerCam;
        // Start is called before the first frame update
        protected void Start()
        {
            ManualUpdate();
        }

        // Update is called once per frame
        public void ManualUpdate()
        {
            transform.position = -playerCam.transform.position;
            transform.rotation = Quaternion.LookRotation(Vector3.back, Vector3.up);
            transform.rotation *= playerCam.transform.rotation;
        }
    }
}
