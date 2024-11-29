using UnityEngine;
using OpenRelativity;

namespace OpenRelativity.Samples {
public class AudioSenderTest : MonoBehaviour
{
    private RelativisticObject myRO;

    // Start is called before the first frame update
    protected void Start()
    {
        myRO = GetComponent<RelativisticObject>();
    }

    // Update is called once per frame
    protected void FixedUpdate()
    {
        myRO.viw = (1 - 0.01f * Time.fixedDeltaTime) * myRO.viw;
    }
}
}