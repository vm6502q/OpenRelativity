using UnityEngine;
using System.Collections;

public class StandButton : MonoBehaviour
{
    public StandButtonReactor reactor;
    public AudioSource sound;

    // Use this for initialization
    protected void Start()
    {
        sound = GetComponent<AudioSource>();
    }

    protected void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag.Contains("Player"))
        {
            if (sound != null)
            {
                sound.Play();
            }
            reactor.ButtonPress();
        }
    }
}
