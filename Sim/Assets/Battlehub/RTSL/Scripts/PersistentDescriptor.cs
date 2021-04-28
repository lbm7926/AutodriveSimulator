using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using UnityObject = UnityEngine.Object;

namespace Battlehub.RTSL
{
    [ProtoContract]
    public class PersistentDescriptor 
    {
        [ProtoIgnore]
        public PersistentDescriptor Parent;

        [ProtoMember(1)]
        public PersistentDescriptor[] Children;

        [ProtoMember(2)]
        public PersistentDescriptor[] Components;

        [ProtoMember(3)]
        public long PersistentID;

        [ProtoMember(4)]
        public Guid PersistentTypeGuid;

        [ProtoMember(5)]
        public string Name;

        public PersistentDescriptor()
        {
        }

        public PersistentDescriptor(Guid persistentTypeGuid, long persistentID, string name)
        {
            PersistentID = persistentID;
            PersistentTypeGuid = persistentTypeGuid;
            Name = name;

            Children = new PersistentDescriptor[0];
            Components = new PersistentDescriptor[0];
        }

        [ProtoAfterDeserialization]
        public void OnDeserialized()
        {
            if (Components != null)
            {
                for (int i = 0; i < Components.Length; ++i)
                {
                    Components[i].Parent = this;
                }
            }

            if (Children != null)
            {
                for (int i = 0; i < Children.Length; ++i)
                {
                    Children[i].Parent = this;
                }
            }
        }

        public override string ToString()
        {
            string pathToDesriptor = string.Empty;
            PersistentDescriptor descriptor = this;
            if (descriptor.Parent == null)
            {
                pathToDesriptor += "/";
            }
            else
            {
                while (descriptor.Parent != null)
                {
                    pathToDesriptor += "/" + descriptor.Parent.PersistentID;
                    descriptor = descriptor.Parent;
                }
            }
            return string.Format("Descriptor InstanceId = {0}, Type = {1}, Path = {2}, Children = {3} Components = {4}", PersistentID, PersistentTypeGuid, pathToDesriptor, Children != null ? Children.Length : 0, Components != null ? Components.Length : 0);
        }
    }
}



