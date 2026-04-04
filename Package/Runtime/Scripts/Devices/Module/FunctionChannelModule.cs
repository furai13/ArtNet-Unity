using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ArtNet.Devices.Modular
{
    public class FunctionChannelModule : DmxModuleBase
    {
        public enum FunctionAction
        {
            None,
            ResetFixture
        }

        [Serializable]
        public sealed class FunctionRange
        {
            public string label = "Function";
            [Range(0, 255)] public int min = 0;
            [Range(0, 255)] public int max = 255;
            public FunctionAction action;
            public UnityEvent onTriggered;

            public bool Contains(int value)
            {
                return value >= min && value <= max;
            }
        }

        private static readonly IReadOnlyList<DmxChannelDescriptor> ChannelDescriptors = new[]
        {
            new DmxChannelDescriptor("Function", 0)
        };
        private static readonly IReadOnlyList<DmxQuickActionDefinition> QuickActions = new[]
        {
            new DmxQuickActionDefinition(DmxQuickActionIds.Reset, "Reset")
        };

        [SerializeField] private bool triggerOnlyOnChange = true;
        [SerializeField] private FunctionRange[] functions = Array.Empty<FunctionRange>();

        private int _activeFunctionIndex = -1;

        public override int ChannelCount => 1;

        public override void Apply(in DmxFrame frame)
        {
            var functionIndex = FindFunctionIndex(frame.Get8(0));
            if (triggerOnlyOnChange && functionIndex == _activeFunctionIndex)
            {
                return;
            }

            _activeFunctionIndex = functionIndex;
            if (functionIndex < 0)
            {
                return;
            }

            var function = functions[functionIndex];
            if (function.action == FunctionAction.ResetFixture)
            {
                Fixture.RequestReset();
            }

            function.onTriggered?.Invoke();
        }

        public override IReadOnlyList<DmxChannelDescriptor> GetChannelDescriptors()
        {
            return ChannelDescriptors;
        }

        public override IReadOnlyList<DmxQuickActionDefinition> GetQuickActionDefinitions()
        {
            return QuickActions;
        }

        public override bool TryBuildQuickAction(string actionId, List<DmxQuickActionOverride> overrides)
        {
            if (actionId != DmxQuickActionIds.Reset)
            {
                return false;
            }

            for (var i = 0; i < functions.Length; i++)
            {
                var function = functions[i];
                if (function == null || function.action != FunctionAction.ResetFixture)
                {
                    continue;
                }

                var midpoint = Mathf.Clamp((function.min + function.max) / 2, 0, 255);
                overrides.Add(new DmxQuickActionOverride(0, (byte)midpoint));
                return true;
            }

            return false;
        }

        public void Configure(FunctionRange[] configuredFunctions, bool onlyOnChange)
        {
            functions = configuredFunctions ?? Array.Empty<FunctionRange>();
            triggerOnlyOnChange = onlyOnChange;
            _activeFunctionIndex = -1;
        }

        private int FindFunctionIndex(int value)
        {
            for (var i = 0; i < functions.Length; i++)
            {
                if (functions[i] != null && functions[i].Contains(value))
                {
                    return i;
                }
            }

            return -1;
        }
    }
}
