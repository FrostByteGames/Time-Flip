// This file is provided under The MIT License as part of Steamworks.NET.
// Copyright (c) 2013-2015 Riley Labrecque
// Please see the included LICENSE.txt for additional information.

// Changes to this file will be reverted when you update Steamworks.NET

namespace Steamworks {
	public struct HhostListRequest : System.IEquatable<HhostListRequest> {
		public static readonly HhostListRequest Invalid = new HhostListRequest(System.IntPtr.Zero);
		public System.IntPtr m_HhostListRequest;

		public HhostListRequest(System.IntPtr value) {
			m_HhostListRequest = value;
		}

		public override string ToString() {
			return m_HhostListRequest.ToString();
		}

		public override bool Equals(object other) {
			return other is HhostListRequest && this == (HhostListRequest)other;
		}

		public override int GetHashCode() {
			return m_HhostListRequest.GetHashCode();
		}

		public static bool operator ==(HhostListRequest x, HhostListRequest y) {
			return x.m_HhostListRequest == y.m_HhostListRequest;
		}

		public static bool operator !=(HhostListRequest x, HhostListRequest y) {
			return !(x == y);
		}

		public static explicit operator HhostListRequest(System.IntPtr value) {
			return new HhostListRequest(value);
		}

		public static explicit operator System.IntPtr(HhostListRequest that) {
			return that.m_HhostListRequest;
		}

		public bool Equals(HhostListRequest other) {
			return m_HhostListRequest == other.m_HhostListRequest;
		}
	}
}
