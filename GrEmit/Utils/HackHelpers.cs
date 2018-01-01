using System;
using System.Linq.Expressions;
using System.Reflection;

namespace GrEmit.Utils
{
    public static class HackHelpers
    {
        public static Type GetValueTypeForNullableOrNull(Type mayBeNullable)
        {
            if(!mayBeNullable.IsGenericType || mayBeNullable.IsGenericTypeDefinition ||
               mayBeNullable.GetGenericTypeDefinition() != typeof(Nullable<>)) return null;
            var valueType = mayBeNullable.GetGenericArguments()[0];
            return valueType;
        }

        //NOTE MetadataToken's reassigned on compilation !!! use inside appdomain
        public static ulong GetMemberUniqueToken(MemberInfo mi)
        {
            return ((ulong)mi.Module.MetadataToken) << 32 | (ulong)mi.MetadataToken;
        }

        //BUG может быть одинаков для типов из разных сборок
        public static ulong GetTypeUniqueToken(Type type)
        {
            return ((ulong)type.Module.MetadataToken) << 32 | (ulong)type.MetadataToken;
        }

        public static ConstructorInfo GetObjectConstruction<T>(Expression<Func<T>> constructorCall,
                                                               params Type[] classGenericArgs)
        {
            var expression = constructorCall.Body;
            var sourceCi = ObjectConstruction(EliminateConvert(expression));
            if(typeof(T).IsValueType && sourceCi == null)
                throw new NotSupportedException("Struct creation without arguments");
            var type = sourceCi.ReflectedType;
            var resultReflectedType = type.IsGenericType && classGenericArgs != null && classGenericArgs.Length > 0
                                          ? type.GetGenericTypeDefinition().MakeGenericType(classGenericArgs)
                                          : type;
            var methodBase = MethodBase.GetMethodFromHandle(
                sourceCi.MethodHandle, resultReflectedType.TypeHandle);
            var constructedCtorForResultType = (ConstructorInfo)methodBase;
            return constructedCtorForResultType;
        }

        public static MethodInfo ConstructMethodDefinition<T>(Expression<Action<T>> callExpr, Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            var methodInfo = methodCallExpression.Method;
            if(!methodInfo.IsGenericMethod)
                return methodInfo;
            return methodInfo.GetGenericMethodDefinition().MakeGenericMethod(methodGenericArgs);
        }

        public static MethodInfo ConstructStaticMethodDefinition(Expression<Action> callExpr, Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            var methodInfo = methodCallExpression.Method;
            if(!methodInfo.IsGenericMethod)
                return methodInfo;
            return methodInfo.GetGenericMethodDefinition().MakeGenericMethod(methodGenericArgs);
        }

        public static MethodInfo GetMethodDefinition<T>(Expression<Action<T>> callExpr)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            return methodCallExpression.Method;
        }

        public static MethodInfo GetMethodDefinition<T>(Expression<Func<T, object>> callExpr)
        {
            return GetMethodDefinitionImpl(EliminateConvert(callExpr.Body));
        }

        public static object CallMethod<T>(T target, Expression<Action<T>> callExpr, Type[] methodGenericArgs,
                                           object[] methodArgs)
        {
            var methodInfo = ConstructMethodDefinition(callExpr, methodGenericArgs);
            return methodInfo.Invoke(target, methodArgs);
        }

        public static object CallStaticMethod(Expression<Action> callExpr, Type[] methodGenericArgs,
                                              object[] methodArgs)
        {
            var methodInfo = ConstructStaticMethodDefinition(callExpr, methodGenericArgs);
            return methodInfo.Invoke(null, methodArgs);
        }

        public static PropertyInfo GetStaticProperty(Expression<Func<object>> callExpr)
        {
            var expression = EliminateConvert(callExpr.Body);
            var memberInfo = ((MemberExpression)expression).Member;
            var propertyInfo = memberInfo as PropertyInfo;
            if(propertyInfo == null)
                throw new ArgumentException(string.Format("Bad expression. {0} is not a PropertyInfo", memberInfo));
            return propertyInfo;
        }

        public static FieldInfo GetStaticField(Expression<Func<object>> callExpr)
        {
            var expression = EliminateConvert(callExpr.Body);
            var memberInfo = ((MemberExpression)expression).Member;
            var fi = memberInfo as FieldInfo;
            if(fi == null)
                throw new ArgumentException(string.Format("Bad expression. {0} is not a FieldInfo", memberInfo));
            return fi;
        }

