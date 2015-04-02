using System;

namespace GrEmit
{
    public static class TypeExtensions
    {
        public static Type[] GetTypesArray(this Type node)
        {
            if(node == null)
                return Type.EmptyTypes;
            var baseArray = node.BaseType.GetTypesArray();
            var interfaces = node.GetInterfaces().Subtract(baseArray);
            var index = interfaces.Length + baseArray.Length;
            var typeArray = new Type[1 + index];
            typeArray[index] = node;
            Array.Sort(interfaces, interfaces.CoverageComparison());
            Array.Copy(interfaces, 0, typeArray, index - interfaces.Length, interfaces.Length);
            Array.Copy(baseArray, typeArray, baseArray.Length);
            return typeArray;
        }

        public static Type FindInterfaceWith(this Type type1, Type type2)
        {
            var array = type2.GetTypesArray().Intersect(type1.GetTypesArray());
            var typeCurrent = default(Type);

            for(var i = array.Length; i-- > 0;)
            {
                if(((typeCurrent = array[i]) == null || typeCurrent.BaseType == null) && i > 0)
                {
                    var typeNext = array[i - 1];

                    if(typeNext.FindInterfaceWith(typeCurrent) != typeNext)
                        return null;
                    break;
                }
            }

            return typeof(object) != typeCurrent ? typeCurrent : null;
        }

        public static Type FindBaseClassWith(this Type type1, Type type2)
        {
            if(null == type1)
                return type2;

            if(null == type2)
                return type1;

            for(var currentType2 = type2; currentType2 != null; currentType2 = currentType2.BaseType)
            {
                for(var currentType1 = type1; currentType1 != null; currentType1 = currentType1.BaseType)
                {
                    if(currentType2 == currentType1)
                        return currentType2;
                }
            }

            return null;
        }

        public static Type FindEqualTypeWith(this Type type1, Type type2)
        {
            var baseClass = type2.FindBaseClassWith(type1);
            var interfaze = type2.FindInterfaceWith(type1);

            if(interfaze == null)
                return baseClass;
            if(baseClass == null || baseClass == typeof(object) || baseClass.IsAbstract)
                return interfaze;
            return baseClass;
        }

        private static T[] Subtract<T>(this T[] ax, T[] ay)
        {
            return Array.FindAll(ax, x => false == Array.Exists(ay, y => y.Equals(x)));
        }

        private static T[] Intersect<T>(this T[] ax, T[] ay)
        {
            return Array.FindAll(ax, x => Array.Exists(ay, y => y.Equals(x)));
        }

        private static int GetOccurrenceCount(this Type[] ax, Type ty)
        {
            return Array.FindAll(ax, x => Array.Exists(x.GetInterfaces(), tx => tx == ty)).Length;
        }

        private static int GetOverlappedCount<T>(this T[] ax, T[] ay)
        {
            return ay.Intersect(ax).Length;
        }

        private static Comparison<Type> CoverageComparison(this Type[] az)
        {
            return
                (tx, ty) =>
                    {
                        var ay = ty.GetInterfaces();
                        var ax = tx.GetInterfaces();
                        var overlapped = az.GetOverlappedCount(ax).CompareTo(az.GetOverlappedCount(ay));

                        if(overlapped != 0)
                            return overlapped;
                        var occurrence = az.GetOccurrenceCount(tx).CompareTo(az.GetOccurrenceCount(ty));

                        return occurrence != 0 ? occurrence : 0;
                    };
        }
    }
}