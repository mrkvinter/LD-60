using System.Collections.Generic;
using VContainer;

namespace Game.Utilities.Extensions
{
    public static class VContainerExtensions
    {
        public static IReadOnlyList<T> ResolveAll<T>(this IObjectResolver container)
        {
            return container.Resolve<IReadOnlyList<T>>();
        }
        
        public static T CreateInstance<T>(this IObjectResolver container)
        {
            var scope = container.CreateScope(builder =>
            {
                builder.Register<T>(Lifetime.Singleton).AsSelf();
            });
            
            return scope.Resolve<T>();
        }
        
        public static T CreateInstance<T>(this IObjectResolver container, params object[] parameters)
        {
            var scope = container.CreateScope(builder =>
            {
                var registrationBuilder = builder.Register<T>(Lifetime.Singleton).AsSelf();
                foreach (var parameter in parameters)
                {
                    registrationBuilder.WithParameter(parameter.GetType(), parameter);
                }
            });
            
            return scope.Resolve<T>();
        }
        
        public static object CreateInstance(this IObjectResolver container, System.Type type)
        {
            var scope = container.CreateScope(builder =>
            {
                builder.Register(type, Lifetime.Singleton).AsSelf();
            });
            
            return scope.Resolve(type);
        }
    }
}