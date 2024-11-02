using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using System.Linq;

namespace EditorToolbars
{

    public class CreateFromAttribute : Attribute
    {
        static Dictionary<Type, Dictionary<Type, CreateFromInfo>> fromToTargetTypes;
        static Dictionary<Type, Dictionary<Type, CreateFromInfo>> FromToTargetTypes
        {
            get
            {
                if (fromToTargetTypes == null)
                {
                    Initalize();
                }
                return fromToTargetTypes;
            }
        }
        class CreateFromInfo
        {
            public Type fromType;
            public Type toType;
            public MethodInfo methodInfo;
        }

        public static IEnumerable<MethodInfo> MethodInfos => FromToTargetTypes.Values.SelectMany(o => o.Values).Select(o => o.methodInfo);

        static void Initalize()
        {
            if (fromToTargetTypes == null)
            {
                fromToTargetTypes = new();
                foreach (var method in TypeCache.GetMethodsWithAttribute<CreateFromAttribute>())
                {
                    Type toType = method.ReturnType;
                    if (toType == typeof(void))
                        continue;
                    var ps = method.GetParameters();
                    if (ps.Length == 0)
                        continue;
                    Type fromType = ps[0].ParameterType;
                    CreateFromInfo info = new CreateFromInfo()
                    {
                        fromType = fromType,
                        toType = toType,
                        methodInfo = method
                    };

                    if (!fromToTargetTypes.TryGetValue(fromType, out var map))
                    {
                        map = new();
                        fromToTargetTypes[fromType] = map;
                    }
                    map[toType] = info;

                }
            }
        }
        public static MethodInfo GetCreateFromMethod(Type fromType, Type targetType)
        {
            Type t = fromType;
            while (t != null)
            {
                if (FromToTargetTypes.TryGetValue(fromType, out var dic))
                {
                    if (dic.TryGetValue(targetType, out var info))
                    {
                        return info.methodInfo;
                    }
                }
                t = t.BaseType;
            }
            return null;
        }

        public static bool CreateFrom<T>(object fromObj, out T result)
        {
            if (CreateFrom(fromObj, typeof(T), out var obj))
            {
                result = (T)obj;
                return true;
            }
            result = default;
            return false;
        }

        public static bool CreateFrom(object fromObj, Type targetType, out object result)
        {
            result = null;
            var method = GetCreateFromMethod(fromObj.GetType(), targetType);
            if (method == null)
                return false;
            result = method.Invoke(null, new object[] { fromObj });
            return true;
        }
    }

}