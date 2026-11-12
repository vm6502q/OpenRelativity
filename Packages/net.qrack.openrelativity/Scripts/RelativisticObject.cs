using OpenRelativity.ConformalMaps;
using OpenRelativity.Support;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace OpenRelativity
{
    [ExecuteInEditMode]
    public class RelativisticObject : RelativisticBehavior
    {
        #region Public Settings
        public bool isLightMapStatic = false;
        public bool useGravity = false;
        // Use this instead of relativistic parent
        public bool isParent = false;
        // Combine colliders under us in the hierarchy
        public bool isCombinedColliderParent = false;
        // Use this if not using an explicitly relativistic shader
        public bool isNonrelativisticShader = false;
        // We set the Rigidbody "drag" parameter in this object.
        public float unityDrag = 0;
        // We also set the Rigidbody "angularDrag" parameter in this object.
        public float unityAngularDrag = 0;
        // Comove via ConformalMap acceleration, or ComoveOptical?
        public bool comoveViaAcceleration = false;
        // The composite scalar monopole graviton gas is described by statistical mechanics and heat flow equations
        public float gravitonEmissivity = 0.1f;
        // By default, 12g per baryon mole would be carbon-12, and this controls the total baryons estimated in the object
        public float fundamentalAverageMolarMass = 0.012f;
        public float initialAverageMolarMass = 0.012f;
        public float currentAverageMolarMass
        {
            get
            {
                if (myRigidbody)
                {
                    return myRigidbody.mass * SRelativityUtil.avogadroNumber / baryonCount;
                }

                return 0;
            }
            protected set
            {
                if (myRigidbody)
                {
                    myRigidbody.mass = value * baryonCount / SRelativityUtil.avogadroNumber;
                }
            }
        }

        // Set with Rigidbody isKinematic flag instead
        public bool isKinematic
        {
            get
            {
                if (myRigidbody)
                {
                    return myRigidbody.isKinematic;
                }

                return false;
            }

            set
            {
                if (myRigidbody)
                {
                    myRigidbody.isKinematic = value;
                }
            }
        }
        #endregion

        protected bool isPhysicsUpdateFrame;

        protected float updateViwTimeFactor;
        protected float updatePlayerViwTimeFactor;
        protected float updateTisw;
        protected Matrix4x4 updateMetric;
        protected Vector4 updateWorld4Acceleration;

        #region Local Time
        //Acceleration desyncronizes our clock from the world clock:
        public float localTimeOffset { get; private set; }
        private float oldLocalTimeUpdate;
        public float localDeltaTime
        {
            get
            {
                return GetLocalTime() - oldLocalTimeUpdate;
            }
        }
        public float localFixedDeltaTime
        {
            get
            {
                return GetLocalTime() - oldLocalTimeFixedUpdate;
            }
        }
        private float oldLocalTimeFixedUpdate;
        public float GetLocalTime()
        {
            return state.TotalTimeWorld + localTimeOffset;
        }
        public void ResetLocalTime()
        {
            localTimeOffset = 0;
        }
        public float GetTisw()
        {
            if (isPhysicsCacheValid)
            {
                return updateTisw;
            }

            return piw.GetTisw(viw, GetComoving4Acceleration());
        }
        public float GetVisualTime()
        {
            return GetLocalTime() + GetTisw();
        }
        #endregion

        #region 4-vector relativity
        // This is the metric tensor in an accelerated frame in special relativity.
        // Special relativity assumes a flat metric, (the "Minkowski metric").
        // In general relativity, the underlying metric could be curved, according to the Einstein field equations.
        // The (flat) metric appears to change due to proper acceleration from the player's/camera's point of view, since acceleration is not physically relative like velocity.
        // (Physically, proper acceleration could be detected by a force on the observer in the opposite direction from the acceleration,
        // like being pushed back into the seat of an accelerating car. When we stand still on the surface of earth, we feel our weight as the ground exerts a normal force "upward,"
        // which is equivalent to an acceleration in the opposite direction from the ostensible Newtonian gravity field, similar to the car.
        // "Einstein equivalence principle" says that, over small enough regions, we can't tell the difference between
        // a uniform acceleration and a gravitational field, that the two are physically equivalent over small enough regions of space.
        // In free-fall, gravitational fields disappear. Hence, when the player is in free-fall, their acceleration is considered to be zero,
        // while it is considered to be "upwards" when they are at rest under the effects of gravity, so they don't fall through the surface they're feeling pushed into.)
        // The apparent deformation of the Minkowski metric also depends on an object's distance from the player, so it is calculated by and for the object itself.

        public Matrix4x4 GetMetric()
        {
            if (isPhysicsCacheValid)
            {
                return updateMetric;
            }

            Matrix4x4 metric = SRelativityUtil.GetRindlerMetric(piw);
            Matrix4x4 invVpcLorentz = state.PlayerLorentzMatrix.inverse;
            metric = invVpcLorentz.transpose * metric * invVpcLorentz;

            Matrix4x4 intrinsicMetric = state.conformalMap.GetMetric(piw);
            return intrinsicMetric * metric * intrinsicMetric.inverse;
        }

        public Vector4 Get4Velocity()
        {
            return viw.ToMinkowski4Viw();
        }

        public Vector4 GetComoving4Acceleration()
        {
            if (isPhysicsCacheValid)
            {
                return updateWorld4Acceleration;
            }

            return comovingAccel.ProperToWorldAccel(viw, GetTimeFactor());
        }

        public Vector4 GetWorld4Acceleration()
        {
            return aiw.ProperToWorldAccel(viw, GetTimeFactor());
        }

        // This is the factor commonly referred to as "gamma," for length contraction and time dilation,
        // only also with consideration for a gravitationally curved background, such as due to Rindler coordinates.
        // (Rindler coordinates are actually Minkowski flat, but the same principle applies.)
        public float GetTimeFactor()
        {
            if (isPhysicsCacheValid)
            {
                return updateViwTimeFactor;
            }

            // However, sometimes we want a different velocity, at this space-time point,
            // such as this RO's own velocity.
            return viw.InverseGamma(GetMetric());
        }
        #endregion

        #region Rigid body physics
        private bool wasKinematic;
        private CollisionDetectionMode collisionDetectionMode;
        private PhysicsMaterial[] origPhysicMaterials;

        private Vector3 oldVelocity;
        // Celerity (gamma v) handed to PhysX at the start of the current physics step, for collision correction.
        private Vector3 preStepCelerity;
        // Physics time at which this object last read its state back from PhysX.
        private float afterPhysicsStamp = -1;
        // PhysX resolves collisions conserving m*u (exactly relativistic momentum) but also the Newtonian energy
        // m u^2 / 2, which is not relativistic energy: elastic collisions between unequal masses then create or
        // destroy energy at relativistic speed. With this on, the normal part of each RelativisticObject pair
        // collision is redone in the pair's centre-of-momentum frame (PhysX's restitution and friction kept).
        public bool relativisticCollisions = true;
        private float lastFixedUpdateDeltaTime;

        public float baryonCount { get; set; }

        public float mass
        {
            get
            {
                if (myRigidbody)
                {
                    return myRigidbody.mass;
                }

                return 0;
            }

            set
            {
                if (myRigidbody)
                {
                    myRigidbody.mass = value;
                }
            }
        }

        public void AddForce(Vector3 force, ForceMode mode = ForceMode.Force)
        {
            if (!myRigidbody) {
                if (mode == ForceMode.Impulse)
                {
                    return;
                }
                if (mode == ForceMode.Force)
                {
                    return;
                }
            }

            switch (mode)
            {
                case ForceMode.Impulse:
                    // Rest-frame impulse J changes rest-frame celerity by J/m; compose relativistically.
                    peculiarVelocity = peculiarVelocity.AddVelocity((force / mass).RapidityToVelocity());
                    break;
                case ForceMode.VelocityChange:
                    // Rest-frame velocity change (|force| < c), composed relativistically.
                    peculiarVelocity = peculiarVelocity.AddVelocity(force);
                    break;
                case ForceMode.Force:
                    nonGravAccel += force / mass;
                    break;
                case ForceMode.Acceleration:
                    nonGravAccel += force;
                    break;
            }
        }

        //Store world position, mostly for a nonrelativistic shader:
        protected Vector3 _piw;
        public Vector3 piw {
            get
            {
                return _piw;
            }
            set
            {
                if ((value - _piw).sqrMagnitude <= SRelativityUtil.FLT_EPSILON) {
                    return;
                }

                _piw = value;
                UpdatePhysicsCaches();
            }
        }

        public Vector3 opticalPiw {
            get
            {
                return piw.WorldToOptical(peculiarVelocity, GetComoving4Acceleration());
            }
            set
            {
                piw = value.OpticalToWorld(peculiarVelocity, GetComoving4Acceleration());
            }
        }

        public void ResetPiw()
        {
            piw = isNonrelativisticShader ? transform.position.OpticalToWorld(peculiarVelocity, GetComoving4Acceleration()) : transform.position;
        }
        //Store rotation quaternion
        public Quaternion riw { get; set; }

        public Vector3 _peculiarVelocity = Vector3.zero;
        public Vector3 peculiarVelocity
        {
            get
            {
                return _peculiarVelocity;
            }

            set
            {
                // Skip this all, if the change is negligible.
                if ((value - _peculiarVelocity).sqrMagnitude <= SRelativityUtil.FLT_EPSILON)
                {
                    return;
                }

                UpdateMotion(value, nonGravAccel);
                UpdateRigidbodyVelocity();
            }
        }

        public Vector3 vff
        {
            get
            {
                return state.conformalMap ? state.conformalMap.GetFreeFallVelocity(piw) : Vector3.zero;
            }
        }

        public Vector3 viw
        {
            get
            {
                return vff.AddVelocity(peculiarVelocity);
            }

            set
            {
                peculiarVelocity = (-vff).AddVelocity(value);
            }
        }

        //Store this object's angular velocity here.
        public Vector3 _aviw;
        public Vector3 aviw
        {
            get
            {
                return _aviw;
            }
            set
            {
                if (!isKinematic)
                {
                    _aviw = value;
                    UpdateRigidbodyVelocity();
                }
            }
        }

        // This is the part of acceleration that can be set.
        public Vector3 _nonGravAccel;
        public Vector3 nonGravAccel
        {
            get
            {
                return _nonGravAccel;
            }

            set
            {
                // Skip this all, if the change is negligible.
                if (isKinematic || (value - _nonGravAccel).sqrMagnitude <= SRelativityUtil.FLT_EPSILON)
                {
                    return;
                }

                UpdateMotion(peculiarVelocity, value);
                UpdateRigidbodyVelocity();
            }
        }

        // This would be the object's properAccleration if at rest in "world coordinates."
        public Vector3 aiw
        {
            get
            {
                Vector3 accel = nonGravAccel + leviCivitaDevAccel;

                if (useGravity)
                {
                    accel += Physics.gravity;
                }

                accel += state.conformalMap.GetRindlerAcceleration(piw);

                return accel;
            }

            set
            {
                Vector3 accel = value;

                if (state.conformalMap)
                {
                    accel -= state.conformalMap.GetRindlerAcceleration(piw);
                }

                if (useGravity)
                {
                    accel -= Physics.gravity;
                }

                accel -= leviCivitaDevAccel;

                nonGravAccel = accel;
            }
        }

        // This would be the object's "proper acceleration" in comoving coordinates,
        // but we compose with Rindler metric for Physics.gravity, so that we can
        // keep book as the Physics.gravity vector effect being an intrinsic property.
        public Vector3 comovingAccel
        {
            get
            {
                if (myRigidbody && myRigidbody.IsSleeping())
                {
                    return Vector3.zero;
                }

                return nonGravAccel;
            }
            set
            {
                nonGravAccel = value;
            }
        }

        public float monopoleTemperature
        {
            get
            {
                if (!myRigidbody)
                {
                    return 0;
                }

                // Per Strano 2019, due to the interaction with the thermal graviton gas radiated by the Rindler horizon,
                // there is also a change in mass. However, the monopole waves responsible for this are seen from a first-person perspective,
                // (i.e. as due to "player" acceleration).

                // If a gravitating body this RO is attracted to is already excited above the rest mass vacuum,
                // (which seems to imply the Higgs field vacuum)
                // then it will spontaneously emit this excitation, with a coupling constant proportional to the
                // gravitational constant "G" times (baryon) constituent particle rest mass.

                double nuclearMass = mass / baryonCount;
                double fundamentalNuclearMass = fundamentalAverageMolarMass / SRelativityUtil.avogadroNumber;

                if (nuclearMass < fundamentalNuclearMass)
                {
                    // We can't support negative temperatures, yet, for realistic constants,
                    // (not that we'd necessarily want them).
                    return 0;
                }

                double excitationEnergy = (nuclearMass - fundamentalNuclearMass) * state.SpeedOfLightSqrd;
                double temperature = 2 * excitationEnergy / state.boltzmannConstant;

                return (float)temperature;

                //... But just turn "doDegradeAccel" off, if you don't want this effect for any reason.
            }

            set
            {
                if (!myRigidbody)
                {
                    return;
                }

                double fundamentalNuclearMass = fundamentalAverageMolarMass / SRelativityUtil.avogadroNumber;
                double excitationEnergy = value * state.boltzmannConstant / 2;
                if (excitationEnergy < 0)
                {
                    excitationEnergy = 0;
                }
                double nuclearMass = excitationEnergy / state.SpeedOfLightSqrd + fundamentalNuclearMass;

                mass = (float)(nuclearMass * baryonCount);
            }
        }

        public Vector3 leviCivitaDevAccel = Vector3.zero;

        // Strano 2019 monopole (spin-0 gravitational mode) self-force state; see EvaporateMonopole().
        // Coupling of the monopole mode to rest mass, relative to gravitational strength (tau0 = coupling * G * m / c^3).
        public float monopoleCoupling = 1.0f;
        // Total energy radiated into monopole waves so far (supplied by the applied forces, not the rest mass).
        public double monopoleRadiatedEnergy = 0;
        private Vector3 lastMonopoleAccel = Vector3.zero;
        private bool hasLastMonopoleAccel = false;
        // Optional, independent of the jerk reaction: restore the Strano 2019 thermal-field postulate, in which the
        // Rindler horizon of a constantly accelerated frame evaporates and the proper acceleration slowly decays
        // (a choice of "Unruh-like" quantum state for the monopole scalar; off = Minkowski vacuum, i.e. equilibrium).
        public bool isMonopoleHorizonEvaporation = false;
        // Horizon-evaporation law for the monopole channel: black-body (Hawking-order, ~1/M^2) by default, with this many
        // massless scalar channels; set useLegacy2019PowerLaw to reproduce the original 2019 (~1/M) rule.
        public float monopoleHorizonChannels = 1.0f;
        public bool useLegacy2019PowerLaw = false;
        private Vector3 horizonDevAccel = Vector3.zero;
        // Optional: the static (near-field) force the same mass-coupled scalar must mediate, i.e. a Yukawa "fifth
        // force" between RelativisticObjects, alpha_ij * G * m_i * m_j * exp(-r/lambda) * (1 + r/lambda) / r^2, with
        // alpha_ij = sqrt(alpha_i * alpha_j) and lambda = state.monopoleFifthForceRange (0 = unscreened, infinite range).
        // Both objects need a coupling > 0; this object needs isMonopoleAccel to feel it. 0 (default) = off.
        public float monopoleFifthForceCoupling = 0;
        private static readonly List<RelativisticObject> monopoleRegistry = new List<RelativisticObject>();

        public void UpdateMotion(Vector3 pvf, Vector3 af)
        {
            // Changing velocities lose continuity of position,
            // unless we transform the world position to optical position with the old velocity,
            // and inverse transform the optical position with the new the velocity.
            // (This keeps the optical position fixed.)

            Vector3 pvi = peculiarVelocity;
            Vector3 ai = comovingAccel;
            _nonGravAccel = af;
            _peculiarVelocity = pvf;

            float timeFac = GetTimeFactor();

            _piw = piw.WorldToOptical(pvi, ai.ProperToWorldAccel(pvi, timeFac)).OpticalToWorld(pvf, comovingAccel.ProperToWorldAccel(pvf, timeFac));

            if (isNonrelativisticShader)
            {
                UpdateContractorPosition();
            }
            else
            {
                transform.position = piw;
            }

            UpdatePhysicsCaches();
            UpdateShaderParams();
        }
        #endregion

        #region Nonrelativistic Shader/Collider
        //If the shader is not relativistic, we need to handle length contraction with a "contractor" transform.
        private Transform contractor;
        //Changing a transform's parent is expensive, but we can avoid it with this:
        public Vector3 _localScale;
        public Vector3 localScale
        {
            get
            {
                return _localScale;
            }
            set
            {
                transform.localScale = value;
                _localScale = value;
            }
        }
        //If we specifically have a mesh collider, we need to know to transform the verts of the mesh itself.
        private bool isMyColliderMesh;
        private bool isMyColliderVoxel;
        private bool isMyColliderGeneral {
            get {
                return !isMyColliderMesh && !isMyColliderVoxel;
            }
        }

        //If we have a collider to transform, we cache it here
        private Collider[] myColliders;
        private SphereCollider[] mySphereColliders;
        private BoxCollider[] myBoxColliders;
        private CapsuleCollider[] myCapsuleColliders;

        private Vector3[] colliderPiw { get; set; }
        public void MarkStaticColliderPos()
        {
            if (isMyColliderGeneral)
            {
                List<Vector3> sttcPosList = new List<Vector3>();

                for (int i = 0; i < mySphereColliders.Length; ++i)
                {
                    sttcPosList.Add(mySphereColliders[i].center);
                }

                for (int i = 0; i < myBoxColliders.Length; ++i)
                {
                    sttcPosList.Add(myBoxColliders[i].center);
                }

                for (int i = 0; i < myCapsuleColliders.Length; ++i)
                {
                    sttcPosList.Add(myCapsuleColliders[i].center);
                }

                colliderPiw = sttcPosList.ToArray();
            }
        }
        #endregion

        #region RelativisticObject properties and caching
        //Don't render if object has relativistic parent
        private bool hasParent = false;
        //Keep track of our own Mesh Filter
        private MeshFilter meshFilter;

        //When was this object created? use for moving objects
        private bool hasStarted;
        private float _StartTime = float.NegativeInfinity;
        public float StartTime { get { return _StartTime; } set { _StartTime = value; } }
        //When should we die? again, for moving objects
        private float _DeathTime = float.PositiveInfinity;
        public float DeathTime { get { return _DeathTime; } set { _DeathTime = value; } }

        //We save and reuse the transformed vert array to avoid garbage collection 
        private Vector3[] trnsfrmdMeshVerts;
        //We create a new collider mesh, so as not to interfere with primitives, and reuse it
        private Mesh trnsfrmdMesh;
        //If we have a Rigidbody, we cache it here
        private Rigidbody myRigidbody;
        //If we have a Renderer, we cache it, too.
        public Renderer myRenderer { get; set; }

        //We need to freeze any attached rigidbody if the world states is frozen 
        public bool wasFrozen { get; set; }

        // Based on Strano 2019, (preprint).
        // (I will always implement potentially "cranky" features so you can toggle them off, but I might as well.)
        public bool isMonopoleAccel = false;
        public float monopoleAccelerationSoften = 0;
        #endregion

        #region Collider transformation and update
        // We use an attached shader to transform the collider verts:
        public ComputeShader colliderShader;
        // If the object is light map static, we need a duplicate of its mesh
        public Mesh colliderShaderMesh;
        // We set global constants in a struct
        private ShaderParams colliderShaderParams;
        // Mesh collider params
        private ComputeBuffer paramsBuffer;
        // Mesh collider vertices
        private ComputeBuffer vertBuffer;

        private void UpdateMeshCollider(MeshCollider transformCollider)
        {
            if (colliderShaderMesh == null || colliderShaderMesh.vertexCount == 0)
            {
                return;
            }

            if (paramsBuffer == null)
            {
                paramsBuffer = new ComputeBuffer(1, System.Runtime.InteropServices.Marshal.SizeOf(colliderShaderParams));
            }

            if (vertBuffer == null)
            {
                vertBuffer = new ComputeBuffer(colliderShaderMesh.vertexCount, System.Runtime.InteropServices.Marshal.SizeOf(new Vector3()));
            }

            //Freeze the physics if the global state is frozen.
            if (state.isMovementFrozen)
            {
                if (!wasFrozen)
                {
                    //Read the state of the rigidbody and shut it off, once.
                    wasFrozen = true;
                    if (!myRigidbody) {
                        wasKinematic = true;
                        collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                    }
                    else      
                    {
                        wasKinematic = myRigidbody.isKinematic;
                        collisionDetectionMode = myRigidbody.collisionDetectionMode;
                        myRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                        myRigidbody.isKinematic = true;
                    }
                }

                return;
            }
            
            if (wasFrozen)
            {
                //Restore the state of the rigidbody, once.
                wasFrozen = false;
                if (myRigidbody)
                {
                    myRigidbody.isKinematic = wasKinematic;
                    myRigidbody.collisionDetectionMode = collisionDetectionMode;
                }
            }

            //Set remaining global parameters:
            colliderShaderParams.ltwMatrix = transform.localToWorldMatrix;
            colliderShaderParams.wtlMatrix = transform.worldToLocalMatrix;
            colliderShaderParams.vpc = -state.PlayerVelocityVector / state.SpeedOfLight;
            colliderShaderParams.pap = state.PlayerAccelerationVector;
            colliderShaderParams.avp = state.PlayerAngularVelocityVector;
            colliderShaderParams.playerOffset = state.playerTransform.position;
            colliderShaderParams.spdOfLight = state.SpeedOfLight;
            colliderShaderParams.vpcLorentzMatrix = state.PlayerLorentzMatrix;
            colliderShaderParams.invVpcLorentzMatrix = state.PlayerLorentzMatrix.inverse;
            Matrix4x4 metric = state.conformalMap.GetMetric(piw);
            colliderShaderParams.intrinsicMetric = metric;
            colliderShaderParams.invIntrinsicMetric = metric.inverse;

            ShaderParams[] spa = new ShaderParams[1];
            spa[0] = colliderShaderParams;
            //Put verts in R/W buffer and dispatch:
            paramsBuffer.SetData(spa);
            vertBuffer.SetData(colliderShaderMesh.vertices);
            int kernel = colliderShader.FindKernel("CSMain");
            colliderShader.SetBuffer(kernel, "glblPrms", paramsBuffer);
            colliderShader.SetBuffer(kernel, "verts", vertBuffer);
            colliderShader.Dispatch(kernel, colliderShaderMesh.vertexCount, 1, 1);
            vertBuffer.GetData(trnsfrmdMeshVerts);

            //Change mesh:
            trnsfrmdMesh.vertices = trnsfrmdMeshVerts;
            trnsfrmdMesh.RecalculateBounds();
            trnsfrmdMesh.RecalculateNormals();
            trnsfrmdMesh.RecalculateTangents();
            transformCollider.sharedMesh = trnsfrmdMesh;

            if (myRigidbody)
            {
                myRigidbody.ResetCenterOfMass();
                myRigidbody.ResetInertiaTensor();
            }
        }

        public void UpdateColliders()
        {
            MeshCollider[] myMeshColliders = GetComponents<MeshCollider>();

            //Get the vertices of our mesh
            if (!colliderShaderMesh && meshFilter && meshFilter.sharedMesh.isReadable)
            {
                colliderShaderMesh = Instantiate(meshFilter.sharedMesh);
            }

            if (colliderShaderMesh)
            {
                trnsfrmdMesh = Instantiate(colliderShaderMesh);
                trnsfrmdMeshVerts = (Vector3[])trnsfrmdMesh.vertices.Clone();
                trnsfrmdMesh.MarkDynamic();

                if (!enabled || !gameObject.activeInHierarchy)
                {
                    UpdateMeshCollider(myMeshColliders[0]);
                }
            }

            if (GetComponent<ObjectBoxColliderDensity>())
            {
                isMyColliderVoxel = true;
                isMyColliderMesh = false;
            }
            else
            {
                myColliders = myMeshColliders;
                if (myColliders.Length > 0)
                {
                    isMyColliderMesh = true;
                    isMyColliderVoxel = false;
                }
                else
                {
                    isMyColliderMesh = false;
                    isMyColliderVoxel = false;
                }
            }

            mySphereColliders = GetComponents<SphereCollider>();
            myBoxColliders = GetComponents<BoxCollider>();
            myCapsuleColliders = GetComponents<CapsuleCollider>();

            myColliders = GetComponents<Collider>();
            List<PhysicsMaterial> origMaterials = new List<PhysicsMaterial>();
            for (int i = 0; i < myColliders.Length; ++i)
            {
                // Friction needs a relativistic correction, so we need variable PhysicMaterial parameters.
                Collider collider = myColliders[i];
                origMaterials.Add(collider.material);
                collider.material = Instantiate(collider.material);
            }
            origPhysicMaterials = origMaterials.ToArray();
        }

        public void UpdateColliderPosition()
        {
            if (isMyColliderVoxel || isNonrelativisticShader || (myColliders == null) || (myColliders.Length == 0))
            {
                return;
            }

            //If we have a MeshCollider and a compute shader, transform the collider verts relativistically:
            if (isMyColliderMesh && colliderShader && SystemInfo.supportsComputeShaders)
            {
                UpdateMeshCollider((MeshCollider)myColliders[0]);
            }
            //If we have a Collider, transform its center to its optical position
            else if (isMyColliderGeneral)
            {
                Vector4 aiw4 = GetComoving4Acceleration();

                int iTot = 0;
                for (int i = 0; i < mySphereColliders.Length; ++i)
                {
                    SphereCollider collider = mySphereColliders[i];
                    Vector3 pos = transform.TransformPoint((Vector4)colliderPiw[iTot]);
                    Vector3 pw = pos.WorldToOptical(peculiarVelocity, aiw4);
                    Vector3 testPos = transform.InverseTransformPoint(pw);
                    collider.center = testPos;
                    ++iTot;
                }

                for (int i = 0; i < myBoxColliders.Length; ++i)
                {
                    BoxCollider collider = myBoxColliders[i];
                    Vector3 pos = transform.TransformPoint((Vector4)colliderPiw[iTot]);
                    Vector3 pw = pos.WorldToOptical(peculiarVelocity, aiw4);
                    Vector3 testPos = transform.InverseTransformPoint(pw);
                    collider.center = testPos;
                    ++iTot;
                }

                for (int i = 0; i < myCapsuleColliders.Length; ++i)
                {
                    CapsuleCollider collider = myCapsuleColliders[i];
                    Vector3 pos = transform.TransformPoint((Vector4)colliderPiw[iTot]);
                    Vector3 pw = pos.WorldToOptical(peculiarVelocity, aiw4);
                    Vector3 testPos = transform.InverseTransformPoint(pw);
                    collider.center = testPos;
                    ++iTot;
                }
            }
        }
        #endregion

        #region Nonrelativistic shader
        private void SetUpContractor()
        {
            _localScale = transform.localScale;
            if (contractor)
            {
                contractor.parent = null;
                contractor.localScale = new Vector3(1, 1, 1);
                transform.parent = null;
                Destroy(contractor.gameObject);
            }
            GameObject contractorGO = new GameObject();
            contractorGO.name = gameObject.name + " Contractor";
            contractor = contractorGO.transform;
            contractor.parent = transform.parent;
            contractor.position = transform.position;
            transform.parent = contractor;
            transform.localPosition = Vector3.zero;
        }

        public void ContractLength()
        {
            Vector3 playerVel = state.PlayerVelocityVector;
            Vector3 relVel = (-playerVel).AddVelocity(peculiarVelocity);
            float relVelMag = relVel.sqrMagnitude;

            if (relVelMag > (state.MaxSpeed))
            {
                relVel.Normalize();
                relVelMag = state.MaxSpeed;
                relVel = relVelMag * relVel;
            }

            //Undo length contraction from previous state, and apply updated contraction:
            // - First, return to world frame:
            contractor.localScale = new Vector3(1, 1, 1);
            transform.localScale = _localScale;

            if (relVelMag > SRelativityUtil.FLT_EPSILON)
            {
                Quaternion rot = transform.rotation;

                relVelMag = Mathf.Sqrt(relVelMag);
                // - If we need to contract the object, unparent it from the contractor before rotation:
                //transform.parent = cparent;

                Quaternion origRot = transform.rotation;

                // - Rotate contractor to point parallel to velocity relative player:
                contractor.rotation = Quaternion.FromToRotation(Vector3.forward, relVel / relVelMag);

                // - Re-parent the object to the contractor before length contraction:
                transform.rotation = origRot;

                // - Set the scale based only on the velocity relative to the player:
                contractor.localScale = new Vector3(1, 1, 1).ContractLengthBy(relVelMag * Vector3.forward);
            }
        }

        public void UpdateContractorPosition()
        {
            if (!Application.isPlaying || !isNonrelativisticShader)
            {
                return;
            }

            if (!contractor)
            {
                SetUpContractor();
            }

            contractor.position = opticalPiw;
            transform.localPosition = Vector3.zero;
            ContractLength();
        }
        #endregion

        #region RelativisticObject internals

        // Get the start time of our object, so that we know where not to draw it
        public void SetStartTime()
        {
            Vector3 playerPos = state.playerTransform.position;
            float timeDelayToPlayer = Mathf.Sqrt((opticalPiw - playerPos).sqrMagnitude / state.SpeedOfLightSqrd);
            timeDelayToPlayer *= GetTimeFactor();
            StartTime = state.TotalTimeWorld - timeDelayToPlayer;
            hasStarted = false;
            if (myRenderer)
                myRenderer.enabled = false;
        }

        //Set the death time, so that we know at what point to destroy the object in the player's view point.
        public virtual void SetDeathTime()
        {
            Vector3 playerPos = state.playerTransform.position;
            float timeDelayToPlayer = Mathf.Sqrt((opticalPiw - playerPos).sqrMagnitude / state.SpeedOfLightSqrd);
            timeDelayToPlayer *= GetTimeFactor();
            DeathTime = state.TotalTimeWorld - timeDelayToPlayer;
        }
        public void ResetDeathTime()
        {
            DeathTime = float.PositiveInfinity;
        }

        void CombineParent()
        {
            if (GetComponent<ObjectMeshDensity>())
            {
                GetComponent<ObjectMeshDensity>().enabled = false;
            }
            bool wasStatic = gameObject.isStatic;
            gameObject.isStatic = false;
            int vertCount = 0, triangleCount = 0;
            Matrix4x4 worldLocalMatrix = transform.worldToLocalMatrix;

            //This code combines the meshes of children of parent objects
            //This increases our FPS by a ton
            //Get an array of the meshfilters
            MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>(true);
            //Count submeshes
            int[] subMeshCount = new int[meshFilters.Length];
            //Get all the meshrenderers
            MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>(true);
            //Length of our original array
            int meshFilterLength = meshFilters.Length;
            //And a counter
            int subMeshCounts = 0;
            //We can optimize further for duplicate materials:
            Dictionary<string, Material> uniqueMaterials = new Dictionary<string, Material>();
            List<string> uniqueMaterialNames = new List<string>();

            //For every meshfilter,
            for (int y = 0; y < meshFilterLength; ++y)
            {
                //If it's null, ignore it.
                if (!meshFilters[y]) continue;
                if (!meshFilters[y].sharedMesh) continue;
                if (!meshFilters[y].sharedMesh.isReadable) continue;
                //else add its vertices to the vertcount
                vertCount += meshFilters[y].sharedMesh.vertices.Length;
                //Add its triangles to the count
                triangleCount += meshFilters[y].sharedMesh.triangles.Length;
                //Add the number of submeshes to its spot in the array
                subMeshCount[y] = meshFilters[y].mesh.subMeshCount;
                //And add up the total number of submeshes
                subMeshCounts += meshFilters[y].mesh.subMeshCount;
            }
            // Get a temporary array of EVERY vertex
            Vector3[] tempVerts = new Vector3[vertCount];
            //And make a triangle array for every submesh
            int[][] tempTriangles = new int[subMeshCounts][];

            for (int u = 0; u < subMeshCounts; ++u)
            {
                //Make every array the correct length of triangles
                tempTriangles[u] = new int[triangleCount];
            }
            //Also grab our UV texture coordinates
            Vector2[] tempUVs = new Vector2[vertCount];
            //And store a number of materials equal to the number of submeshes.
            Material[] tempMaterials = new Material[subMeshCounts];

            int vertIndex = 0;
            Mesh MFs;
            int subMeshIndex = 0;
            //For all meshfilters
            for (int i = 0; i < meshFilterLength; ++i)
            {
                //just doublecheck that the mesh isn't null
                MFs = meshFilters[i].sharedMesh;
                if (!MFs) continue;
                if (!MFs.isReadable) continue;

                //Otherwise, for all submeshes in the current mesh
                for (int q = 0; q < subMeshCount[i]; ++q)
                {
                    //turn off the original renderer
                    meshRenderers[i].enabled = false;
                    RelativisticObject ro = meshRenderers[i].GetComponent<RelativisticObject>();
                    if (ro)
                    {
                        ro.hasParent = true;
                    }
                    //grab its material
                    tempMaterials[subMeshIndex] = meshRenderers[i].materials[q];
                    //Check if material is unique
                    string name = meshRenderers[i].materials[q].name.Replace(" (Instance)", "");
                    if (!uniqueMaterials.ContainsKey(name))
                    {
                        uniqueMaterials.Add(name, meshRenderers[i].materials[q]);
                        uniqueMaterialNames.Add(name);
                    }
                    //Grab its triangles
                    int[] tempSubTriangles = MFs.GetTriangles(q);
                    //And put them into the submesh's triangle array
                    for (int k = 0; k < tempSubTriangles.Length; ++k)
                    {
                        tempTriangles[subMeshIndex][k] = tempSubTriangles[k] + vertIndex;
                    }
                    //Increment the submesh index
                    ++subMeshIndex;
                }
                Matrix4x4 cTrans = worldLocalMatrix * meshFilters[i].transform.localToWorldMatrix;
                //For all the vertices in the mesh
                for (int v = 0; v < MFs.vertices.Length; ++v)
                {
                    //Get the vertex and the UV coordinate
                    tempVerts[vertIndex] = cTrans.MultiplyPoint3x4(MFs.vertices[v]);
                    tempUVs[vertIndex] = MFs.uv[v];
                    ++vertIndex;
                }
            }

            //Put it all together now.
            Mesh myMesh = new Mesh();
            //If any materials are the same, we can combine triangles and give them the same material.
            myMesh.subMeshCount = uniqueMaterials.Count;
            myMesh.vertices = tempVerts;
            Material[] finalMaterials = new Material[uniqueMaterials.Count];
            for (int i = 0; i < uniqueMaterialNames.Count; ++i)
            {
                string uniqueName = uniqueMaterialNames[i];
                List<int> combineTriangles = new List<int>();
                for (int j = 0; j < tempMaterials.Length; ++j)
                {
                    string name = tempMaterials[j].name.Replace(" (Instance)", "");
                    if (uniqueName.Equals(name))
                    {
                        combineTriangles.AddRange(tempTriangles[j]);
                    }
                }
                myMesh.SetTriangles(combineTriangles.ToArray(), i);
                finalMaterials[i] = uniqueMaterials[uniqueMaterialNames[i]];
            }
            //Just shunt in the UV coordinates, we don't need to change them
            myMesh.uv = tempUVs;
            //THEN totally replace our object's mesh with this new, combined mesh

            MeshFilter meshy = gameObject.GetComponent<MeshFilter>();
            if (!GetComponent<MeshFilter>())
            {
                gameObject.AddComponent<MeshRenderer>();
                meshy = gameObject.AddComponent<MeshFilter>();
            }
            meshy.mesh = myMesh;

            GetComponent<MeshRenderer>().enabled = false;

            meshy.mesh.RecalculateNormals();
            if (uniqueMaterials.Count == 1)
            {
                meshy.GetComponent<Renderer>().material = finalMaterials[0];
            }
            else
            {
                meshy.GetComponent<Renderer>().materials = finalMaterials;
            }

            MeshCollider mCollider = GetComponent<MeshCollider>();
            if (mCollider)
            {
                mCollider.sharedMesh = myMesh;
            }

            transform.gameObject.SetActive(true);
            gameObject.isStatic = wasStatic;

            if (isCombinedColliderParent)
            {
                MeshCollider myMeshCollider = GetComponent<MeshCollider>();
                if (myMeshCollider)
                {
                    myMeshCollider.sharedMesh = myMesh;
                }  
            }
            else
            {
                MeshCollider[] childrenColliders = GetComponentsInChildren<MeshCollider>();
                List<Collider> dupes = new List<Collider>();
                for (int i = 0; i < childrenColliders.Length; ++i)
                {
                    MeshCollider orig = childrenColliders[i];
                    MeshCollider dupe = CopyComponent(childrenColliders[i], gameObject);
                    dupe.convex = orig.convex;
                    dupe.sharedMesh = Instantiate(orig.sharedMesh);
                    dupes.Add(dupe);
                }
                if (myColliders == null)
                {
                    myColliders = dupes.ToArray();
                }
                else
                {
                    dupes.AddRange(myColliders);
                    myColliders = dupes.ToArray();
                }
            }
            //"Delete" all children.
            for (int i = 0; i < transform.childCount; ++i)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (child.tag != "Contractor" && child.tag != "Voxel Collider")
                {
                    transform.GetChild(i).gameObject.SetActive(false);
                    Destroy(transform.GetChild(i).gameObject);
                }
            }
            GetComponent<MeshRenderer>().enabled = true;
            GetComponent<RelativisticObject>().enabled = true;
        }

        T CopyComponent<T>(T original, GameObject destination) where T : Component
        {
            System.Type type = original.GetType();
            Component copy = destination.AddComponent(type);
            System.Reflection.FieldInfo[] fields = type.GetFields();
            foreach (System.Reflection.FieldInfo field in fields)
            {
                field.SetValue(copy, field.GetValue(original));
            }
            return copy as T;
        }

        private void UpdateShaderParams()
        {
            if (!Application.isPlaying || !myRenderer)
            {
                return;
            }

            //Send our object's v/c (Velocity over the Speed of Light) to the shader

            Vector3 tempViw = peculiarVelocity / state.SpeedOfLight;
            Vector4 tempPao = GetComoving4Acceleration();
            Vector4 tempVr = (-state.PlayerVelocityVector).AddVelocity(peculiarVelocity) / state.SpeedOfLight;

            //Velocity of object Lorentz transforms are the same for all points in an object,
            // so it saves redundant GPU time to calculate them beforehand.
            Matrix4x4 viwLorentzMatrix = SRelativityUtil.GetLorentzTransformMatrix(tempViw);

            // Metric default (doesn't have correct signature):
            Matrix4x4 intrinsicMetric = state.conformalMap.GetMetric(piw);

            colliderShaderParams.viw = tempViw;
            colliderShaderParams.pao = tempPao;
            colliderShaderParams.viwLorentzMatrix = viwLorentzMatrix;
            colliderShaderParams.invViwLorentzMatrix = viwLorentzMatrix.inverse;
            for (int i = 0; i < myRenderer.materials.Length; ++i)
            {
                myRenderer.materials[i].SetVector("_viw", tempViw);
                myRenderer.materials[i].SetVector("_pao", tempPao);
                myRenderer.materials[i].SetMatrix("_viwLorentzMatrix", viwLorentzMatrix);
                myRenderer.materials[i].SetMatrix("_invViwLorentzMatrix", viwLorentzMatrix.inverse);
                myRenderer.materials[i].SetMatrix("_intrinsicMetric", intrinsicMetric);
                myRenderer.materials[i].SetMatrix("_invIntrinsicMetric", intrinsicMetric.inverse);
                myRenderer.materials[i].SetVector("_vr", tempVr);
                myRenderer.materials[i].SetFloat("_lastUpdateSeconds", Time.time);
            }
        }

        private void UpdateBakingShaderParams()
        {
            if (!myRenderer)
            {
                return;
            }

            //Send our object's v/c (Velocity over the Speed of Light) to the shader

            Vector3 tempViw = peculiarVelocity / state.SpeedOfLight;
            Vector4 tempPao = GetComoving4Acceleration();
            Vector4 tempVr = (-state.PlayerVelocityVector).AddVelocity(peculiarVelocity) / state.SpeedOfLight;

            //Velocity of object Lorentz transforms are the same for all points in an object,
            // so it saves redundant GPU time to calculate them beforehand.
            Matrix4x4 viwLorentzMatrix = SRelativityUtil.GetLorentzTransformMatrix(tempViw);

            // Metric default (doesn't have correct signature):
            Matrix4x4 intrinsicMetric = state.conformalMap.GetMetric(piw);

            colliderShaderParams.viw = tempViw;
            colliderShaderParams.pao = tempPao;
            colliderShaderParams.viwLorentzMatrix = viwLorentzMatrix;
            colliderShaderParams.invViwLorentzMatrix = viwLorentzMatrix.inverse;
            for (int i = 0; i < myRenderer.sharedMaterials.Length; ++i)
            {
                if (myRenderer.sharedMaterials[i] == null) {
                    continue;
                }
                myRenderer.sharedMaterials[i] = Instantiate(myRenderer.sharedMaterials[i]);
                myRenderer.sharedMaterials[i].SetVector("_viw", tempViw);
                myRenderer.sharedMaterials[i].SetVector("_pao", tempPao);
                myRenderer.sharedMaterials[i].SetMatrix("_viwLorentzMatrix", viwLorentzMatrix);
                myRenderer.sharedMaterials[i].SetMatrix("_invViwLorentzMatrix", viwLorentzMatrix.inverse);
                myRenderer.sharedMaterials[i].SetMatrix("_intrinsicMetric", intrinsicMetric);
                myRenderer.sharedMaterials[i].SetMatrix("_invIntrinsicMetric", intrinsicMetric.inverse);
                myRenderer.sharedMaterials[i].SetVector("_vr", tempVr);
                myRenderer.sharedMaterials[i].SetFloat("_lastUpdateSeconds", Time.time);
            }
        }

        public void KillObject()
        {
            gameObject.SetActive(false);
            //Destroy(this.gameObject);
        }

        //This is a function that just ensures we're slower than our maximum speed. The VIW that Unity sets SHOULD (it's creator-chosen) be smaller than the maximum speed.
        private void checkSpeed()
        {
            if (isLightMapStatic)
            {
                return;
            }

            float maxSpeed = state.MaxSpeed - 0.01f;
            float maxSpeedSqr = maxSpeed * maxSpeed;

            if (peculiarVelocity.sqrMagnitude > maxSpeedSqr)
            {
                peculiarVelocity = peculiarVelocity.normalized * maxSpeed;
            }
            
            if (trnsfrmdMeshVerts == null)
            {
                return;
            }

            // The tangential velocities of each vertex should also not be greater than the maximum speed.
            // (This is a relatively computationally costly check, but it's good practice.

            for (int i = 0; i < trnsfrmdMeshVerts.Length; ++i)
            {
                Vector3 disp = Vector3.Scale(trnsfrmdMeshVerts[i], transform.lossyScale);
                Vector3 tangentialVel = Vector3.Cross(aviw, disp);
                float tanVelMagSqr = tangentialVel.sqrMagnitude;
                if (tanVelMagSqr > maxSpeedSqr)
                {
                    aviw = aviw.normalized * maxSpeed / disp.magnitude;
                }
            }
        }

        private void UpdateRigidbodyVelocity()
        {
            float gamma = GetTimeFactor();

            if (myRigidbody && !myRigidbody.isKinematic)
            {
                // If movement is frozen, set to zero.
                // If we're in an invalid state, (such as before full initialization,) set to zero.
                if (state.isMovementFrozen)
                {
                    myRigidbody.linearVelocity = Vector3.zero;
                    myRigidbody.angularVelocity = Vector3.zero;
                }
                else
                {
                    // Factor of gamma corrects for length-contraction, (which goes like 1/gamma).
                    // Effectively, this replaces Time.DeltaTime with Time.DeltaTime / gamma.
                    myRigidbody.linearVelocity = gamma * peculiarVelocity;
                    myRigidbody.angularVelocity = gamma * aviw;
                    preStepCelerity = myRigidbody.linearVelocity;

                    // PhysX integrates celerity u = gamma v in world time. A proper (rest-frame) acceleration alpha
                    // changes it at du/dt = alpha_par + alpha_perp / gamma (= F_world / m), with no overall gamma.
                    Vector3 celerityRate = (nonGravAccel + leviCivitaDevAccel).ProperAccelToCelerityRate(peculiarVelocity, gamma);
                    if (comoveViaAcceleration)
                    {
                        // Coordinate (Rindler) acceleration: handling unchanged from the original.
                        celerityRate += gamma * state.conformalMap.GetRindlerAcceleration(piw);
                    }
                    if (celerityRate.sqrMagnitude > SRelativityUtil.FLT_EPSILON)
                    {
                        myRigidbody.AddForce(celerityRate, ForceMode.Acceleration);
                    }
                }

                nonGravAccel = Vector3.zero;

                // Factor of 1/gamma corrects for time-dilation, (which goes like gamma).
                // Unity's (physically inaccurate) drag formula is something like,
                // velocity = velocity * (1 - drag * Time.deltaTime),
                // where we counterbalance the time-dilation factor above, for observer path invariance.
                myRigidbody.linearDamping = unityDrag / gamma;
                myRigidbody.angularDamping = unityAngularDrag / gamma;
            } else {
                nonGravAccel = Vector3.zero;
            }

            if (myColliders == null) {
                return;
            }

            for (int i = 0; i < myColliders.Length; ++i)
            {
                Collider collider = myColliders[i];

                // Energy dissipation goes like mu * F_N * d.
                // If the path parallel to d is also parallel to relative velocity,
                // d "already looks like" d' / gamma, to player, so multiply gamma * mu.
                // If the path parallel to d is perpendicular to relative velocity,
                // F_N "already looks like" it's being applied in time t' * gamma, to player, so multiply gamma * mu.
                collider.material.staticFriction = gamma * origPhysicMaterials[i].staticFriction;
                collider.material.dynamicFriction = gamma * origPhysicMaterials[i].dynamicFriction;
                // rapidity_after / rapidity_before - Doesn't seem to need an adjustment.
                collider.material.bounciness = origPhysicMaterials[i].bounciness;
            }
        }
        #endregion

        #region Unity lifecycle
        protected void OnDisable()
        {
            monopoleRegistry.Remove(this);
        }

        protected void OnDestroy()
        {
            if (paramsBuffer != null) paramsBuffer.Release();
            if (vertBuffer != null) vertBuffer.Release();
            if (Application.isPlaying && (contractor != null)) Destroy(contractor.gameObject);
        }

        protected void OnEnable() {
            if (!monopoleRegistry.Contains(this))
            {
                monopoleRegistry.Add(this);
            }

            //Also get the meshrenderer so that we can give it a unique material
            if (myRenderer == null)
            {
                myRenderer = GetComponent<Renderer>();
            }
            //If we have a MeshRenderer on our object and it's not world-static
            if (myRenderer)
            {
                UpdateBakingShaderParams();
            }
        }

        protected void Awake()
        {
            _localScale = transform.localScale;
            myRigidbody = GetComponent<Rigidbody>();
        }

        protected void Start()
        {
            hasStarted = false;
            isPhysicsUpdateFrame = false;

            if (myRigidbody)
            {
                myRigidbody.linearDamping = unityDrag;
                myRigidbody.angularDamping = unityAngularDrag;
                baryonCount = mass * SRelativityUtil.avogadroNumber / initialAverageMolarMass;
            }
            
            _piw = isNonrelativisticShader ? transform.position.OpticalToWorld(peculiarVelocity, GetComoving4Acceleration()) : transform.position;
            riw = transform.rotation;
            checkSpeed();
            UpdatePhysicsCaches();

            if (isNonrelativisticShader)
            {
                UpdateContractorPosition();
            }
            else
            {
                transform.position = piw;
            }

            // Update the shader parameters if necessary
            UpdateShaderParams();

            wasKinematic = false;
            wasFrozen = false;

            UpdateColliders();

            MarkStaticColliderPos();

            //Get the meshfilter
            if (isParent)
            {
                CombineParent();
            }

            meshFilter = GetComponent<MeshFilter>();

            if (myRigidbody)
            {
                //Native rigidbody gravity should not be used except during isFullPhysX.
                myRigidbody.useGravity = useGravity && !isLightMapStatic;
            }
        }

        protected bool isPhysicsCacheValid;

        protected void UpdatePhysicsCaches()
        {
            isPhysicsCacheValid = false;

            updateMetric = GetMetric();
            updatePlayerViwTimeFactor = state.PlayerVelocityVector.InverseGamma(updateMetric);
            updateViwTimeFactor = viw.InverseGamma(updateMetric);
            updateWorld4Acceleration = comovingAccel.ProperToWorldAccel(viw, updateViwTimeFactor);
            updateTisw = piw.GetTisw(viw, updateWorld4Acceleration);

            isPhysicsCacheValid = true;
        }

        protected void AfterPhysicsUpdate()
        {
            afterPhysicsStamp = Time.fixedTime;
            oldLocalTimeFixedUpdate = GetLocalTime();

            if (myRigidbody)
            {
                if (!isNonrelativisticShader)
                {
                    // Get the relativistic position and rotation after the physics update:
                    riw = myRigidbody.rotation;
                    _piw = myRigidbody.position;
                }

                // Now, update the velocity and angular velocity based on the collision result:
                // Same factor as written in UpdateRigidbodyVelocity() (this object's, cached before the step),
                // so a torque-free spin is unchanged by the round trip. (Previously the player's factor: the spin
                // was rescaled by gamma_object / gamma_player every physics step.)
                _aviw = myRigidbody.angularVelocity / updateViwTimeFactor;
                peculiarVelocity = myRigidbody.linearVelocity.RapidityToVelocity(updateMetric);
            }

            if (isNonrelativisticShader)
            {
                // Immediately correct the nonrelativistic shader position.
                UpdateContractorPosition();
            }

            if (isMonopoleAccel)
            {
                float softenFactor = 1 + monopoleAccelerationSoften;
                float tempSoftenFactor = Mathf.Pow(softenFactor, 1.0f / 4);

                monopoleTemperature /= tempSoftenFactor;
                float origBackgroundTemp = state.gravityBackgroundPlanckTemperature;
                state.gravityBackgroundPlanckTemperature /= tempSoftenFactor;

                Vector3 accel = ((peculiarVelocity - oldVelocity) / lastFixedUpdateDeltaTime + aiw) / softenFactor;
                EvaporateMonopole(softenFactor * lastFixedUpdateDeltaTime, accel);

                state.gravityBackgroundPlanckTemperature = origBackgroundTemp;
                monopoleTemperature *= tempSoftenFactor;
            }

            checkSpeed();
            UpdatePhysicsCaches();
            UpdateColliderPosition();
        }

        protected void Update()
        {
            if (isPhysicsUpdateFrame)
            {
                AfterPhysicsUpdate();
            }
            isPhysicsUpdateFrame = false;
        }

        protected void LateUpdate()
        {
            oldLocalTimeUpdate = GetLocalTime();
        }

        protected void FixedUpdate()
        {
            if (isPhysicsUpdateFrame)
            {
                AfterPhysicsUpdate();
            }
            isPhysicsUpdateFrame = false;

            if (!isPhysicsCacheValid)
            {
                UpdatePhysicsCaches();
            }

            if (state.isMovementFrozen || !state.isInitDone)
            {
                // If our rigidbody is not null, and movement is frozen, then set the object to standstill.
                UpdateRigidbodyVelocity();
                UpdateShaderParams();

                // We're done.
                return;
            }

            float deltaTime = state.FixedDeltaTimePlayer * GetTimeFactor();
            localTimeOffset += deltaTime - state.FixedDeltaTimeWorld;

            if (isLightMapStatic)
            {
                if (isMonopoleAccel)
                {
                    EvaporateMonopole(deltaTime, aiw);
                }

                UpdateColliderPosition();

                return;
            }

            if (!comoveViaAcceleration)
            {
                Comovement cm = state.conformalMap.ComoveOptical(deltaTime, piw, riw);
                riw = cm.riw;
                _piw = cm.piw;

                if (!isNonrelativisticShader && myRigidbody)
                {
                    // We'll MovePosition() for isNonrelativisticShader, further below.
                    myRigidbody.MovePosition(piw);
                }
            }

            //As long as our object is actually alive, perform these calculations
            if (meshFilter && transform) 
            {
                //If we're past our death time (in the player's view, as seen by tisw)
                if (state.TotalTimeWorld + localTimeOffset + deltaTime > DeathTime)
                {
                    KillObject();
                }
                else if (!hasStarted && (state.TotalTimeWorld + localTimeOffset + deltaTime > StartTime))
                {
                    hasStarted = true;
                    //Grab our renderer.
                    Renderer tempRenderer = GetComponent<Renderer>();
                    if (!tempRenderer.enabled)
                    {
                        tempRenderer.enabled = !hasParent;
                        AudioSource[] audioSources = GetComponents<AudioSource>();
                        if (audioSources.Length > 0)
                        {
                            for (int i = 0; i < audioSources.Length; ++i)
                            {
                                audioSources[i].enabled = true;
                            }
                        }
                    }
                }
            }

            #region rigidbody
            // The rest of the updates are for objects with Rigidbodies that move and aren't asleep.
            if (isKinematic || !myRigidbody || myRigidbody.IsSleeping())
            {
                if (myRigidbody && !myRigidbody.isKinematic)
                {
                    myRigidbody.angularVelocity = Vector3.zero;
                    myRigidbody.linearVelocity = Vector3.zero;

                    if (!isKinematic)
                    {
                        _aviw = Vector3.zero;
                        UpdateMotion(Vector3.zero, Vector3.zero);
                    }
                }

                if (!isLightMapStatic)
                {
                    transform.position = isNonrelativisticShader ? opticalPiw : piw;
                }

                UpdateShaderParams();

                // We're done.
                return;
            }

            if (isNonrelativisticShader)
            {
                // Update riw
                float aviwMag = aviw.magnitude;
                Quaternion diffRot;
                if (aviwMag <= SRelativityUtil.FLT_EPSILON)
                {
                    diffRot = Quaternion.identity;
                }
                else
                {
                    diffRot = Quaternion.AngleAxis(Mathf.Rad2Deg * deltaTime * aviwMag, aviw / aviwMag);
                }
                riw = riw * diffRot;
                myRigidbody.MoveRotation(riw);

                // Update piw from "peculiar velocity" in free fall coordinates.
                _piw += deltaTime * peculiarVelocity;

                transform.parent = null;
                myRigidbody.MovePosition(opticalPiw);
                contractor.position = myRigidbody.position;
                transform.parent = contractor;
                transform.localPosition = Vector3.zero;
                ContractLength();
            }
            UpdateColliderPosition();
            #endregion

            // FOR THE PHYSICS UPDATE ONLY, we give our rapidity to the Rigidbody
            UpdateRigidbodyVelocity();

            oldVelocity = peculiarVelocity;
            lastFixedUpdateDeltaTime = deltaTime;

            isPhysicsUpdateFrame = true;
        }

        // Yukawa scalar ("fifth") force on this object from every other registered object with monopole coupling.
        // O(N^2) over objects with nonzero coupling; returns zero when this object's coupling is zero.
        protected Vector3 MonopoleFifthForceAccel()
        {
            if (monopoleFifthForceCoupling <= 0)
            {
                return Vector3.zero;
            }

            double range = state.monopoleFifthForceRange;
            Vector3 myPos = piw;
            Vector3 total = Vector3.zero;
            foreach (RelativisticObject other in monopoleRegistry)
            {
                if ((other == this) || (other == null) || (other.monopoleFifthForceCoupling <= 0) || (other.mass <= 0))
                {
                    continue;
                }

                Vector3 d = other.piw - myPos;
                double r = d.magnitude;
                if (r <= SRelativityUtil.FLT_EPSILON)
                {
                    continue;
                }

                double alpha = Math.Sqrt((double)monopoleFifthForceCoupling * other.monopoleFifthForceCoupling);
                double yukawa = (range > 0) ? Math.Exp(-r / range) * (1 + r / range) : 1;
                double accel = alpha * state.gConst * other.mass * yukawa / (r * r);
                total += (float)accel * (d / (float)r);
            }

            return total;
        }

        protected void EvaporateMonopole(float deltaTime, Vector3 myAccel)
        {
            // Strano 2019, corrected update rule (with Claude, Anthropic, 2026).
            //
            // The "monopole" mode is a spin-0 mode of the gravitational field coupled to rest mass (the trace of the
            // stress-energy tensor). A scalar charge q coupled this way obeys the flat-spacetime self-force
            //     f^mu = (q^2 / 3) (da^mu/dtau - a^2 u^mu),
            // which VANISHES for uniform (hyperbolic) acceleration: a body held at constant proper acceleration (e.g. at
            // rest on the ground) does not lose acceleration over time, since its Rindler horizon sits in equilibrium with
            // the Unruh bath rather than evaporating. Monopole radiation, and its reaction, arise only while the
            // acceleration CHANGES. In the instantaneous rest frame the reaction is the Abraham-Lorentz term
            //     a_self = tau0 * da/dtau,   tau0 = monopoleCoupling * G * m / c^3,
            // evaluated in Landau-Lifshitz reduced order (the jerk of the external acceleration, excluding a_self itself),
            // which removes the runaway and pre-acceleration solutions of the bare Abraham-Lorentz equation.
            // The Larmor-like power radiated in the process, m * tau0 * a^2, is supplied by whatever applies the force (the
            // rest mass is unchanged in flat spacetime), and is tallied in monopoleRadiatedEnergy.
            // For realistic constants (G, c, kg-scale masses, monopoleCoupling <~ 1e-5 from Cassini and binary pulsars)
            // tau0 is ~1e-35 s and the effect is invisible; raise monopoleCoupling (or gConst) to see it.
            // isMonopoleHorizonEvaporation (below) optionally adds the original 2019 horizon-evaporation rule.

            Vector3 extAccel = myAccel - leviCivitaDevAccel;
            Vector3 jerkDevAccel = Vector3.zero;
            if (hasLastMonopoleAccel && (deltaTime > SRelativityUtil.FLT_EPSILON))
            {
                jerkDevAccel = SRelativityUtil.MonopoleSelfAccel(extAccel, lastMonopoleAccel, deltaTime, mass, monopoleCoupling);
            }
            lastMonopoleAccel = extAccel;
            hasLastMonopoleAccel = true;
            monopoleRadiatedEnergy += SRelativityUtil.MonopoleRadiatedPower(mass, extAccel.magnitude, monopoleCoupling) * deltaTime;

            // Thermal exchange of any rest-mass excitation ("monopole temperature") with the ambient bath: the Unruh
            // temperature of the current proper acceleration (equivalently, the Hawking temperature of a Schwarzschild
            // horizon with the same surface gravity), combined with the background graviton temperature. With the
            // default (Minkowski-vacuum) state, the horizon itself does not shrink: at constant acceleration this is an
            // equilibrium, not an evaporation.
            double alpha = extAccel.magnitude;
            bool isNonZeroTemp = alpha > SRelativityUtil.FLT_EPSILON;

            double r = double.PositiveInfinity;
            if (isNonZeroTemp)
            {
                // Equivalent Schwarzschild radius for the same surface acceleration:
                r = state.SpeedOfLightSqrd / (2 * alpha);
                r = SRelativityUtil.EffectiveRaditiativeRadius((float)r, state.gravityBackgroundPlanckTemperature);
            }

            if (r < state.planckLength)
            {
                // For minimum area calculation, below.
                r = state.planckLength;
            }

            isNonZeroTemp = !double.IsInfinity(r) && !double.IsNaN(r);

            if (isMonopoleHorizonEvaporation)
            {
                // Optional: Strano 2019 thermal-field postulate (the original update rule, restored as written).
                // Choose the "Unruh-like" state for the scalar instead of the Minkowski vacuum: the accelerated frame's
                // Rindler horizon radiates into a cold exterior and evaporates as a Schwarzschild horizon of the same
                // surface gravity would (E_P / (2 r_s) per Planck time), so constant proper acceleration slowly decays.
                // Independent of, and additive with, the jerk (Abraham-Lorentz) reaction above.
                double alphaH = myAccel.magnitude;
                if (alphaH > SRelativityUtil.FLT_EPSILON)
                {
                    double rH = state.SpeedOfLightSqrd / (2 * alphaH);
                    rH = SRelativityUtil.EffectiveRaditiativeRadius((float)rH, state.gravityBackgroundPlanckTemperature);
                    if (rH < state.planckLength)
                    {
                        rH = state.planckLength;
                    }

                    if (!double.IsInfinity(rH) && !double.IsNaN(rH))
                    {
                        rH += useLegacy2019PowerLaw
                            ? SRelativityUtil.SchwarzschildRadiusDecay(deltaTime, rH)
                            : SRelativityUtil.MonopoleHorizonRadiusDecay(deltaTime, rH, monopoleHorizonChannels);
                        if (rH <= SRelativityUtil.FLT_EPSILON)
                        {
                            horizonDevAccel += myAccel;
                        }
                        else
                        {
                            double alphaF = state.SpeedOfLightSqrd / (2 * rH);
                            horizonDevAccel += (float)(alphaH - alphaF) * myAccel.normalized;
                        }
                        // The ambient (horizon) temperature follows the evaporating horizon, as in the original rule.
                        r = rH;
                        isNonZeroTemp = true;
                    }
                }
            }
            else
            {
                horizonDevAccel = Vector3.zero;
            }

            leviCivitaDevAccel = jerkDevAccel + horizonDevAccel + MonopoleFifthForceAccel();

            if (myRigidbody)
            {
                double myTemperature = monopoleTemperature;
                double surfaceArea;
                if (meshFilter == null)
                {
                    Vector3 lwh = transform.localScale;
                    surfaceArea = 2 * (lwh.x * lwh.y + lwh.x * lwh.z + lwh.y * lwh.z);
                }
                else
                {
                    surfaceArea = meshFilter.sharedMesh.SurfaceArea();
                }
                // This is the ambient temperature, including contribution from comoving accelerated rest temperature.
                double ambientTemperature = isNonZeroTemp ? SRelativityUtil.SchwarzRadiusToPlanckScaleTemp(r) : state.gravityBackgroundPlanckTemperature;
                // Black-body exchange of the rest-mass excitation with the ambient bath, as a mass change over this step.
                // (Both temperatures in Planck units; Planck power times deltaTime, over c^2.)
                double dm = SRelativityUtil.MonopoleThermalMassChange(gravitonEmissivity, surfaceArea, myTemperature, ambientTemperature, deltaTime);

                double camm = (mass - dm) * SRelativityUtil.avogadroNumber / baryonCount;

                if ((myTemperature >= 0) && (camm < fundamentalAverageMolarMass))
                {
                    currentAverageMolarMass = fundamentalAverageMolarMass;
                }
                else if (camm <= 0)
                {
                    mass = 0;
                }
                else
                {
                    mass -= (float)dm;
                }

                // Emission (or absorption) that is isotropic in the rest frame carries momentum v * E / c^2 and leaves
                // the 4-velocity unchanged, so peculiarVelocity is deliberately NOT rescaled by the mass change.
            }
        }

        public void OnCollisionEnter(Collision collision)
        {
            RelativisticCollisionCorrection(collision);
            OnCollision();
        }

        public void OnCollisionStay(Collision collision)
        {
            OnCollision();
        }

        public void OnCollisionExit(Collision collision)
        {
            
        }
        #endregion

        #region Rigidbody mechanics
        // Redo the normal part of a RelativisticObject-RelativisticObject collision relativistically, in the pair's
        // centre-of-momentum frame, keeping PhysX's restitution (inferred from its own result) and its tangential
        // (friction) change. Exactly conserves relativistic momentum and energy (heat into rest mass if inelastic);
        // reduces to PhysX's result at low speed. Collisions with static or non-relativistic bodies are left to PhysX
        // (reflection off a body at rest in world coordinates is already exact).
        private void RelativisticCollisionCorrection(Collision collision)
        {
            if (!relativisticCollisions || isKinematic || state.isMovementFrozen || !myRigidbody || (collision.contactCount < 1))
            {
                return;
            }

            RelativisticObject other = collision.gameObject.GetComponent<RelativisticObject>();
            if (!other || (other == this) || other.isKinematic || !other.myRigidbody || !other.relativisticCollisions)
            {
                return;
            }

            // One object of the pair does the work for both.
            if (GetEntityId() > other.GetEntityId())
            {
                return;
            }

            double cc = SRelativityUtil.c;
            double mA = mass, mB = other.mass;
            if ((mA <= 0) || (mB <= 0))
            {
                return;
            }

            double[] n = D(collision.GetContact(0).normal);
            double nn = Math.Sqrt(Dot(n, n));
            if (nn <= 0)
            {
                return;
            }
            n = Scale(n, 1 / nn);

            double[] uA0 = D(preStepCelerity), uB0 = D(other.preStepCelerity);
            double[] uA1 = D(myRigidbody.linearVelocity), uB1 = D(other.myRigidbody.linearVelocity);

            double rel0 = Dot(Sub(uA0, uB0), n), rel1 = Dot(Sub(uA1, uB1), n);
            if (rel0 >= 0)
            {
                return; // not approaching along the normal before the step: nothing to correct
            }
            double e = Math.Min(Math.Max(-rel1 / rel0, 0), 1);

            // PhysX's tangential (friction) changes, kept as they are.
            double[] dA = Sub(uA1, uA0), dB = Sub(uB1, uB0);
            double[] tA = Sub(dA, Scale(n, Dot(dA, n))), tB = Sub(dB, Scale(n, Dot(dB, n)));

            // 4-momenta (E/c, p), with p = m u.
            double[] pA = Scale(uA0, mA), pB = Scale(uB0, mB);
            double EA = Math.Sqrt(mA * mA * cc * cc + Dot(pA, pA)), EB = Math.Sqrt(mB * mB * cc * cc + Dot(pB, pB));
            double[] beta = Scale(Add(pA, pB), 1 / (EA + EB));
            double b2 = Dot(beta, beta);
            if (b2 >= 1)
            {
                return;
            }
            double gb = 1 / Math.Sqrt(1 - b2);

            double EAs = Boost(EA, pA, beta, gb, out double[] pAs);
            double EBs = Boost(EB, pB, beta, gb, out double[] _);

            // Centre-of-momentum frame: reverse the normal component, scaled by the restitution.
            double[] pAs2 = Sub(pAs, Scale(n, (1 + e) * Dot(pAs, n)));
            double[] pBs2 = Scale(pAs2, -1);

            double[] betaBack = Scale(beta, -1);
            Boost(EAs, pAs2, betaBack, gb, out double[] pA2);
            Boost(EBs, pBs2, betaBack, gb, out double[] pB2);

            Vector3 uA = V(Add(Scale(pA2, 1 / mA), tA));
            Vector3 uB = V(Add(Scale(pB2, 1 / mB), tB));

            myRigidbody.linearVelocity = uA;
            other.myRigidbody.linearVelocity = uB;

            // If the other object already read its state back this step, update it directly.
            if (other.afterPhysicsStamp == Time.fixedTime)
            {
                other._peculiarVelocity = uB.RapidityToVelocity(other.GetMetric());
            }
        }

        // Lorentz boost of (E, p) by velocity beta (in units of c): returns E', sets p'.
        private static double Boost(double E, double[] p, double[] beta, double g, out double[] pOut)
        {
            double b2 = Dot(beta, beta);
            if (b2 <= 1e-30)
            {
                pOut = (double[])p.Clone();
                return E;
            }
            double bp = Dot(beta, p);
            pOut = Add(p, Scale(beta, (g - 1) * bp / b2 - g * E));
            return g * (E - bp);
        }

        private static double[] D(Vector3 v) { return new double[] { v.x, v.y, v.z }; }
        private static Vector3 V(double[] v) { return new Vector3((float)v[0], (float)v[1], (float)v[2]); }
        private static double Dot(double[] a, double[] b) { return a[0] * b[0] + a[1] * b[1] + a[2] * b[2]; }
        private static double[] Add(double[] a, double[] b) { return new double[] { a[0] + b[0], a[1] + b[1], a[2] + b[2] }; }
        private static double[] Sub(double[] a, double[] b) { return new double[] { a[0] - b[0], a[1] - b[1], a[2] - b[2] }; }
        private static double[] Scale(double[] a, double s) { return new double[] { a[0] * s, a[1] * s, a[2] * s }; }

        public void OnCollision()
        {
            if (isKinematic || state.isMovementFrozen || !myRigidbody)
            {
                return;
            }

            // Like how Rigidbody components are co-opted for efficient relativistic motion,
            // it's feasible to get (at least reasonable, if not exact) relativistic collision
            // handling by transforming the end state after PhysX collisions.

            // We pass the RelativisticObject's rapidity to the rigidbody, right before the physics update
            // We restore the time-dilated visual apparent velocity, afterward

            if (isNonrelativisticShader)
            {
                riw = myRigidbody.rotation;
                _piw = myRigidbody.position.OpticalToWorld(peculiarVelocity, updateWorld4Acceleration);
            }

            isPhysicsUpdateFrame = true;
            AfterPhysicsUpdate();
            isPhysicsUpdateFrame = false;
        }
        #endregion
    }
}
