using System;
using System.Collections.Generic;

namespace GrEmit.Utils
{
    public static class TypeExtensions
    {
        public static void GetInterfacesCollectionStupid(this Type node, ICollection<Type> result)
        {
            if(node == null)
                return;
            GetInterfacesCollectionStupid(ReflectionExtensions.GetBaseType(node), result);
            if(node.IsInterface)
                result.Add(node);
            foreach(var interfaCe in ReflectionExtensions.GetInterfaces(node))
                result.Add(interfaCe);
        }

        public static Type[] GetTypesArray(this Type node)
        {
            if(node == null)
                return Type.EmptyTypes;
            var baseArray = ReflectionExtensions.GetBaseType(node).GetTypesArray();
            var interfaces = ReflectionExtensions.GetInterfaces(node).Subtract(baseArray);
            var index = interfaces.Length + baseArray.Length;
            var typeArray = new Type[1 + index];
            typeArray[index] = node;
            Array.Sort(interfaces, interfaces.CoverageComparison());
            Array.Copy(interfaces, 0, typeArray, index - interfaces.Length, interfaces.Length);
            Array.Copy(baseArray, typeArray, baseArray.Length);
            return typeArray;
        }

//        public static Type FindInterfaceWith(this Type type1, Type type2)
//        {
//            var array = type2.GetTypesArray().Intersect(type1.GetTypesArray());
//            var typeCurrent = default(Type);
//
//            for(var i = array.Length; i-- > 0;)
//            {
//                if(((typeCurrent = array[i]) == null || typeCurrent.BaseType == null) && i > 0)
//                {
//                    var typeNext = array[i - 1];
//
//                    if(typeNext.FindInterfaceWith(typeCurrent) != typeNext)
//                        return null;
//                    break;
//                }
//            }
//
//            return typeof(object) != typeCurrent ? typeCurrent : null;
//        }
//
        public static Type FindBaseClassWith(this Type type1, Type type2)
        {
            if(null == type1)
                return type2;

            if(null == type2)
                return type1;

            for(var currentType2 = type2; currentType2 != null; currentType2 = ReflectionExtensions.GetBaseType(currentType2))
            {
                for(var currentType1 = type1; currentType1 != null; currentType1 = ReflectionExtensions.GetBaseType(currentType1))
                {
                    if(ReflectionExtensions.Equal(currentType2, currentType1))
                        return currentType2;
                }
            }

            return null;
        }

//
//        public static Type FindEqualTypeWith(this Type type1, Type type2)
//        {
//            var baseClass = type2.FindBaseClassWith(type1);
//            var interfaze = type2.FindInterfaceWith(type1);
//
//            if(interfaze == null)
//                return baseClass;
//            if(baseClass == null || baseClass == typeof(object) || baseClass.IsAbstract)
//                return interfaze;
//            return baseClass;
//        }

        public static Type[] Intersect(this Type[] ax, Type[] ay)
        {
            return Array.FindAll(ax, x => Array.Exists(ay, y => ReflectionExtensions.Equal(x, y)));
        }

        private static Type[] Subtract(this Type[] ax, Type[] ay)
        {
            return Array.FindAll(ax, x => false == Array.Exists(ay, y => ReflectionExtensions.Equal(x, y)));
        }

        private static int GetOccurrenceCount(this Type[] ax, Type ty)
        {
            return Array.FindAll(ax, x => Array.Exists(ReflectionExtensions.GetInterfaces(x), tx => ReflectionExtensions.Equal(tx, ty))).Length;
        }

        private static int GetOverlappedCount(this Type[] ax, Type[] ay)
        {
            return ay.Intersect(ax).Length;
        }

        private static Comparison<Type> CoverageComparison(this Type[] az)
        {
            return
                (tx, ty) =>
                    {
                        var ay = ReflectionExtensions.GetInterfaces(ty);
                        var ax = ReflectionExtensions.GetInterfaces(tx);
                        var overlapped = az.GetOverlappedCount(ax).CompareTo(az.GetOverlappedCount(ay));

                        if(overlapped != 0)
                            return overlapped;
                        var occurrence = az.GetOccurrenceCount(tx).CompareTo(az.GetOccurrenceCount(ty));

                        return occurrence != 0 ? occurrence : 0;
                    };
        }
    }
}