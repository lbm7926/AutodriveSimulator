using System;
using System.Collections.Generic;

namespace Battlehub.RTSL
{
    public class GetDepsContext
    {
        public readonly HashSet<long> Dependencies = new HashSet<long>();
        public readonly HashSet<object> VisitedObjects = new HashSet<object>();

        public void Clear()
        {
            Dependencies.Clear();
            VisitedObjects.Clear();
        }
    }

    public class GetDepsFromContext
    {
        public readonly HashSet<object> Dependencies = new HashSet<object>();
        public readonly HashSet<object> VisitedObjects = new HashSet<object>();

        public void Clear()
        {
            Dependencies.Clear();
            VisitedObjects.Clear();
        }
    }

    public interface IPersistentSurrogate
    {
        void ReadFrom(object obj);

        object WriteTo(object obj);

        void GetDeps(GetDepsContext context);

        void GetDepsFrom(object obj, GetDepsFromContext context);

        bool CanInstantiate(Type type);

        object Instantiate(Type type);
    }
}

