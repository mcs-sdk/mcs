using System;
//using System.Collections.Generic;
//using UnityEngine;
//
//public class Profiler
//{
//	public static Profiler FPS    = new Profiler();
//	public static Profiler Paint0 = new Profiler();
//	public static Profiler Paint1 = new Profiler();
//	public static Profiler Paint2 = new Profiler();
//	public static Profiler Project0 = new Profiler();
//	public static Profiler Project1 = new Profiler();
//	float m_start;
//	float m_stop;
//	float m_duration;
//
//
//	public void reset() {
//		m_duration = 0;
//	}
//
//	public void start() {
//		reset();
//		m_start = Time.realtimeSinceStartup;
//	}
//
//	public void stop()
//	{
//		m_stop = Time.realtimeSinceStartup;
//		m_duration += (m_stop - m_start);
//	}
//
//	public void cont() {
//		m_start = Time.realtimeSinceStartup;
//	}
//
//	public float Duration {
//		get {
//			return m_duration*1000;
//		}
//	}
//
//	public int DurationMCS {
//		get {
//			return (int)(m_duration*10000);
//		}
//	}
//}
//
