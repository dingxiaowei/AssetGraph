using UnityEngine;
using UnityEditor;

using System;
using System.Linq;
using System.IO;
using System.Collections.Generic;
using System.Reflection;

namespace AssetBundleGraph {

	/**
	 * IPrefabBuilder is an interface to create Prefab Asset from incoming asset group.
	 * Subclass of IPrefabBuilder must have CUstomPrefabBuilder attribute.
	 */
	public interface IPrefabBuilder {
		/**
		 * Test if prefab can be created with incoming assets.
		 * @result Name of prefab file if prefab can be created. null if not.
		 */
		string CanCreatePrefab (string groupKey, List<UnityEngine.Object> objects);

		/**
		 * Create Prefab.
		 */ 
		UnityEngine.GameObject CreatePrefab (string groupKey, List<UnityEngine.Object> objects);

		/**
		 * Draw Inspector GUI for this PrefabBuilder.
		 */ 
		void OnInspectorGUI (Action onValueChanged);

		/**
		 * Serialize this PrefabBuilder to JSON using JsonUtility.
		 */ 
		string Serialize();
	}

	[AttributeUsage(AttributeTargets.Class)] 
	public class CustomPrefabBuilder : Attribute {
		private string m_name;

		public string Name {
			get {
				return m_name;
			}
		}

		public CustomPrefabBuilder (string name) {
			m_name = name;
		}
	}

	public class PrefabBuilderUtility {

		private static  Dictionary<string, string> s_attributeClassNameMap;

		public static Dictionary<string, string> GetAttributeClassNameMap () {

			if(s_attributeClassNameMap == null) {
				// attribute name or class name : class name
				s_attributeClassNameMap = new Dictionary<string, string>(); 

				var builders = Assembly
					.GetExecutingAssembly()
					.GetTypes()
					.Where(t => !t.IsInterface)
					.Where(t => typeof(IPrefabBuilder).IsAssignableFrom(t));

				foreach (var type in builders) {
					// set attribute-name as key of dict if atribute is exist.
					CustomPrefabBuilder attr = 
						type.GetCustomAttributes(typeof(CustomPrefabBuilder), true).FirstOrDefault() as CustomPrefabBuilder;

					var typename = type.ToString();


					if (attr != null) {
						if (!s_attributeClassNameMap.ContainsKey(attr.Name)) {
							s_attributeClassNameMap[attr.Name] = typename;
						}
					} else {
						s_attributeClassNameMap[typename] = typename;
					}
				}
			}
			return s_attributeClassNameMap;
		}

		public static string GetPrefabBuilderGUIName(IPrefabBuilder builder) {
			CustomPrefabBuilder attr = 
				builder.GetType().GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr.Name;
		}

		public static bool HasValidCustomPrefabBuilderAttribute(Type t) {
			CustomPrefabBuilder attr = 
				t.GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
			return attr != null && !string.IsNullOrEmpty(attr.Name);
		}

		public static string GetPrefabBuilderGUIName(string className) {
			var type = Type.GetType(className);
			if(type != null) {
				CustomPrefabBuilder attr = 
					Type.GetType(className).GetCustomAttributes(typeof(CustomPrefabBuilder), false).FirstOrDefault() as CustomPrefabBuilder;
				if(attr != null) {
					return attr.Name;
				}
			}
			return string.Empty;
		}

		public static string GUINameToClassName(string guiName) {
			var map = GetAttributeClassNameMap();

			if(map.ContainsKey(guiName)) {
				return map[guiName];
			}

			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilder(NodeData node, BuildTarget target) {
			return CreatePrefabBuilder(node, BuildTargetUtility.TargetToGroup(target));
		}

		public static IPrefabBuilder CreatePrefabBuilder(NodeData node, BuildTargetGroup targetGroup) {

			var data  = node.InstanceData[targetGroup];
			var className = node.ScriptClassName;
			Type dataType = null;

			if(!string.IsNullOrEmpty(className)) {
				dataType = Type.GetType(className);
			}

			if(data != null && dataType != null) {
				return JsonUtility.FromJson(data, dataType) as IPrefabBuilder;
			}

			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilder(string guiName) {
			var className = GUINameToClassName(guiName);
			if(className != null) {
				return (IPrefabBuilder) Assembly.GetExecutingAssembly().CreateInstance(className);
			}
			return null;
		}

		public static IPrefabBuilder CreatePrefabBuilderByClassName(string className) {

			if(className == null) {
				return null;
			}

			Type t = Type.GetType(className);
			if(t == null) {
				return null;
			}

			if(!HasValidCustomPrefabBuilderAttribute(t)) {
				return null;
			}

			return (IPrefabBuilder) Assembly.GetExecutingAssembly().CreateInstance(className);
		}
	}
}