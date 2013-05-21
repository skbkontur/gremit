using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

namespace GrEmit
{
    public static class HackHelpers
    {
        public static T CreateDelegate<T>(DynamicMethod dm, object target) where T : class
        {
            Delegate @delegate = dm.CreateDelegate(typeof(T), target);
            var result = (@delegate as T);
            if(result == null)
                throw new ArgumentException(String.Format("Type {0} not a delegate", typeof(T)));
            return result;
        }

        public static Type GetValueTypeForNullableOrNull(Type mayBeNullable)
        {
            if(!mayBeNullable.IsGenericType || mayBeNullable.IsGenericTypeDefinition ||
               mayBeNullable.GetGenericTypeDefinition() != typeof(Nullable<>)) return null;
            Type valueType = mayBeNullable.GetGenericArguments()[0];
            return valueType;
        }

        public static T CreateDelegate<T>(DynamicMethod dm) where T : class
        {
            Delegate @delegate = dm.CreateDelegate(typeof(T));
            var result = (@delegate as T);
            if(result == null)
                throw new ArgumentException(String.Format("Type {0} not a delegate", typeof(T)));
            return result;
        }

        public static ConstructorInfo GetObjectConstruction<T>(Expression<Func<T>> constructorCall,
                                                               params Type[] classGenericArgs)
        {
            Expression expression = constructorCall.Body;
            ConstructorInfo sourceCi;
            switch(expression.NodeType)
            {
            case ExpressionType.Convert:
                sourceCi = ObjectConstructionFromConvert(expression);
                break;
            case ExpressionType.New:
                sourceCi = ObjectConstruction(expression);
                break;
            default:
                throw new NotSupportedException("Bad expression " + constructorCall);
            }
            if(typeof(T).IsValueType && sourceCi == null)
                throw new NotSupportedException("Struct creation without arguments");
            Type type = typeof(T);
            Type resultReflectedType = type.IsGenericType
                                           ? type.GetGenericTypeDefinition().MakeGenericType(classGenericArgs)
                                           : type;
            MethodBase methodBase = MethodBase.GetMethodFromHandle(
                sourceCi.MethodHandle, resultReflectedType.TypeHandle);
            var constructedCtorForResultType = (ConstructorInfo)methodBase;
            return constructedCtorForResultType;
        }

        public static MethodInfo ConstructMethodDefinition<T>(Expression<Action<T>> callExpr, Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            MethodInfo methodInfo = methodCallExpression.Method;
            if(!methodInfo.IsGenericMethod)
                return methodInfo;
            return methodInfo.GetGenericMethodDefinition().MakeGenericMethod(methodGenericArgs);
        }

        public static MethodInfo ConstructStaticMethodDefinition(Expression<Action> callExpr, Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            MethodInfo methodInfo = methodCallExpression.Method;
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
            Expression expression = callExpr.Body;
            switch(expression.NodeType)
            {
            case ExpressionType.Convert:
                return GetMethodDefinitionImpl(((UnaryExpression)expression).Operand);
            default:
                return GetMethodDefinitionImpl(expression);
            }
        }

        public static object CallMethod<T>(T target, Expression<Action<T>> callExpr, Type[] methodGenericArgs,
                                           object[] methodArgs)
        {
            MethodInfo methodInfo = ConstructMethodDefinition(callExpr, methodGenericArgs);
            return methodInfo.Invoke(target, methodArgs);
        }

        public static object CallStaticMethod(Expression<Action> callExpr, Type[] methodGenericArgs,
                                              object[] methodArgs)
        {
            MethodInfo methodInfo = ConstructStaticMethodDefinition(callExpr, methodGenericArgs);
            return methodInfo.Invoke(null, methodArgs);
        }

        public static PropertyInfo GetProp<T, TProp>(Expression<Func<T, TProp>> readPropFunc)
        {
            Expression expression = readPropFunc.Body;
            MemberInfo memberInfo = ((MemberExpression)expression).Member;
            var propertyInfo = memberInfo as PropertyInfo;
            if(propertyInfo == null)
                throw new ArgumentException(string.Format("Bad expression. {0} is not a PropertyInfo", memberInfo));
            return propertyInfo;
        }

        public static MethodInfo ConstructGenericMethodDefinitionForGenericClass<T>(Expression<Action<T>> callExpr,
                                                                                    Type[] classGenericArgs,
                                                                                    Type[] methodGenericArgs)
        {
            var methodCallExpression = (MethodCallExpression)callExpr.Body;
            MethodInfo methodInfo = methodCallExpression.Method;
            Type type = typeof(T);
            Type resultReflectedType = type.IsGenericType
                                           ? type.GetGenericTypeDefinition().MakeGenericType(classGenericArgs)
                                           : type;
            MethodInfo sourceMethodDefinition = methodInfo.IsGenericMethod
                                                    ? methodInfo.GetGenericMethodDefinition()
                                                    : methodInfo;
            MethodBase methodBase = MethodBase.GetMethodFromHandle(
                sourceMethodDefinition.MethodHandle, resultReflectedType.TypeHandle);
            var constructedMethodForResultType = (MethodInfo)methodBase;
            if(methodInfo.IsGenericMethod)
                return constructedMethodForResultType.MakeGenericMethod(methodGenericArgs);
            return constructedMethodForResultType;
        }

        private static ConstructorInfo ObjectConstructionFromConvert(Expression expression)
        {
            var unaryExpression = (UnaryExpression)expression;
            var newExpression = (NewExpression)unaryExpression.Operand;
            return newExpression.Constructor;
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
            var methodCallExpression = body as MethodCallExpression;
            if(methodCallExpression != null)
                return (methodCallExpression).Method;
            throw new InvalidOperationException("unknown expression " + body);
        }
    }
}