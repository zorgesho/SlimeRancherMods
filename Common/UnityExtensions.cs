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
	}
}