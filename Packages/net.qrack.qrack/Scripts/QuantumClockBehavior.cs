#if OPEN_RELATIVITY_INCLUDED
using OpenRelativity;
#endif

using UnityEngine;

namespace Qrack {
/// <summary>
/// The embedding of a quantum simulation in space-time coordinates, shared by
/// QuantumSystem and QuantumBridge: the simulation's clock is the proper time
/// of the RelativisticObject on the same GameObject, (or world coordinate time,
/// without one,) and its "visual" time is that clock as the player presently
/// sees it, delayed by the light travel time to the player.
/// </summary>
#if OPEN_RELATIVITY_INCLUDED
public abstract class QuantumClockBehavior : RelativisticBehavior
#else
public abstract class QuantumClockBehavior : MonoBehaviour
#endif
{
  public float ClockOffset;

#if OPEN_RELATIVITY_INCLUDED
  private RelativisticObject _myRO;

  private RelativisticObject myRO {
    get { return _myRO ? _myRO : _myRO = GetComponent<RelativisticObject>(); }
  }
#endif

  public float LocalTime {
    get {
#if OPEN_RELATIVITY_INCLUDED
      return ClockOffset + (myRO ? myRO.GetLocalTime() : state.TotalTimeWorld);
#else
      return ClockOffset + Time.time;
#endif
    }
  }

  public float LocalDeltaTime {
    get {
#if OPEN_RELATIVITY_INCLUDED
      return (float)(myRO ? myRO.localDeltaTime : state.DeltaTimeWorld);
#else
      return Time.deltaTime;
#endif
    }
  }

  public float LocalFixedDeltaTime {
    get {
#if OPEN_RELATIVITY_INCLUDED
      return (float)(myRO ? myRO.localFixedDeltaTime
                          : state.FixedDeltaTimeWorld);
#else
      return Time.fixedDeltaTime;
#endif
    }
  }

  public float VisualTime {
    get {
#if OPEN_RELATIVITY_INCLUDED
      return ClockOffset + (myRO ? myRO.GetVisualTime() : state.TotalTimeWorld);
#else
      return ClockOffset + Time.time;
#endif
    }
  }
}
}
