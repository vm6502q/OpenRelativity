#if OPEN_RELATIVITY_INCLUDED
using OpenRelativity;
using OpenRelativity.Objects;
#endif

using UnityEngine;

namespace Qrack
{
    public class QuantumRegister : QuantumSystem
    {
        public uint RegisterStartIndex = 0;
        public QuantumSystem QuantumSystem;

        private uint RegisterEnd
        {
            get
            {
                return RegisterStartIndex + QubitCount;
            }
        }

        private uint GetSystemIndex(uint registerIndex)
        {
            return registerIndex + RegisterStartIndex;
        }

#if OPEN_RELATIVITY_INCLUDED
        private RelativisticObject _myRO;

        private RelativisticObject myRO
        {
            get
            {
                return _myRO != null ? _myRO : _myRO = GetComponent<RelativisticObject>();
            }
        }
#endif

        private float LocalTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return myRO.GetLocalTime();
#else
                return Time.time;
#endif
            }
        }

        private float LocalDeltaTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return (float)myRO.localDeltaTime;
#else
                return Time.deltaTime;
#endif
            }
        }

        private float LocalFixedDeltaTime
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return (float)myRO.localFixedDeltaTime;
#else
                return Time.fixedDeltaTime;
#endif
            }
        }

        // Start is called before the first frame update
        override protected void Start()
        {
            if (RegisterEnd > 64)
            {
                // Invalid bounds
                RegisterStartIndex = 0;
                QubitCount = 0;
            }

            if (QuantumSystem.QubitCount < RegisterEnd)
            {
                QuantumSystem.QubitCount = RegisterEnd;
            }
        }

        // Update is called once per frame
        override protected void Update()
        {
            if (RegisterEnd > 64)
            {
                // Invalid bounds
                RegisterStartIndex = 0;
                QubitCount = 0;
            }

            if (QuantumSystem.QubitCount < RegisterEnd)
            {
                QuantumSystem.QubitCount = RegisterEnd;
            }

#if !OPEN_RELATIVITY_INCLUDED
            LocalTimeUpdate();
#endif
        }

#if OPEN_RELATIVITY_INCLUDED
        protected void LateUpdate()
        {
            LocalTimeUpdate();
        }
#endif

        private void LocalTimeUpdate() { 
        }
    }
}
