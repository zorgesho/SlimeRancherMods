using System;
using UnityEngine;

namespace Common
{
	static class ObjectAndComponentExtensions
	{
		public static C ensureComponent<C>(this GameObject go) where C: Component => go.ensureComponent(typeof(C)) as C;
		public static Component ensureComponent(this GameObject go, Type type) => go.GetComponent(type) ?? go.AddComponent(type);

		public static GameObject getParent(this GameObject go) => go.transform.parent?.gameObject;
		public static GameObject getChild(this GameObject go, string name) => go.transform.Find(name)?.gameObject;

		public static void setTransform(this GameObject go, Vector3? pos = null, Vector3? localPos = null, Vector3? localAngles = null, Vector3? localScale = null)
		{
			var tr = go.transform;

			if (pos != null)			tr.position = (Vector3)pos;
			if (localPos != null)		tr.localPosition = (Vector3)localPos;
			if (localAngles != null)	tr.localEulerAngles = (Vector3)localAngles;
			if (localScale != null)		tr.localScale = (Vector3)localScale;
		}

		public static void setParent(this GameObject go, GameObject parent, Vector3? localPos = null, Vector3? localAngles = null)
		{
			go.transform.SetParent(parent.transform, false);
			go.setTransform(localPos: localPos, localAngles: localAngles);
		}

		public static GameObject createChild(this GameObject go, string name, Vector3? localPos = null)
		{
			GameObject child = new (name);
			child.setParent(go);

			child.setTransform(localPos: localPos);

			return child;
		}
	}
}