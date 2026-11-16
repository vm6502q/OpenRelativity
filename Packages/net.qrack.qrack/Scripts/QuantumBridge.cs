using System;
using UnityEngine;

namespace Qrack
{
    /// <summary>
    /// Qrack's "QBridge" approximation method, embedded in space-time coordinates like QuantumSystem.
    ///
    /// A QBridge is a chain, (or optionally a ring,) of "patches," each one an ordinary Qrack simulator that holds a
    /// contiguous block of the logical qubits. Gates inside a patch run natively and exactly. Gates between patches
    /// are bridged over "bond" qubits, and the bonds across each cut between neighboring patches are truncated to at
    /// most MaxBondQubits. That truncation is the approximation: the whole is a matrix product state, with entire
    /// simulators for its sites.
    ///
    /// The qubit count and patch layout are fixed when the bridge is allocated, in Awake(). QBridge has no
    /// (collapsing) measurement gate: read it out with MeasureShots(), Prob(), Prob3Axis(), and PauliExpectation(),
    /// none of which disturb the state.
    ///
    /// An exception in the C++ library leaves the bridge in an error state, as for QuantumSystem. ResetAll() clears it.
    /// </summary>
    public class QuantumBridge : QuantumClockBehavior
    {
        // Logical qubits, in total
        public ulong QubitCount = 2;
        // The qubits are split into this many contiguous patches, equal in size up to remainder.
        // (The last "QubitCount % PatchCount" patches get one extra qubit.)
        public ulong PatchCount = 2;
        // Maximum bond qubits per cut between neighboring patches, (so, bond dimension 2^MaxBondQubits)
        public ulong MaxBondQubits = 3;
        // Close the chain of patches into a ring, (for 3 or more patches)
        public bool IsRing = false;
        // Truncate bonds by Born-rule sampling, instead of greedily keeping the largest weights
        public bool IsBornTruncation = false;
        // Extra bond qubits that a cut may buffer before a truncating sweep, (0 to truncate eagerly)
        public ulong LazyBondQubits = 0;
        // Optional cap on the total width of any patch, with its bond qubits, that also forces a sweep, (0 for none)
        public ulong MaxPatchQubits = 0;
        // Seed of the sampler for MeasureShots() (and Born-rule truncation), or 0 to pick one at random
        public ulong Seed = 0;

        // Simulator layers of every patch, as for QuantumManager.InitType(): (All false is a plain CPU state vector.)
        public bool IsTensorNetwork = false;
        public bool IsSchmidtDecomposeMulti = false;
        public bool IsSchmidtDecompose = false;
        public bool IsStabilizerHybrid = false;
        public bool IsBinaryDecisionTree = false;
        public bool IsPaged = false;
        public bool IsCpuGpuHybrid = false;
        public bool IsOpenCL = false;

        public ulong BridgeId { get; private set; }

        // The layout that the bridge was actually allocated with, (whatever the inspector says, later)
        protected ulong allocQubitCount;
        protected ulong allocPatchCount;
        protected bool isAllocated = false;

        private QuantumManager _qMan = null;

        private QuantumManager qMan
        {
            get
            {
                if (_qMan == null)
                {
                    _qMan = FindAnyObjectByType<QuantumManager>();
                }

                return _qMan;
            }
        }

        // Awake() is called before Start()
        protected void Awake()
        {
            Allocate();
        }

        protected void OnDestroy()
        {
            Deallocate();
        }

        protected void Allocate()
        {
            if (QubitCount < 1)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning("Tried to initialize bridge with fewer than 1 qubit, clamped to 1, original: " + QubitCount);
                }
                QubitCount = 1;
            }

