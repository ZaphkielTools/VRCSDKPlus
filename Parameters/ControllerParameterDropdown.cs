using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Zaphkiel.VRCSDKPlus
{
    internal sealed class ControllerParameterDropdown : AdvancedDropdown
    {
        readonly IReadOnlyList<AnimatorControllerParameter> parameters;
        readonly Action<string> onSelected;

        public ControllerParameterDropdown(IReadOnlyList<AnimatorControllerParameter> parameters, Action<string> onSelected)
            : base(new AdvancedDropdownState())
        {
            this.parameters = parameters;
            this.onSelected = onSelected;
            minimumSize = new Vector2(240, 320);
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Controller Parameters");
            foreach (var parameter in parameters)
                root.AddChild(new Item(parameter.name, $"{parameter.name}   ({parameter.type})"));
            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (item is Item parameterItem) onSelected(parameterItem.ParameterName);
        }

        sealed class Item : AdvancedDropdownItem
        {
            public readonly string ParameterName;

            public Item(string parameterName, string displayName) : base(displayName)
            {
                ParameterName = parameterName;
            }
        }
    }
}
