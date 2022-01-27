using System;
using System.Reflection;

namespace Common.Reflection
{
	static class MemberInfoExtensions
	{
		public static string fullName(this MemberInfo memberInfo)
		{
			if (memberInfo == null)
				return "[null]";

			if ((memberInfo.MemberType & (MemberTypes.Method | MemberTypes.Field | MemberTypes.Property)) != 0)
				return $"{memberInfo.DeclaringType.FullName}.{memberInfo.Name}";

			if ((memberInfo.MemberType & (MemberTypes.TypeInfo | MemberTypes.NestedType)) != 0)
				return (memberInfo as Type).FullName;

			return memberInfo.Name;
		}
	}

	static class TypeExtensions
	{
		public static FieldInfo[] fields(this Type type, BindingFlags bf = ReflectionHelper.bfAll) => type.GetFields(bf);
		public static PropertyInfo[] properties(this Type type, BindingFlags bf = ReflectionHelper.bfAll) => type.GetProperties(bf);
	}
}