using System.Collections.Generic;
using UnityEngine;
using OpenRelativity.Objects;

namespace OpenRelativity.Audio
{
    public class RelativisticAudioSource : RelativisticBehavior
    {
        public Transform AudioSourceTransform;
        public AudioSource[] audioSources;
        public float[] pitches;

        public RelativisticObject relativisticObject { get; protected set; }

        protected RelativisticAudioSystem audioSystem;

        protected class RelativisticAudioSourcePlayTimeHistoryPoint
        {
            public Vector4 sourceWorldSpaceTimePos { get; set; }
            public int audioSourceIndex { get; set; }

            public RelativisticAudioSourcePlayTimeHistoryPoint(Vector4 sourceWorldSTPos, int audioSource)
            {
                sourceWorldSpaceTimePos = sourceWorldSTPos;
                audioSourceIndex = audioSource;
            }
        }

        protected List<RelativisticAudioSourcePlayTimeHistoryPoint> playTimeHistory;

        protected class RelativisticAudioSourceVelocityHistoryPoint
        {
            public float WorldSoundTime { get; set; }
            public Vector3 viw { get; set; }
            public Vector3 piw { get; set; }

            public RelativisticAudioSourceVelocityHistoryPoint(float t, Vector3 p, Vector3 v)
            {
                WorldSoundTime = t;
                piw = v;
                viw = v;
            }
        }

        protected List<RelativisticAudioSourceVelocityHistoryPoint> pvHistory;

        public Vector3 piw
        {
            get
            {
                return ((pvHistory != null) && (pvHistory.Count > 0)) ? pvHistory[0].piw : relativisticObject.piw;
            }
        }

        public Vector3 viw
        {
            get
            {
                return ((pvHistory != null) && (pvHistory.Count > 0)) ? pvHistory[0].viw : relativisticObject.viw;
            }
        }

        // Rindler metric responds immediately to player local acceleration,
        // but it would be better 
        public Matrix4x4 metric { get; protected set; }

        protected float tisw
        {
            get
            {
                return relativisticObject.GetTisw();
            }
        }

        protected Vector3 listenerPiw
        {
            get
            {
                return RelativisticAudioSystem.PlayerAudioListener.piw;
            }
        }

        protected Vector3 listenerViw
        {
            get
            {
                return RelativisticAudioSystem.PlayerAudioListener.viw;
            }
        }

        public Vector3 soundVelocity
        {
            get
            {
                Vector3 dispUnit = (listenerPiw - piw).normalized;

                return (audioSystem.RapidityOfSound * dispUnit).RapidityToVelocity(metric)
                    .AddVelocity(audioSystem.WorldSoundMediumRapidity.RapidityToVelocity(metric));
            }
        }
        protected Vector3 soundPosition
        {
            get
            {
                // opticalPiw is theoretically invariant under velocity changes
                // and therefore need not be cached
                return relativisticObject.opticalPiw + soundLightDelayTime * viw;
            }
        }

        protected float soundLightDelayTime
        {
            get
            {
                Vector3 dispUnit = (listenerPiw - piw).normalized;

                return ((Vector3.Dot(audioSystem.WorldSoundMediumRapidity, dispUnit) + audioSystem.RapidityOfSound) * dispUnit)
                    .RapidityToVelocity(metric).magnitude / state.SpeedOfLight * tisw;
            }
        }

        // Start is called before the first frame update
        void Start()
        {
            audioSystem = RelativisticAudioSystem.Instance;
            relativisticObject = GetComponent<RelativisticObject>();
            audioSources = AudioSourceTransform.GetComponents<AudioSource>();
            playTimeHistory = new List<RelativisticAudioSourcePlayTimeHistoryPoint>();
            pvHistory = new List<RelativisticAudioSourceVelocityHistoryPoint>();

            pitches = new float[audioSources.Length];
            for (int i = 0; i < audioSources.Length; i++)
            {
                pitches[i] = audioSources[i].pitch;

                // Turn off built-in Doppler
                audioSources[i].dopplerLevel = 0;
            }
        }

        private void Update()
        {
            // Note that pvHistory points are (retrospectively) speculative, not guaranteed points in the history.
            // The discontinuities in the speculative history are exactly at the points of change in world-frame velocity.
            // The same is true, basically verbatim, for "piw" as we have handled it elsewhere in the physics module,
            // including in collision.
            //
            // It is the "OPTICAL" position which must maintain continuity under instantaneous changes in velocity,
            // which correspond with non-integrable jumps in "piw" and now also "soundPosition." (Think about this.
            // This maintains the integrability of the continuous "image" world-line of the object at any distance
            // and observer relative velocity.)
            //
            // Hence, if (rapidity-based) "sound history points" of world-frame position and velocity introduce a
            // prediction of EARLIER world-frame history points after velocity change than what is already foremost on
            // the history, then this speculative history is KNOWN to be discontinuous from the point where velocity
            // changed. The true history would correspond with some (instantaneous) pitch distortion of the history
            // points recorded earlier, hence we just want to "fast-forward" past any speculative points that are not
            // monotonically increasing over world-frame time in the history. These were an incorrect "prediction."

            float soundWorldTime = state.TotalTimeWorld - soundLightDelayTime;
            if (pvHistory.Count == 0 || pvHistory[0].WorldSoundTime < soundWorldTime)
            {
                pvHistory.Add(new RelativisticAudioSourceVelocityHistoryPoint(state.TotalTimeWorld - soundLightDelayTime, relativisticObject.piw, relativisticObject.viw));
            }

            while (pvHistory.Count > 1)
            {
                if (pvHistory[1].WorldSoundTime >= state.TotalTimeWorld)
                {
                    pvHistory.RemoveAt(0);
                }
                else
                {
                    break;
                }
            }

            metric = SRelativityUtil.GetRindlerMetric(piw);

            AudioSourceTransform.position = soundPosition;

            audioSystem.WorldSoundDopplerShift(this);

            if (playTimeHistory.Count == 0)
            {
                return;
            }

            while (playTimeHistory[0].sourceWorldSpaceTimePos.w >= (state.TotalTimeWorld - audioSystem.WorldSoundVelocityDelay(this)))
            {
                audioSources[playTimeHistory[0].audioSourceIndex].Play();
                playTimeHistory.RemoveAt(0);
            }
        }

        public void ShiftPitches(float frequencyFactor)
        {
            for (int i = 0; i < pitches.Length; i++)
            {
                audioSources[i].pitch = frequencyFactor * pitches[i];
            }
        }

        public void PlayOnWorldClock(int audioSourceIndex = 0)
        {
            playTimeHistory.Add(new RelativisticAudioSourcePlayTimeHistoryPoint(relativisticObject.piw, audioSourceIndex));
        }
    }
}