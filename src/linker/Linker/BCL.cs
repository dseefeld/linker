using Mono.Cecil;

namespace Mono.Linker
{
	public static class BCL
	{
		public static class EventTracingForWindows
		{
			public static bool IsEventSourceImplementation (TypeDefinition type, LinkContext context)
			{
				if (!type.IsClass)
					return false;

				while (type.BaseType != null) {
					var bt = context.ResolveTypeDefinition (type.BaseType);

					if (bt == null)
						return false;

					if (IsEventSourceType (bt))
						return true;

					type = bt;
				}

				return false;
			}

			public static bool IsEventSourceType (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "EventSource";
			}

			public static bool IsNonEventAtribute (TypeReference type)
			{
				return type.Namespace == "System.Diagnostics.Tracing" && type.Name == "NonEventAttribute";
			}

			public static bool IsProviderName (string name)
			{
				return name == "Keywords" || name == "Tasks" || name == "Opcodes";
			}
		}

		public static bool IsIDisposableImplementation (MethodDefinition method)
		{
			if (method.Name != "Dispose" || method.ReturnType.MetadataType != MetadataType.Void)
				return false;

			if (method.HasParameters || method.HasGenericParameters || method.IsStatic)
				return false;

			if (!method.IsFinal)
				return false;

			return true;
		}

		static readonly string[] corlibNames = new[] {
			"System.Private.CoreLib",
			"mscorlib",
			"System.Runtime",
			"netstandard"
		};

		public static TypeDefinition FindPredefinedType (string ns, string name, LinkContext context)
		{
			foreach (var corlibName in corlibNames) {
				AssemblyDefinition corlib = context.TryResolve (corlibName);
				if (corlib == null)
					continue;

				TypeDefinition type = corlib.MainModule.GetType (ns, name);
				// The assembly could be a facade with type forwarders, in which case we don't find the type in this assembly.
				if (type != null)
					return type;
			}

			return null;
		}
	}
}