            if (PatchCount < 1)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning("Tried to initialize bridge with fewer than 1 patch, clamped to 1, original: " + PatchCount);
                }
                PatchCount = 1;
            }

            if (PatchCount > QubitCount)
            {
                if (Debug.isDebugBuild)
                {
                    Debug.LogWarning("Tried to initialize bridge with more patches than qubits, clamped to " + QubitCount + ", original: " + PatchCount);
                }
                PatchCount = QubitCount;
            }

            ulong seed = Seed;
            if (seed == 0)
            {
                seed = ((ulong)(uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue) << 32) | (uint)UnityEngine.Random.Range(int.MinValue, int.MaxValue);
            }

            BridgeId = qMan.AllocateBridge(QubitCount, MaxBondQubits, PatchCount, IsRing, IsBornTruncation, LazyBondQubits, MaxPatchQubits, seed,
                IsTensorNetwork, IsSchmidtDecomposeMulti, IsSchmidtDecompose, IsStabilizerHybrid, IsBinaryDecisionTree, IsPaged, IsCpuGpuHybrid, IsOpenCL);
            allocQubitCount = QubitCount;
            allocPatchCount = PatchCount;
            isAllocated = true;
        }

        protected void Deallocate()
        {
            if (isAllocated && (qMan != null))
            {
                qMan.DeallocateBridge(BridgeId);
            }
            isAllocated = false;
        }

        /// <summary>
        /// Start over from all qubits in |0>, with the settings presently in the inspector fields.
        /// (This also clears any error state, and resets the fidelity estimate and other statistics.)
        /// </summary>
        public void ResetAll()
        {
            Deallocate();
            Allocate();
        }

        public int GetError()
        {
            return QuantumManager.GetBridgeError(BridgeId);
        }

        protected void CheckError()
        {
            if (GetError() != 0)
            {
                throw new InvalidOperationException("QBridge C++ library raised exception.");
            }
        }

        protected void CheckQubit(ulong qubitId)
        {
            if (qubitId >= allocQubitCount)
            {
                throw new ArgumentOutOfRangeException("qubitId", "Qubit " + qubitId + " is out of range for bridge " + BridgeId + ", with " + allocQubitCount + " qubits.");
            }
        }

        protected void CheckPair(ulong target1, ulong target2)
        {
            CheckQubit(target1);
            CheckQubit(target2);

            if (target1 == target2)
            {
                throw new ArgumentException("Two-qubit gate needs two different qubits, got " + target1 + " twice.");
            }
        }

        // 2x2 complex number matrices are serialized as 8 doubles, real-imaginary adjacent, then row-major.
        protected static readonly double[] mtrxX = { 0, 0, 1, 0, 1, 0, 0, 0 };
        protected static readonly double[] mtrxY = { 0, 0, 0, -1, 0, 1, 0, 0 };
        protected static readonly double[] mtrxZ = { 1, 0, 0, 0, 0, 0, -1, 0 };

        #region Single qubit gates

        // 2x2 complex number matrix gate, (serialized as 8 doubles, real-imaginary adjacent, then row-major)
        public void Mtrx(double[] m, ulong targetId)
        {
            CheckQubit(targetId);
            QuantumManager.BridgeMtrx(BridgeId, m, targetId);
            CheckError();
        }

        public void U(ulong targetId, double theta, double phi, double lambda)
        {
            CheckQubit(targetId);
            QuantumManager.BridgeU(BridgeId, targetId, theta, phi, lambda);
            CheckError();
        }

        public void X(ulong control, ulong targetId)
        {
            Mtrx(mtrxX, targetId);
        }

        public void Y(ulong control, ulong targetId)
        {
            Mtrx(mtrxY, targetId);
        }

        public void Z(ulong control, ulong targetId)
        {
            Mtrx(mtrxZ, targetId);
        }
        #endregion

        #region Couplers

        // These act between any two qubits, (bridged, if they are in different patches).

        protected void Controlled(ulong control, double[] m, ulong targetId, bool isAnti)
        {
            CheckPair(control, targetId);
            ulong[] controls = new ulong[] { control };
            if (isAnti)
            {
                QuantumManager.BridgeMACMtrx(BridgeId, 1, controls, m, targetId);
            }
            else
            {
                QuantumManager.BridgeMCMtrx(BridgeId, 1, controls, m, targetId);
            }
            CheckError();
        }

        public void CNOT(ulong control, ulong targetId)
        {
            Controlled(control, mtrxX, targetId, false);
        }

        public void CY(ulong control, ulong targetId)
        {
            Controlled(control, mtrxY, targetId, false);
        }

        public void CZ(ulong control, ulong targetId)
        {
            Controlled(control, mtrxZ, targetId, false);
        }

        // "Anti-"controlled gates act if the control is |0>.
        public void AntiCNOT(ulong control, ulong targetId)
        {
            Controlled(control, mtrxX, targetId, true);
        }

        public void AntiCY(ulong control, ulong targetId)
        {
            Controlled(control, mtrxY, targetId, true);
        }

        public void AntiCZ(ulong control, ulong targetId)
        {
            Controlled(control, mtrxZ, targetId, true);
        }

        public void SWAP(ulong target1, ulong target2)
        {
            CheckPair(target1, target2);
            QuantumManager.BridgeSWAP(BridgeId, target1, target2);
            CheckError();
        }

        public void ISWAP(ulong target1, ulong target2)
        {
            CheckPair(target1, target2);
            QuantumManager.BridgeISWAP(BridgeId, target1, target2);
            CheckError();
        }

        public void AdjISWAP(ulong target1, ulong target2)
        {
            CheckPair(target1, target2);
            QuantumManager.BridgeAdjISWAP(BridgeId, target1, target2);
            CheckError();
        }

        #endregion

        #region Readout

        /// <summary>
        /// Exact samples of the stored state, one per element of "measureResults," without collapsing the state.
        /// Bit i of each result is the outcome for qubits[i], (so, up to 64 qubits per call).
        /// </summary>
        public void MeasureShots(ulong[] qubits, ulong[] measureResults)
        {
            for (int i = 0; i < qubits.Length; ++i)
            {
                CheckQubit(qubits[i]);
            }

            QuantumManager.BridgeMeasureShots(BridgeId, (ulong)qubits.Length, qubits, (ulong)measureResults.Length, measureResults);
            CheckError();
        }

        /// <summary>
        /// Exact expectation value, for the stored state, of the tensor product of bases[i] on qubits[i],
        /// (and identity on every other qubit)
        /// </summary>
        public double PauliExpectation(ulong[] qubits, Pauli[] bases)
        {
            if (qubits.Length != bases.Length)
            {
                throw new ArgumentException("Qubit and Pauli basis lists differ in length.");
            }

            ulong[] b = new ulong[bases.Length];
            for (int i = 0; i < qubits.Length; ++i)
            {
                CheckQubit(qubits[i]);
                b[i] = (ulong)bases[i];
            }

            double toRet = QuantumManager.BridgePauliExpectation(BridgeId, (ulong)qubits.Length, qubits, b);
            CheckError();

            return toRet;
        }

        public double PauliExpectation(Pauli basis, ulong targetId)
        {
            return PauliExpectation(new ulong[] { targetId }, new Pauli[] { basis });
        }

        // Probability of |1>, in the Z basis
        public float Prob(ulong targetId)
        {
            return (float)((1 - PauliExpectation(Pauli.PauliZ, targetId)) / 2);
        }

        // (Unlike for QuantumSystem, this reads the three axes directly, without rotating the qubit there and back.)
        public BlochSphereCoordinates Prob3Axis(ulong targetId)
        {
            double probX = (1 - PauliExpectation(Pauli.PauliX, targetId)) / 2;
            double probY = (1 - PauliExpectation(Pauli.PauliY, targetId)) / 2;
            double probZ = (1 - PauliExpectation(Pauli.PauliZ, targetId)) / 2;

            return new BlochSphereCoordinates(probX, probY, probZ);
        }

        #endregion

        #region Statistics

        // QBridge's running estimate of fidelity, from the bond weight kept at each truncation, so far
        public double GetFidelityEstimate()
        {
            return QuantumManager.GetBridgeFidelityEstimate(BridgeId);
        }

        // Count of bond truncations that discarded weight
        public ulong GetTruncationCount()
        {
            return QuantumManager.GetBridgeTruncations(BridgeId);
        }

        // Count of truncating sweeps
        public ulong GetFlushCount()
        {
            return QuantumManager.GetBridgeFlushes(BridgeId);
        }

        // Most bond qubits that any cut has held
        public ulong GetPeakBondQubits()
        {
            return QuantumManager.GetBridgePeakBonds(BridgeId);
        }

        // Widest patch simulator ever allocated, (data qubits plus bond qubits)
        public ulong GetPeakPatchQubits()
        {
            return QuantumManager.GetBridgePeakPatchQubits(BridgeId);
        }

        // Bond qubits presently held by each cut between neighboring patches, (including the wrap-around cut, for a ring)
        public ulong[] GetBondQubits()
        {
            ulong[] bonds = new ulong[QuantumManager.GetBridgeCutCount(BridgeId)];
            if (bonds.Length > 0)
            {
                QuantumManager.GetBridgeBonds(BridgeId, bonds);
            }

            return bonds;
        }

        #endregion
    }
}
