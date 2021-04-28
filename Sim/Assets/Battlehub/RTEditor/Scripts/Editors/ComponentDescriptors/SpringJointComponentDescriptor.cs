using Battlehub.Utils;
using System;
using System.Reflection;
using UnityEngine;

namespace Battlehub.RTEditor
{
    public class SpringJointComponentDescriptor : ComponentDescriptorBase<SpringJoint>
    {
        public override PropertyDescriptor[] GetProperties(ComponentEditor editor, object converter)
        {
            MemberInfo connectedBodyInfo = Strong.PropertyInfo((SpringJoint x) => x.connectedBody, "connectedBody");
            MemberInfo anchorInfo = Strong.PropertyInfo((SpringJoint x) => x.anchor, "anchor");
            MemberInfo autoConfigAnchorInfo = Strong.PropertyInfo((SpringJoint x) => x.autoConfigureConnectedAnchor, "autoConfigureConnectedAnchor");
            MemberInfo connectedAnchorInfo = Strong.PropertyInfo((SpringJoint x) => x.connectedAnchor, "connectedAnchor");
            MemberInfo springInfo = Strong.PropertyInfo((SpringJoint x) => x.spring, "spring");
            MemberInfo damperInfo = Strong.PropertyInfo((SpringJoint x) => x.damper, "damper");
            MemberInfo minDistanceInfo = Strong.PropertyInfo((SpringJoint x) => x.minDistance, "minDistance");
            MemberInfo maxDistanceInfo = Strong.PropertyInfo((SpringJoint x) => x.maxDistance, "maxDistance");
            MemberInfo toleranceInfo = Strong.PropertyInfo((SpringJoint x) => x.tolerance, "tolerance");
            MemberInfo breakForceInfo = Strong.PropertyInfo((SpringJoint x) => x.breakForce, "breakForce");
            MemberInfo breakTorqueInfo = Strong.PropertyInfo((SpringJoint x) => x.breakTorque, "breakTorque");
            MemberInfo enableCollisionInfo = Strong.PropertyInfo((SpringJoint x) => x.enableCollision, "enableCollision");
            MemberInfo enablePreporcessingInfo = Strong.PropertyInfo((SpringJoint x) => x.enablePreprocessing, "enablePreprocessing");

            return new[]
            {
                new PropertyDescriptor("ConnectedBody", editor.Component, connectedBodyInfo),
                new PropertyDescriptor("Anchor", editor.Component, anchorInfo),
                new PropertyDescriptor("Auto Configure Connected Anchor", editor.Component, autoConfigAnchorInfo),
                new PropertyDescriptor("Connected Anchor", editor.Component, connectedAnchorInfo),
                new PropertyDescriptor("Spring", editor.Component, springInfo),
                new PropertyDescriptor("Damper", editor.Component, damperInfo),
                new PropertyDescriptor("MinDistance", editor.Component, minDistanceInfo),
                new PropertyDescriptor("MaxDistance", editor.Component, maxDistanceInfo),
                new PropertyDescriptor("Tolerance", editor.Component, toleranceInfo),
                new PropertyDescriptor("Break Force", editor.Component, breakForceInfo),
                new PropertyDescriptor("Break Torque", editor.Component, breakTorqueInfo),
                new PropertyDescriptor("Enable Collision", editor.Component, enableCollisionInfo),
                new PropertyDescriptor("Enable Preprocessing", editor.Component, enablePreporcessingInfo),
            };            
        }
    }
}
