using UnityEngine;
using Battlehub.Utils;
using System.Reflection;

namespace Battlehub.RTEditor
{
    public class AudioSourceComponentDescriptor : ComponentDescriptorBase<AudioSource>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo clipInfo = Strong.PropertyInfo((AudioSource x) => x.clip, "clip");
            MemberInfo volumeInfo = Strong.PropertyInfo((AudioSource x) => x.volume, "volume");

            return new[]
            {
                new PropertyDescriptor("Clip", editor.Component, clipInfo),
                new PropertyDescriptor("Volume", editor.Component, volumeInfo, volumeInfo,
                    null, new Range(0.0f, 1.0f)),
            };
        }
    }
}
