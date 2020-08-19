using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuantumRegister : MonoBehaviour
{
    public QuantumSystem QuantumSystem;
    public uint RegisterStartIndex = 0;
    public uint RegisterLength = 2;
    
    private uint RegisterEnd
    {
        get
        {
            return RegisterStartIndex + RegisterLength;
        }
    }

    private uint GetSystemIndex(uint registerIndex)
    {
        return registerIndex + RegisterStartIndex;
    }

    // Start is called before the first frame update
    void Start()
    {
        if (RegisterEnd > 64)
        {
            // Invalid bounds
            RegisterStartIndex = 0;
            RegisterLength = 0;
        }

        if (QuantumSystem.QubitCount < RegisterEnd)
        {
            QuantumSystem.QubitCount = RegisterEnd;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (RegisterEnd > 64)
        {
            // Invalid bounds
            RegisterStartIndex = 0;
            RegisterLength = 0;
        }

        if (QuantumSystem.QubitCount < RegisterEnd)
        {
            QuantumSystem.QubitCount = RegisterEnd;
        }
    }
}
