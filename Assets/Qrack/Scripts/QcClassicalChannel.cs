using UnityEngine;
using System.Collections.Generic;

#if OPEN_RELATIVITY_INCLUDED
using OpenRelativity;
using OpenRelativity.Objects;
#endif

namespace Qrack
{
#if OPEN_RELATIVITY_INCLUDED
    public class QcClassicalChannel : RelativisticBehavior
#else
    public class QcClassicalChannel : MonoBehaviour
#endif
    {
        public ActionIndicator visualUnitPrefab;
        private List<ActionIndicator> visualUnitsActive;
        private List<ActionIndicator> visualUnitsFree;
        private Dictionary<int, QcClassicalSignal> transmittingSignals;

        public RealTimeQasmProgram source;
        public RealTimeQasmProgram destination;
        public float emissionInterval = 1;
        public float historyLength = 50;
#if !OPEN_RELATIVITY_INCLUDED
        public float IndicatorSpeed = 100;
#endif

        private Vector3 oldCameraPos;
#if !OPEN_RELATIVITY_INCLUDED
        private Transform _playerTransform = null;
#endif

        protected float speed
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return state.SpeedOfLight;
#else
                return IndicatorSpeed;
#endif
            }
        }

        protected Transform playerTransform
        {
            get
            {
#if OPEN_RELATIVITY_INCLUDED
                return state.playerTransform;
#else
                if (_playerTransform == null) {
                    _playerTrasnform = GameObject.FindGameObjectWithTag("Player").transform;
                }
                return _playerTrasnform;
#endif
            }
        }

        public class QcClassicalSignal
        {
            public bool bit { get; set; }
            public int destination { get; set; }
        }

        // Use this for initialization
        protected void Start()
        {
            visualUnitsActive = new List<ActionIndicator>();
            visualUnitsFree = new List<ActionIndicator>();
            transmittingSignals = new Dictionary<int, QcClassicalSignal>();
            oldCameraPos = playerTransform.position;
        }

        public void EmitBit(int sourceIndex, int destIndex)
        {
            
            ActionIndicator emittedVU;
            if (visualUnitsFree.Count > 0)
            {
                emittedVU = visualUnitsFree[0];
                visualUnitsFree.RemoveAt(0);
            }
            else
            {
                emittedVU = Instantiate(visualUnitPrefab);
            }
            visualUnitsActive.Add(emittedVU);
            transmittingSignals[sourceIndex] = (new QcClassicalSignal { bit = source.ClassicalBitRegisters[sourceIndex], destination = destIndex });
            emittedVU.SetState(true);
            emittedVU.transform.parent = transform;
            emittedVU.transform.position = source.RelativisticObject.opticalPiw;
        }

        protected void FixedUpdate()
        {
#if OPEN_RELATIVITY_INCLUDED
            if (!state.isMovementFrozen)
            {
                TranslateSignals(state.FixedDeltaTimeWorld);
            }
#else
            TranslateSignals(Time.fixedDeltaTime);
#endif
        }

        private void TranslateSignals(float deltaWordTime)
        {
            Vector3 cameraPos = playerTransform.position;
            Vector3 destPos = destination.RelativisticObject.opticalPiw;
            Vector3 vuPos, dispUnit, velUnit;
            float perspectiveFactor;
            int vuIndex = 0;
            int signalIndex = 0;
            while (vuIndex < visualUnitsActive.Count) {
                ActionIndicator vu = visualUnitsActive[vuIndex];
#if OPEN_RELATIVITY_INCLUDED
                RelativisticObject vuRO = vu.GetComponent<RelativisticObject>();
#endif
                vuPos = vu.transform.position;
                dispUnit = (destPos - vuPos).normalized;
                velUnit = speed * dispUnit;
                perspectiveFactor = Mathf.Pow(2, Vector3.Dot((cameraPos - vuPos).normalized, dispUnit));
                float cameraDispChange = (oldCameraPos - vuPos).magnitude - (cameraPos - vuPos).magnitude;
#if OPEN_RELATIVITY_INCLUDED
                Vector3 disp = (vuRO.GetTimeFactor() * deltaWordTime * velUnit + cameraDispChange * dispUnit) * perspectiveFactor;
#else
                Vector3 disp = (deltaWordTime * velUnit + cameraDispChange * dispUnit) * perspectiveFactor;
#endif

                if (disp.sqrMagnitude > (destPos - vuPos).sqrMagnitude)
                {
                    visualUnitsActive.Remove(vu);
                    QcClassicalSignal signal = transmittingSignals[signalIndex];
                    transmittingSignals.Remove(signalIndex);
                    destination.ClassicalBitRegisters[signal.destination] = signal.bit;
                    destination.isSignalledSources.Add(this);
                    visualUnitsFree.Add(vu);
                    vu.SetState(false);
                }
                else
                {
                    vu.transform.position = vuPos + disp;
#if OPEN_RELATIVITY_INCLUDED
                    vuRO.piw = vu.transform.position;
#endif
                    ++vuIndex;
                }
                ++signalIndex;
            }
            oldCameraPos = cameraPos;
        }
    }
}