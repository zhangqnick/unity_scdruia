using UnityEngine;
using System.Collections;

public class Timer {
	/** The time at which this timer has been reset. */
	private float	m_base = 0.0f;
	public Timer() { Reset(); }
	/** Resets the timer. */
	public void Reset() {
		m_base = Time.realtimeSinceStartup;
	}
	/** Gets the elapsed time (in seconds) since the timer has been reset. */
	public float GetElapsedTime() {
		return Time.realtimeSinceStartup - m_base;
	}
}
