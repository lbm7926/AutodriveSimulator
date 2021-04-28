using UnityEngine;
using System.Reflection;
using System;
using Battlehub.Utils;
using System.Collections.Generic;
using Battlehub.RTGizmos;

namespace Battlehub.RTEditor
{
    public class LightComponentDescriptor : ComponentDescriptorBase<Light, LightGizmo>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            Light light = (Light)editor.Component;

            PropertyEditorCallback valueChanged = () => editor.BuildEditor();

            MemberInfo enabledInfo = Strong.PropertyInfo((Light x) => x.enabled, "enabled");
            MemberInfo lightTypeInfo = Strong.PropertyInfo((Light x) => x.type, "type");
            MemberInfo colorInfo = Strong.PropertyInfo((Light x) => x.color, "color");
            MemberInfo intensityInfo = Strong.PropertyInfo((Light x) => x.intensity, "intensity");
            MemberInfo bounceIntensityInfo = Strong.PropertyInfo((Light x) => x.bounceIntensity, "bounceIntensity");
            MemberInfo shadowTypeInfo = Strong.PropertyInfo((Light x) => x.shadows, "shadows");
            MemberInfo cookieInfo = Strong.PropertyInfo((Light x) => x.cookie, "cookie");
            MemberInfo cookieSizeInfo = Strong.PropertyInfo((Light x) => x.cookieSize, "cookieSize");
            MemberInfo flareInfo = Strong.PropertyInfo((Light x) => x.flare, "flare");
            MemberInfo renderModeInfo = Strong.PropertyInfo((Light x) => x.renderMode, "renderMode");

            List<PropertyDescriptor> descriptors = new List<PropertyDescriptor>();
            descriptors.Add(new PropertyDescriptor("Enabled", editor.Component, enabledInfo, enabledInfo));
            descriptors.Add(new PropertyDescriptor("Type", editor.Component, lightTypeInfo, lightTypeInfo, valueChanged));
            if (light.type == LightType.Point)
            {
                MemberInfo rangeInfo = Strong.PropertyInfo((Light x) => x.range, "range");
                descriptors.Add(new PropertyDescriptor("Range", editor.Component, rangeInfo, rangeInfo));
            }
            else if (light.type == LightType.Spot)
            {
                MemberInfo rangeInfo = Strong.PropertyInfo((Light x) => x.range, "range");
                MemberInfo spotAngleInfo = Strong.PropertyInfo((Light x) => x.spotAngle, "spotAngle");
                descriptors.Add(new PropertyDescriptor("Range", editor.Component, rangeInfo, rangeInfo));
                descriptors.Add(new PropertyDescriptor("Spot Angle", editor.Component, spotAngleInfo, spotAngleInfo, null, new Range(1, 179)));
            }

            descriptors.Add(new PropertyDescriptor("Color", editor.Component, colorInfo, colorInfo));
            descriptors.Add(new PropertyDescriptor("Intensity", editor.Component, intensityInfo, intensityInfo, null, new Range(0, 8)));
            descriptors.Add(new PropertyDescriptor("Bounce Intensity", editor.Component, bounceIntensityInfo, bounceIntensityInfo, null, new Range(0, 8)));

            if (light.type != LightType.Area)
            {
                descriptors.Add(new PropertyDescriptor("Shadow Type", editor.Component, shadowTypeInfo, shadowTypeInfo, valueChanged));
                if (light.shadows == LightShadows.Soft || light.shadows == LightShadows.Hard)
                {
                    MemberInfo shadowStrengthInfo = Strong.PropertyInfo((Light x) => x.shadowStrength, "shadowStrength");
                    MemberInfo shadowResolutionInfo = Strong.PropertyInfo((Light x) => x.shadowResolution, "shadowResolution");
                    MemberInfo shadowBiasInfo = Strong.PropertyInfo((Light x) => x.shadowBias, "shadowBias");
                    MemberInfo shadowNormalBiasInfo = Strong.PropertyInfo((Light x) => x.shadowNormalBias, "shadowNormalBias");
                    MemberInfo shadowNearPlaneInfo = Strong.PropertyInfo((Light x) => x.shadowNearPlane, "shadowNearPlane");

                    descriptors.Add(new PropertyDescriptor("Strength", editor.Component, shadowStrengthInfo, shadowStrengthInfo, null, new Range(0, 1)));
                    descriptors.Add(new PropertyDescriptor("Resoultion", editor.Component, shadowResolutionInfo, shadowResolutionInfo));
                    descriptors.Add(new PropertyDescriptor("Bias", editor.Component, shadowBiasInfo, shadowBiasInfo, null, new Range(0, 2)));
                    descriptors.Add(new PropertyDescriptor("Normal Bias", editor.Component, shadowNormalBiasInfo, shadowNormalBiasInfo, null, new Range(0, 3)));
                    descriptors.Add(new PropertyDescriptor("Shadow Near Plane", editor.Component, shadowNearPlaneInfo, shadowNearPlaneInfo, null, new Range(0, 10)));
                }

                descriptors.Add(new PropertyDescriptor("Cookie", editor.Component, cookieInfo, cookieInfo));
                descriptors.Add(new PropertyDescriptor("Cookie Size", editor.Component, cookieSizeInfo, cookieSizeInfo));
            }

            descriptors.Add(new PropertyDescriptor("Flare", editor.Component, flareInfo, flareInfo));
            descriptors.Add(new PropertyDescriptor("Render Mode", editor.Component, renderModeInfo, renderModeInfo));

            return descriptors.ToArray();
        }
    }
}