        public static PropertyInfo GetProp<T>(Expression<Func<T, object>> readPropFunc, params Type[] classGenericArgs)
        {
            var expression = EliminateConvert(readPropFunc.Body);
            var memberInfo = ((MemberExpression)expression).Member;
            var propertyInfo = memberInfo as PropertyInfo;
            if(propertyInfo == null)
                throw new ArgumentException(string.Format("Bad expression. {0} is not a PropertyInfo", memberInfo));
            if(classGenericArgs.Length == 0)
                return propertyInfo;
            var mt = propertyInfo.MetadataToken;
            var type = propertyInfo.ReflectedType;
            var resultReflectedType = type.GetGenericTypeDefinition().MakeGenericType(classGenericArgs);
            var propertyInfos = resultReflectedType.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);

            foreach(var info in propertyInfos)
            {
                if(info.MetadataToken == mt)
                    return info;
            }
            throw new NotSupportedException("not found");
        }

        public static FieldInfo GetField<T>(Expression<Func<T, object>> readPropFunc)
        {
            var expression = EliminateConvert(readPropFunc.Body);
            var memberInfo = ((MemberExpression)expression).Member;
            var propertyInfo = memberInfo as FieldInfo;
            if(propertyInfo == null)
                throw new ArgumentException(string.Format("Bad expression. {0} is not a FieldInfo", memberInfo));
            return propertyInfo;
        }

        public static MethodInfo ConstructGenericMethodDefinitionForGenericClass<T>(Expression<Action<T>> callExpr,
                                                                                    Type[] classGenericArgs,
                                                                                    Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            var methodInfo = methodCallExpression.Method;
            return ConstructGenericMethodDefinitionForGenericClass(methodInfo.ReflectedType, methodInfo, classGenericArgs, methodGenericArgs);
        }

        public static MethodInfo GetMethodByMetadataToken(Type type, int methodMetedataToken)
        {
            var methodInfos = type.GetMethods(BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            //todo сделать нормально
            foreach(var methodInfo in methodInfos)
            {
                if(methodInfo.MetadataToken == methodMetedataToken)
                    return methodInfo;
            }
            return null;
        }

        public static MethodInfo ConstructGenericMethodDefinitionForGenericClass(Type type, MethodInfo methodInfo, Type[] classGenericArgs, Type[] methodGenericArgs)
        {
            var resultReflectedType = type.IsGenericType
                                          ? type.GetGenericTypeDefinition().MakeGenericType(classGenericArgs)
                                          : type;
            var sourceMethodDefinition = methodInfo.IsGenericMethod
                                             ? methodInfo.GetGenericMethodDefinition()
                                             : methodInfo;

            var methodBase = MethodBase.GetMethodFromHandle(
                sourceMethodDefinition.MethodHandle, resultReflectedType.TypeHandle);
            var constructedMethodForResultType = (MethodInfo)methodBase;
            if(methodInfo.IsGenericMethod)
                return constructedMethodForResultType.MakeGenericMethod(methodGenericArgs);
            return constructedMethodForResultType;
        }

        //public static FieldInfo ConstructFieldDefinitionForGenericClass<T>(Expression<Func<T, object>> callExpr, Type[] classGenericArgs)
        //{
        //    var methodCallExpression = (MemberExpression)EliminateConvert(callExpr.Body);
        //    Type type = typeof(T);
        //    MemberInfo memberInfo = methodCallExpression.Member;
        //    var fieldInfo = memberInfo as FieldInfo;
        //    if(fieldInfo != null)
        //        return ConstructFieldForGenericClass(type, fieldInfo, classGenericArgs);
        //    throw new ArgumentException("Expression access not a field");
        //}

        //public static FieldInfo ConstructFieldForGenericClass(Type classType, FieldInfo fieldInfo, Type[] classGenericArgs)
        //{
        //    Type resultReflectedType = classType.IsGenericType
        //                                   ? classType.GetGenericTypeDefinition().MakeGenericType(classGenericArgs)
        //                                   : classType;
        //BUG not works same as MethodBase.GetMethodFromHandle
        //    FieldInfo result = FieldInfo.GetFieldFromHandle(
        //        fieldInfo.FieldHandle, resultReflectedType.TypeHandle);
        //    return result;
        //}

        private static Expression EliminateConvert(Expression expression)
        {
            if(expression.NodeType == ExpressionType.Convert)
                return ((UnaryExpression)expression).Operand;
            return expression;
        }

        private static ConstructorInfo ObjectConstruction(Expression expression)
        {
            var newExpression = (NewExpression)expression;
            return newExpression.Constructor;
        }

        private static MethodInfo GetMethodDefinitionImpl(Expression body)
        {
            var binaryExpression = body as BinaryExpression;
            if(binaryExpression != null)
                return (binaryExpression).Method;
            var unaryExpression = body as UnaryExpression;
            if(unaryExpression != null)
                return (unaryExpression).Method;
            var methodCallExpression = body as MethodCallExpression;
            if(methodCallExpression != null)
                return (methodCallExpression).Method;
            throw new InvalidOperationException("unknown expression " + body);
        }
    }
}